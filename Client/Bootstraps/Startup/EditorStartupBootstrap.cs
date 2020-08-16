using System;
using System.Net;
using GameHost.Core;
using GameHost.Core.RPC.AvailableRpcCommands;
using GameHost.InputBackendFeature;
using GameHost.ShareSimuWorldFeature;
using PataNext.Client.DataScripts.Interface.Bubble;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Bootstrapping;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			var param = EntityManager.GetComponentData<BootstrapParameters>(bootstrapSingleton)
			                         .Values;

			var connector = World.GetExistingSystem<GameHostConnector>();
			if (step == 0)
			{
				connector.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse(param[0])));
				step++;
			}

			if (step == 1 && connector.Client.ConnectedPeersCount > 0)
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

				var cghInputBackend = World.GetExistingSystem<CreateGameHostInputBackendSystem>();
				cghInputBackend.Create(0);

				connector.BroadcastRequest("displayallcon", default);

				using (var writer = new DataBufferWriter(0, Allocator.Temp))
				{
					writer.WriteStaticString("enet");
					writer.WriteStaticString(new IPEndPoint(IPAddress.Parse("127.0.0.1"), cghInputBackend.Address.Address.Port).ToString());
					connector.BroadcastRequest("addinputsystem", writer);
				}

				var request = EntityManager.CreateEntity(typeof(RequestMapLoad));
				EntityManager.SetComponentData(request, new RequestMapLoad {Key = new FixedString512("arena_of_tolerance")});

				step++;
			}

			if (step == 2)
				EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}
}