using System.Collections.Generic;
using System.Diagnostics;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using Patapon4TLB.Core.Networking;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Patapon4TLB.Core.Tests
{
    [AlwaysUpdateSystem]
    public unsafe class NetworkSnapshotMgr : ComponentSystem, INativeEventOnGUI
    {
        public struct ClientSnapshotState : ISystemStateComponentData
        {
        }

        private struct SnapshotDataToApply
        {
            public SnapshotSender Sender;
            public DataBufferReader Data;
            public PatternBankExchange Exchange;
        }
        
        private PatternResult m_SnapshotPattern;

        private ComponentGroup m_ClientWithoutState;
        private ComponentGroup m_DestroyedClientWithState;
        private ComponentGroup m_EntitiesToGenerate;
        
        private StSnapshotRuntime m_CurrentRuntime;

        private Dictionary<Entity, StSnapshotRuntime> m_ClientRuntimes;
        private List<SnapshotDataToApply> m_SnapshotDataToApply;

        private float m_AvgSnapshotSize;
        private int m_ReceivedSnapshotOnFrame;

        protected override void OnCreateManager()
        {
            m_ClientRuntimes = new Dictionary<Entity, StSnapshotRuntime>(16);
            m_SnapshotDataToApply = new List<SnapshotDataToApply>();
        }

        protected override void OnStartRunning()
        {
            m_SnapshotPattern = World.GetExistingManager<NetPatternSystem>()
                                     .GetLocalBank()
                                     .Register(new PatternIdent("SyncSnapshot"));

            m_ClientWithoutState = GetComponentGroup(typeof(Patapon4Client), ComponentType.Subtractive<ClientSnapshotState>());
            m_DestroyedClientWithState = GetComponentGroup(typeof(ClientSnapshotState), ComponentType.Subtractive<Patapon4Client>());
            m_EntitiesToGenerate = GetComponentGroup(typeof(GenerateEntitySnapshot));
            
            m_CurrentRuntime = new StSnapshotRuntime(default, Allocator.Persistent);
            
            World.GetExistingManager<AppEventSystem>().SubscribeToAll(this);
        }

        protected override void OnUpdate()
        {
            // We need to complete all jobs that are writing to entity components.
            EntityManager.CompleteAllJobs();
            m_SnapshotDataToApply.Clear();
            
            var gameTime = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton();
            var snapshotMgr = World.GetExistingManager<SnapshotManager>();
            var networkMgr = World.GetExistingManager<NetworkManager>();
            var netPatternSystem = World.GetExistingManager<NetPatternSystem>();
            
            // Receive data from server
            ForEach((DynamicBuffer<EventBuffer> eventBuffer, ref NetworkInstanceData data) =>
            {
                var exchange = netPatternSystem.GetLocalExchange(data.Id);
                var bank = netPatternSystem.GetBank(data.Id);

                for (int i = 0; i != eventBuffer.Length; i++)
                {
                    var ev = eventBuffer[i].Event;
                    
                    if (ev.Type != NetworkEventType.DataReceived)
                        continue;

                    var reader = new DataBufferReader(ev.Data, ev.DataLength);
                    var msgType = reader.ReadValue<MessageType>();

                    if (msgType != MessageType.MessagePattern)
                        continue;

                    var patternId = reader.ReadValue<int>();
                    if (m_SnapshotPattern != exchange.GetOriginId(patternId))
                        continue;
                    
                    m_SnapshotDataToApply.Add(new SnapshotDataToApply
                    {
                        Sender = new SnapshotSender{Client = networkMgr.GetNetworkInstanceEntity(ev.Invoker.Id)},
                        Data = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length),
                        Exchange = exchange
                    });
                }
            });

            m_ReceivedSnapshotOnFrame = 0;

            foreach (var value in m_SnapshotDataToApply)
            {
                var data = value.Data;
                m_CurrentRuntime = snapshotMgr.ApplySnapshotFromData(value.Sender, ref data, ref m_CurrentRuntime, value.Exchange);

                m_AvgSnapshotSize = Mathf.Lerp(m_AvgSnapshotSize, data.Length, 0.5f);
                m_ReceivedSnapshotOnFrame++;
            }

            using (var entityArray = m_ClientWithoutState.ToEntityArray(Allocator.TempJob))
            {
                foreach (var e in entityArray)
                {
                    m_ClientRuntimes[e] = new StSnapshotRuntime(default, Allocator.Persistent);
                    
                    EntityManager.AddComponent(e, ComponentType.Create<ClientSnapshotState>());
                }
            }
            
            using (var entityArray = m_DestroyedClientWithState.ToEntityArray(Allocator.TempJob))
            {
                foreach (var e in entityArray)
                {
                    m_ClientRuntimes.Remove(e);
                    
                    EntityManager.RemoveComponent(e, ComponentType.Create<ClientSnapshotState>());
                }
            }

            using (var entities = m_EntitiesToGenerate.ToEntityArray(Allocator.TempJob))
            {
                // Send data to clients
                ForEach((ref NetworkInstanceData networkInstanceData, ref NetworkInstanceToClient networkToClient) =>
                {
                    if (networkInstanceData.InstanceType != InstanceType.Client)
                        return;
                    
                    var clientEntity  = networkToClient.Target;
                    var clientRuntime = m_ClientRuntimes[clientEntity];

                    var data = new DataBufferWriter(Allocator.TempJob);

                    data.CpyWrite(MessageType.MessagePattern);
                    data.CpyWrite(m_SnapshotPattern.Id);

                    var generation = snapshotMgr.GenerateForConnection(clientEntity, entities, true, gameTime, Allocator.Persistent, ref data, ref clientRuntime);
                    
                    data.WriteStatic(generation.Data);

                    networkInstanceData.Commands.Send(data, default, Delivery.Reliable);
                    
                    m_AvgSnapshotSize = Mathf.Lerp(m_AvgSnapshotSize, data.Length, 0.5f);
                    
                    data.Dispose();

                    m_ClientRuntimes[clientEntity] = clientRuntime;
                });
            }
        }

        public void NativeOnGUI()
        {
            using (new GUILayout.VerticalScope())
            {
                GUI.color = Color.black;
                GUILayout.Label("Snapshot System:");
                GUILayout.Space(1);
                GUILayout.Label($"Avg Snapshot Size={m_AvgSnapshotSize}B");
                GUILayout.Label($"Frame Snapshot Count={m_ReceivedSnapshotOnFrame}");
            }
        }
    }
}