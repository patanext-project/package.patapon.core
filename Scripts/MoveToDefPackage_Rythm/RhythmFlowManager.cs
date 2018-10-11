using package.patapon.def.Data;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace package.patapon.def
{
    public class RhythmFlowManager : ComponentSystem
    {
        struct GroupEngines
        {
            public EntityArray                           Entities;
            public ComponentDataArray<ShardRhythmEngine> Engines;

            public readonly int Length;
        }

        [Inject] private GroupEngines m_GroupEngines;

        struct GroupPressures
        {
            public            EntityArray                           Entities;
            public            ComponentDataArray<RhythmPressure>    Pressures;
            [ReadOnly] public ComponentDataArray<RhythmShardTarget> Targets;

            public readonly int Length;
        }

        [Inject] private GroupPressures m_GroupPressures;
        
        struct GroupBeats
        {
            public            EntityArray                           Entities;
            public            ComponentDataArray<RhythmBeatData>    Beats;
            [ReadOnly] public ComponentDataArray<RhythmShardTarget> Targets;

            public readonly int Length;
        }

        [Inject] private GroupBeats m_GroupBeats;

        protected override void OnUpdate()
        {
            for (int i = 0; i != m_GroupPressures.Length; i++)
            {
                var pressureEntity = m_GroupPressures.Entities[i];
                var engine         = EntityManager.GetComponentData<ShardRhythmEngine>(m_GroupPressures.Targets[i].Target);
                if (engine.EngineType.TypeIndex == 0)
                {
                    Debug.LogError("Invalid engine for " + m_GroupPressures.Targets[i]);
                }

                foreach (var manager in AppEvent<EventRhythmFlowPressureAction.IEv>.eventList)
                {
                    manager.Callback(new EventRhythmFlowPressureAction.Arguments(engine, pressureEntity));
                }

                PostUpdateCommands.DestroyEntity(m_GroupPressures.Entities[i]);
            }
            
            for (int i = 0; i != m_GroupBeats.Length; i++)
            {
                var beatEntity = m_GroupBeats.Entities[i];
                var engine         = EntityManager.GetComponentData<ShardRhythmEngine>(m_GroupBeats.Targets[i].Target);
                if (engine.EngineType.TypeIndex == 0)
                {
                    Debug.LogError("Invalid engine for " + m_GroupBeats.Targets[i]);
                }

                foreach (var manager in AppEvent<EventRhythmFlowNewBeat.IEv>.eventList)
                {
                    manager.Callback(new EventRhythmFlowNewBeat.Arguments(engine, beatEntity));
                }

                PostUpdateCommands.DestroyEntity(m_GroupBeats.Entities[i]);
            }
        }
    }
}