using Unity.Entities;
using UnityEngine;

namespace DataScripts.Interface.Menu.ServerRoom
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class SetServerRoomMenu : ComponentSystem
	{
		private ClientMenuSystem m_MenuSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_MenuSystem = World.GetOrCreateSystem<ClientMenuSystem>();
		}

		protected override void OnUpdate()
		{
			if (m_MenuSystem.CurrentMenu == null && HasSingleton<CurrentServerSingleton>()
			                                     && HasSingleton<ExecutingGameMode>())
			{
				var data = EntityManager.GetComponentData<GameModeHudSettings>(GetSingletonEntity<ExecutingGameMode>());
				if (!data.EnablePreMatchInterface)
					return;
				
				m_MenuSystem.SetMenu<ServerRoomMenu>();
			}
			else if (m_MenuSystem.CurrentMenu == typeof(ServerRoomMenu) && HasSingleton<ExecutingGameMode>())
			{
				var data = EntityManager.GetComponentData<GameModeHudSettings>(GetSingletonEntity<ExecutingGameMode>());
				if (data.EnablePreMatchInterface)
					return;
				
				m_MenuSystem.SetDefaultMenu();
				m_MenuSystem.SetBackgroundCanvasColor(Color.clear);
			}
		}
	}
}