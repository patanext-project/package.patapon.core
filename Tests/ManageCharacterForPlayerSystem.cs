using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.shared;
using Patapon4TLB.Core.Networking;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Patapon4TLB.Core.Tests
{
    [ExecuteAlways]
    [UpdateAfter(typeof(CreateClientForNetworkInstanceSystem))]
    public class SpawnCharacterForPlayerSystem : ComponentSystem, IModelSpawnEntityCallback, IModelDestroyEntityCallback
    {
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

        private ModelIdent m_CharacterModelIdent;
        private GameObject m_CharacterGameObject;

        private ComponentGroup m_CreateCharacterForClientGroup;
        private ComponentGroup m_RemoveUselessCharacter;

        protected override void OnCreateManager()
        {
            Addressables.InitializationOperation.Completed += op => { OnLoadAssets(); };

            m_CreateCharacterForClientGroup = GetComponentGroup(typeof(Patapon4Client), typeof(ClientToNetworkInstance), ComponentType.Subtractive<PlayerToCharacterLink>());
            m_RemoveUselessCharacter = GetComponentGroup(typeof(PlayerCharacter), typeof(SimulateEntity));
        }
        
        public Entity SpawnEntity(Entity origin, StSnapshotRuntime snapshotRuntime)
        {
            var worldEntity = Object.Instantiate(m_CharacterGameObject)
                                    .GetComponent<GameObjectEntity>()
                                    .Entity;
            
            worldEntity.SetOrAddComponentData(new ModelIdent());
            worldEntity.SetOrAddComponentData(new Position());
            worldEntity.SetOrAddComponentData(new Rotation());
            worldEntity.SetOrAddComponentData(new TransformState());
            worldEntity.SetOrAddComponentData(new PlayerCharacter());
            worldEntity.SetOrAddComponentData(new GenerateEntitySnapshot());

            return worldEntity;
        }

        public void DestroyEntity(Entity worldEntity)
        {
            var gameObject = EntityManager.GetComponentObject<Transform>(worldEntity).gameObject;
            
            Object.Destroy(gameObject);
        }

        protected void OnLoadAssets()
        {
            Addressables.LoadAsset<GameObject>("CharacterTest")
                        .Completed += op => m_CharacterGameObject = op.Result;
        }

        protected override void OnStartRunning()
        {
            var modelMgr = World.GetExistingManager<EntityModelManager>();
            m_CharacterModelIdent = modelMgr.Register("CharacterTest", this, this);
        }

        protected override void OnUpdate()
        {
            using (var entityArray = m_CreateCharacterForClientGroup.ToEntityArray(Allocator.TempJob))
            using (var clientToNetworkArray = m_CreateCharacterForClientGroup.ToComponentDataArray<ClientToNetworkInstance>(Allocator.TempJob))
            {
                for (var i = 0; i != entityArray.Length; i++)
                {
                    var entity = entityArray[i];
                    var clientToNetwork = clientToNetworkArray[i];
                    var instanceData = EntityManager.GetComponentData<NetworkInstanceData>(clientToNetwork.Target);
                    if (instanceData.InstanceType != InstanceType.LocalServer
                        && instanceData.InstanceType != InstanceType.Client)
                        continue;

                    var characterEntity = SpawnEntity(default, default);
                    
                    EntityManager.AddComponentData(entity, new PlayerToCharacterLink(characterEntity));
                    EntityManager.SetComponentData(characterEntity, m_CharacterModelIdent);
                    EntityManager.SetComponentData(characterEntity, new PlayerCharacter(entity));
                    EntityManager.AddComponentData(characterEntity, new SimulateEntity());
                }
            }

            using (var entityArray = m_RemoveUselessCharacter.ToEntityArray(Allocator.TempJob))
            using (var playerCharacterArray = m_RemoveUselessCharacter.ToComponentDataArray<PlayerCharacter>(Allocator.TempJob))
            {
                for (var i = 0; i != entityArray.Length; i++)
                {
                    var entity          = entityArray[i];
                    var playerCharacter = playerCharacterArray[i];

                    if (!EntityManager.Exists(playerCharacter.Owner))
                        DestroyEntity(entity);
                }
            }
        }
    }
}