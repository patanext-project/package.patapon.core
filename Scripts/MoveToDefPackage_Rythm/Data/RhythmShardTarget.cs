using Unity.Entities;

namespace package.patapon.def.Data
{
    public struct RhythmShardTarget : IComponentData
    {
        public Entity Target;

        public RhythmShardTarget(Entity target)
        {
            Target = target;
        }
    }
}