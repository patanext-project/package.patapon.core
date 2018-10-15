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
    public class FlowCommandManager : ComponentSystem
    {
        #region Constants

        public const int DefaultMaxBeats = 4;

        #endregion

        #region Injections

        [Inject] private EndFrameBarrier m_EndFrameBarrier;

        #endregion

        protected override void OnUpdate()
        {
        }

        public float GetBeatAt(Entity shard, NativeArray<FlowCommandSequence> commandSequences, int index)
        {
            var settings = EntityManager.GetComponentData<FlowCommandManagerSettingsData>(shard);

            return (float) (commandSequences[index].BeatFract + 1) / settings.MaxBeats;
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
                typeof(FlowCommandManagerSettingsData)
            );

            EntityManager.SetComponentData(entity, new FlowCommandManagerSettingsData(DefaultMaxBeats));

            return entity;
        }
    }
    
    // this is a header
    public struct FlowCommandManagerTypeDefinition : IComponentData
    {
        
    }

    public struct FlowCommandSequence
    {
        public int BeatFract;
        public int Key;

        public FlowCommandSequence(int beatFract, int key)
        {
            BeatFract = beatFract;
            Key = key;
        }
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