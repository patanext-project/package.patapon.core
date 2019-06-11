using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace package.patapon.core
{
    // this is a header
    public struct FlowCommandManagerTypeDefinition : IComponentData
    {
        
    }

    public struct FlowCurrentCommand : IComponentData
    {
        public Entity CommandTarget;
        
        /// <summary>
        /// When will the command be active?
        /// >=0 = the active beat (will have the same effect as -1 if CommandTarget don't exist or is null)
        /// -1 = not in effect
        /// -2 = forever
        /// </summary>
        public int    ActiveAtBeat;

        /// <summary>
        /// If you want to set a custom beat ending.
        /// >0 = the ending beat
        /// 0 = the command will never be executed (but why)
        /// -1 = not in effect
        /// -2 = forever (you can make a combo with ActiveAtBeat set at -1 to have a forever non ending command)
        /// </summary>
        public int CustomEndBeat;

        /// <summary>
        /// Power is associated with beat score, this is a value between 0 and 100.
        /// </summary>
        /// <remarks>
        /// This is not associated at all with fever state, the command will check if there is fever or not on the engine.
        /// </remarks>
        public int Power;
    }

    public struct FlowCommandSequence
    {
        public RangeInt BeatRange;
        public int Key;

        public FlowCommandSequence(int beatFract, int key)
        {
            BeatRange = new RangeInt(beatFract, 0);
            Key = key;
        }
        
        public FlowCommandSequence(int beatFract, int beatFractLength, int key)
        {
            BeatRange = new RangeInt(beatFract, beatFractLength);
            Key       = key;
        }

        public int BeatEnd => BeatRange.end;
    }

    public struct FlowCommandSequenceContainer : IBufferElementData
    {
        public FlowCommandSequence Value;
    }
}