using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Bootstraping;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Bootstraps
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class SimpleRhythmBootstrap : BaseBootstrapSystem
	{
		public struct IsActive : IComponentData
		{
		}

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(SimpleRhythmBootstrap)});
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
	public class SimpleRhythmTestSystem : GameBaseSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			RequireSingletonForUpdate<SimpleRhythmBootstrap.IsActive>();
		}

		protected override void OnUpdate()
		{
			if (IsServer)
			{
				Entities.ForEach((ref PlayerConnectedEvent connected) =>
				{
					// Create RhythmEngine
					var provider = World.GetOrCreateSystem<RhythmEngineProvider>();
					var rhythmEnt = provider.SpawnLocalEntityWithArguments(new RhythmEngineProvider.Create
					{
						UseClientSimulation = true
					});

					EntityManager.AddComponent(rhythmEnt, typeof(GhostEntity));

					var process = EntityManager.GetComponentData<FlowEngineProcess>(rhythmEnt);
					process.StartTime = GetTick(true).Ms;
					EntityManager.SetComponentData(rhythmEnt, process);
				});
			}
			else
			{

			}
		}
	}
}