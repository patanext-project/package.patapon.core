using Unity.Entities;

namespace Patapon4TLB.Core
{
    public struct GameTime
    {
        public int Frame;
        public int Tick;
        public int DeltaTick;
        public int FixedTickPerSecond;
        public double Time;
        public float DeltaTime;
    }

    public struct GameTimeComponent : IComponentData
    {
        public GameTime Value;

        public GameTimeComponent(GameTime value)
        {
            Value = value;
        }
    }
}