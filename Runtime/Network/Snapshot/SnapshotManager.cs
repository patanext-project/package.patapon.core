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

namespace StormiumShared.Core.Networking
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

        protected override void OnUpdate()
        {
        }

        [BurstCompile]
        struct TransformEntityArrayJob : IJobParallelFor
        {
            public NativeArray<Entity> EntityArray;
            public NativeArray<SnapshotEntityInformation> Entities;

            [ReadOnly]
            public ComponentDataFromEntity<ModelIdent> Component;
            
            public void Execute(int index)
            {
                Entities[index] = new SnapshotEntityInformation(EntityArray[index], Component[EntityArray[index]].Id);
            }
        }

        public NativeArray<SnapshotEntityInformation> TransformEntityArray(NativeArray<Entity> entityArray, Allocator allocator)
        {
            var entityLength = entityArray.Length;
            var entities     = new NativeArray<SnapshotEntityInformation>(entityLength, allocator);
            
            new TransformEntityArrayJob
            {
                EntityArray = entityArray,
                Entities    = entities,
                
                Component = GetComponentDataFromEntity<ModelIdent>()
            }.Run(entityLength);

            return entities;
        }

        /*public GenerateResult GenerateLocalSnapshot(NativeArray<Entity> nfEntities, Allocator allocator, SnapshotReceiverFlags flags, ref GenerateResult previousResult)
        {
            var entities      = TransformEntityArray(nfEntities, allocator);
            var localReceiver = new SnapshotReceiver(m_LocalClientGroup.GetEntityArray()[0], flags);
            var data          = new DataBufferWriter(allocator, true, 128 + entities.Length * 8);
            var gameTime      = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton();
            var result        = GenerateSnapshot(localReceiver, gameTime, entities, allocator, ref data, ref previousResult.Runtime);

            result.Runtime.Entities = entities;

            return result;
        }*/

        public GenerateResult GenerateForConnection(Entity         client,
                                                    NativeArray<Entity> nfEntities,
                                                    bool           fullSnapshot,
                                                    GameTime       gameTime,
                                                    Allocator      allocator,
                                                    ref DataBufferWriter data,
                                                    ref StSnapshotRuntime previousRuntime)
        {
            var entities = TransformEntityArray(nfEntities, allocator);
            var receiver = new SnapshotReceiver(client, fullSnapshot ? SnapshotReceiverFlags.FullData : SnapshotReceiverFlags.None);

            return GenerateSnapshot(receiver, gameTime, entities, allocator, ref data, ref previousRuntime);
        }
        
        private unsafe void WriteFullEntities(ref DataBufferWriter data, ref NativeArray<SnapshotEntityInformation> entities)
        {
            data.Write((byte) 0);
            data.WriteDynInteger((ulong) entities.Length);
            if (entities.Length > 0) data.WriteDataSafe((byte*) entities.GetUnsafePtr(), entities.Length * sizeof(SnapshotEntityInformation), default);
        }

        private unsafe void WriteIncrementalEntities(ref DataBufferWriter data, ref NativeArray<SnapshotEntityInformation> entities, ref StSnapshotRuntime previousRuntime)
        {
            WriteFullEntities(ref data, ref entities);
            return;

            // If 'previousResult' is null or there is no entities on both side, we fallback to writing all entities
            /*if (!previousRuntime.Entities.IsCreated || entities.Length == 0 || previousRuntime.Entities.Length == 0)
            {
                WriteFullEntities(ref data, ref entities);
                return;
            }

            ref var previousEntities = ref previousRuntime.Entities;

            data.Write((byte) 1);

            // -----------------------------------------------------------------
            // -> Removed entities to the buffer
            var removedLengthMarker = data.Write(0);
            var removedLength       = 0;

            // Detect entities that don't exist anymore
            for (var i = 0; i != previousEntities.Length; i++)
            {
                var prev  = previousEntities[i];
                var exist = false;

                for (var j = 0; j != entities.Length; j++)
                {
                    var curr = entities[j];
                    if (prev != curr) continue;

                    exist = true;
                    break;
                }

                if (exist) continue;

                data.Write(ref prev);
                removedLength++;
            }

            data.Write(ref removedLength, removedLengthMarker);

            // -----------------------------------------------------------------
            // -> Added entities to the buffer
            var addedLengthMarker = data.Write(0);
            var addedLength       = 0;

            for (var i = 0; i != entities.Length; i++)
            {
                var curr  = entities[i];
                var exist = false;

                for (var j = 0; j != previousEntities.Length; j++)
                {
                    var prev = previousEntities[j];
                    if (curr != prev) continue;

                    exist = true;
                    break;
                }

                if (exist) continue;

                data.Write(ref curr);
                addedLength++;
            }*/
        }

        public unsafe GenerateResult GenerateSnapshot(SnapshotReceiver    receiver,
                                                      GameTime            gt,
                                                      NativeArray<SnapshotEntityInformation> entities,
                                                      Allocator           allocator,
                                                      ref DataBufferWriter    data,
                                                      ref StSnapshotRuntime      runtime)
        {
            IntPtr previousEntityArrayPtr = default;
            var header = new StSnapshotHeader(gt);

            runtime.Header = header;
            if (!runtime.Entities.IsCreated)
                runtime.Entities = entities;
            else
                previousEntityArrayPtr = new IntPtr(runtime.Entities.GetUnsafePtr());
            
            runtime.UpdateHashMapFromLocalData();

            // Write Game time
            data.Write(ref gt);

            // Write entity data
            if ((receiver.Flags & SnapshotReceiverFlags.FullData) != 0)
            {
                WriteFullEntities(ref data, ref entities);
            }
            else
            {
                WriteIncrementalEntities(ref data, ref entities, ref runtime);
            }

            if (runtime.Entities.IsCreated && new IntPtr(runtime.Entities.GetUnsafePtr()) == previousEntityArrayPtr)
            {
                runtime.Entities.Dispose();
                runtime.Entities = entities;
            }

            foreach (var obj in AppEvent<ISnapshotSubscribe>.GetObjEvents())
                obj.SubscribeSystem();

            var systemsMfc = AppEvent<ISnapshotManageForClient>.GetObjEvents();
            data.Write(systemsMfc.Length);

            // Write system data
            foreach (var obj in systemsMfc)
            {
                //Debug.Log("Writing " + obj.GetSystemPattern().InternalIdent.Name);
                
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

        public unsafe StSnapshotRuntime ApplySnapshotFromData(SnapshotSender sender, ref DataBufferReader data, ref StSnapshotRuntime previousRuntime, PatternBankExchange exchange)
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
                return AppEvent<ISnapshotManageForClient>.GetObjEvents().FirstOrDefault(system => system.GetSystemPattern() == id);
            }

            // --------------------------------------------------------------------------- //
            // Actual code
            // --------------------------------------------------------------------------- //
            var gameTime = data.ReadValue<GameTime>();

            var header  = new StSnapshotHeader(gameTime);
            var runtime = new StSnapshotRuntime(header, previousRuntime, allocator);

            // Read Entity Data
            SnapshotManageEntities.UpdateResult entitiesUpdateResult = default;
            
            var entityDataType = data.ReadValue<byte>();
            switch (entityDataType)
            {
                // Full data
                case 0:
                {
                    ReadFullEntities(out var tempEntities, ref data, ref allocator, exchange);
                    entitiesUpdateResult = SnapshotManageEntities.UpdateFrom(previousRuntime.Entities, tempEntities, allocator);
                    
                    if (runtime.Entities.IsCreated)
                        runtime.Entities.Dispose();
                    runtime.Entities = tempEntities;
                    
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

            SnapshotManageEntities.CreateEntities(entitiesUpdateResult, World, ref runtime);
            SnapshotManageEntities.DestroyEntities(entitiesUpdateResult, World, ref runtime, true);

            foreach (var obj in AppEvent<ISnapshotSubscribe>.GetObjEvents())
                obj.SubscribeSystem();

            // Read System Data
            var systemLength = data.ReadValue<int>();
            for (var i = 0; i != systemLength; i++)
            {
                JobHandle uselessHandle = default;

                var foreignSystemPattern = (int) data.ReadDynInteger();
                var length               = (int) data.ReadDynInteger();

                var system        = GetSystem(exchange.GetOriginId(foreignSystemPattern));

                //Debug.Log($"Reading {system.GetSystemPattern().InternalIdent.Name}");

                system.ReadData(sender, runtime, new DataBufferReader(data, data.CurrReadIndex, data.CurrReadIndex + length), ref uselessHandle);

                data.CurrReadIndex += length;
            }

            return runtime;
        }

        private unsafe void ReadFullEntities(out NativeArray<SnapshotEntityInformation> entities, ref DataBufferReader data, ref Allocator allocator, PatternBankExchange exchange)
        {
            var entityLength = (int) data.ReadDynInteger();
            if (entityLength <= 0) Debug.LogWarning("No entities.");

            entities = new NativeArray<SnapshotEntityInformation>(entityLength, allocator);
            UnsafeUtility.MemCpy(entities.GetUnsafePtr(), data.DataPtr + data.CurrReadIndex, entityLength * sizeof(SnapshotEntityInformation));

            for (var i = 0; i != entityLength; i++)
            {
                var s = entities[i];
                s.ModelId   = exchange.GetOriginId(s.ModelId);
                entities[i] = s;
            }

            data.CurrReadIndex += entityLength * sizeof(SnapshotEntityInformation);
        }
    }
}