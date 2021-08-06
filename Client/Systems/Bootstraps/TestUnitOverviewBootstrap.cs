using System;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameHost.Core;
using GameHost.Core.RPC.AvailableRpcCommands;
using GameHost.ShareSimuWorldFeature;
using PataNext.Client.DataScripts.Interface.Menu.__Barracks;
using PataNext.Simulation.Client.Rpc;
using StormiumTeam.GameBase.Bootstrapping;
using Unity.Entities;
using Utilities;

namespace PataNext.Client.Systems.Bootstraps
{
	public class TestUnitOverviewBootstrap : BaseBootstrapSystem
	{
		private int                          step = 0;
		private GameHostConnector            connector;
		private ReceiveSimulationWorldSystem receiveSimulation;

		protected override void OnCreate()
		{
			base.OnCreate();

			connector                    = World.GetExistingSystem<GameHostConnector>();
			receiveSimulation = World.GetExistingSystem<ReceiveSimulationWorldSystem>();
		}

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(TestUnitOverviewBootstrap)});
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			var param = EntityManager.GetComponentData<BootstrapParameters>(bootstrapSingleton)
			                         .Values;
			
			if (step == 0)
			{
				//EntityManager.AddComponentData(EntityManager.CreateEntity(), new TestHomeScreenSpawn());

				connector.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse(param[0])));
				step++;
			}

			if (step == 1 && connector.IsConnected)
			{
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

							         connector.RpcClient.SendRequest<HeadquartersGetUnitsRpc, HeadquartersGetUnitsRpc.Response>(default)
							                  .ContinueWith(async (Task<HeadquartersGetUnitsRpc.Response> rpcT) =>
							                  {
								                  var result = rpcT.Result;
								                  Console.WriteLine(result.Squads[0].Leader);

								                  Entity entity;

								                  await UniTask.SwitchToMainThread();
								                  while (!receiveSimulation.ghToUnityEntityMap.TryGetValue(result.Squads[0].Leader, out entity))
									                  await UniTask.WaitForEndOfFrame();

								                  //EntityManager.AddComponentData(EntityManager.CreateEntity(), new CurrentUnitOverview(entity));
							                  });
						         }
					         }
				         });

				step++;
			}

			if (step == 3)
				EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}
}