using GameModes.VSHeadOn;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Units;
using Patapon4TLB.Core.Snapshots;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class StartMapBlock : BlockCollection
	{
		public WaitingTickBlock ShowMapTextBlock;
		public Block            SpawnBlock;

		public StartMapBlock(string name) : base(name)
		{
			Add(SpawnBlock       = new Block("Test block"));
			Add(ShowMapTextBlock = new WaitingTickBlock("Time to show map start text"));
		}

		protected override bool OnRun()
		{
			if (RunNext(SpawnBlock))
			{
				EntityQuery currQuery;

				var queries   = Context.GetExternal<MpVersusHeadOnGameMode.QueriesContext>();
				var gmContext = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
				var worldCtx  = Context.GetExternal<WorldContext>();

				// ----------------------------- //
				// Get spawn points
				currQuery = queries.SpawnPoint;
				Debug.Log($"spawn point count = {currQuery.CalculateEntityCount()}");
				using (var entities = currQuery.ToEntityArray(Allocator.TempJob))
				using (var teamTargetArray = currQuery.ToComponentDataArray<HeadOnTeamTarget>(Allocator.TempJob))
				{
					for (int ent = 0, length = entities.Length; ent < length; ent++)
					{
						var tTarget = teamTargetArray[ent];
						if (tTarget.TeamIndex < 0)
							continue;

						ref var team = ref gmContext.Teams[tTarget.TeamIndex];
						team.SpawnPoint = entities[ent];
					}
				}

				// ----------------------------- //
				// Get flags
				currQuery = queries.Flag;
				using (var entities = currQuery.ToEntityArray(Allocator.TempJob))
				using (var teamTargetArray = currQuery.ToComponentDataArray<HeadOnTeamTarget>(Allocator.TempJob))
				{
					for (int ent = 0, length = entities.Length; ent < length; ent++)
					{
						var tTarget = teamTargetArray[ent];
						if (tTarget.TeamIndex < 0)
							continue;

						ref var team = ref gmContext.Teams[tTarget.TeamIndex];
						team.Flag = entities[ent];
					}
				}

				// ----------------------------- //
				// Destroy rhythm engines
				worldCtx.EntityMgr.DestroyEntity(queries.GetEntityQueryBuilder().WithAll<RhythmEngineDescription, Relative<PlayerDescription>>().ToEntityQuery());

				// ----------------------------- //
				// Add players
				worldCtx.EntityMgr.AddComponent(queries.PlayerWithoutGameModeData, typeof(VersusHeadOnPlayer));

				// ----------------------------- //
				// Set team of players
				// Create player rhythm engines
				// Create player unit targets
				queries.GetEntityQueryBuilder().With(queries.Player).ForEach(player =>
				{
					var entMgr = worldCtx.EntityMgr;
					// Player without NetworkOwner mean that it's a bot.
					if (entMgr.TryGetComponentData(player, out NetworkOwner networkOwner))
					{
						var rhythmEngineProvider = worldCtx.GetOrCreateSystem<RhythmEngineProvider>();
						var rhythmEnt = rhythmEngineProvider.SpawnLocalEntityWithArguments(new RhythmEngineProvider.Create
						{
							UseClientSimulation = true
						});

						entMgr.SetOrAddComponentData(rhythmEnt, networkOwner);
						entMgr.SetOrAddComponentData(rhythmEnt, new FlowEngineProcess {StartTime = gmContext.GetTick().Ms});
						entMgr.AddComponent(rhythmEnt, typeof(GhostEntity));
						entMgr.AddComponentData(rhythmEnt, new OwnerServerId {Value = worldCtx.EntityMgr.GetComponentData<GamePlayer>(player).ServerId});

						entMgr.ReplaceOwnerData(rhythmEnt, player);
					}

					if (entMgr.HasComponent<HeadOnSpectating>(player))
						entMgr.RemoveComponent<HeadOnSpectating>(player);
					entMgr.AddComponent<HeadOnPlaying>(player);

					var unitTarget = entMgr.CreateEntity(typeof(UnitTargetDescription), typeof(Translation), typeof(LocalToWorld), typeof(Relative<PlayerDescription>),
						typeof(TranslationDirectSnapshot.Exclude), typeof(TranslationInterpolatedSnapshot.Use));
					entMgr.AddComponent(unitTarget, typeof(GhostEntity));
					entMgr.AddComponentData(unitTarget, EntityDescription.New<UnitTargetDescription>());
					entMgr.ReplaceOwnerData(unitTarget, player);
				});

				// ----------------------------- //
				// Create towers
				queries.GetEntityQueryBuilder().ForEach((Entity entity, ref HeadOnStructure headOnStructure) =>
				{
					// create health entity
					var healthProvider = worldCtx.GetExistingSystem<DefaultHealthData.InstanceProvider>();
					var healthEntity = healthProvider.SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance
					{
						max   = 0,
						value = 0,
						owner = entity
					});
					worldCtx.EntityMgr.AddComponent(healthEntity, typeof(GhostEntity));
					worldCtx.EntityMgr.SetOrAddComponentData(entity, new LivableHealth
					{
						IsDead = true
					});
				});

				ShowMapTextBlock.SetTicksFromMs(10);

				return false;
			}

			if (RunNext(ShowMapTextBlock))
				return false;

			return true;
		}

		protected override void OnReset()
		{
			base.OnReset();

			ShowMapTextBlock.TickGetter = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
		}
	}
}