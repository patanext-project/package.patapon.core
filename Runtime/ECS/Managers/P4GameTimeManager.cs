using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using static Unity.Mathematics.math;

namespace Patapon4TLB.Core
{
    [UpdateAfter(typeof(Initialization.PlayerUpdateTime))]
    public class StGameTimeManager : JobComponentSystem
    {
        [RequireComponentTag(typeof(SimulateEntity)), BurstCompile]
        private struct Job : IJobProcessComponentData<GameTimeComponent>
        {
            public int ActualTick;
            public int ActualFrame;

            public void Execute(ref GameTimeComponent data)
            {
                var tps = clamp(ActualTick - data.Value.Tick, 1, 300);

                data.Value.Tick      = ActualTick;
                data.Value.Time      = ActualTick * 0.001f;
                data.Value.Frame     = ActualFrame;
                data.Value.DeltaTick = tps;
                data.Value.DeltaTime = tps * 0.001f;

                // For now we estimate it.
                data.Value.FixedTickPerSecond = tps;
            }
        }

        private Entity m_SingletonEntity;

        protected override void OnCreateManager()
        {
            m_SingletonEntity = World.Active.GetExistingManager<EntityManager>().CreateEntity(typeof(GameTimeComponent), typeof(SimulateEntity));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                ActualTick  = (int)(Time.unscaledTime * 1000),
                ActualFrame = Time.frameCount
            }.Schedule(this, inputDeps);
        }

        public void SetSingleton(Entity entity)
        {
            if (EntityManager.HasComponent<GameTimeComponent>(entity) && EntityManager.HasComponent<SimulateEntity>(entity))
            {
                m_SingletonEntity = entity;
            }
            else
            {
                throw new Exception();
            }
        }

        public GameTime GetTimeFromSingleton()
        {
            return EntityManager.GetComponentData<GameTimeComponent>(m_SingletonEntity).Value;
        }
    }
}