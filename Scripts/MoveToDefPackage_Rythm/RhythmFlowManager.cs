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

                Debug.Log("Processed Pressure");

                foreach (var manager in AppEvent<EventRhythmFlowPressureAction.IEv>.eventList)
                {
                    manager.Callback(new EventRhythmFlowPressureAction.Arguments(engine, pressureEntity));
                }

                PostUpdateCommands.DestroyEntity(m_GroupPressures.Entities[i]);
            }
        }
    }
}