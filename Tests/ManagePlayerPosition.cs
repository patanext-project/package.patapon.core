using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Patapon4TLB.Core.Tests
{
    public class ManagePlayerPosition : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(SimulateEntity))]
        private struct Job : IJobProcessComponentDataWithEntity<Position, PlayerCharacter>
        {
            public float                                DeltaTime;
            public ComponentDataFromEntity<PlayerInput> PlayerInputArray;

            public void Execute(Entity entity, int index, ref Position position, ref PlayerCharacter playerCharacter)
            {
                var inputs = PlayerInputArray[playerCharacter.Owner];

                position.Value += float3(inputs.Value * DeltaTime, 0);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                DeltaTime        = Time.deltaTime,
                PlayerInputArray = GetComponentDataFromEntity<PlayerInput>()
            }.Schedule(this, inputDeps);
        }
    }
}