using package.stormiumteam.networking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.Networking
{
    public static class SnapshotManageEntities
    {
        public struct UpdateResult
        {
            public NativeArray<SnapshotEntityInformation> ToCreate;
            public NativeArray<SnapshotEntityInformation> ToDestroy;
        }

        public static UpdateResult UpdateFrom(NativeArray<SnapshotEntityInformation> previousArray, NativeArray<SnapshotEntityInformation> nextArray, Allocator allocator)
        {
            if (!previousArray.IsCreated)
            {
                return new UpdateResult
                {
                    ToCreate = new NativeArray<SnapshotEntityInformation>(nextArray, allocator)
                };
            }

            var result   = new UpdateResult();
            var tempList = new NativeList<SnapshotEntityInformation>(nextArray.Length, allocator);
            foreach (var next in nextArray)
            {
                var ct = false;
                foreach (var previous in previousArray)
                {
                    if (previous.Source == next.Source)
                    {
                        ct = true;
                        break;
                    }
                }

                if (ct)
                    continue;

                tempList.Add(next);
            }

            result.ToCreate = tempList.ToArray(allocator);
            tempList.Clear();

            foreach (var previous in previousArray)
            {
                var ct = false;
                foreach (var next in nextArray)
                {
                    if (previous.Source == next.Source)
                    {
                        ct = true;
                        break;
                    }
                }

                if (ct)
                    continue;

                tempList.Add(previous);
            }

            result.ToDestroy = tempList.ToArray(allocator);
            tempList.Dispose();

            return result;
        }

        public static void CreateEntities(UpdateResult result, World world, ref StSnapshotRuntime snapshotRuntime)
        {
            var modelMgr  = world.GetExistingManager<EntityModelManager>();

            foreach (var e in result.ToCreate)
            {
                var worldEntity = modelMgr.SpawnEntity(e.ModelId, e.Source, snapshotRuntime);

                PrivateSet(snapshotRuntime.SnapshotToWorld, e.Source, worldEntity);
                PrivateSet(snapshotRuntime.WorldToSnapshot, worldEntity, e.Source);
                
                Debug.Log("Created " + worldEntity);
            }
        }

        public static void DestroyEntities(UpdateResult entitiesUpdateResult, World world, ref StSnapshotRuntime snapshotRuntime, bool removeLinks = true)
        {
            var entityMgr = world.GetExistingManager<EntityManager>();
            var modelMgr = world.GetExistingManager<EntityModelManager>();

            foreach (var e in entitiesUpdateResult.ToDestroy)
            {
                var worldEntity = snapshotRuntime.EntityToWorld(e.Source);
                if (worldEntity == default || !entityMgr.Exists(worldEntity))
                {
                    Debug.LogError($"Inconsistency when removing entity (W: {worldEntity}, S: {e.Source})");
                    if (removeLinks)
                    {
                        snapshotRuntime.SnapshotToWorld.Remove(e.Source);
                        snapshotRuntime.SnapshotToWorld.Remove(worldEntity);
                    }
                    continue;
                }
                
                if (removeLinks)
                {
                    snapshotRuntime.SnapshotToWorld.Remove(e.Source);
                    snapshotRuntime.SnapshotToWorld.Remove(worldEntity);
                }
                
                Debug.Log("Destroyed " + worldEntity);
                
                modelMgr.DestroyEntity(worldEntity, e.ModelId);
            }
        }

        private static void PrivateSet(NativeHashMap<Entity, Entity> hashmap, Entity first, Entity second)
        {
            if (hashmap.TryGetValue(first, out _))
                hashmap.Remove(first);

            hashmap.TryAdd(first, second);
        }
    }
}