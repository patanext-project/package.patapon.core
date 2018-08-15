using package.patapon.def.Data;
using package.stormiumteam.shared;

namespace package.patapon.def
{
    public abstract class EventRythmFlowNewBeat
    {
        public struct Arguments : IDelayComponentArguments
        {
            public RythmBeatData Data;
        }

        public interface IEv : IAppEvent
        {
            void Callback(Arguments args);
        }

        internal abstract void Sealed();
    }
}