using package.stormiumteam.shared.ecs;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Rules;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Patapon.Mixed.Systems
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	public class ProcessRhythmEngineSystem : JobGameBaseSystem
	{
		private LazySystem<OrderGroup.Simulation.DeleteEntities.CommandBufferSystem> m_DeleteBarrier;

		private EntityQuery m_DestroyEventQuery;

		private LazySystem<OrderGroup.Simulation.SpawnEntities.CommandBufferSystem> m_SpawnBarrier;

		[BurstDiscard]
		private static void NonBurst_ThrowWarning(Entity entity)
		{
			Debug.LogWarning($"Engine '{entity}' had a FlowRhythmEngineSettingsData.BeatInterval of 0 (or less), this is not accepted.");
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			m_DestroyEventQuery = GetEntityQuery(typeof(EventCreated), typeof(FlowBeatEvent), typeof(Relative<RhythmEngineDescription>));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var spawnEcb   = this.L(ref m_SpawnBarrier).CreateCommandBuffer().ToConcurrent();
			var destroyEcb = this.L(ref m_DeleteBarrier).CreateCommandBuffer();
			var tick       = GetTick(GetSingleton<P4NetworkRules.Data>().RhythmEngineUsePredicted);
			var isServer = IsServer;

			if (!isServer && !GetSingleton<P4NetworkRules.Data>().RhythmEngineUsePredicted)
				tick.Value += (GetTick(true).Value - GetTick(false).Value) / 4;

			destroyEcb.DestroyEntity(m_DestroyEventQuery);

			// Originally, it was only tasked for systems with 'FlowProcessTag'
			// I don't remember why this tag was needed
			inputDeps = Entities
			            .ForEach((Entity entity, int nativeThreadIndex, ref FlowEngineProcess process, ref RhythmEngineState state, in RhythmEngineSettings settings) =>
			            {
				            if (state.IsPaused || process.Milliseconds < 0)
				            {
					            if (isServer)
					            {
						            state.RecoveryTick     = 0;
						            state.NextBeatRecovery = -1;
					            }
					            else
					            {
						            state.NextBeatRecovery = -1;
					            }
				            }

				            var previousBeat = process.GetActivationBeat(settings.BeatInterval);

				            process.Milliseconds = tick.Ms - process.StartTime;
				            if (settings.BeatInterval <= 0.0001f)
				            {
					            NonBurst_ThrowWarning(entity);
					            return;
				            }
				            
				            if (state.IsPaused || process.Milliseconds < 0)
				            {
					            state.LastPressureBeat = 0;
				            }

				            state.IsNewBeat = false;

				            var beatDiff = math.abs(previousBeat - process.GetActivationBeat(settings.BeatInterval));
				            if (beatDiff > 0) // the original plan was to create multiple events if there were multiple beats in the same frame
					            // but maybe it would be too much if we do change the StartTime.
				            {
					            state.IsNewBeat = true;
					            var ent = spawnEcb.CreateEntity(nativeThreadIndex);
					            spawnEcb.AddComponent(nativeThreadIndex, ent, new FlowBeatEvent(process.GetActivationBeat(settings.BeatInterval)));
					            spawnEcb.AddComponent(nativeThreadIndex, ent, new Relative<RhythmEngineDescription>(entity));
					            spawnEcb.AddComponent(nativeThreadIndex, ent, new EventCreated());

					            var mercy = isServer ? 2 : 0;
					            if (state.LastPressureBeat > process.GetActivationBeat(settings.BeatInterval) + mercy)
						            state.LastPressureBeat = 0;
				            }
			            })
			            .Schedule(inputDeps);

			m_SpawnBarrier.Value.AddJobHandleForProducer(inputDeps);
			m_DeleteBarrier.Value.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}

		private struct EventCreated : IComponentData
		{
		}
	}
}