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
        public int ActiveAtBeat;
        public byte IsActive;

        public FlowCurrentCommand(Entity commandTarget, int activeAtBeat, bool isActive)
        {
            CommandTarget = commandTarget;
            ActiveAtBeat = activeAtBeat;
            IsActive = (byte)(isActive ? 1 : 0);
        }
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