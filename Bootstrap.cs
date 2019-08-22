//using P4.Core.RythmEngine;

using package.stormiumteam.shared.modding;
using StormiumTeam.GameBase;
using Unity.Entities;

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