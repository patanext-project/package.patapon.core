using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.Core;
using Patapon4TLB.Default;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Bootstraping;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Bootstraps
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class GameModeBootstrap : BaseBootstrapSystem
	{
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
					var ent = world.EntityManager.CreateEntity(typeof(IsActive));
					world.EntityManager.SetComponentData(ent, new IsActive {RequiredPlayers = 1});
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

		public struct IsActive : IComponentData
		{
			public int RequiredPlayers;
		}
	}

	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class GameModeBootstrapTestSystem : GameBaseSystem
	{
		private bool        m_Created;
		private EntityQuery m_PlayerQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PlayerQuery = GetEntityQuery(typeof(GamePlayer), typeof(GamePlayerReadyTag));

			RequireSingletonForUpdate<GameModeBootstrap.IsActive>();
		}

		private void SetAsYarida(ref UnitStatistics statistics, DynamicBuffer<UnitDefinedAbilities> definedAbilities)
		{
			statistics.Health = 160;
			statistics.Attack = 30;
			statistics.Defense = 0;
			statistics.Weight = 6;
			
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("yarida/basic_attack"), 0));
		}

		private void SetAsTaterazay(ref UnitStatistics statistics, DynamicBuffer<UnitDefinedAbilities> definedAbilities)
		{
			statistics.Health  = 225;
			statistics.Attack  = 24;
			statistics.Defense = 7;
			statistics.Weight  = 8.5f;

			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_attack"), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_defense"), 0));
		}

		enum ClassType
		{
			Taterazay,
			Yarida
		}
		
		protected override void OnUpdate()
		{
			if (!IsServer || m_Created)
				return;

			if (m_PlayerQuery.CalculateEntityCount() != GetSingleton<GameModeBootstrap.IsActive>().RequiredPlayers)
				return;

			m_Created = true;

			var playerEntities = m_PlayerQuery.ToEntityArray(Allocator.TempJob);
			var count = playerEntities.Length;
			count = 1;

			// Create formation
			const int formationCount = 2;
			for (var _ = 0; _ != formationCount; _++)
			{
				var formationRoot = EntityManager.CreateEntity(typeof(GameFormationTag), typeof(FormationTeam), typeof(FormationRoot));
				{
					for (var i = 0; i != count; i++)
					{
						var armyEntity = EntityManager.CreateEntity(typeof(ArmyFormation), typeof(FormationParent), typeof(FormationChild));
						EntityManager.SetComponentData(armyEntity, new FormationParent {Value = formationRoot});

						var unitEntity = EntityManager.CreateEntity(typeof(UnitFormation), typeof(UnitStatistics), typeof(UnitDefinedAbilities), typeof(FormationParent));
						EntityManager.SetComponentData(unitEntity, new FormationParent {Value = armyEntity});

						var definedAbilities = EntityManager.GetBuffer<UnitDefinedAbilities>(unitEntity);
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_march"), 0));
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_backward"), 0));
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_retreat"), 0));
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_jump"), 0));
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_party"), 0));

						var statistics = new UnitStatistics
						{
							Health  = 1000,
							Attack  = 24,
							Defense = 7,

							BaseWalkSpeed       = 2f,
							FeverWalkSpeed      = 2.2f,
							AttackSpeed         = 2.0f,
							MovementAttackSpeed = 2.22f,
							Weight              = 8.5f,
							AttackSeekRange     = 20f
						};

						if (playerEntities.Length > i && playerEntities[i] != Entity.Null)
						{
							SetAsTaterazay(ref statistics, definedAbilities);
							EntityManager.ReplaceOwnerData(unitEntity, playerEntities[i]);
						}
						else
						{
							SetAsTaterazay(ref statistics, definedAbilities);

							// create a fake player
							var playerArchetype = World.GetExistingSystem<GamePlayerProvider>().EntityArchetype;
							var playerEntity    = EntityManager.CreateEntity(playerArchetype);

							EntityManager.AddComponent(playerEntity, typeof(GamePlayerReadyTag));
							EntityManager.AddComponent(playerEntity, typeof(GhostEntity));
							EntityManager.ReplaceOwnerData(unitEntity, playerEntity);
						}

						EntityManager.SetComponentData(unitEntity, statistics);

						if (playerEntities.Length > i) playerEntities[i] = Entity.Null;
					}
				}

				EntityManager.SetComponentData(formationRoot, new FormationTeam {TeamIndex = _ + 1});
			}

			playerEntities.Dispose();

			// START THE GAMEMODE
			var gamemodeMgr = World.GetOrCreateSystem<GameModeManager>();
			gamemodeMgr.SetGameMode(new MpVersusHeadOn());
		}
	}
}