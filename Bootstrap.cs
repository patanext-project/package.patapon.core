//using P4.Core.RythmEngine;

using System;
using package.stormiumteam.shared.modding;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace P4.Core
{
    [BurstCompile]
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
            BurstCompiler.CompileFunctionPointer<Action>(Test);
        }

        [BurstCompile]
        public static void Test()
        {
            var i = 0;
            i++;
        }
    }

    public class SetVersionSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            GameStatic.Version = 1;
        }

        protected override void OnUpdate()
        {

        }
    }
}