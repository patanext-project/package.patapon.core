//using P4.Core.RythmEngine;

using System;
using package.stormiumteam.shared.modding;
using Unity.Burst;

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
}