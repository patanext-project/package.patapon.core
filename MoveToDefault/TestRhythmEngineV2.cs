using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
	
	[UpdateBefore(typeof(RhythmEngineGroup))]
	public class TestRhythmEngineV2_CreatePressureSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{	
			Entities.ForEach((Entity e, ref DefaultRhythmEngineState predicted) =>
			{
				if (Input.GetKeyDown(KeyCode.Keypad4)) CreatePressure(RhythmKeys.Left, e);
				if (Input.GetKeyDown(KeyCode.Keypad6)) CreatePressure(RhythmKeys.Right, e);
				if (Input.GetKeyDown(KeyCode.Keypad8)) CreatePressure(RhythmKeys.Up, e);
				if (Input.GetKeyDown(KeyCode.Keypad2)) CreatePressure(RhythmKeys.Down, e);
			});
		}

		private void CreatePressure(int key, Entity engine)
		{
			var entity = PostUpdateCommands.CreateEntity();

			PostUpdateCommands.AddComponent(entity, new PressureEvent {Key = key, Engine = engine});
		}
	}
}