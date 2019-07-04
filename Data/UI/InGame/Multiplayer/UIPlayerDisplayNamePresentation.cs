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
		public TextMeshProUGUI[] NameLabels;
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
		private int m_Counter = 0;

		protected override void OnUpdate()
		{
			m_Counter = 1;
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

				foreach (var label in presentation.NameLabels)
					label.color = presentation.NormalColor;

				var playerRelative = EntityManager.GetComponentData<Relative<PlayerDescription>>(backend.DstEntity).Target;
				if (playerRelative == default || !EntityManager.HasComponent<PlayerName>(playerRelative))
					return;

				if (EntityManager.HasComponent<GamePlayerLocalTag>(playerRelative))
				{
					foreach (var label in presentation.NameLabels)
						label.color = presentation.ControlledColor;
				}

				var nativeStr = EntityManager.GetComponentData<PlayerName>(playerRelative).Value;
				if (backend.PreviousName.Equals(nativeStr))
					return;

				var txt = $"{m_Counter++.ToString()}.{nativeStr.ToString()}";
				foreach (var label in presentation.NameLabels)
					label.text = txt;
			});
		}
	}
}