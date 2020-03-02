using System;
using DataScripts.Interface.Menu;
using DataScripts.Interface.Menu.TemporaryMenu;
using DataScripts.Interface.Menu.UIECS;
using DataScripts.Interface.Popup;
using Discord;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.Training;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer.Data;
using Patapon4TLB.GameModes.Training;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using StormiumTeam.GameBase.External.Discord;
using StormiumTeam.GameBase.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Patapon4TLB.GameModes
{

	[UpdateInGroup(typeof(ClientGameModeSystemGroup))]
	public class TrainingClientGameMode : AbsGameBaseSystem
	{
		private EntityQuery m_ExecutingMapQuery;
		private EntityQuery m_GameModeQuery;

		private MapManager  m_MapManager;
		private EntityQuery m_ServerMapQuery;

		private EntityQuery m_PlayerQuery;

		private Entity m_PopupEntity;

		private float m_ActivityDelay;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameModeQuery     = GetEntityQuery(typeof(ReplicatedEntity), typeof(SoloTraining));
			m_ExecutingMapQuery = GetEntityQuery(typeof(ExecutingMapData));
			m_ServerMapQuery    = GetEntityQuery(typeof(ExecutingServerMap));
			m_PlayerQuery       = GetEntityQuery(typeof(GamePlayer));

			m_MapManager = World.GetOrCreateSystem<MapManager>();
			
			m_PopupEntity = EntityManager.CreateEntity(typeof(UIPopup), typeof(PopupDescription));
			EntityManager.SetComponentData(m_PopupEntity, new UIPopup
			{
				Title = "Menu", 
				Content = "In training room."
			});

			Entity button;

			var popupButtonPrefab = EntityManager.CreateEntity(typeof(UIButton), typeof(UIButtonText), typeof(UIGridPosition), typeof(Prefab));
			var continueChoice = EntityManager.Instantiate(popupButtonPrefab);
			var taterazayChoice = EntityManager.Instantiate(popupButtonPrefab);
			var yaridaChoice    = EntityManager.Instantiate(popupButtonPrefab);
			var yumiyachaChoice    = EntityManager.Instantiate(popupButtonPrefab);
			var exitChoice      = EntityManager.Instantiate(popupButtonPrefab);

			var i = 0;
			
			button = continueChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value              = "Continue"});
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction {Value = false});
			EntityManager.SetComponentData(button, new UIGridPosition {Value            = new int2(0, i++)});
			EntityManager.AddComponentData(button, new UIFirstSelected());
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);
			
			button = taterazayChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value = "Taterazay"});
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction());
			EntityManager.AddComponentData(button, new ButtonChangeKit {Id = 0});
			EntityManager.SetComponentData(button, new UIGridPosition {Value    = new int2(0, i++)});
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);
			
			button = yaridaChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value = "Yarida"});
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction());
			EntityManager.AddComponentData(button, new ButtonChangeKit {Id = 1});
			EntityManager.SetComponentData(button, new UIGridPosition {Value    = new int2(0, i++)});
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);
			
			button = yumiyachaChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value = "Yumiyacha"});
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction());
			EntityManager.AddComponentData(button, new ButtonChangeKit {Id   = 2});
			EntityManager.SetComponentData(button, new UIGridPosition {Value = new int2(0, i++)});
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);
			
			button = exitChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value = "Return to desktop"});
			EntityManager.AddComponentData(button, new TempMenu.QuitGamePopupAction());
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction {Value = false});
			EntityManager.SetComponentData(button, new UIGridPosition {Value = new int2(0, i++)});
			EntityManager.AddComponentData(button, new UIFirstSelected());
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);
			
			EntityManager.SetEnabled(m_PopupEntity, false);
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

			if (m_ServerMapQuery.IsEmptyIgnoreFilter && !m_ExecutingMapQuery.IsEmptyIgnoreFilter)
			{
				EntityManager.CreateEntity(typeof(RequestMapUnload));
			}

			if (m_GameModeQuery.CalculateEntityCount() <= 0)
				return;

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				EntityManager.SetEnabled(m_PopupEntity, true);
			}

			World.GetExistingSystem<ClientMenuSystem>().SetBackgroundCanvasColor(Color.clear);

			if (m_ActivityDelay < UnityEngine.Time.time && HasSingleton<GameModeHudSettings>() && HasSingleton<CurrentServerSingleton>())
			{
				if (BaseDiscordSystem.Instance is P4DiscordSystem discord)
				{
					discord.PushActivity(new Activity
					{
						Type          = ActivityType.Playing,
						ApplicationId = 609427243395055616,
						Name          = "P4TLB",
						Details       = String.Empty,
						State         = "In training room",
						Assets = new ActivityAssets
						{
							LargeImage = "in-menu",
						},
						Party = new ActivityParty
						{
							Id = "party:" + GetSingleton<CurrentServerSingleton>().ServerId,
							Size = new PartySize
							{
								CurrentSize = m_PlayerQuery.CalculateEntityCount(),
								MaxSize     = 8
							}
						},
						Secrets = new ActivitySecrets
						{
							Join = "join:" + GetSingleton<CurrentServerSingleton>().ServerId
						},
						Instance = true
					});
				}

				m_ActivityDelay = UnityEngine.Time.time + 5f;
			}
		}
	}
}