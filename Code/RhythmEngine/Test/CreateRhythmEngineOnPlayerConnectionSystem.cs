using package.patapon.core;
using Patapon4TLB.Core;
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

			public void Execute(Entity _, int jobIndex, ref PlayerConnectedEvent ev)
			{
				var reEnt = CommandBuffer.CreateEntity(jobIndex, RhythmEngineArchetype);

				CommandBuffer.SetComponent(jobIndex, reEnt, new FlowRhythmEngineSettingsData(0.5f));
				CommandBuffer.SetComponent(jobIndex, reEnt, new FlowCommandManagerSettingsData(4));
				CommandBuffer.SetComponent(jobIndex, reEnt, new Owner {Target = ev.Player});
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
				RhythmEngineArchetype = m_RhythmEngineArchetype
			}.Schedule(this, inputDeps);
			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}