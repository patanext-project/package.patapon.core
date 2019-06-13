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

    public struct RhythmCurrentCommand : IComponentData
    {
        public Entity CommandTarget;
        
        /// <summary>
        /// When will the command be active?
        /// </summary>
        /// <remarks>
        /// >=0 = the active beat (will have the same effect as -1 if CommandTarget don't exist or is null).
        /// -1 = not in effect.
        /// -2 = forever.
        /// </remarks>
        public int    ActiveAtBeat;

        /// <summary>
        /// If you want to set a custom beat ending.
        /// </summary>
        /// <remarks>
        /// >0 = the ending beat.
        /// 0 = the command will never be executed (but why).
        /// -1 = not in effect.
        /// -2 = forever (you can make a combo with ActiveAtBeat set at -1 to have a forever non ending command).
        /// </remarks>
        public int CustomEndBeat;

        /// <summary>
        /// Power is associated with beat score, this is a value between 0 and 100.
        /// </summary>
        /// <remarks>
        /// This is not associated at all with fever state, the command will check if there is fever or not on the engine.
        /// The game will check if it can enable hero mode if power is 100.
        /// </remarks>
        public int Power;
    }
    
    public struct GamePredictedCommandState : IComponentData
    {
        public bool IsActive;
        public int  StartBeat;
        public int  EndBeat;
    }

    public struct GameComboState : IComponentData
    {
        /// <summary>
        /// The score of the current combo. A perfect combo do a +5
        /// </summary>
        public int Score;

        /// <summary>
        /// The current chain of the combo
        /// </summary>
        public int Chain;

        /// <summary>
        /// It will be used to know when we should have the fever, it shouldn't be used to know the current chain.
        /// </summary>
        public int ChainToFever;

        /// <summary>
        /// The fever state, enabled if we have a score or 6 or more.
        /// </summary>
        public bool IsFever;

        public int JinnEnergy;
        public int JinnEnergyMax;
    }

    public struct GameComboChain : IBufferElementData
    {
        
    }

    public struct GameCommandState : IComponentData
    {
        public bool IsActive;
        public int  StartBeat;
        public int  EndBeat;
    }

    public struct RhythmCommandSequence
    {
        public RangeInt BeatRange;
        public int Key;

        public RhythmCommandSequence(int beatFract, int key)
        {
            BeatRange = new RangeInt(beatFract, 0);
            Key = key;
        }
        
        public RhythmCommandSequence(int beatFract, int beatFractLength, int key)
        {
            BeatRange = new RangeInt(beatFract, beatFractLength);
            Key       = key;
        }

        public int BeatEnd => BeatRange.end;
    }

    public struct RhythmCommandSequenceContainer : IBufferElementData
    {
        public RhythmCommandSequence Value;
    }

    public struct RhythmCommandData : IComponentData
    {
        public NativeString64 Identifier;
        public int            BeatLength;
    }
}