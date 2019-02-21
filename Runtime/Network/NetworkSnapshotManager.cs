using System;
using System.Collections.Generic;
using System.Diagnostics;
using package.stormiumteam.networking;
using package.stormiumteam.networking.lz4;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using Runtime;
using Stormium.Core;
using StormiumShared.Core;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using UpdateLoop = package.stormiumteam.networking.runtime.highlevel.UpdateLoop;

namespace Patapon4TLB.Core
{
    public unsafe class NetworkSnapshotManager : GameBaseSystem, INativeEventOnGUI
    {
        public struct ClientSnapshotState : ISystemStateComponentData
        {
        }

        private struct ClientSnapshotInformation
        {
            public float GenerationTimeAvg;
            public StSnapshotRuntime Runtime;
        }

        private struct SnapshotDataToApply
        {
            public SnapshotSender      Sender;
            public int TotalDataSize;
            public bool IsCompressed;
            public DataBufferReader    Data;
            public PatternBankExchange Exchange;

            public void Alloc(IntPtr originalData, int start, int end)
            {
                var length = end - start;
                
                var dataPtr = UnsafeUtility.Malloc(length, UnsafeUtility.AlignOf<byte>(), Allocator.Persistent);
                UnsafeUtility.MemCpy(dataPtr, (void*)(originalData + start), length);
                
                Data = new DataBufferReader((IntPtr) dataPtr, length);
            }
            
            public void Free()
            {
                UnsafeUtility.Free(Data.DataPtr, Allocator.Persistent);
            }
        }

        private PatternResult m_SnapshotPattern;

        private ComponentGroup m_ClientWithoutState;
        private ComponentGroup m_DestroyedClientWithState;
        private ComponentGroup m_EntitiesToGenerate;

        private StSnapshotRuntime m_CurrentRuntime;

        private Dictionary<Entity, ClientSnapshotInformation> m_ClientSnapshots;
        private List<SnapshotDataToApply>             m_SnapshotDataToApply;

        private float m_AvgSnapshotSize;
        private float m_AvgSnapshotSizeCompressed;
        private int   m_ReceivedSnapshotOnFrame;

        private long m_ReadTime;

        private int m_SnapshotCount;
        private int m_SnapshotInQueue;
        public StSnapshotRuntime CurrentSnapshot => m_CurrentRuntime;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            
            m_SnapshotPattern = World.GetExistingManager<NetPatternSystem>()
                                     .GetLocalBank()
                                     .Register(new PatternIdent("SyncSnapshot"));

            m_ClientWithoutState       = GetComponentGroup(typeof(NetworkClient), ComponentType.Subtractive<ClientSnapshotState>());
            m_DestroyedClientWithState = GetComponentGroup(typeof(ClientSnapshotState), ComponentType.Subtractive<NetworkClient>());
            m_EntitiesToGenerate       = GetComponentGroup(typeof(GenerateEntitySnapshot));

            m_CurrentRuntime = new StSnapshotRuntime(default, Allocator.Persistent);

            World.GetExistingManager<AppEventSystem>().SubscribeToAll(this);
            
            m_ClientSnapshots      = new Dictionary<Entity, ClientSnapshotInformation>(16);
            m_SnapshotDataToApply = new List<SnapshotDataToApply>();

            m_SnapshotCount = 1;
        }

        protected override void OnUpdate()
        {
            DoClientUpdate();
            DoServerUpdate();
        }

        public void DoClientUpdate()
        {
            EntityManager.CompleteAllJobs();
            m_SnapshotDataToApply.Clear();
            
            ReceiveServerSnapshots();
 
            m_ReceivedSnapshotOnFrame = 0;

            ReadServerSnapshots();

            m_SnapshotInQueue = m_SnapshotDataToApply.Count;
            
            ForEach((ref NetworkInstanceData data, ref GameTimeComponent clientTime) => { clientTime.Value.Tick += TickDelta; });
        }

        public void DoServerUpdate()
        {
            EntityManager.CompleteAllJobs();
            m_SnapshotDataToApply.Clear();
            
            using (var entityArray = m_ClientWithoutState.ToEntityArray(Allocator.TempJob))
            {
                foreach (var e in entityArray)
                {
                    m_ClientSnapshots[e] = new ClientSnapshotInformation
                    {
                        Runtime = new StSnapshotRuntime(default, Allocator.Persistent)
                    };

                    EntityManager.AddComponent(e, ComponentType.Create<ClientSnapshotState>());
                }
            }

            using (var entityArray = m_DestroyedClientWithState.ToEntityArray(Allocator.TempJob))
            {
                foreach (var e in entityArray)
                {
                    m_ClientSnapshots.Remove(e);

                    EntityManager.RemoveComponent(e, ComponentType.Create<ClientSnapshotState>());
                }
            }
            
            SendClientSnapshots();
        }

        public void ReceiveServerSnapshots()
        {
            var networkMgr       = World.GetExistingManager<NetworkManager>();
            var netPatternSystem = World.GetExistingManager<NetPatternSystem>();

            ForEach((DynamicBuffer<EventBuffer> eventBuffer, ref NetworkInstanceData data) =>
            {
                for (int i = 0; i != eventBuffer.Length; i++)
                {
                    var ev = eventBuffer[i].Event;

                    if (ev.Type != NetworkEventType.DataReceived)
                        continue;

                    var reader  = new DataBufferReader(ev.Data, ev.DataLength);
                    var msgType = reader.ReadValue<MessageType>();

                    if (msgType != MessageType.MessagePattern)
                        continue;

                    var exchange  = netPatternSystem.GetLocalExchange(ev.Invoker.Id);
                    var patternId = reader.ReadValue<int>();

                    if (m_SnapshotPattern != exchange.GetOriginId(patternId))
                        continue;

                    var size         = reader.ReadValue<int>();
                    var isCompressed = reader.ReadValue<bool>();
                    
                    //Debug.Log($"[{Time.frameCount}] Received from {data.ParentId} (s={size}, c={isCompressed})");
                    var toApply = new SnapshotDataToApply
                    {
                        Sender        = new SnapshotSender {Client = networkMgr.GetNetworkInstanceEntity(ev.Invoker.Id), Flags = SnapshotFlags.None},
                        TotalDataSize = size,
                        IsCompressed  = isCompressed,
                        Exchange      = exchange
                    };
                    
                    toApply.Alloc((IntPtr) reader.DataPtr, reader.CurrReadIndex, reader.Length);

                    /*while (m_SnapshotDataToApply.Count > 0) // not a good idea for deltas
                    {
                        m_SnapshotDataToApply[m_SnapshotDataToApply.Count - 1].Free();
                        m_SnapshotDataToApply.RemoveAt(m_SnapshotDataToApply.Count - 1);
                    }*/
                    
                    m_SnapshotDataToApply.Add(toApply);
                }
            });
        }
        
        public void ReadServerSnapshots()
        {
            var gameTime         = World.GetExistingManager<P4GameTimeManager>().GetTimeFromSingleton();
            var snapshotMgr      = World.GetExistingManager<SnapshotManager>();
            var sw = new Stopwatch();

            //Debug.Log("Message before crash...");
            foreach (var value in m_SnapshotDataToApply)
            {
                var dataSize       = value.TotalDataSize;
                var compressedData = value.Data;
                var isCompressed = value.IsCompressed;

                DataBufferReader data = default;
                
                // Decompress data...
                if (!isCompressed)
                {
                    data = value.Data;

                    try
                    {
                        m_CurrentRuntime = snapshotMgr.ApplySnapshotFromData(value.Sender, ref data, ref m_CurrentRuntime, value.Exchange);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error when applying snapshot (t={m_CurrentRuntime.Header.GameTime.Tick})\n{ex.Source}");
                        Debug.LogException(ex);
                    }

                    m_AvgSnapshotSize = Mathf.Lerp(m_AvgSnapshotSize, data.Length, 0.5f);
                    m_ReceivedSnapshotOnFrame++;
                }
                else
                {
                    using (var decompressed = new UnsafeAllocationLength<byte>(Allocator.Temp, dataSize))
                    {
                        Lz4Wrapper.Decompress(compressedData.DataPtr, (byte*) decompressed.Data, compressedData.Length, dataSize);

                        data = new DataBufferReader((byte*) decompressed.Data, dataSize);

                        try
                        {
                            Profiler.BeginSample("Applying snapshot");
                            sw.Restart();
                            m_CurrentRuntime = snapshotMgr.ApplySnapshotFromData(value.Sender, ref data, ref m_CurrentRuntime, value.Exchange);
                            sw.Stop();

                            m_ReadTime = sw.ElapsedTicks;
                            Profiler.EndSample();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error when applying compressed snapshot (t={m_CurrentRuntime.Header.GameTime.Tick})\n{ex.Source}");
                            Debug.LogException(ex);
                        }

                        m_AvgSnapshotSize           = Mathf.Lerp(m_AvgSnapshotSize, dataSize, 0.5f);
                        m_AvgSnapshotSizeCompressed = Mathf.Lerp(m_AvgSnapshotSizeCompressed, compressedData.Length, 0.5f);
                        m_ReceivedSnapshotOnFrame++;
                    }
                }
                
                value.Free();

                // Apply time values
                if (EntityManager.HasComponent<GameTimeComponent>(value.Sender.Client))
                {
                    var clientTime = EntityManager.GetComponentData<GameTimeComponent>(value.Sender.Client);
                    if (Mathf.Abs(clientTime.Value.Tick - m_CurrentRuntime.Header.GameTime.Tick) > gameTime.DeltaTick * 2)
                    {
                        var diff = m_CurrentRuntime.Header.GameTime.Tick - clientTime.Value.Tick;
                        Debug.Log($"L.T.D. c={clientTime.Value.Tick} s={m_CurrentRuntime.Header.GameTime.Tick} d={diff} dt={m_CurrentRuntime.Header.GameTime.DeltaTick}");
                        clientTime.Value.Tick = m_CurrentRuntime.Header.GameTime.Tick;

                        EntityManager.SetComponentData(value.Sender.Client, clientTime);
                    }
                }
                else
                {
                    EntityManager.AddComponentData(value.Sender.Client, new GameTimeComponent(m_CurrentRuntime.Header.GameTime));
                }
            }
            //Debug.Log("Message after crash? (or not)");
        }

        public void SendClientSnapshots()
        {
            var gameMgr = World.GetExistingManager<P4GameManager>();
            var gameTime         = World.GetExistingManager<P4GameTimeManager>().GetTimeFromSingleton();
            var snapshotMgr      = World.GetExistingManager<SnapshotManager>();
            var localClient = gameMgr.Client;
            var sw = new Stopwatch();
            
            using (var entities = m_EntitiesToGenerate.ToEntityArray(Allocator.TempJob))
            {
                // Send data to clients
                ForEach((ref NetworkInstanceData networkInstanceData, ref NetworkInstanceToClient networkToClient) =>
                {
                    if (networkInstanceData.InstanceType != InstanceType.Client)
                        return;

                    var clientEntity  = networkToClient.Target;
                    var clientSnapshotInfo = m_ClientSnapshots[clientEntity];
                    var clientRuntime = clientSnapshotInfo.Runtime;
                    var isFullSnapshot = clientSnapshotInfo.GenerationTimeAvg <= 1;

                    sw.Restart();
                    
                    var data = new DataBufferWriter(0, Allocator.TempJob);

                    data.WriteUnmanaged(MessageType.MessagePattern);
                    data.WriteUnmanaged<int>(m_SnapshotPattern.Id);

                    var genData = new DataBufferWriter(0, Allocator.Persistent);
                    Profiler.BeginSample("Generation for " + clientEntity);
                    var generation = snapshotMgr.GenerateForConnection(localClient, clientEntity, entities, isFullSnapshot, m_SnapshotCount++, gameTime, Allocator.Persistent, ref genData, ref clientRuntime);
                    Profiler.EndSample();
                    
                    // Write the length of uncompressed data.
                    data.WriteInt(generation.Data.Length);

                    var isCompressed = generation.Data.Length > 96;
                    
                    // Write an information about if the data is compressed or not
                    data.WriteRef(ref isCompressed);

                    var compressedLength = 0;
                    // If it should not be compressed, we write the data directly
                    if (!isCompressed)
                    {
                        data.WriteBuffer(generation.Data);
                    }
                    // If it is, we compress the data
                    else
                    {
                        using (var compressed = new UnsafeAllocationLength<byte>(Allocator.Temp, generation.Data.Length))
                        {
                            var uncompressedData = (byte*) generation.Data.GetSafePtr();
                            compressedLength = Lz4Wrapper.Compress(uncompressedData, generation.Data.Length, (byte*) compressed.Data);
                            data.WriteDataSafe((byte*) compressed.Data, compressedLength, default);
                        }
                    }

                    networkInstanceData.Commands.Send(data, default, Delivery.Reliable);

                    m_AvgSnapshotSize = Mathf.Lerp(m_AvgSnapshotSize, generation.Data.Length, 0.5f);
                    m_AvgSnapshotSizeCompressed = Mathf.Lerp(m_AvgSnapshotSizeCompressed, compressedLength, 0.5f);
                    
                    genData.Dispose();
                    data.Dispose();
                    
                    sw.Stop();

                    clientSnapshotInfo.Runtime = clientRuntime;
                    clientSnapshotInfo.GenerationTimeAvg = Mathf.Lerp(clientSnapshotInfo.GenerationTimeAvg, sw.ElapsedTicks, 0.5f);

                    m_ClientSnapshots[clientEntity] = clientSnapshotInfo;
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
                GUILayout.Label($"Avg Snapshot Size={m_AvgSnapshotSize:F0}B (c={m_AvgSnapshotSizeCompressed:F0}B)");
                GUILayout.Label($"Frame Snapshot Count={m_ReceivedSnapshotOnFrame}");
                GUILayout.Space(1);
                if (GameMgr.GameType == GameType.Client)
                {
                    GUILayout.Label($"ReadTime={m_ReadTime}t");
                }
                
                ForEach((ref NetworkInstanceData networkInstanceData, ref NetworkInstanceToClient networkToClient) =>
                {
                    if (networkInstanceData.InstanceType != InstanceType.Client)
                        return;

                    var clientEntity = networkToClient.Target;
                    var genTime = (long) m_ClientSnapshots[clientEntity].GenerationTimeAvg;
                    GUILayout.Label($"Id={networkInstanceData.Id} GenTime={genTime}t ({genTime * 0.0001f}ms)");
                });
            }
        }
    }
}