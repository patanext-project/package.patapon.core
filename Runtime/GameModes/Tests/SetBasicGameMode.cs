using P4.Core;
using Patapon4TLB.GameModes.Basic;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.GameModes.Tests
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class SetBasicGameMode : ComponentSystem
	{
		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			var gameModeEntity = EntityManager.CreateEntity(typeof(BasicGameModeData));
			var team           = EntityManager.CreateEntity(typeof(TeamDescription), typeof(TeamAllies), typeof(TeamEnemies), typeof(TeamBlockMovableArea), typeof(GhostComponent));
			var enemyTeam      = EntityManager.CreateEntity(typeof(TeamDescription), typeof(TeamAllies), typeof(TeamEnemies), typeof(TeamBlockMovableArea), typeof(GhostComponent));

			// set enemies of 'team'
			{
				EntityManager.GetBuffer<TeamEnemies>(team).Add(new TeamEnemies{Target = enemyTeam});
			}
			
			// set enemies of 'enemyTeam'
			{
				EntityManager.GetBuffer<TeamEnemies>(enemyTeam).Add(new TeamEnemies{Target = team});
			}


			EntityManager.SetComponentData(gameModeEntity, new BasicGameModeData {PlayerTeam = team, EnemyTeam = enemyTeam});
		}

		protected override void OnUpdate()
		{

		}
	}
}