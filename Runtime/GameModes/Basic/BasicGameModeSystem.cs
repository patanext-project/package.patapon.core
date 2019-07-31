using System.Collections.Generic;
using DefaultNamespace;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.GameModes.Basic
{
	public struct BasicGameModeData : IComponentData, IComponentFromSnapshot<BasicGameModeSnapshot>
	{
		public uint   StartTime;
		public Entity PlayerTeam;
		public Entity EnemyTeam;

		public void Set(BasicGameModeSnapshot snapshot, NativeHashMap<int, GhostEntity> ghostMap)
		{
			StartTime  = snapshot.StartTime;
			PlayerTeam = snapshot.PlayerTeamGhostId != 0 ? ghostMap[(int) snapshot.PlayerTeamGhostId].entity : default;
			EnemyTeam  = snapshot.EnemyTeamGhostId != 0 ? ghostMap[(int) snapshot.EnemyTeamGhostId].entity : default;
		}
	}

	public struct BasicGameModePlayer : IComponentData
	{
		public Entity RhythmEngine;
		public Entity Unit;
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class BasicGameModeSystem : ComponentSystemGroup
	{
		public BasicGameModeData GameModeData;
		public NativeList<Entity> NewPlayers;
		
		private EntityQuery m_GameModeQuery;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_GameModeQuery = GetEntityQuery(typeof(BasicGameModeData));
			NewPlayers = new NativeList<Entity>(Allocator.Persistent);
		}

		protected override void OnUpdate()
		{
			if (m_GameModeQuery.CalculateEntityCount() < 0)
				return;

			GameModeData = m_GameModeQuery.GetSingleton<BasicGameModeData>();
			NewPlayers.Clear();

			var ecb = new EntityCommandBuffer(Allocator.TempJob);
			Entities.WithNone<BasicGameModePlayer>().WithAll<GamePlayerReadyTag>().ForEach(e =>
			{
				// ReSharper disable AccessToDisposedClosure
				ecb.AddComponent(e, new BasicGameModePlayer());
				NewPlayers.Add(e);
				
				Debug.Log("--------- New Player: " + e);
				// ReSharper restore AccessToDisposedClosure
			});
			ecb.Playback(EntityManager);
			ecb.Dispose();

			base.OnUpdate();
		}

		public override void SortSystemUpdateList()
		{
			m_systemsToUpdate = new List<ComponentSystemBase>
			{
				World.GetOrCreateSystem<BasicGameModeManageRhythmEngineSystem>(),
				World.GetOrCreateSystem<BasicGameModeCreateUnitSystem>()
			};
		}
	}
}