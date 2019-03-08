using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Patapon4TLB.Core.Tests
{
    public class PlayerInputDataStreamer : SnapshotEntityDataStreamer<PlayerInput>
    {

    }

    public struct PlayerInput : IStateData, IComponentData
    {
        public float2 Value;
    }

    [UpdateAfter(typeof(CreateClientForNetworkInstanceSystem))]
    public class PlayerInputSystemSync : ComponentSystem
    {
        private ComponentGroup m_Group, m_GroupWithoutInputs, m_EventInstances;
        private PatternResult  m_SyncPattern;

        protected override void OnCreateManager()
        {
            m_Group = GetComponentGroup
            (
                typeof(PlayerInput),
                typeof(Patapon4LocalTag)
            );
            m_GroupWithoutInputs = GetComponentGroup
            (
                typeof(Patapon4Client),
                ComponentType.Exclude<PlayerInput>()
            );
            m_EventInstances = GetComponentGroup
            (
                typeof(NetworkInstanceData),
                typeof(EventBuffer),
                typeof(ValidInstanceTag)
            );
        }

        protected override void OnStartRunning()
        {
            m_SyncPattern = World.GetExistingManager<NetPatternSystem>()
                                 .GetLocalBank()
                                 .Register(new PatternIdent("PlayerInput.Sync"));
        }

        private void DealWithGroupWithoutInputs()
        {
            var length = m_GroupWithoutInputs.CalculateLength();
            if (length == 0)
                return;

            var entityArray = m_GroupWithoutInputs.GetEntityArray();
            for (var i = 0; i != length; i++)
            {
                PostUpdateCommands.AddComponent(entityArray[i], new PlayerInput());
            }
        }

        private void DealWithGroupWithInputs()
        {
            ForEach((Entity entity, ref PlayerInput playerInput) =>
            {
                playerInput.Value = new float2
                (
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")     
                );

                if (!EntityManager.HasComponent<ClientToNetworkInstance>(entity))
                    return;

                var instanceEntity = EntityManager.GetComponentData<ClientToNetworkInstance>(entity).Target;
                if (!EntityManager.Exists(instanceEntity))
                {
                    Debug.LogError("Error");
                    return;
                }

                var data = EntityManager.GetComponentData<NetworkInstanceData>(instanceEntity);
                if (data.InstanceType != InstanceType.LocalClient)
                    return;
                
                var buffer = new DataBufferWriter(Allocator.Temp);
                buffer.CpyWrite(MessageType.MessagePattern);
                buffer.CpyWrite(m_SyncPattern.Id);
                buffer.Write(ref playerInput);

                var serverConCmd = EntityManager.GetComponentData<NetworkInstanceData>(data.Parent).Commands;
                serverConCmd.Send(buffer, default, Delivery.Unreliable);

            }, m_Group);
        }

        private void SyncMessages()
        {
            var netPatternSystem = World.GetExistingManager<NetPatternSystem>();
            var networkMgr       = World.GetExistingManager<NetworkManager>();

            ForEach((DynamicBuffer<EventBuffer> eventBuffer, ref NetworkInstanceData instanceData) =>
            {
                var exchange = netPatternSystem.GetLocalExchange(instanceData.Id);

                // Process events
                for (var evIndex = 0; evIndex != eventBuffer.Length; evIndex++)
                {
                    var ev = eventBuffer[evIndex].Event;
                    // Process only 'received' event.
                    if (ev.Type != NetworkEventType.DataReceived)
                        continue;

                    var reader  = new DataBufferReader(ev.GetDataSafe());
                    var msgType = reader.ReadValue<MessageType>();
                    // Process only 'MessagePattern' messages.
                    if (msgType != MessageType.MessagePattern)
                        continue;

                    var foreignPatternId = reader.ReadValue<int>();
                    if (m_SyncPattern != exchange.GetOriginId(foreignPatternId))
                        continue;

                    var input        = reader.ReadValue<PlayerInput>();
                    var entityOrigin = networkMgr.GetNetworkInstanceEntity(ev.Invoker.Id);
                    var clientEntity = EntityManager.GetComponentData<NetworkInstanceToClient>(entityOrigin)
                                                    .Target;

                    EntityManager.SetComponentData(clientEntity, input);
                }
            }, m_EventInstances);
        }

        protected override void OnUpdate()
        {
            DealWithGroupWithoutInputs();
            DealWithGroupWithInputs();

            SyncMessages();
        }
    }
}