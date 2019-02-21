using System;
using System.Collections.Generic;
using System.Linq;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;

namespace Runtime
{
    public interface ISystemProviderExcludeComponents : IAppEvent
    {
        void ExcludeComponentsFor(Type type, List<ComponentType> components);
    }
    
    public abstract class SystemProvider : ComponentSystem, ISnapshotManageForClient
    {
        private EntityModelManager m_ModelManager;
        private ModelIdent m_ModelIdent;
        private PatternResult m_SnapshotPattern;
        
        private ComponentType[] m_EntityComponents;
        private ComponentType[] m_ExcludedComponents;

        private BlockComponentSerialization[] m_BlockedComponents;

        public ComponentType[] EntityComponents => m_EntityComponents;
        public ComponentType[] ExcludedComponents => m_ExcludedComponents;

        public ComponentType[] ComponentsToExcludeFromStreamers { get; private set; }

        protected override void OnCreateManager()
        {
            GetManager();
        }

        protected override void OnUpdate()
        {
        }

        public EntityModelManager GetManager()
        {
            if (m_ModelManager == null)
            {
                m_ModelManager = World.GetOrCreateManager<EntityModelManager>();

                GetComponents(out m_EntityComponents, out m_ExcludedComponents);
                if (EntityComponents == null && ExcludedComponents == null)
                {
                    m_ModelIdent = m_ModelManager.Register
                    (
                        $"EntityProvider.{GetType().Name}", SpawnEntity, DestroyEntity
                    );
                }
                else
                {
                    // todo: I was lazy when making this, this should be remade as it's slow
                    var l = new Dictionary<ComponentType, byte>();
                    if (m_EntityComponents != null)
                        foreach (var c in m_EntityComponents)
                            l[c] = 0;
                    if (m_ExcludedComponents != null)
                        foreach (var c in m_ExcludedComponents)
                            l[c] = 0;
                    var foreignList = new List<ComponentType>();
                    foreach (var obj in AppEvent<ISystemProviderExcludeComponents>.GetObjEvents())
                        obj.ExcludeComponentsFor(GetType(), foreignList);
                    foreach (var c in foreignList)
                        l[c] = 0;

                    ComponentsToExcludeFromStreamers = l.Keys.ToArray();
                    m_BlockedComponents              = new BlockComponentSerialization[ComponentsToExcludeFromStreamers.Length];

                    for (var i = 0; i != m_BlockedComponents.Length; i++)
                    {
                        m_BlockedComponents[i] = new BlockComponentSerialization {TypeIdx = ComponentsToExcludeFromStreamers[i].TypeIndex};
                    }

                    var patternName = $"EntityProvider.Full.{GetType().Name}";
                    m_ModelIdent = m_ModelManager.RegisterFull
                    (
                        patternName + ".Model", ComponentsToExcludeFromStreamers, ProviderSpawnEntity, DestroyEntity, SerializeCollection, DeserializeCollection
                    );

                    m_SnapshotPattern = World.GetOrCreateManager<NetPatternSystem>().GetLocalBank().Register(new PatternIdent(patternName + ".Snapshot"));
                    World.GetOrCreateManager<AppEventSystem>().SubscribeToAll(this);
                }
            }

            return m_ModelManager;
        }

        public ModelIdent GetModelIdent()
        {
            return m_ModelIdent;
        }

        protected abstract Entity SpawnEntity(Entity origin, StSnapshotRuntime snapshotRuntime);
        protected abstract void DestroyEntity(Entity worldEntity);

        public Entity ProviderSpawnEntity(Entity origin, StSnapshotRuntime snapshotRuntime)
        {
            var e = SpawnEntity(origin, snapshotRuntime);

            if (ComponentsToExcludeFromStreamers != null)
            {
                var blockedComponents = EntityManager.AddBuffer<BlockComponentSerialization>(e);
                blockedComponents.CopyFrom(m_BlockedComponents);
            }

            return e;
        }

        public void ProviderDestroyEntity(Entity worldEntity)
        {
            DestroyEntity(worldEntity);
        }
        
        public virtual void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedComponents)
        {
            entityComponents = null;
            excludedComponents = null;
        }
        
        public virtual void SerializeCollection(ref DataBufferWriter data, SnapshotReceiver receiver, StSnapshotRuntime snapshotRuntime)
        {}
        
        public virtual void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, StSnapshotRuntime snapshotRuntime)
        {}

        public virtual Entity SpawnLocalEntityDelayed(EntityCommandBuffer entityCommandBuffer)
        {
            throw new NotImplementedException();
        }

        // Snapshot Implementation
        public PatternResult GetSystemPattern()
        {
            return m_SnapshotPattern;
        }

        public DataBufferWriter WriteData(SnapshotReceiver receiver, StSnapshotRuntime runtime)
        {
            var buffer = new DataBufferWriter(4096, Allocator.TempJob);
            SerializeCollection(ref buffer, receiver, runtime);
            return buffer;
        }

        public void ReadData(SnapshotSender sender, StSnapshotRuntime runtime, DataBufferReader sysData)
        {
            DeserializeCollection(ref sysData, sender, runtime);
        }
    }
}