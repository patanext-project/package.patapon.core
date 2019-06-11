using package.patapon.core;
using Patapon4TLB.Core;
using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Patapon4TLB.Default.Test
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	[UpdateBefore(typeof(CreateGamePlayerSystem))]
	public class CreateRhythmEngineOnPlayerConnectionSystem : JobComponentSystem
	{
		private struct CreateJob : IJobForEachWithEntity<PlayerConnectedEvent>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;
			public EntityArchetype                RhythmEngineArchetype;

			public uint ServerTime;

			public void Execute(Entity _, int jobIndex, ref PlayerConnectedEvent ev)
			{
				var reEnt = CommandBuffer.CreateEntity(jobIndex, RhythmEngineArchetype);

				CommandBuffer.SetComponent(jobIndex, reEnt, new RhythmEngineSettings {MaxBeats     = 4, BeatInterval  = 500, UseClientSimulation = true});
				CommandBuffer.SetComponent(jobIndex, reEnt, new RhythmCurrentCommand {CustomEndBeat  = -1, ActiveAtBeat = -1, Power                = 0});
				CommandBuffer.SetComponent(jobIndex, reEnt, new RhythmEngineProcess {StartTime = (int) ServerTime});
				CommandBuffer.SetComponent(jobIndex, reEnt, new Owner {Target                      = ev.Player});
				CommandBuffer.SetComponent(jobIndex, reEnt, new NetworkOwner {Value                = ev.Connection});
			}
		}

		private EndSimulationEntityCommandBufferSystem m_Barrier;
		private EntityArchetype                        m_RhythmEngineArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_Barrier               = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
			m_RhythmEngineArchetype = World.GetOrCreateSystem<RhythmEngineProvider>().EntityArchetypeWithAuthority;
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new CreateJob
			{
				CommandBuffer         = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				RhythmEngineArchetype = m_RhythmEngineArchetype,

				ServerTime = World.GetExistingSystem<SynchronizedSimulationTimeSystem>().Value.Predicted
			}.Schedule(this, inputDeps);
			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}