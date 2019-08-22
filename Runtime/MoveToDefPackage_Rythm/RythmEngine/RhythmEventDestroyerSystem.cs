using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace package.patapon.core
{
    public class RhythmEventDestroySystem<T>
        where T : struct, IComponentData
    {
        public RhythmEventDestroySystem(World world)
        {
            world.GetOrCreateSystem<System>();
        }
        
        private struct DestroyJob : IJobForEachWithEntity<T>
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(Entity entity, int index, ref T c0)
            {
                CommandBuffer.DestroyEntity(index, entity);
            }
        }

        private class System : JobComponentSystem
        {
            private EndSimulationEntityCommandBufferSystem   m_SimulationBarrier;
            private EndPresentationEntityCommandBufferSystem m_PresentationBarrier;

            protected override void OnCreate()
            {
                base.OnCreate();

                var serverGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
                var clientGroup = World.GetExistingSystem<ClientPresentationSystemGroup>();

                serverGroup?.AddSystemToUpdateList(this);
                clientGroup?.AddSystemToUpdateList(this);
            }

            protected override void OnStartRunning()
            {
                m_SimulationBarrier   = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
                m_PresentationBarrier = World.GetExistingSystem<EndPresentationEntityCommandBufferSystem>();
            }

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                if (m_PresentationBarrier != null)
                {
                    inputDeps = new DestroyJob {CommandBuffer = m_PresentationBarrier.CreateCommandBuffer().ToConcurrent()}.Schedule(this, inputDeps);
                    m_PresentationBarrier.AddJobHandleForProducer(inputDeps);
                }
                else
                {
                    inputDeps = new DestroyJob {CommandBuffer = m_SimulationBarrier.CreateCommandBuffer().ToConcurrent()}.Schedule(this, inputDeps);
                    m_SimulationBarrier.AddJobHandleForProducer(inputDeps);
                }

                return inputDeps;
            }
        }
    }
}