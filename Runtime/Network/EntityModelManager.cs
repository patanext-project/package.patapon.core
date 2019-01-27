using System.Collections.Generic;
using package.stormiumteam.networking;
using Unity.Entities;
using UnityEngine;

namespace StormiumShared.Core.Networking
{
    public interface IModelSpawnEntityCallback
    {
        Entity SpawnEntity(Entity origin, StSnapshotRuntime snapshotRuntime);
    }

    public interface IModelDestroyEntityCallback
    {
        void DestroyEntity(Entity worldEntity);
    }

    public class EntityModelManager : ComponentSystem
    {
        private PatternBank m_PatternBank;
        private readonly Dictionary<int, IModelSpawnEntityCallback> m_SpawnCallbacks = new Dictionary<int, IModelSpawnEntityCallback>();
        private readonly Dictionary<int, IModelDestroyEntityCallback> m_DestroyCallbacks = new Dictionary<int, IModelDestroyEntityCallback>();

        protected override void OnStartRunning()
        {
            m_PatternBank = World.GetExistingManager<NetPatternSystem>().GetLocalBank();
            
            if (m_PatternBank == null)
                Debug.LogError("The local bank is invalid.");
        }

        protected override void OnUpdate()
        {
            
        }

        public ModelIdent Register<TSpawnCall, TDestroyCall>(string name, TSpawnCall spawnCall, TDestroyCall destroyCall)
            where TSpawnCall : class, IModelSpawnEntityCallback
            where TDestroyCall : class, IModelDestroyEntityCallback
        {
            // If someone register and we haven't even started running, we need to do it manually
            if (m_PatternBank == null)
            {
                OnStartRunning();
            }
                
            var pattern = m_PatternBank.Register(new PatternIdent(name));

            m_SpawnCallbacks[pattern.Id] = spawnCall;
            m_DestroyCallbacks[pattern.Id] = destroyCall;

            return new ModelIdent(pattern.Id);
        }

        public Entity SpawnEntity(int modelId, Entity origin, StSnapshotRuntime snapshotRuntime)
        {
            var entity = m_SpawnCallbacks[modelId].SpawnEntity(origin, snapshotRuntime);

            EntityManager.SetComponentData(entity, new ModelIdent(modelId));
            
            return entity;
        }

        public void DestroyEntity(Entity worldEntity, int modelId)
        {
            var callbackObj = m_DestroyCallbacks[modelId];
            if (callbackObj == null)
            {
                EntityManager.DestroyEntity(worldEntity);
                return;
            }

            callbackObj.DestroyEntity(worldEntity);
        }
    }
}