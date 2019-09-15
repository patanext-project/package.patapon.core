//using P4.Core.RythmEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using Karambolo.Common;
using package.stormiumteam.shared.modding;
using Revolution;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEditor.Compilation;
using UnityEngine;

namespace Patapon4TLBCore
{
    public class Bootstrap : CModBootstrap
    {
        protected override void OnRegister()
        {
            /*var entityManager = World.Active.GetOrCreateSystem<EntityManager>();
            var entity = entityManager.CreateEntity(typeof(DRythmBeatData), typeof(DRythmTimeData));
            entityManager.SetComponentData(entity, new DRythmBeatData()
            {
                Interval = 0.5f
            });*/
        }

        protected override void OnUnregister()
        {

        }
    }

    public class SetCollectionSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            World.GetOrCreateSystem<SnapshotManager>().SetFixedSystemsFromBuilder((world, builder) =>
            {
                var i = 1;
                foreach (var type in GetTypes(typeof(ISystemDelegateForSnapshot), typeof(ComponentSystemBase))
                    .OrderBy(t => t.FullName))
                {
                    Debug.Log($"{i}-snapshot:{type}");
                    builder.Add(world.GetOrCreateSystem(type));
                    i++;
                }

                foreach (var type in GetTypes(typeof(IEntityDescription), null)
                    .OrderBy(t => t.FullName))
                {
                    Debug.Log($"{i}-snapshot:desc:{type}");
                    builder.Add(world.GetOrCreateSystem(typeof(ComponentSnapshotSystemTag<>).MakeGenericType(type)));
                    i++;
                }
            });
            World.GetOrCreateSystem<RpcCollectionSystem>().SetFixedCollection((world, builder) =>
            {
                foreach (var type in GetTypes(typeof(IRpcCommand), null)
                    .OrderBy(t => t.FullName))
                {
                    Debug.Log($"rpc:{type}");
                    try
                    {
                        builder.Add((RpcProcessSystemBase) world.GetOrCreateSystem(typeof(DefaultRpcProcessSystem<>).MakeGenericType(type)));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error at making {type}");
                        throw;
                    }
                }
            });
            World.GetOrCreateSystem<CommandCollectionSystem>().SetFixedCollection((world, builder) =>
            {
                foreach (var type in GetTypes(typeof(ICommandData<>), null)
                    .OrderBy(t => t.FullName))
                {
                    Debug.Log($"cmd:{type}");
                    try
                    {
                        builder.Add((CommandProcessSystemBase) world.GetOrCreateSystem(typeof(DefaultCommandProcessSystem<>).MakeGenericType(type)));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error at making {type}");
                        throw;
                    }
                }
            });
        }

        protected override void OnUpdate()
        {

        }

        private static IEnumerable<Type> GetTypes(Type interfaceType, Type subclass)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            return from asm in assemblies
                   from type in asm.GetTypes()
                   where type.HasInterface(interfaceType)
                         && (subclass == null || type.IsSubclassOf(subclass))
                         && !type.IsAbstract
                   select type;
        }
    }

    public class SetVersionSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            GameStatic.Version = 2;
        }

        protected override void OnUpdate()
        {

        }
    }
}