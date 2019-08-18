using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.UI.InGame
{
	public class UIPlayerTargetCursorPresentation : RuntimeAssetPresentation<UIPlayerTargetCursorPresentation>
	{
		private bool[] m_ActiveStates;

		public GameObject Controlled;
		public GameObject NotOwned;

		private void OnEnable()
		{
			m_ActiveStates = new bool[2];
			Controlled.SetActive(false);
			NotOwned.SetActive(false);
		}

		public void SetActive(int index, bool state)
		{
			if (m_ActiveStates[index] == state)
				return;

			m_ActiveStates[index] = state;
			switch (index)
			{
				case 0:
					Controlled.SetActive(state);
					break;
				case 1:
					NotOwned.SetActive(state);
					break;
			}
		}
	}

	public class UIPlayerTargetCursorBackend : RuntimeAssetBackend<UIPlayerTargetCursorPresentation>
	{
		public override void OnTargetUpdate()
		{
			DstEntityManager.AddComponentData(BackendEntity, RuntimeAssetDisable.All);
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(GenerateUIPlayerTargetCursorSystem))]
	public class UIPlayerTargetCursorSystem : UIGameSystemBase
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((UIPlayerTargetCursorBackend backend) =>
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

				if (!EntityManager.HasComponent<Relative<PlayerDescription>>(backend.DstEntity))
					return;
				var playerRelative = EntityManager.GetComponentData<Relative<PlayerDescription>>(backend.DstEntity).Target;
				if (playerRelative == default)
				{
					presentation.SetActive(0, false);
					presentation.SetActive(1, false);
					return;
				}

				var isLocal = EntityManager.HasComponent<GamePlayerLocalTag>(playerRelative);

				presentation.SetActive(0, isLocal);
				presentation.SetActive(1, !isLocal);
			});
		}
	}
}