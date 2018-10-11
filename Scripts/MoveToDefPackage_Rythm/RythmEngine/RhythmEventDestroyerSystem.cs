using package.patapon.def.Data;
using Scripts;
using Unity.Entities;

namespace package.patapon.core
{
    [UpdateAfter(typeof(PlayUpdateOrder.RhythmEngineOrder))]
    public class RhythmEventDestroyerSystem : ComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<RhythmShardEvent> EventArray;
            public SubtractiveComponent<VoidSystem<RhythmEventDestroyerSystem>> Void1;

            public EntityArray Entities;

            public readonly int Length;
        }
        
        [Inject] private Group m_Group;

        private EndFrameBarrier m_EndFrameBarrier;

        protected override void OnCreateManager()
        {
            m_EndFrameBarrier = World.GetExistingManager<EndFrameBarrier>();
        }

        protected override void OnUpdate()
        {
            var endFrameCb = m_EndFrameBarrier.CreateCommandBuffer();
            
            for (int i = 0; i != m_Group.Length; i++)
            {
                endFrameCb.DestroyEntity(m_Group.Entities[i]);
            }
        }
    }
}