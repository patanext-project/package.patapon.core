using System;
using DataScripts.Interface.Menu.UIECS;
using DataScripts.Interface.Popup;
using GameHost.Transports.enet;
using package.patapon.core.Animation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DataScripts.Interface.Menu.TemporaryMenu
{
	public class TempMenu : ComponentSystem, IMenu, IMenuCallbacks
	{
		public void OnMenuSet(TargetAnimation current)
		{
			World.GetExistingSystem<ClientMenuSystem>()
			     .SetBackgroundCanvasColor(Color.black);
			
			EntityManager.SetEnabled(m_PopupEntity, true);
		}

		public void OnMenuUnset(TargetAnimation current)
		{
			EntityManager.SetEnabled(m_PopupEntity, false);
		}

		private Entity m_PopupEntity;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PopupEntity = EntityManager.CreateEntity(typeof(Disabled), typeof(UIPopup), typeof(PopupDescription));
			EntityManager.SetComponentData(m_PopupEntity, new UIPopup
			{
				Title   = "Menu",
				Content = "As the current menu is in progress, you are instead welcomed with this menu.\nAny feedback goes to discord!"
			});

			Entity button;

			var popupButtonPrefab = EntityManager.CreateEntity(typeof(UIButton), typeof(UIButtonText), typeof(UIGridPosition), typeof(Prefab));
			var trainingChoice    = EntityManager.Instantiate(popupButtonPrefab);
			var multiplayerChoice = EntityManager.Instantiate(popupButtonPrefab);
			var exitChoice        = EntityManager.Instantiate(popupButtonPrefab);

			var i = 0;

			button = trainingChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value = "Go to Training Room! (Solo Only)"});
			EntityManager.AddComponentData(button, new GoToTrainingRoom());
			EntityManager.SetComponentData(button, new UIGridPosition {Value = new int2(0, i++)});
			EntityManager.AddComponentData(button, new UIFirstSelected());
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);

			button = multiplayerChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value = "Play online!"});
			EntityManager.AddComponentData(button, new GoToMenu {Target = typeof(ConnectionMenu)});
			EntityManager.SetComponentData(button, new UIGridPosition {Value = new int2(0, i++)});
			EntityManager.AddComponentData(button, new UIFirstSelected());
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);

			button = exitChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value = "Exit the game."});
			EntityManager.AddComponentData(button, new QuitGamePopupAction());
			EntityManager.SetComponentData(button, new UIGridPosition {Value = new int2(0, i++)});
			EntityManager.AddComponentData(button, new UIFirstSelected());
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);
		}

		public struct QuitGamePopupAction : IComponentData
		{
		}

		public class GoToMenu : IComponentData
		{
			public Type Target;
		}
		
		public class GoToTrainingRoom : IComponentData
		{
		}

		[UpdateInGroup(typeof(InteractionButtonSystemGroup))]
		[UpdateBefore(typeof(DisablePopupActionSystem))]
		[AlwaysSynchronizeSystem]
		public class ButtonEvents : ComponentSystem
		{
			protected override void OnUpdate()
			{
				Entities.WithAll<UIButton.ClickedEvent>().ForEach((ref QuitGamePopupAction quitGame) =>
				{
#if UNITY_EDITOR
					UnityEditor.EditorApplication.isPlaying = false;
#else
          Application.Quit();
#endif
				});

				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity ent, GoToMenu goTo) =>
				{
					World.GetExistingSystem<ClientMenuSystem>()
					     .SetMenu(goTo.Target);

					EntityManager.RemoveComponent(ent, typeof(UIButton.ClickedEvent));
				});

				Entities.WithAll<UIButton.ClickedEvent>().ForEach((Entity ent, GoToTrainingRoom goTo) =>
				{
					var menuSystem = World.GetExistingSystem<ClientMenuSystem>();
					menuSystem.SetBackgroundCanvasColor(Color.clear);
					menuSystem.SetDefaultMenu();

					// Start client and server
					foreach (var world in World.AllWorlds)
					{
						var network = world.GetOrCreateSystem<NetworkStreamReceiveSystem>();
						if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
						{
							var ep = new Address();
							ep.SetIP("127.0.0.1");
							ep.Port = 8250;
							network.Connect(ep);
						}
						else if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
						{
							var ep = new Address {Port = 8250};
							network.Listen(ep);
							
							world.GetOrCreateSystem<GameModeManager>()
							     .SetGameMode(new SoloTraining());
						}
					}

					EntityManager.RemoveComponent(ent, typeof(UIButton.ClickedEvent));
				});
			}
		}

		protected override void OnUpdate()
		{
		}
	}
}