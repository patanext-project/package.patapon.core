using package.stormiumteam.networking;
using Patapon4TLB.Core.Networking;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core.Tests
{
    [ExecuteAlways]
    public class ManageCharacterForPlayerSystem : JobComponentSystem, IModelCreateEntityCallback
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
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<PlayerToCharacterLink> LinkArray;

            public void Execute(Entity entity, int index, ref PlayerCharacter playerCharacter)
            {
                // If the entity is null or destroyed, don't run the iteration
                if (!LinkArray.Exists(playerCharacter.Owner))
                    return;
                    
                LinkArray[playerCharacter.Owner] = new PlayerToCharacterLink(entity);
            }
        }

        [BurstCompile]
        private struct RemoveUselessCharacterJob : IJobProcessComponentDataWithEntity<PlayerCharacter>
        {
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Patapon4Client> ClientArray;

            public EntityCommandBuffer.Concurrent Ecb;

            public void Execute(Entity entity, int index, ref PlayerCharacter playerCharacter)
            {
                if (!ClientArray.Exists(playerCharacter.Owner))
                    Ecb.DestroyEntity(index, entity);
            }
        }

        private EntityArchetype m_CharacterArchetype;
        private int             m_LocalCharacterArchetypeId;

        protected override void OnCreateManager()
        {
            m_CharacterArchetype = EntityManager.CreateArchetype
            (
                typeof(Position),
                typeof(PlayerCharacter),
                typeof(GenerateEntitySnapshot),
                typeof(SimulateEntity)
            );
        }

        protected override void OnStartRunning()
        {
            var localBank = World.GetExistingManager<NetPatternSystem>().GetLocalBank();
            m_LocalCharacterArchetypeId = localBank.Register(new PatternIdent(nameof(m_CharacterArchetype))).Id;

            var modelMgr = World.GetExistingManager<EntityModelManager>();
            modelMgr.Register(nameof(m_LocalCharacterArchetypeId), this);
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

        public Entity SnapshotCreateEntity(Entity origin, StSnapshotRuntime snapshotRuntime)
        {
            return EntityManager.CreateEntity(m_CharacterArchetype);
        }
    }
}