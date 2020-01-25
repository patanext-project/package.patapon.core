using ENet;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.Training;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Bootstraping;
using Unity.Entities;
using Unity.NetCode;

namespace Bootstraps
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class TrainingBootstrap : BaseBootstrapSystem
	{
		public struct IsActive : IComponentData
		{
		}

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(TrainingBootstrap)});
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			foreach (var world in World.AllWorlds)
			{
				world.EntityManager.CreateEntity(typeof(IsActive));
			}
			EntityManager.DestroyEntity(bootstrapSingleton);
		}

		[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
		public class ClientSystem : ComponentSystem
		{
			protected override void OnCreate()
			{
				RequireSingletonForUpdate<IsActive>();
			}

			protected override void OnStartRunning()
			{
				base.OnStartRunning();
				
				var network = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
				var ep      = new Address();
				ep.SetIP("127.0.0.1");
				ep.Port = 7979;
				network.Connect(ep);
			}

			protected override void OnUpdate()
			{
			}
		}

		[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
		public class ServerSystem : ComponentSystem
		{
			protected override void OnCreate()
			{
				RequireSingletonForUpdate<IsActive>();
			}

			protected override void OnStartRunning()
			{
				base.OnStartRunning();
				
				var network = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
				var ep      = new Address();
				ep.Port = 7979;
				network.Listen(ep);

				World.GetOrCreateSystem<GameModeManager>()
				     .SetGameMode(new SoloTraining());
			}

			protected override void OnUpdate()
			{

			}
		}
	}
}