using Patapon4TLB.GameModes.Basic;
using StormiumTeam.GameBase;
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
			var team           = EntityManager.CreateEntity(typeof(TeamDescription), typeof(GhostComponent));

			EntityManager.SetComponentData(gameModeEntity, new BasicGameModeData {PlayerTeam = team});
		}

		protected override void OnUpdate()
		{

		}
	}
}