using Unity.Entities;

namespace package.patapon.def.Data
{
    public struct RythmBeatData : IComponentData
    {
        public Entity EngineId;
        public int Beat;
        public byte Side;
    }
}