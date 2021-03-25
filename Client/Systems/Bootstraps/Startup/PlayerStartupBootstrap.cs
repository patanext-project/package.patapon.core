using System;
using System.Net;
using GameHost.Core;
using GameHost.Core.RPC.AvailableRpcCommands;
using GameHost.InputBackendFeature;
using GameHost.ShareSimuWorldFeature;
using Mono.Options;
using PataNext.Client.Systems;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Bootstrapping;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Utilities;

namespace PataNext.Client.Bootstraps.Startup
{
	public class PlayerStartupBootstrap : BaseBootstrapSystem
	{
		private GameHostConnector gameHostConnector;

		protected override void Register(Entity bootstrap)
		{
			gameHostConnector = World.GetExistingSystem<GameHostConnector>();
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(PlayerStartupBootstrap)});
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			gameHostConnector.Connected += () =>
			{
				Console.WriteLine("Connected here!");
				var cghInputBackend = World.GetExistingSystem<CreateGameHostInputBackendSystem>();
				cghInputBackend.Create(0);

				gameHostConnector.RpcClient.SendRequest<GetDisplayedConnectionRpc, GetDisplayedConnectionRpc.Response>(default)
				         .ContinueWith(t =>
				         {
					         var connections = t.Result.Connections;
					         if (!connections.TryGetValue("SimulationApplication", out var connectionList)) 
						         return;
					         
					         foreach (var con in connectionList)
					         {
						         var appName = "client";
						         if (con.Type != "enet" || con.Name != appName)
							         continue;
							         
						         World.GetExistingSystem<ConnectToGameHostSimulationSystem>()
						              .Connect(IPEndPointUtility.Parse(con.Address));
						
						         var request = EntityManager.CreateEntity(typeof(RequestMapLoad));
						         EntityManager.SetComponentData(request, new RequestMapLoad {Key = new FixedString512("arena_of_tolerance")});
					         }
				         });
			};
			
			var args = Environment.GetCommandLineArgs();
			var options = new OptionSet
			{
				{
					"g|ghaddr=", str => gameHostConnector.Connect(IPEndPointUtility.Parse(str))
				},
				{
					"p|parent", str => { }
				}
			};
			Console.WriteLine("Args: " + string.Join(" ", args));
			var r = options.Parse(args);
			Console.WriteLine("Options: " + string.Join(", ", r));
			
			EntityManager.AddComponentData(EntityManager.CreateEntity(), new TestHomeScreenSpawn());

			EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}
}