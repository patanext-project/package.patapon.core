using System;
using System.Security.Cryptography.X509Certificates;
using package.stormiumteam.networking;
using package.stormiumteam.networking.extensions.NetEcs;
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
    public interface IStateData
    {
    }

    public abstract class SnapshotEntityDataStreamer<TState> : JobComponentSystem, ISnapshotSubscribe, ISnapshotManageForClient
        where TState : struct, IStateData, IComponentData
    {
        //[BurstCompile]
        [RequireComponentTag(typeof(GenerateEntitySnapshot))]
        private struct WriteDataJob : IJobProcessComponentDataWithEntity<TState>
        {
            public SnapshotReceiver Receiver;
            public DataBufferWriter Data;

            [ReadOnly]
            public ComponentDataFromEntity<DataChanged<TState>> ChangeFromEntity;

            public void Execute(Entity entity, int chunkIndex, ref TState state)
            {
                var change = new DataChanged<TState> {IsDirty = 1};
                if (ChangeFromEntity.Exists(entity))
                    change = ChangeFromEntity[entity];

                if (SnapshotOutputUtils.ShouldSkip(Receiver, change))
                {
                    Data.WriteDynInteger(0);
                    return;
                }

                Data.WriteDynInteger((ulong) entity.Version);
                Data.WriteDynInteger((ulong) entity.Index);
                Data.Write(ref state);
            }
        }

        private struct ReadDataJob : IJob
        {
            public int EntityLength;
            
            public SnapshotSender                  Sender;
            public StSnapshotRuntime               Runtime;
            public DataBufferReader                Data;
            public EntityCommandBuffer  EntityCommandBuffer;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<TState> StateFromEntity;

            public void Execute()
            {
                for (var index = 0; index != EntityLength; index++)
                {
                    var entityVersion = (int) Data.ReadDynInteger();
                    if (entityVersion == 0)
                    {
                        continue; // skip
                    }

                    var entityIndex = (int) Data.ReadDynInteger();
                    var worldEntity = Runtime.EntityToWorld(new Entity {Index = entityIndex, Version = entityVersion});
                    var state = Data.ReadValue<TState>();
                    
                    if (StateFromEntity.Exists(worldEntity))
                        StateFromEntity[worldEntity] = state;
                    else
                        EntityCommandBuffer.AddComponent(worldEntity, state);
                }
            }
        }

        private PatternResult m_SystemPattern;

        protected ComponentGroup WriteGroup;
        protected ComponentGroup ReadGroup;
        protected ComponentDataFromEntity<DataChanged<TState>> Changed;
        protected ComponentDataFromEntity<TState> States;

        public PatternResult SystemPattern => m_SystemPattern;

        private readonly int m_SizeOfState = UnsafeUtility.SizeOf<TState>();
        private readonly int m_SizeOfEntity = UnsafeUtility.SizeOf<Entity>();
        
        protected override void OnCreateManager()
        {
            World.GetOrCreateManager<AppEventSystem>().SubscribeToAll(this);
            World.CreateManager<DataChangedSystem<TState>>();

            m_SystemPattern = RegisterPattern();

            WriteGroup = GetComponentGroup(typeof(TState), typeof(GenerateEntitySnapshot));
            ReadGroup = GetComponentGroup(typeof(TState));
            Changed = GetComponentDataFromEntity<DataChanged<TState>>();
            States = GetComponentDataFromEntity<TState>();
        }

        protected virtual PatternResult RegisterPattern()
        {
            return World.GetOrCreateManager<NetPatternSystem>()
                        .GetLocalBank()
                        .Register(new PatternIdent($"auto." + GetType().Namespace + "." + GetType().Name));
        }

        public PatternResult GetSystemPattern()
        {
            return m_SystemPattern;
        }
        
        public void SubscribeSystem()
        {
            Changed = GetComponentDataFromEntity<DataChanged<TState>>();
            States  = GetComponentDataFromEntity<TState>();
        }

        public DataBufferWriter WriteData(SnapshotReceiver receiver, StSnapshotRuntime runtime, ref JobHandle jobHandle)
        {
            var length = WriteGroup.CalculateLength();
            var buffer = new DataBufferWriter(Allocator.TempJob, true, length * m_SizeOfState + length * m_SizeOfEntity);

            buffer.WriteDynInteger((ulong) length);
            new WriteDataJob
            {
                Data           = buffer,
                Receiver         = receiver,
                ChangeFromEntity = Changed
            }.Run(this);

            return buffer;
        }

        public void ReadData(SnapshotSender sender, StSnapshotRuntime runtime, DataBufferReader sysData, ref JobHandle jobHandle)
        {
            using (var ecb = new EntityCommandBuffer(Allocator.TempJob))
            {
                var length = (int) sysData.ReadDynInteger();
                new ReadDataJob
                {
                    EntityLength        = length,
                    Sender              = sender,
                    Data                = sysData,
                    Runtime             = runtime,
                    StateFromEntity     = States,
                    EntityCommandBuffer = ecb
                }.Run();

                ecb.Playback(EntityManager);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
}