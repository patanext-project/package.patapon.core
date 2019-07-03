using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.UI.InGame
{
	public class UIPlayerDisplayNamePresentation : RuntimeAssetPresentation<UIPlayerDisplayNamePresentation>
	{
		
	}

	public class UIPlayerDisplayNameBackend : RuntimeAssetBackend<UIPlayerDisplayNamePresentation>
	{
		
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
			});
		}
	}
}