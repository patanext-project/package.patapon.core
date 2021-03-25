using System.Net;
using GameHost.Core;
using GameHost.Core.RPC.AvailableRpcCommands;
using GameHost.InputBackendFeature;
using GameHost.ShareSimuWorldFeature;
using PataNext.Client.Systems;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Bootstrapping;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Utilities;

namespace PataNext.Client.Bootstraps.Startup
{
	public class EditorStartupBootstrap : BaseBootstrapSystem
	{
		private int step = 0;

		protected override void Register(Entity bootstrap)
		{
			Debug.LogError("Register Bootstrap " + World.Name);
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(EditorStartupBootstrap)});

			//EntityManager.CreateEntity(typeof(CurrentUnitOverview));
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			var param = EntityManager.GetComponentData<BootstrapParameters>(bootstrapSingleton)
			                         .Values;

			var connector = World.GetExistingSystem<GameHostConnector>();
			if (step == 0)
			{
				EntityManager.AddComponentData(EntityManager.CreateEntity(), new TestHomeScreenSpawn());
				
				connector.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse(param[0])));
				step++;
			}

			if (step == 1 && connector.IsConnected)
			{
				var cghInputBackend = World.GetExistingSystem<CreateGameHostInputBackendSystem>();
				cghInputBackend.Create(0);

				connector.RpcClient.SendRequest<GetDisplayedConnectionRpc, GetDisplayedConnectionRpc.Response>(default)
				         .ContinueWith(t =>
				         {
					         var connections = t.Result.Connections;
					         if (connections.TryGetValue("SimulationApplication", out var connectionList))
					         {
						         foreach (var con in connectionList)
						         {
							         var appName = "client";
							         if (param.Length == 2)
								         appName = param[1];
							
							         if (con.Type != "enet" || con.Name != appName)
								         continue;

							         World.GetExistingSystem<ConnectToGameHostSimulationSystem>()
							              .Connect(IPEndPointUtility.Parse(con.Address));
						         }
					         }
				         });

				var request = EntityManager.CreateEntity(typeof(RequestMapLoad));
				EntityManager.SetComponentData(request, new RequestMapLoad {Key = new FixedString512("arena_of_tolerance")});

				step++;
			}

			if (step == 2)
				EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}
}