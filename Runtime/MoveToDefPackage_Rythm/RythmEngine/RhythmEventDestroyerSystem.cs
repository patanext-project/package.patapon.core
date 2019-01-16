using package.patapon.def.Data;
using Scripts;
using Unity.Entities;

namespace package.patapon.core
{
    [UpdateAfter(typeof(EndFrameBarrier))]
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

        protected override void OnUpdate()
        {
            for (int i = 0; i != m_Group.Length; i++)
            {
                PostUpdateCommands.DestroyEntity(m_Group.Entities[i]);
            }
        }
    }
}