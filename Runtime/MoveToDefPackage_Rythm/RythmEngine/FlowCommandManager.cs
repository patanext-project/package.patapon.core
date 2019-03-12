using System;
using package.patapon.def;
using package.patapon.def.Data;
using package.stormiumteam.shared;
using Scripts;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace package.patapon.core
{
    // Currently a WIP, so there are a lot of tests and unit tests in this class
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class FlowCommandManager : ComponentSystem
    {
        #region Constants

        public const int DefaultMaxBeats = 4;

        #endregion

        protected override void OnUpdate()
        {
        }

        public RangeFloat GetBeatProgressionAt(Entity shard, NativeArray<FlowCommandSequence> commandSequences, int index)
        {
            if (commandSequences.Length <= index)
                return new RangeFloat(float.NaN, float.NaN);
            
            var settings = EntityManager.GetComponentData<FlowCommandManagerSettingsData>(shard);

            
            
            var start = (float) (commandSequences[index].BeatRange.start + 1) / settings.MaxBeats;
            var length = (float) (commandSequences[index].BeatRange.length) / settings.MaxBeats;
            
            return new RangeFloat(start, length);
        }

        public RangeInt GetBeatAt(Entity shard, NativeArray<FlowCommandSequence> commandSequences, int index)
        {
            if (commandSequences.Length <= index)
                return new RangeInt(-1, -1);
            
            var settings = EntityManager.GetComponentData<FlowCommandManagerSettingsData>(shard);

            return commandSequences[index].BeatRange;
        }

        // TODO: Make it as an abstract method
        /// <summary>
        /// Create a new rhythm engine.
        /// </summary>
        /// <returns>The new entity which contain engine data</returns>
        public Entity AddEngine()
        {
            var entity = EntityManager.CreateEntity
            (
                typeof(FlowCommandManagerTypeDefinition),
                typeof(FlowCommandManagerSettingsData),
                typeof(FlowCurrentCommand)
            );

            EntityManager.SetComponentData(entity, new FlowCommandManagerSettingsData(DefaultMaxBeats));

            return entity;
        }
    }
    
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

    public struct FlowCommandManagerSettingsData : IComponentData
    {
        public int MaxBeats;

        public FlowCommandManagerSettingsData(int maxBeats)
        {
            MaxBeats = maxBeats;
        }
    }
}