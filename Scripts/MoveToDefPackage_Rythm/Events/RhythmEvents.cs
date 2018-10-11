using package.patapon.def.Data;
using package.stormiumteam.shared;
using Unity.Entities;

namespace package.patapon.def
{
    public abstract class EventRhythmFlowNewBeat
    {
        public struct Arguments : IDelayComponentArguments
        {
            public ShardRhythmEngine Engine;
            public Entity BeatEntity;

            public Arguments(ShardRhythmEngine engine, Entity entity)
            {
                Engine = engine;
                BeatEntity = entity;
            }
        }

        public interface IEv : IAppEvent
        {
            void Callback(Arguments args);
        }

        internal abstract void Sealed();
    }
    
    public abstract class EventRhythmFlowPressureAction
    {
        public struct Arguments : IDelayComponentArguments
        {
            public ShardRhythmEngine Engine;
            public Entity PressureEntity;

            public Arguments(ShardRhythmEngine engine, Entity entity)
            {
                Engine = engine;
                PressureEntity = entity;
            }
        }

        public interface IEv : IAppEvent
        {
            void Callback(Arguments args);
        }

        internal abstract void Sealed();
    }
}