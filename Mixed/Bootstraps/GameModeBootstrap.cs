using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Bootstraping;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Bootstraps
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class GameModeBootstrap : BaseBootstrapSystem
	{
		public struct IsActive : IComponentData
		{
		}

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(GameModeBootstrap)});
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			foreach (var world in World.AllWorlds)
			{
				var network = world.GetExistingSystem<NetworkStreamReceiveSystem>();
				if (world.GetExistingSystem<SimpleRhythmTestSystem>() != null)
				{
					world.EntityManager.CreateEntity(typeof(IsActive));
				}

				if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
				{
					// Client worlds automatically connect to localhost
					var ep = NetworkEndPoint.LoopbackIpv4;
					ep.Port = 7979;
					network.Connect(ep);
				}
				else if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
				{
					// Server world automatically listen for connections from any host
					var ep = NetworkEndPoint.AnyIpv4;
					ep.Port = 7979;
					network.Listen(ep);
				}
			}

			EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}

	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class GameModeBootstrapTestSystem : ComponentSystem
	{
		protected override void OnCreate()
		{
			RequireSingletonForUpdate<GameModeBootstrap.IsActive>();
		}

		protected override void OnStartRunning()
		{
			var gamemodeMgr = World.GetOrCreateSystem<GameModeManager>();
			gamemodeMgr.SetGameMode(new MpVersusHeadOn {});
		}

		protected override void OnUpdate()
		{

		}
	}
}