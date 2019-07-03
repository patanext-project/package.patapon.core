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
	}

	public class UIPlayerDisplayNameBackend : RuntimeAssetBackend<UIPlayerDisplayNamePresentation>
	{
		public NativeString64 PreviousName;
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

				var playerRelative = EntityManager.GetComponentData<Relative<PlayerDescription>>(backend.DstEntity).Target;
				if (playerRelative == default || !EntityManager.HasComponent<PlayerName>(playerRelative))
					return;

				var nativeStr = EntityManager.GetComponentData<PlayerName>(playerRelative).Value;
				if (backend.PreviousName.Equals(nativeStr))
					return;

				presentation.NameLabel.text = nativeStr.ToString();
			});
		}
	}
}