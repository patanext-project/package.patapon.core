using Patapon4TLB.Core.Networking;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Patapon4TLB.Core.Tests
{
    [ExecuteAlways]
    public class ManageCharacterForPlayerSystem : JobComponentSystem
    {
        [RequireSubtractiveComponent(typeof(PlayerToCharacterLink))]
        struct CreateCharacterForClientJob : IJobProcessComponentDataWithEntity<Patapon4Client>
        {
            public EntityArchetype                Archetype;
            public EntityCommandBuffer.Concurrent Ecb;

            public void Execute(Entity entity, int index, ref Patapon4Client clientData)
            {
                Ecb.AddComponent(index, entity, new PlayerToCharacterLink(Entity.Null));

                Ecb.CreateEntity(index, Archetype);
                Ecb.SetComponent(index, new PlayerCharacter(entity));
            }
        }

        [BurstCompile]
        private struct LinkPlayerToCharacterJob : IJobProcessComponentDataWithEntity<PlayerCharacter>
        {
            public ComponentDataFromEntity<PlayerToCharacterLink> LinkArray;

            public void Execute(Entity entity, int index, ref PlayerCharacter playerCharacter)
            {
                LinkArray[playerCharacter.Owner] = new PlayerToCharacterLink(entity);
            }
        }

        [BurstCompile]
        private struct RemoveUselessCharacterJob : IJobProcessComponentDataWithEntity<PlayerCharacter>
        {
            public ComponentDataFromEntity<Patapon4Client> ClientArray;
            public EntityCommandBuffer.Concurrent          Ecb;

            public void Execute(Entity entity, int index, ref PlayerCharacter playerCharacter)
            {
                if (!ClientArray.Exists(playerCharacter.Owner))
                    Ecb.DestroyEntity(index, entity);
            }
        }

        private EntityArchetype m_CharacterArchetype;

        protected override void OnCreateManager()
        {
            m_CharacterArchetype = EntityManager.CreateArchetype(typeof(PlayerCharacter), typeof(GenerateEntitySnapshot));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new CreateCharacterForClientJob
            {
                Archetype = m_CharacterArchetype,
                Ecb = World.GetExistingManager<EndFrameBarrier>()
                           .CreateCommandBuffer()
                           .ToConcurrent()
            }.Schedule(this, inputDeps);

            inputDeps = new LinkPlayerToCharacterJob
            {
                LinkArray = GetComponentDataFromEntity<PlayerToCharacterLink>()
            }.Schedule(this, inputDeps);

            inputDeps = new RemoveUselessCharacterJob
            {
                ClientArray = GetComponentDataFromEntity<Patapon4Client>(),
                Ecb = World.GetExistingManager<EndFrameBarrier>()
                           .CreateCommandBuffer()
                           .ToConcurrent()
            }.Schedule(this, inputDeps);

            return inputDeps;
        }
    }
}