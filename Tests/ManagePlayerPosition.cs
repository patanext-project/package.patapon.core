using StormiumShared.Core.Networking;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Patapon4TLB.Core.Tests
{
    [UpdateAfter(typeof(SpawnCharacterForPlayerSystem))]
    [UpdateAfter(typeof(UpdateLoop.ReadStates))]
    public class ManagePlayerPosition : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(SimulateEntity))]
        private struct Job : IJobProcessComponentDataWithEntity<Position, PlayerCharacter>
        {
            [ReadOnly]
            public float                                DeltaTime;
            
            [ReadOnly]
            public ComponentDataFromEntity<PlayerInput> PlayerInputArray;

            public void Execute(Entity entity, int index, ref Position position, ref PlayerCharacter playerCharacter)
            {
                if (!PlayerInputArray.Exists(playerCharacter.Owner))
                    return;
                    
                var inputs = PlayerInputArray[playerCharacter.Owner];

                position.Value += float3(inputs.Value * DeltaTime * 2.5f, 0);
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