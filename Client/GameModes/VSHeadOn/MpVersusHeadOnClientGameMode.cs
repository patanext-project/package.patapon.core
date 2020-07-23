using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.DOTSxUI.Components;
using PataNext.Client.DataScripts.Interface.Popup;
using PataNext.Module.Simulation.Components.Units;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.GameModes.VSHeadOn
{

	[UpdateInGroup(typeof(ClientGameModeSystemGroup))]
	public class MpVersusHeadOnClientGameMode : AbsGameBaseSystem
	{
		private EntityQuery m_ExecutingMapQuery;
		private EntityQuery m_GameModeQuery;

		private MapManager  m_MapManager;
		private EntityQuery m_ServerMapQuery;

		private EntityQuery m_PlayerQuery;

		private float m_ActivityDelay;
		
		private Entity m_PopupEntity;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameModeQuery     = GetEntityQuery(typeof(ReplicatedEntity), typeof(MpVersusHeadOn));
			m_ExecutingMapQuery = GetEntityQuery(typeof(ExecutingMapData));
			m_ServerMapQuery    = GetEntityQuery(typeof(ExecutingServerMap));
			m_PlayerQuery       = GetEntityQuery(typeof(GamePlayer));

			m_MapManager = World.GetOrCreateSystem<MapManager>();
			
			m_PopupEntity = EntityManager.CreateEntity(typeof(UIPopup), typeof(PopupDescription));
			EntityManager.SetComponentData(m_PopupEntity, new UIPopup
			{
				Title   = "Menu",
				Content = "do something instead of looking at this plain boring text..."
			});

			Entity button;
			
						var popupButtonPrefab = EntityManager.CreateEntity(typeof(UIButton), typeof(UIButtonText), typeof(UIGridPosition), typeof(Prefab));
			var continueChoice = EntityManager.Instantiate(popupButtonPrefab);
			var spectatorChoice = EntityManager.Instantiate(popupButtonPrefab);
			var exitChoice      = EntityManager.Instantiate(popupButtonPrefab);

			var i = 0;
			
			button = continueChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value              = "Continue"});
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction {Value = false});
			EntityManager.SetComponentData(button, new UIGridPosition {Value            = new int2(0, i++)});
			EntityManager.AddComponentData(button, new UIFirstSelected());
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);
			
			button = spectatorChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value              = "Spectate"});
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction {Value = false});
			EntityManager.AddComponentData(button, new ButtonSpectate());
			EntityManager.SetComponentData(button, new UIGridPosition {Value            = new int2(0, i++)});
			EntityManager.AddComponentData(button, new UIFirstSelected());
			EntityManager.ReplaceOwnerData(button, m_PopupEntity);

			button = exitChoice;
			EntityManager.SetComponentData(button, new UIButtonText {Value = "Exit"});
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction {Value = false});
			EntityManager.AddComponentData(button, new ButtonGoBackToPreviousMenu {PreviousMenu = typeof(ServerListMenu)});
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

			var gameMode = EntityManager.GetComponentData<MpVersusHeadOn>(m_GameModeQuery.GetSingletonEntity());
			if (gameMode.Team0 == default || gameMode.Team1 == default)
				return;
			
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				EntityManager.SetEnabled(m_PopupEntity, !EntityManager.GetEnabled(m_PopupEntity));
			}

			for (var i = 0; i != 2; i++)
			{
				var team = i == 0 ? gameMode.Team0 : gameMode.Team1;
				EntityManager.SetOrAddComponentData(team, new UnitDirection {Value = (sbyte) (i == 0 ? 1 : -1)});
			}

			if (m_ActivityDelay < UnityEngine.Time.time && HasSingleton<GameModeHudSettings>() && HasSingleton<CurrentServerSingleton>())
			{
				if (BaseDiscordSystem.Instance is P4DiscordSystem discord)
				{
					discord.PushActivity(new Activity
					{
						Type          = ActivityType.Playing,
						ApplicationId = 609427243395055616,
						Name          = "P4TLB",
						Details       = $"HeadOn ({gameMode.GetPoints(0)} - {gameMode.GetPoints(1)})",
						State         = GetSingleton<GameModeHudSettings>().EnableGameModeInterface ? "In Arena" : "Prematch",
						Assets = new ActivityAssets
						{
							LargeImage = GetSingleton<GameModeHudSettings>().EnableGameModeInterface ? "map_thumb_testvs" : "in-menu",
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