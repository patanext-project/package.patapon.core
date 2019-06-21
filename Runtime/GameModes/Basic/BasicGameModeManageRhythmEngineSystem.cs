using package.patapon.core;
using Patapon4TLB.Default;
using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Patapon4TLB.GameModes.Basic
{
	[DisableAutoCreation]
	public class BasicGameModeManageRhythmEngineSystem : GameBaseSystem
	{
		private RhythmEngineProvider m_RhythmEngineProvider;

		private EntityQueryBuilder m_PlayerQueryBuilder;
		private EntityQuery        m_EngineQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_RhythmEngineProvider = World.GetOrCreateSystem<RhythmEngineProvider>();
			m_PlayerQueryBuilder   = Entities.WithAll<GamePlayer, GamePlayerReadyTag, BasicGameModePlayer>();
			m_EngineQuery          = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));
		}

		protected override void OnUpdate()
		{
			var gameMode = World.GetExistingSystem<BasicGameModeSystem>();
			for (var pl = 0; pl != gameMode.NewPlayers.Length; pl++)
			{
				var playerEntity = gameMode.NewPlayers[pl];
				var engineEntity = EntityManager.CreateEntity(m_RhythmEngineProvider.EntityArchetypeWithAuthority);

				EntityManager.SetComponentData(engineEntity, new RhythmEngineSettings {MaxBeats      = 4, BeatInterval  = 500, UseClientSimulation = true});
				EntityManager.SetComponentData(engineEntity, new RhythmCurrentCommand {CustomEndTime = -1, ActiveAtTime = -1, Power                = 0});
				EntityManager.SetComponentData(engineEntity, new RhythmEngineProcess {StartTime      = (int) gameMode.GameModeData.StartTime});
				EntityManager.SetComponentData(engineEntity, new DestroyChainReaction(playerEntity));
				EntityManager.SetComponentData(engineEntity, new Owner {Target = playerEntity});
				EntityManager.SetComponentData(engineEntity, EntityManager.GetComponentData<NetworkOwner>(playerEntity));

				EntityManager.AddComponentData(engineEntity, new Relative<PlayerDescription> {Target = playerEntity});

				var playerData = EntityManager.GetComponentData<BasicGameModePlayer>(playerEntity);
				playerData.RhythmEngine = engineEntity;
				EntityManager.SetComponentData(playerEntity, playerData);
			}
		}
	}
}