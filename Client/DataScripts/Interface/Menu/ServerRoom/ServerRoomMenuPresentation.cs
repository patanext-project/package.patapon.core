using System;
using System.Collections.Generic;
using System.Linq;
using DataScripts.Interface.Menu.UIECS;
using DataScripts.Interface.Popup;
using DefaultNamespace;
using P4TLB.MasterServer;
using package.patapon.core.Animation;
using package.stormiumteam.shared.ecs;
using Patapon.Client.PoolingSystems;
using Patapon.Mixed.GameModes;
using Patapon4TLB.Core.MasterServer.Data;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace DataScripts.Interface.Menu.ServerRoom
{
	public class ServerRoomMenuPresentation : RuntimeAssetPresentation<ServerRoomMenuPresentation>
	{
		public GameObject waitingFrame;
		public TextMeshProUGUI waitingLabel;
		
		public TextMeshProUGUI instanceLabel;
		public RectTransform buttonBoard;

		public ServerRoomTeamColumn[] teamColumns;
		
		private List<Entity> m_LinkedEntities;

		private void OnEnable()
		{
			m_LinkedEntities = new List<Entity>();
		}

		public override void OnBackendSet()
		{
			base.OnBackendSet();
			var entityMgr = Backend.DstEntityManager;

			var changeKitPopup = entityMgr.CreateEntity(typeof(Disabled), typeof(UIPopup), typeof(PopupDescription));
			entityMgr.SetComponentData(changeKitPopup, new UIPopup {Title = "Kit Selection", Content = "Change your UberHero class.\nThe choice is currently limited!"});

			var popupButtonPrefab = entityMgr.CreateEntity(typeof(UIButton), typeof(UIButtonText), typeof(UIGridPosition), typeof(Prefab));
			var buttonPrefab = entityMgr.CreateEntity(typeof(ServerRoomUIButton), typeof(UIButton), typeof(UIButtonText), typeof(UIGridPosition), typeof(Prefab));
			entityMgr.SetComponentData(buttonPrefab, new UIButtonText {Value = "not init"});

			var backButton      = entityMgr.Instantiate(buttonPrefab);
			var team0Button     = entityMgr.Instantiate(buttonPrefab);
			var team1Button     = entityMgr.Instantiate(buttonPrefab);
			var changeKitButton = entityMgr.Instantiate(buttonPrefab);
			var readyButton     = entityMgr.Instantiate(buttonPrefab);

			Entity button;

			button = backButton;
			entityMgr.SetComponentData(button, new UIButtonText {Value   = "Return"});
			entityMgr.SetComponentData(button, new UIGridPosition {Value = new int2(0, 0)});
			entityMgr.AddComponentData(button, new ButtonGoBackToPreviousMenu {PreviousMenu = Backend.DstEntityManager.World.GetExistingSystem<ServerRoomMenu>().PreviousMenu});

			button = team0Button;
			entityMgr.SetComponentData(button, new UIButtonText {Value          = "Join Team 0"});
			entityMgr.SetComponentData(button, new UIGridPosition {Value        = new int2(0, 1)});
			entityMgr.AddComponentData(button, new ButtonChangeTeam {TeamTarget = 0});

			button = team1Button;
			entityMgr.SetComponentData(button, new UIButtonText {Value          = "Join Team 1"});
			entityMgr.SetComponentData(button, new UIGridPosition {Value        = new int2(0, 2)});
			entityMgr.AddComponentData(button, new ButtonChangeTeam {TeamTarget = 1});

			button = changeKitButton;
			entityMgr.SetComponentData(button, new UIButtonText {Value              = "Change class"});
			entityMgr.SetComponentData(button, new UIGridPosition {Value            = new int2(0, 3)});
			entityMgr.AddComponentData(button, new SetEnableStatePopupAction {Popup = changeKitPopup, Value = true});

			button = readyButton;
			entityMgr.SetComponentData(button, new UIButtonText {Value   = "Ready"});
			entityMgr.SetComponentData(button, new UIGridPosition {Value = new int2(0, 4)});
			entityMgr.AddComponentData(button, new UIFirstSelected());
			entityMgr.AddComponentData(button, new ButtonChangeReadyState());
			
			// add popup buttons...
			var taterazayChoice = entityMgr.Instantiate(popupButtonPrefab);
			var yaridaChoice = entityMgr.Instantiate(popupButtonPrefab);
			var exitChoice = entityMgr.Instantiate(popupButtonPrefab);

			button = taterazayChoice;
			entityMgr.SetComponentData(button, new UIButtonText {Value = "Taterazay"});
			entityMgr.AddComponentData(button, new SetEnableStatePopupAction());
			entityMgr.AddComponentData(button, new ButtonChangeKit { Target = P4OfficialKit.Taterazay });
			entityMgr.SetComponentData(button, new UIGridPosition {Value = new int2(0, 0)});
			entityMgr.ReplaceOwnerData(button, changeKitPopup);
			
			button = yaridaChoice;
			entityMgr.SetComponentData(button, new UIButtonText {Value = "Yarida"});
			entityMgr.AddComponentData(button, new SetEnableStatePopupAction());
			entityMgr.AddComponentData(button, new ButtonChangeKit { Target = P4OfficialKit.Yarida });
			entityMgr.SetComponentData(button, new UIGridPosition {Value = new int2(0, 1)});
			entityMgr.ReplaceOwnerData(button, changeKitPopup);

			button = exitChoice;
			entityMgr.SetComponentData(button, new UIButtonText {Value = "Exit"});
			entityMgr.AddComponentData(button, new SetEnableStatePopupAction());
			entityMgr.SetComponentData(button, new UIGridPosition {Value = new int2(0, 2)});
			entityMgr.AddComponentData(button, new UIFirstSelected());
			entityMgr.ReplaceOwnerData(button, changeKitPopup);

			m_LinkedEntities.Add(changeKitPopup);
			m_LinkedEntities.Add(buttonPrefab);
			m_LinkedEntities.Add(backButton);
			m_LinkedEntities.Add(team0Button);
			m_LinkedEntities.Add(team1Button);
			m_LinkedEntities.Add(changeKitButton);
			m_LinkedEntities.Add(readyButton);
			m_LinkedEntities.Add(taterazayChoice);
			m_LinkedEntities.Add(yaridaChoice);
			m_LinkedEntities.Add(exitChoice);
		}

		private void OnDisable()
		{
			foreach (var entity in m_LinkedEntities.Where(entity => Backend.DstEntityManager.Exists(entity)))
			{
				Backend.DstEntityManager.DestroyEntity(entity);
			}

			m_LinkedEntities.Clear();
			m_LinkedEntities = null;
		}
	}

	public class ServerRoomMenuBackend : RuntimeAssetBackend<ServerRoomMenuPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;

		public string LastServerName { get; set; }
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ServerRoomMenu : ComponentSystem, IMenu, IMenuCallbacks
	{
		public Type PreviousMenu;
		
		public struct IsActive : IComponentData
		{
		}

		protected override void OnUpdate()
		{
		}

		public void OnMenuSet(TargetAnimation current)
		{
			if (current.PreviousType != null && current.PreviousType != typeof(ServerRoomMenu))
				PreviousMenu = current.PreviousType;
			EntityManager.CreateEntity(typeof(IsActive));
		}

		public void OnMenuUnset(TargetAnimation current)
		{
			EntityManager.DestroyEntity(GetSingletonEntity<IsActive>());
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ServerRoomMenuPoolingSystem : PoolingSystem<ServerRoomMenuBackend, ServerRoomMenuPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("Menu")
			              .Folder("ServerRoom")
			              .GetFile("ServerRoom.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(ServerRoomMenu.IsActive));
		}

		private Canvas m_Canvas;

		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
			{
				m_Canvas = CanvasUtility.Create(World, 0, "ServerRoom");
				CanvasUtility.DisableInteractionOnActivePopup(World, m_Canvas);
			}

			base.SpawnBackend(target);
			var rt = LastBackend.GetComponent<RectTransform>();
			rt.SetParent(m_Canvas.transform, false);

			CanvasUtility.ExtendRectTransform(rt);
		}
	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class ServerRoomMenuRenderSystem : BaseRenderSystem<ServerRoomMenuPresentation>
	{
		protected override void PrepareValues()
		{
			
		}

		protected override void Render(ServerRoomMenuPresentation definition)
		{
			var backend = (ServerRoomMenuBackend) definition.Backend;
			
			if (HasSingleton<CurrentServerSingleton>()
			&& EntityManager.TryGetComponent(GetSingletonEntity<CurrentServerSingleton>(), out ResultServerInformation server))
			{
				if (backend.LastServerName != server.Information.Name)
				{
					definition.instanceLabel.SetText($"{server.Information.Name} <color=#969696>{server.Information.ServerUserLogin}#{server.Information.ServerUserId}");
				}

				if (!HasSingleton<GamePlayerLocalTag>())
					definition.waitingLabel.SetText("Waiting GameServer response...");
				else
					definition.waitingFrame.SetActive(false);
			}
			else
			{
				definition.waitingFrame.SetActive(true);
				definition.waitingLabel.SetText("Waiting MasterServer response...");
			}
		}

		protected override void ClearValues()
		{
		}
	}
}