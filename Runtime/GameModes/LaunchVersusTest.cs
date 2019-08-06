using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.GameModes
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class LaunchVersusTest : ComponentSystem
	{
		private bool m_IsLaunch;
		private EntityQuery m_PlayerQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PlayerQuery = GetEntityQuery(typeof(GamePlayerReadyTag));
		}

		protected override void OnUpdate()
		{
			if (m_IsLaunch)
				return;

			if (m_PlayerQuery.CalculateEntityCount() > 0)
			{
				m_IsLaunch = true;
				var mgr = World.GetOrCreateSystem<GameModeManager>();
				mgr.SetGameMode(new MpVersusHeadOn
				{
					
				}, "VS-HeadOn");
			}
		}
	}
}