using System.Net;
using EcsComponents.MasterServer;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer;
using Patapon4TLB.Core.MasterServer.Data;
using Patapon4TLB.Core.MasterServer.P4;
using Patapon4TLB.Core.MasterServer.P4.EntityDescription;
using Patapon4TLB.Default;
using StormiumTeam.GameBase.Bootstraping;
using StormiumTeam.GameBase.External.Discord;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Bootstraps
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class SelectKitMasterServerBootstrap : BaseBootstrapSystem
	{
		public class IsActive : IComponentData
		{
		}

		private EntityQuery m_LocalDiscordUser;

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(SelectKitMasterServerBootstrap)});
			m_LocalDiscordUser = GetEntityQuery(typeof(DiscordLocalUser));

			var masterServer = World.GetOrCreateSystem<MasterServerSystem>();

			// Set the target of our MasterServer here
			masterServer.SetMasterServer(new IPEndPoint(IPAddress.Loopback, 4242));
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			if (m_LocalDiscordUser.CalculateEntityCount() == 0)
				return;

			foreach (var world in World.AllWorlds)
			{
				if (world.GetExistingSystem<SelectKitMasterServerSystem>() != null)
				{
					world.EntityManager.CreateEntity(typeof(IsActive));
				}
			}

			EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class SelectKitMasterServerSystem : ComponentSystem
	{
		private EntityQuery m_AnyConnectionOrPendingQuery;
		private Entity      m_FormationRequest;

		public Entity FormationEntity;

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireSingletonForUpdate<SelectKitMasterServerBootstrap.IsActive>();

			m_AnyConnectionOrPendingQuery = GetEntityQuery(new EntityQueryDesc
			{
				Any = new ComponentType[] {typeof(ConnectedMasterServerClient), typeof(RequestUserLogin)}
			});
		}

		protected override void OnUpdate()
		{
			if (!World.GetExistingSystem<P4ConnectToMasterServerFromDiscord>().IsCurrentlyRequesting && m_AnyConnectionOrPendingQuery.IsEmptyIgnoreFilter)
			{
				World.GetExistingSystem<P4ConnectToMasterServerFromDiscord>().Request();
			}

			if (!HasSingleton<ConnectedMasterServerClient>())
				return;

			var connectedClient = GetSingleton<ConnectedMasterServerClient>();
			if (m_FormationRequest == default)
			{
				Debug.Log("Searching for formation...");
				m_FormationRequest = EntityManager.CreateEntity(typeof(RequestGetUserFormationData));
				EntityManager.SetComponentData(m_FormationRequest, new RequestGetUserFormationData {UserId = connectedClient.UserId});
			}
			else if (EntityManager.TryGetComponentData(m_FormationRequest, out RequestGetUserFormationData.CompletionStatus completionStatus))
			{
				if (completionStatus.error)
				{
					EntityManager.DestroyEntity(m_FormationRequest);
				}
				else if (EntityManager.HasComponent<ResultGetUserFormationData>(m_FormationRequest))
				{
					var result = EntityManager.GetComponentData<ResultGetUserFormationData>(m_FormationRequest);
					Debug.Log("Formation name: " + result.Root.Name);

					EntityManager.DestroyEntity(m_FormationRequest);

					// Create entity formations...
					var formationRoot = EntityManager.CreateEntity(typeof(GameFormationTag), typeof(FormationTeam), typeof(FormationRoot), typeof(FormationChild));
					foreach (var army in result.Armies)
					{
						var armyEntity = EntityManager.CreateEntity(typeof(ArmyFormation), typeof(FormationParent), typeof(FormationChild));
						EntityManager.SetComponentData(armyEntity, new FormationParent {Value = formationRoot});
						foreach (var unit in army.Units)
						{
							var unitEntity = EntityManager.CreateEntity(typeof(UnitFormation), typeof(MasterServerP4UnitMasterServerEntity), typeof(FormationParent));
							EntityManager.SetComponentData(unitEntity, new FormationParent {Value                       = armyEntity});
							EntityManager.SetComponentData(unitEntity, new MasterServerP4UnitMasterServerEntity {UnitId = unit});
							EntityManager.AddComponentData(unitEntity, new RequestGetUnitKit.Automatic());
							EntityManager.AddComponentData(unitEntity, new MasterServerGlobalUnitPush());
						}
					}

					FormationEntity = formationRoot;
				}
			}

			if (FormationEntity != Entity.Null)
			{
				var formationChildren = EntityManager.GetBuffer<FormationChild>(FormationEntity);
				foreach (var army in formationChildren)
				{
					var armyChildren = EntityManager.GetBuffer<FormationChild>(army.Value);
					foreach (var unit in armyChildren)
					{
						if (!EntityManager.TryGetComponentData(unit.Value, out MasterServerP4UnitMasterServerEntity masterServerEntity))
							continue;

						P4OfficialKit selected = P4OfficialKit.NoneOrCustom;
						if (Input.GetKeyDown(KeyCode.Alpha1))
							selected = P4OfficialKit.Taterazay;
						if (Input.GetKeyDown(KeyCode.Alpha2))
							selected = P4OfficialKit.Yarida;
						if (Input.GetKeyDown(KeyCode.Alpha3))
							selected = P4OfficialKit.Yumiyacha;
						if (Input.GetKeyDown(KeyCode.Alpha4))
							selected = P4OfficialKit.Shurika;

						if (selected == P4OfficialKit.NoneOrCustom)
							continue;

						var request = EntityManager.CreateEntity(typeof(RequestSetUnitKit));
						EntityManager.SetComponentData(request, new RequestSetUnitKit
						{
							UnitId = masterServerEntity.UnitId,
							KitId  = selected
						});
					}
				}
			}
		}
	}
}