using package.stormiumteam.networking.runtime.highlevel;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Patapon4TLB.Core.Networking
{
    [ExecuteAlways]
    [UpdateInGroup(typeof(PostLateUpdate))]
    public class CreateClientForNetworkInstanceSystem : ComponentSystem
    {
        private EntityArchetype m_ClientArchetype;
        private ComponentGroup  m_Group, m_DestroyClientGroup;

        protected override void OnCreateManager()
        {
            m_ClientArchetype    = EntityManager.CreateArchetype(typeof(ClientTag), typeof(Patapon4Client), typeof(ClientToNetworkInstance));
            m_Group              = GetComponentGroup(typeof(NetworkInstanceData), ComponentType.Subtractive<NetworkInstanceToClient>());
            m_DestroyClientGroup = GetComponentGroup(typeof(ClientTag), typeof(ClientToNetworkInstance));
        }

        protected override void OnUpdate()
        {
            using (var entityArray = m_Group.ToEntityArray(Allocator.TempJob))
            using (var dataArray = m_Group.ToComponentDataArray<NetworkInstanceData>(Allocator.TempJob))
            {
                for (var i = 0; i != entityArray.Length; i++)
                {
                    var instanceEntity = entityArray[i];
                    var instanceData = dataArray[i];
                    var clientEntity   = EntityManager.CreateEntity(m_ClientArchetype);
                    EntityManager.SetComponentData(clientEntity, new ClientToNetworkInstance(instanceEntity));
                    EntityManager.AddComponentData(instanceEntity, new NetworkInstanceToClient(clientEntity));

                    if (instanceData.IsLocal())
                    {
                        EntityManager.AddComponent(clientEntity, typeof(Patapon4LocalTag));
                    }
                }
            }

            ForEach((Entity clientEntity, ref ClientToNetworkInstance clientToNetworkInstance) =>
            {
                if (EntityManager.Exists(clientToNetworkInstance.Target))
                    return;

                Debug.Log("Destroyed client.");
                PostUpdateCommands.DestroyEntity(clientEntity);
            }, m_DestroyClientGroup);
        }
    }
}