using Unity.Entities;

namespace package.patapon.def.Data
{
    public struct RhythmShardTarget : ISharedComponentData
    {
        public Entity Target;

        public RhythmShardTarget(Entity target)
        {
            Target = target;
        }
    }
}