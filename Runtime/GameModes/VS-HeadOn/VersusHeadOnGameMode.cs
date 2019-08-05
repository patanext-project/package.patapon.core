using System;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	public struct VersusHeadOn : IGameMode
	{
		public enum State
		{
			InitMap,
			RoundStart,
			Playing,
			RoundEnd
		}

		public State PlayState;
	}

	public class VersusHeadOnGameMode : GameModeSystem<VersusHeadOn>
	{
		public struct Team
		{
			public int    Score;
			public Entity Target;
		}

		public Team[] Teams;

		private EntityQuery m_MapQuery;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			//m_MapQuery = GetEntityQuery(typeof(ExecutingMap));
		}

		public override void OnGameModeUpdate(Entity entity, ref VersusHeadOn gameMode)
		{
			// ----------------------------- ----------------------------- //
			// > INIT PHASE
			// ----------------------------- ----------------------------- //
			if (IsInitialization())
			{
				FinishInitialization();
				
				// ----------------------------- //
				// Create teams
				Teams = new Team[2];
				
				var teamProvider = World.GetOrCreateSystem<GameModeTeamProvider>();
				var teamEntities = new NativeList<Entity>(2, Allocator.TempJob);
				for (var t = 0; t != Teams.Length; t++)
				{
					ref var team = ref Teams[t];
					team.Score = 0;
					teamProvider.SpawnLocalEntityWithArguments(new GameModeTeamProvider.Create
					{

					}, teamEntities);
					team.Target = teamEntities[t];
				}

				teamEntities.Dispose();

				// ----------------------------- //
				// Load map
				//EntityManager.CreateEntity(typeof(RequestMapLoad));
				
				// ----------------------------- //
				// Set PlayState
				gameMode.PlayState = VersusHeadOn.State.InitMap;
			}

			// ----------------------------- ----------------------------- //
			// > CLEANUP PHASE
			// ----------------------------- ----------------------------- //
			if (IsCleanUp())
			{
				FinishCleanUp();
				return;
			}
			
			// ----------------------------- ----------------------------- //
			// > LOOP PHASE
			// ----------------------------- ----------------------------- //
			switch (gameMode.PlayState)
			{
				case VersusHeadOn.State.InitMap:
					if (m_MapQuery.CalculateEntityCount() <= 0)
						return;

					break;
				case VersusHeadOn.State.RoundStart:
					break;
				case VersusHeadOn.State.Playing:
					break;
				case VersusHeadOn.State.RoundEnd:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}