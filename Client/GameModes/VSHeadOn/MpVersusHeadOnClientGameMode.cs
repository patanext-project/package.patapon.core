using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using StormiumTeam.GameBase.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	public struct TeamDirection : IComponentData
	{
		public sbyte Value;
	}
	
	[UpdateInGroup(typeof(ClientGameModeSystemGroup))]
	public class MpVersusHeadOnClientGameMode : GameBaseSystem
	{
		/*private EntityQuery m_GameModeQuery;
		private EntityQuery m_ExecutingMapQuery;
		private EntityQuery m_ServerMapQuery;

		private EntityQuery m_InterfaceQuery;
		private EntityQuery m_FlagQuery;

		private UiHeadOnPresentation.Manager m_InterfaceManager;
		private MapManager                   m_MapManager;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameModeQuery     = GetEntityQuery(typeof(ReplicatedEntity), typeof(MpVersusHeadOn));
			m_ExecutingMapQuery = GetEntityQuery(typeof(ExecutingMapData));
			m_ServerMapQuery    = GetEntityQuery(typeof(ExecutingServerMap));

			m_InterfaceManager = World.GetOrCreateSystem<UiHeadOnPresentation.Manager>();
			m_InterfaceQuery   = GetEntityQuery(typeof(UiHeadOnPresentation));
			m_FlagQuery        = GetEntityQuery(typeof(HeadOnFlag), typeof(Relative<TeamDescription>));

			m_MapManager = World.GetOrCreateSystem<MapManager>();
		}

		protected override void OnUpdate()
		{
			// Check if we need to load the map...
			NativeString512 serverMapKey = default, clientMapKey = default;
			if (m_ServerMapQuery.CalculateEntityCount() > 0)
				serverMapKey = m_ServerMapQuery.GetSingleton<ExecutingServerMap>().Key;
			if (m_ExecutingMapQuery.CalculateEntityCount() > 0)
				clientMapKey = m_ExecutingMapQuery.GetSingleton<ExecutingMapData>().Key;

			if (!serverMapKey.Equals(clientMapKey) && !m_MapManager.AnyOperation)
			{
				var request = EntityManager.CreateEntity(typeof(RequestMapLoad));
				EntityManager.SetComponentData(request, new RequestMapLoad {Key = serverMapKey});
			}
			
			if (m_GameModeQuery.CalculateEntityCount() <= 0 || m_FlagQuery.CalculateEntityCount() < 2)
			{
				Debug.Log($"{m_GameModeQuery.CalculateEntityCount()} :: {m_FlagQuery.CalculateEntityCount()}");
				m_InterfaceManager.SetEnabled(false);
				return;
			}

			m_InterfaceManager.SetEnabled(true);

			var gameMode = EntityManager.GetComponentData<MpVersusHeadOn>(m_GameModeQuery.GetSingletonEntity());
			if (gameMode.Team0 == default || gameMode.Team1 == default)
				return;
			
			var serverTick = GetTick(false);

			Entity flag0 = default, flag1 = default;
			using (var entities = m_FlagQuery.ToEntityArray(Allocator.TempJob))
			using (var teamArray = m_FlagQuery.ToComponentDataArray<Relative<TeamDescription>>(Allocator.TempJob))
			{
				for (var ent = 0; ent != entities.Length; ent++)
				{
					if (teamArray[ent].Target == gameMode.Team0) flag0      = entities[ent];
					else if (teamArray[ent].Target == gameMode.Team1) flag1 = entities[ent];
				}
			}

			for (var i = 0; i != 2; i++)
			{
				var team = i == 0 ? gameMode.Team0 : gameMode.Team1;
				EntityManager.SetOrAddComponentData(team, new TeamDirection {Value = (sbyte) (i == 0 ? 1 : -1)});
			}

			var hud = EntityManager.GetComponentObject<UiHeadOnPresentation>(m_InterfaceQuery.GetSingletonEntity());
			if (gameMode.EndTime > 0)
			{
				var endTimeSeconds = gameMode.EndTime / 1000;

				hud.SetTime(endTimeSeconds - (int) serverTick.Seconds);
			}
			else
			{
				hud.SetTime(-1);
			}

			if (gameMode.Team0 != default && gameMode.Team1 != default
			                              && EntityManager.HasComponent(gameMode.Team0, typeof(Relative<ClubDescription>))
			                              && EntityManager.HasComponent(gameMode.Team1, typeof(Relative<ClubDescription>)))
			{
				var club0Info = EntityManager.GetComponentData<ClubInformation>(EntityManager.GetComponentData<Relative<ClubDescription>>(gameMode.Team0).Target);
				var club1Info = EntityManager.GetComponentData<ClubInformation>(EntityManager.GetComponentData<Relative<ClubDescription>>(gameMode.Team1).Target);

				hud.UpdateClubInformation(club0Info, club1Info);
			}


			if (flag0 != default && flag1 != default)
			{
				hud.SetFlagPosition(EntityManager.GetComponentData<Translation>(flag0).Value, EntityManager.GetComponentData<Translation>(flag1).Value);
			}

			for (var i = 0; i != 2; i++)
			{
				hud.SetScore(0, i, gameMode.GetPoints(i));
				hud.SetScore(1, i, gameMode.GetEliminations(i));
			}
		}*/
		protected override void OnUpdate()
		{
			throw new System.NotImplementedException();
		}
	}
}