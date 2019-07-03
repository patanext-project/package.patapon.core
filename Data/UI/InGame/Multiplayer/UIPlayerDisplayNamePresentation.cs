using P4.Core.Code.Networking;
using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.UI.InGame
{
	public class UIPlayerDisplayNamePresentation : RuntimeAssetPresentation<UIPlayerDisplayNamePresentation>
	{
		public TextMeshProUGUI NameLabel;
		public Color ControlledColor;
		public Color NormalColor;
	}

	public class UIPlayerDisplayNameBackend : RuntimeAssetBackend<UIPlayerDisplayNamePresentation>
	{
		public NativeString64 PreviousName;
		
		protected override void Update()
		{
			if (DstEntityManager == null || DstEntityManager.IsCreated && DstEntityManager.Exists(DstEntity))
			{
				base.Update();
				return;
			}
			
			Return(true, true);
		}
	}
	
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(GenerateUIPlayerDisplayNameSystem))]
	public class UIPlayerDisplayNameSystem : UIGameSystemBase
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((UIPlayerDisplayNameBackend backend) =>
			{
				var targetPosition = EntityManager.GetComponentData<Translation>(backend.DstEntity);

				backend.transform.position = new Vector3
				(
					targetPosition.Value.x,
					-0.3f,
					0
				);

				var presentation = backend.Presentation;
				if (presentation == null)
					return;

				presentation.NameLabel.color = presentation.NormalColor;

				var playerRelative = EntityManager.GetComponentData<Relative<PlayerDescription>>(backend.DstEntity).Target;
				if (playerRelative == default || !EntityManager.HasComponent<PlayerName>(playerRelative))
					return;

				if (EntityManager.HasComponent<GamePlayerLocalTag>(playerRelative))
					presentation.NameLabel.color = presentation.ControlledColor;

				var nativeStr = EntityManager.GetComponentData<PlayerName>(playerRelative).Value;
				if (backend.PreviousName.Equals(nativeStr))
					return;

				presentation.NameLabel.text = nativeStr.ToString();
			});
		}
	}
}