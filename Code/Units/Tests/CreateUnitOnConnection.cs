using package.patapon.core;
using Patapon4TLB.Default;
using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Patapon4TLB.Core.Tests
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	[UpdateBefore(typeof(CreateGamePlayerSystem))]
	public class CreateUnitOnConnection : JobComponentSystem
	{
		private struct CreateJob : IJobForEachWithEntity<PlayerConnectedEvent>
		{
			public EntityCommandBuffer.Concurrent CommandBuffer;

			public uint ServerTime;

			public void Execute(Entity _, int jobIndex, ref PlayerConnectedEvent ev)
			{
				var reEnt = CommandBuffer.CreateEntity(jobIndex);
				
				// do smth
			}
		}

		private EndSimulationEntityCommandBufferSystem m_Barrier;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_Barrier               = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new CreateJob
			{
				CommandBuffer         = m_Barrier.CreateCommandBuffer().ToConcurrent(),

				ServerTime = World.GetExistingSystem<SynchronizedSimulationTimeSystem>().Value.Predicted
			}.Schedule(this, inputDeps);
			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}