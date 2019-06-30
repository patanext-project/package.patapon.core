using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.UI.InGame
{
	public class UIPlayerTargetCursorPresentation : RuntimeAssetPresentation<UIPlayerTargetCursorPresentation>
	{
		public GameObject Controlled;
		public GameObject NotOwned;
	}

	public class UIPlayerTargetCursorBackend : RuntimeAssetBackend<UIPlayerTargetCursorPresentation>
	{
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(GenerateUIPlayerTargetCursorSystem))]
	public class UIPlayerTargetCursorSystem : UIGameSystemBase
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((UIPlayerTargetCursorBackend backend) =>
			{
				var targetPosition = EntityManager.GetComponentData<UnitTargetPosition>(backend.DstEntity);

				backend.transform.position = new Vector3
				(
					targetPosition.Value.x,
					-0.33f,
					0
				);
			});
		}
	}
}