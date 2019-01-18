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
            ForEach((Entity instanceEntity, ref NetworkInstanceData instanceData) =>
            {
                var clientEntity = PostUpdateCommands.CreateEntity(m_ClientArchetype);
                PostUpdateCommands.SetComponent(clientEntity, new ClientToNetworkInstance(instanceEntity));
                PostUpdateCommands.AddComponent(instanceEntity, new NetworkInstanceToClient(clientEntity));
            }, m_Group);

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