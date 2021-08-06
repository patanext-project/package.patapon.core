using System;
using System.Diagnostics;
using GameHost.Core;
using GameHost.Core.RPC.AvailableRpcCommands;
using GameHost.ShareSimuWorldFeature;
using Mono.Options;
using PataNext.Client.Systems;
using StormiumTeam.GameBase.Bootstrapping;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Utilities;
using Debug = UnityEngine.Debug;

namespace PataNext.Client.Bootstraps.Startup
{
	public class PlayerStartupBootstrap : BaseBootstrapSystem
	{
		private GameHostConnector gameHostConnector;

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(PlayerStartupBootstrap)});
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			gameHostConnector = World.GetExistingSystem<GameHostConnector>();
			
			gameHostConnector.Connected += () =>
			{
				Debug.Log("WOOHOO");
				Console.WriteLine("Connected here!");

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
			var span = args.AsSpan();
			for (var index = 0; index < span.Length; index++)
			{
				var arg = span[index];
				if (arg == "-parentHWND")
				{
					span = span.Slice(0, index);
					break;
				}
			}

			Process parentProcess = null;
			var options = new OptionSet
			{
				{
					"g|ghaddr=", str => gameHostConnector.Connect(IPEndPointUtility.Parse(str))
				},
				{
					"p|parent=", str =>
					{
						UnityEngine.Debug.Log("p|parent " + str);
						parentProcess = Process.GetProcessById(int.Parse(str));
					}
				}
			};
			Console.WriteLine("Args: " + string.Join(" ", span.ToArray()));
			var r = options.Parse(span.ToArray());
			Console.WriteLine("Options: " + string.Join(", ", r));
			
			EntityManager.AddComponentData(EntityManager.CreateEntity(), new TestHomeScreenSpawn());

			EntityManager.DestroyEntity(bootstrapSingleton);

			if (parentProcess != null)
			{
				parentProcess.Exited += (sender, eventArgs) =>
				{
					Application.Quit();
				};
			}
		}
	}
}