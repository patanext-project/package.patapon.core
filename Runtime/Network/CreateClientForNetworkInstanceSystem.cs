using package.stormiumteam.networking.runtime.highlevel;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.Networking
{
    [ExecuteAlways]
    public class CreateClientForNetworkInstanceSystem : ComponentSystem
    {
        private EntityArchetype m_ClientArchetype;
        private ComponentGroup  m_Group;

        protected override void OnCreateManager()
        {
            m_ClientArchetype = EntityManager.CreateArchetype(typeof(ClientTag), typeof(Patapon4Client), typeof(ClientToNetworkInstance));
            m_Group           = GetComponentGroup(typeof(NetworkInstanceData), ComponentType.Subtractive<NetworkInstanceToClient>());
        }

        protected override void OnUpdate()
        {
            var length = m_Group.CalculateLength();
            if (length == 0)
                return;

            var entityArray = m_Group.GetEntityArray();
            var naEntities  = new NativeArray<Entity>(length, Allocator.Temp);
            for (var i = 0; i != length; i++)
                naEntities[i] = entityArray[i];

            var dataArray = m_Group.GetComponentDataArray<NetworkInstanceData>();
            var naData = new NativeArray<NetworkInstanceData>(length, Allocator.Temp);
            for (var i = 0; i != length; i++)
                naData[i] = dataArray[i];
            
            for (var i = 0; i != length; i++)
            {
                var instanceEntity = naEntities[i];

                var clientEntity = EntityManager.CreateEntity(m_ClientArchetype);
                EntityManager.SetComponentData(clientEntity, new ClientToNetworkInstance(instanceEntity));
                EntityManager.AddComponentData(instanceEntity, new NetworkInstanceToClient(clientEntity));
                
                if (naData[i].IsLocal())
                    EntityManager.AddComponent(clientEntity, typeof(Patapon4LocalTag));
            }

            naEntities.Dispose();
            naData.Dispose();
        }
    }
}