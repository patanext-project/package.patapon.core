using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	[UpdateAfter(typeof(RhythmEngineGroup))]
	public class TestRhythmEngineV2 : ComponentSystem
	{
		protected override void OnCreate()
		{
			var reProvider = World.GetOrCreateSystem<RhythmEngineProvider>();

			// Create our player.
			var player = EntityManager.CreateEntity(typeof(PlayerDescription));
			// Create our rhythm manager.
			Entity engine;
			using (var output = new NativeList<Entity>(1, Allocator.TempJob))
			{
				reProvider.SpawnLocalEntityWithArguments(new RhythmEngineProvider.Create(), output);
				engine = output[0];
			}

			EntityManager.AddComponentData(engine, new DestroyChainReaction(player));
			EntityManager.ReplaceOwnerData(engine, player);
		}

		protected override void OnUpdate()
		{
			
		}
	}
}