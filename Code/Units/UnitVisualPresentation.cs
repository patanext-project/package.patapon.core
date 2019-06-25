using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core
{
	public class UnitVisualPresentation : CustomAsyncAssetPresentation<UnitVisualPresentation>
	{

	}

	public class UnitVisualBackend : CustomAsyncAsset<UnitVisualPresentation>
	{
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(RenderInterpolationSystem))]
	public class UpdateBackend : ComponentSystem
	{
		protected override void OnUpdate()
		{
			EntityManager.CompleteAllJobs();
			
			Entities.ForEach((Transform transform, UnitVisualBackend backend) =>
			{
				if (!EntityManager.Exists(backend.DstEntity))
				{
					backend.DisableNextUpdate = true;
					backend.ReturnToPoolOnDisable = true;
					backend.ReturnPresentationToPoolNextFrame = true;
					return;
				}
				
				transform.position = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
			});
		}
	}
}