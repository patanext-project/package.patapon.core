using package.stormiumteam.networking.runtime.highlevel;
using Patapon4TLB.Core;
using Runtime;
using Runtime.Data;
using Stormium.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace StormiumShared.Core.Networking
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(PostLateUpdate))]
    public class CreateClientForNetworkInstanceSystem : ComponentSystem
    {
        private EntityArchetype m_ClientArchetype;
        private ComponentGroup  m_Group, m_DestroyClientGroup;
        private ModelIdent m_GamePlayerModel;

        protected override void OnCreateManager()
        {
            m_ClientArchetype    = EntityManager.CreateArchetype(typeof(ClientTag), typeof(NetworkClient), typeof(ClientToNetworkInstance));
            m_Group              = GetComponentGroup(typeof(NetworkInstanceData), ComponentType.Exclude<NetworkInstanceToClient>());
            m_DestroyClientGroup = GetComponentGroup(typeof(ClientTag), typeof(ClientToNetworkInstance));

            m_GamePlayerModel = World.GetOrCreateManager<StGamePlayerProvider>().GetModelIdent();
        }

        protected override void OnUpdate()
        {
            var gameMgr = World.GetExistingManager<P4GameManager>();
            var localClient = gameMgr.Client;
            
            using (var entityArray = m_Group.ToEntityArray(Allocator.TempJob))
            using (var dataArray = m_Group.ToComponentDataArray<NetworkInstanceData>(Allocator.TempJob))
            {
                for (var i = 0; i != entityArray.Length; i++)
                {
                    var instanceEntity = entityArray[i];
                    var instanceData   = dataArray[i];
                    
                    var clientEntity   = instanceData.IsLocal() ? localClient : EntityManager.CreateEntity(m_ClientArchetype);
                    if (!EntityManager.HasComponent<ClientToNetworkInstance>(clientEntity))
                        EntityManager.AddComponentData(clientEntity, new ClientToNetworkInstance(instanceEntity));
                    else
                        EntityManager.SetComponentData(clientEntity, new ClientToNetworkInstance(instanceEntity));
                    EntityManager.AddComponentData(instanceEntity, new NetworkInstanceToClient(clientEntity));

                    if (instanceData.InstanceType == InstanceType.Client
                        || instanceData.InstanceType == InstanceType.LocalServer)
                    {
                        Entity gamePlayer = default;
                        if (!EntityManager.HasComponent<StNetworkClientToGamePlayer>(clientEntity))
                        {
                            EntityManager.AddComponent(clientEntity, typeof(StNetworkClientToGamePlayer));

                            gamePlayer = gameMgr.SpawnLocal(m_GamePlayerModel);

                            if (instanceData.IsLocal())
                                EntityManager.SetComponentData(gamePlayer, new P4GamePlayer(0, true));

                            EntityManager.AddComponent(gamePlayer, typeof(StGamePlayerToNetworkClient));
                        }
                        else
                        {
                            // it shouldn't happen?
                            Debug.LogWarning("Shouldn't happen.");
                            gamePlayer = EntityManager.GetComponentData<StNetworkClientToGamePlayer>(clientEntity).Target;
                        }

                        EntityManager.SetComponentData(clientEntity, new StNetworkClientToGamePlayer(gamePlayer));
                        EntityManager.SetComponentData(gamePlayer, new StGamePlayerToNetworkClient(clientEntity));
                    }
                }
            }

            ForEach((Entity clientEntity, ref ClientToNetworkInstance clientToNetworkInstance) =>
            {
                if (EntityManager.Exists(clientToNetworkInstance.Target) || clientEntity == localClient)
                    return;

                Debug.Log("Destroyed client.");
                PostUpdateCommands.DestroyEntity(clientEntity);
            }, m_DestroyClientGroup);
            
            ForEach((Entity entity, ref StGamePlayerToNetworkClient gamePlayerToNetworkClient) =>
            {
                if (!EntityManager.Exists(gamePlayerToNetworkClient.Target))
                    PostUpdateCommands.DestroyEntity(entity);
            });
        }
    }
}