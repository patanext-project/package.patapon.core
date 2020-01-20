using MonoComponents;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace DataScripts.Models.GameMode.Structures
{
	public class GameModeFlagPresentation : RuntimeAssetPresentation<GameModeFlagPresentation>
	{
		
	}

	public class GameModeFlagBackend : RuntimeAssetBackend<GameModeFlagPresentation>
	{
	}
	
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class GameModeFlagPoolingSystem : PoolingSystem<GameModeFlagBackend, GameModeFlagPresentation>
	{
		protected override string AddressableAsset => string.Empty;
		
		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(HeadOnFlag));
		}
	}
	
	[AlwaysSynchronizeSystem]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class GameModeFlagSetPresentation : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			Entities.ForEach((GameModeFlagBackend backend) =>
			{
				if (backend.HasIncomingPresentation)
					return;
				
				var pool = StaticSceneResourceHolder.GetPool("versus:flag");
				if (pool == null)
					return;
				
				backend.SetPresentationFromPool(pool);
			}).WithStructuralChanges().Run();
			
			return default;
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class GameModeFlagRenderSystem : BaseRenderSystem<GameModeFlagPresentation>
	{
		protected override void PrepareValues()
		{
			
		}

		protected override void Render(GameModeFlagPresentation definition)
		{
			var backend = definition.Backend;
			EntityManager.TryGetComponentData(backend.DstEntity, out Translation translation);
			backend.transform.position = new Vector3 {x = translation.Value.x, z = 100};
		}

		protected override void ClearValues()
		{
			
		}
	}
}