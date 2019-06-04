using package.patapon.def.Data;
using Unity.Entities;

namespace package.patapon.core
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class RhythmEventDestroyerSystem : ComponentSystem
    {
        private static EntityCommandBuffer s_StaticPostUpdateCommands;
        
        protected override void OnUpdate()
        {
            s_StaticPostUpdateCommands = PostUpdateCommands;
            
            ForEach((Entity entity, ref RhythmShardEvent ev) =>
            {
                s_StaticPostUpdateCommands.DestroyEntity(entity);
            });
        }
    }
}