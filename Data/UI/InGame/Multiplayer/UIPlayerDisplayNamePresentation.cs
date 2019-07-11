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
		public TextMeshPro[] NameLabels;
		public Color         ControlledColor;
		public Color         NormalColor;
	}

	public class UIPlayerDisplayNameBackend : RuntimeAssetBackend<UIPlayerDisplayNamePresentation>
	{
		public NativeString64 PreviousName;
		public Color TargetColor;
		
		public override void OnPoolSet()
		{
			DstEntityManager.AddComponentData(BackendEntity, RuntimeAssetDisable.All);
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
				var pos            = new Vector3(targetPosition.Value.x, -0.3f, 0);
				backend.transform.position = pos;

				var presentation = backend.Presentation;
				if (presentation == null)
					return;

				var playerRelative = EntityManager.GetComponentData<Relative<PlayerDescription>>(backend.DstEntity).Target;
				if (playerRelative == default)
				{
					return;
				}

				backend.TargetColor = presentation.NormalColor;
				if (EntityManager.HasComponent<GamePlayerLocalTag>(playerRelative))
				{
					backend.TargetColor = presentation.ControlledColor;
				}

				foreach (var label in presentation.NameLabels)
				{
					label.color = backend.TargetColor;
				}

				var nativeStr = new NativeString64();
				if (EntityManager.HasComponent<PlayerName>(playerRelative))
				{
					nativeStr = EntityManager.GetComponentData<PlayerName>(playerRelative).Value;
				}
				else
				{
					nativeStr.CopyFrom("NoName");
				}

				if (backend.PreviousName.Equals(nativeStr))
					return;

				var txt = $"{m_Counter++.ToString()}.{nativeStr.ToString()}";
				foreach (var label in presentation.NameLabels)
					label.text = txt;
			});
		}
	}
}