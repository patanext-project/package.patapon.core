using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace Patapon4TLB.Core.Networking
{
    public class SnapshotManager : ComponentSystem
    {
        public struct GenerateResult
        {
            public StSnapshotRuntime Runtime;
            public DataBufferWriter Data;

            public bool IsCreated => Data.GetSafePtr() != IntPtr.Zero;

            public void Dispose()
            {
                Runtime.Dispose();
                Data.Dispose();
            }
        }
        
        private ComponentDataFromEntity<NetworkInstanceData> m_NetworkInstanceFromEntity;
        private ComponentGroup m_LocalClientGroup;
        private ComponentGroup m_NetworkClientGroup;
        private ComponentGroup m_GenerateSnapshotEntityGroup;

        protected override void OnCreateManager()
        {
            m_LocalClientGroup = GetComponentGroup(typeof(Patapon4Client), typeof(Patapon4LocalTag));
            m_NetworkInstanceFromEntity = GetComponentDataFromEntity<NetworkInstanceData>();
            m_NetworkClientGroup = GetComponentGroup(typeof(Patapon4Client), typeof(ClientToNetworkInstance));
            m_GenerateSnapshotEntityGroup = GetComponentGroup(typeof(GenerateEntitySnapshot));
        }
        
        protected override void OnUpdate()
        {
            return;
            
            /*var entityLength = m_GenerateSnapshotEntityGroup.CalculateLength();
            if (entityLength < 0)
                return;

            var entities = TransformEntityArray(m_GenerateSnapshotEntityGroup.GetEntityArray(), Allocator.TempJob);
            if (!DoLocalGeneration(entities, default, Allocator.TempJob, out _))
            {
                entities.Dispose();
                return;
            }

            var clientLength = m_NetworkClientGroup.CalculateLength();
            if (clientLength < 0)
            {
                entities.Dispose();
                return;
            }

            var entityArray          = m_NetworkClientGroup.GetEntityArray();
            var clientToNetworkArray = m_NetworkClientGroup.GetComponentDataArray<ClientToNetworkInstance>();
            for (int i = 0; i != clientLength; i++)
            {
                // A threaded job for each client snapshot could be possible?

                var entity          = entityArray[i];
                var networkInstance = m_NetworkInstanceFromEntity[clientToNetworkArray[i].Target];
                var netCmd          = networkInstance.Commands;
                var receiver        = new SnapshotReceiver(entity, true);

                DataBufferWriter dataBuffer;
                using (dataBuffer = new DataBufferWriter(Allocator.Temp, 2048))
                {
                    var generation = StartGenerating(receiver, default, Allocator.TempJob, entities);
                    CompleteGeneration(generation);

                    dataBuffer.WriteStatic(generation.Data);
                }
            }

            entities.Dispose();*/
        }

        public NativeArray<Entity> GetSnapshotWorldEntities(Allocator allocator)
        {
            return TransformEntityArray(m_GenerateSnapshotEntityGroup.GetEntityArray(), allocator);
        }

        [BurstCompile]
        struct TransformEntityArrayJob : IJobParallelFor
        {
            public EntityArray EntityArray;
            public NativeArray<Entity> Entities;
            
            public void Execute(int index)
            {
                Entities[index] = EntityArray[index];
            }
        }

        public NativeArray<Entity> TransformEntityArray(EntityArray entityArray, Allocator allocator)
        {
            var entityLength = entityArray.Length;
            var entities     = new NativeArray<Entity>(entityLength, allocator);
            
            new TransformEntityArrayJob
            {
                EntityArray = entityArray,
                Entities    = entities
            }.Run(entityLength);

            return entities;
        }

        public GenerateResult GenerateLocalSnapshot(Allocator allocator, SnapshotReceiverFlags flags, GenerateResult previousResult = default)
        {
            var entities      = GetSnapshotWorldEntities(allocator);
            var localReceiver = new SnapshotReceiver(m_LocalClientGroup.GetEntityArray()[0], flags);
            var data          = new DataBufferWriter(allocator, true, 128 + entities.Length * 8);
            var gameTime      = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton();
            var result        = GenerateSnapshot(localReceiver, gameTime, entities, allocator, data, previousResult);

            result.Runtime.Entities = entities;

            return result;
        }

        public unsafe GenerateResult GenerateSnapshot(SnapshotReceiver    receiver,
                                                      GameTime            gt,
                                                      NativeArray<Entity> entities,
                                                      Allocator           allocator,
                                                      DataBufferWriter    data,
                                                      GenerateResult      previousResult)
        {
            void WriteFullEntities()
            {
                data.Write((byte) 0);
                data.WriteDynInteger((ulong) entities.Length);
                if (entities.Length > 0)
                    data.WriteDataSafe((byte*) entities.GetUnsafePtr(), entities.Length * sizeof(Entity), default);
            }

            void WriteIncrementalEntities()
            {
                WriteFullEntities();
                return;
                
                // If 'previousResult' is null or there is no entities on both side, we fallback to writing all entities
                if (!previousResult.IsCreated || entities.Length == 0 || previousResult.Runtime.Entities.Length == 0)
                {
                    WriteFullEntities();
                    return;
                }

                ref var previousEntities = ref previousResult.Runtime.Entities;

                data.Write((byte) 1);
                
                // -----------------------------------------------------------------
                // -> Removed entities to the buffer
                var removedLengthMarker = data.Write(0);
                var removedLength = 0;
                
                // Detect entities that don't exist anymore
                for (var i = 0; i != previousEntities.Length; i++)
                {
                    var prev = previousEntities[i];
                    var exist = false;
                    
                    for (var j = 0; j != entities.Length; j++)
                    {
                        var curr = entities[j];
                        if (prev != curr) 
                            continue;
                        
                        exist = true;
                        break;
                    }

                    if (exist) 
                        continue;

                    data.Write(ref prev);
                    removedLength++;
                }

                data.Write(ref removedLength, removedLengthMarker);
                
                // -----------------------------------------------------------------
                // -> Added entities to the buffer
                var addedLengthMarker = data.Write(0);
                var addedLength = 0;

                for (var i = 0; i != entities.Length; i++)
                {
                    var curr = entities[i];
                    var exist = false;

                    for (var j = 0; j != previousEntities.Length; j++)
                    {
                        var prev = previousEntities[j];
                        if (curr != prev)
                            continue;

                        exist = true;
                        break;
                    }

                    if (exist)
                        continue;

                    data.Write(ref curr);
                    addedLength++;
                }
            }

            var header = new StSnapshotHeader(gt);
            var runtime = new StSnapshotRuntime(header, allocator)
            {
                Entities = entities
            };

            runtime.UpdateHashMapFromLocalData();

            // Write Game time
            data.Write(ref gt);

            // Write entity data
            if ((receiver.Flags & SnapshotReceiverFlags.FullData) != 0)
            {
                WriteFullEntities();
            }
            else
            {
                WriteIncrementalEntities();
            }

            foreach (var obj in AppEvent<ISnapshotSubscribe>.GetObjEvents())
                obj.SubscribeSystem();

            var systemsMfc = AppEvent<ISnapshotManageForClient>.GetObjEvents();
            data.Write(systemsMfc.Length);

            // Write system data
            foreach (var obj in systemsMfc)
            {
                Debug.Log("Writing " + obj.GetSystemPattern().InternalIdent.Name);
                
                JobHandle uselessHandle = default;

                var pattern = obj.GetSystemPattern();
                var sysData = obj.WriteData(receiver, runtime, ref uselessHandle);
                
                uselessHandle.Complete();
                
                // We need the latest reference from the buffer data
                // If the buffer get resized, the pointer to the buffer is invalid.
                sysData.UpdateReference();

                // Used for skipping data when reading
                data.WriteDynInteger((ulong) pattern.Id);
                data.WriteDynInteger((ulong) sysData.Length);
                data.WriteStatic(sysData);
                sysData.Dispose();
            }

            return new GenerateResult {Data = data, Runtime = runtime};
        }

        public unsafe StSnapshotRuntime ApplySnapshotFromData(SnapshotSender sender, DataBufferReader data, StSnapshotRuntime previousRuntime, PatternBank patternBank)
        {
            // Terminate the function if the runtime is bad.
            if (previousRuntime.Allocator == Allocator.None || previousRuntime.Allocator == Allocator.Invalid)
            {
                throw new Exception($"{nameof(previousRuntime.Allocator)} is set as None or Invalid. This may be caused by a non defined or corrupted runtime.");
            }

            /*using (var file = File.Create(Application.streamingAssetsPath + "/snapshot_" + Environment.TickCount + ".bin"))
            {
                for (int i = 0; i != data.Length; i++)
                {
                    file.WriteByte(data.DataPtr[i]);
                }
            }*/

            // --------------------------------------------------------------------------- //
            // Read Only Data...
            var allocator = previousRuntime.Allocator;

            // --------------------------------------------------------------------------- //
            // Local Functions
            // --------------------------------------------------------------------------- //
            ISnapshotManageForClient GetSystem(int id)
            {
                return AppEvent<ISnapshotManageForClient>.GetObjEvents().FirstOrDefault(system => system.GetSystemPattern().Id == id);
            }

            void ReadFullEntities(out NativeArray<Entity> entities)
            {
                var entityLength = (int) data.ReadDynInteger();
                if (entityLength <= 0) Debug.LogWarning("No entities.");

                entities = new NativeArray<Entity>(entityLength, allocator);
                UnsafeUtility.MemCpy(entities.GetUnsafePtr(), data.DataPtr + data.CurrReadIndex, entityLength * sizeof(Entity));

                data.CurrReadIndex += entityLength * sizeof(Entity);
            }

            // --------------------------------------------------------------------------- //
            // Actual code
            // --------------------------------------------------------------------------- //
            var gameTime = data.ReadValue<GameTime>();

            var header  = new StSnapshotHeader(gameTime);
            var runtime = new StSnapshotRuntime(header, previousRuntime, allocator);

            // Read Entity Data
            var entityDataType = data.ReadValue<byte>();
            switch (entityDataType)
            {
                // Full data
                case 0:
                {
                    ReadFullEntities(out runtime.Entities);
                    break;
                }
                case 1:
                {
                    break;
                }
                default:
                {
                    throw new Exception("Exception when reading.");
                }
            }

            foreach (var obj in AppEvent<ISnapshotSubscribe>.GetObjEvents())
                obj.SubscribeSystem();

            // Read System Data
            var systemLength = data.ReadValue<int>();
            for (var i = 0; i != systemLength; i++)
            {
                JobHandle uselessHandle = default;

                var foreignSystemPattern = (int) data.ReadDynInteger();
                var length               = (int) data.ReadDynInteger();

                var systemPattern = patternBank.GetPatternResult(foreignSystemPattern);
                var system        = GetSystem(systemPattern.Id);

                Debug.Log("Reading " + systemPattern.InternalIdent.Name);

                system.ReadData(sender, runtime, new DataBufferReader(data, data.CurrReadIndex, data.CurrReadIndex + length), ref uselessHandle);

                data.CurrReadIndex += length;
            }

            return runtime;
        }
    }
}