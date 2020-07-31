using System;
using System.Net;
using GameHost.Core;
using GameHost.Core.RPC.AvailableRpcCommands;
using GameHost.InputBackendFeature;
using GameHost.ShareSimuWorldFeature;
using Mono.Options;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Bootstrapping;
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
			World.GetExistingSystem<GetDisplayedConnectionRpc>().OnReply += map =>
			{
				if (map.TryGetValue("SimulationApplication", out var connectionList))
				{
					foreach (var con in connectionList)
					{
						if (con.Type != "enet")
							continue;

						World.GetExistingSystem<ConnectToGameHostSimulationSystem>()
						     .Connect(IPEndPointUtility.Parse(con.Address));
					}
				}
			};
			
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

				gameHostConnector.BroadcastRequest("displayallcon", default);

				using var writer = new DataBufferWriter(0, Allocator.Temp);
				writer.WriteStaticString("enet");
				writer.WriteStaticString(new IPEndPoint(IPAddress.Parse("127.0.0.1"), cghInputBackend.Address.Address.Port).ToString());
				gameHostConnector.BroadcastRequest("addinputsystem", writer);
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

			EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}
}