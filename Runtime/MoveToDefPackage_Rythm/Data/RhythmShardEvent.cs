using Unity.Entities;

namespace package.patapon.def.Data
{
    public struct RhythmShardEvent : IComponentData
    {
        public int Frame;

        public RhythmShardEvent(int frame)
        {
            Frame = frame;
        }
    }
}