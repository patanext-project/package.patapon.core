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
		private EntityQuery m_GameModeQuery;
		private EntityQuery m_ExecutingMapQuery;
		private EntityQuery m_ServerMapQuery;

		private MapManager m_MapManager;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameModeQuery     = GetEntityQuery(typeof(ReplicatedEntity), typeof(MpVersusHeadOn));
			m_ExecutingMapQuery = GetEntityQuery(typeof(ExecutingMapData));
			m_ServerMapQuery    = GetEntityQuery(typeof(ExecutingServerMap));

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

			if (m_GameModeQuery.CalculateEntityCount() <= 0)
				return;

			var gameMode = EntityManager.GetComponentData<MpVersusHeadOn>(m_GameModeQuery.GetSingletonEntity());
			if (gameMode.Team0 == default || gameMode.Team1 == default)
				return;

			for (var i = 0; i != 2; i++)
			{
				var team = i == 0 ? gameMode.Team0 : gameMode.Team1;
				EntityManager.SetOrAddComponentData(team, new TeamDirection {Value = (sbyte) (i == 0 ? 1 : -1)});
			}
		}
	}
}