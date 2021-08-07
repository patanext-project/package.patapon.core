using GameHost.Core;
using GameHost.Core.Native.xUnity;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Behaviors;
using PataNext.Client.Core.DOTSxUI.Components;
using PataNext.Client.DataScripts.Interface.Popup;
using PataNext.Client.Rpc.City;
using PataNext.Module.Simulation.Components.GamePlay.Special;
using PataNext.Module.Simulation.GameModes;
using PataNext.UnityCore.DOTS;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Utility.DOTS;

namespace PataNext.Client.Systems.GameModes.Coop
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	public class MpCoopMissionClientGameMode : AbsGameBaseSystem
	{
		private Entity   popupEntity;
		private DentBank dentBank;

		protected override void OnCreate()
		{
			dentBank = World.GetExistingSystem<DentBank>();
			
			popupEntity = EntityManager.CreateEntity(typeof(UIPopup), typeof(PopupDescription));
			EntityManager.SetComponentData(popupEntity, new UIPopup
			{
				Title   = "Menu",
				Content = "do something instead of looking at this plain boring text..."
			});

			Entity button;

			var popupButtonPrefab = EntityManager.CreateEntity(typeof(UIButton), typeof(UIButtonText), typeof(UIGridPosition), typeof(Prefab));
			var continueChoice    = EntityManager.Instantiate(popupButtonPrefab);
			var exitChoice        = EntityManager.Instantiate(popupButtonPrefab);

			var i = 0;

			button = continueChoice;
			EntityManager.SetComponentData(button, new UIButtonText { Value              = "Continue" });
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction { Value = false });
			EntityManager.SetComponentData(button, new UIGridPosition { Value            = new int2(0, i++) });
			EntityManager.AddComponentData(button, new UIFirstSelected());
			EntityManager.ReplaceOwnerData<PopupDescription>(button, popupEntity);

			button = exitChoice;
			EntityManager.SetComponentData(button, new UIButtonText { Value                     = "Exit" });
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction { Value        = false });
			EntityManager.AddComponentData(button, new UIButtonActionTarget(() =>
			{
				Debug.LogError("quit mission!");
				
				World.GetExistingSystem<GameHostConnector>()
				     .RpcClient
				     .SendNotification(new LeaveMissionAndReturnToCityRpc());
			}));
			EntityManager.AddComponentData(button, new SetEnableStatePopupAction { Value        = false });
			EntityManager.SetComponentData(button, new UIGridPosition { Value                   = new int2(0, i++) });
			EntityManager.AddComponentData(button, new UIFirstSelected());
			EntityManager.ReplaceOwnerData<PopupDescription>(button, popupEntity);

			EntityManager.SetEnabled(popupEntity, false);

			base.OnCreate();
		}

		protected override void OnUpdate()
		{
			if (!HasSingleton<CoopMission>())
				return;

			var singleton = GetSingletonEntity<CoopMission>();
			if (EntityManager.TryGetComponentData(singleton, out ExecutingMissionData executing))
			{
				if (!dentBank.TryGetOutput(executing.Target, out var output))
					dentBank.CallAndStoreLater(executing.Target);
				else
				{
					var data = EntityManager.GetSharedComponentData<MissionDetailsComponent>(output);
					var str  = new FixedString512();
					unsafe
					{
						const int resPathTypePlusProtocolLength = 5;
						fixed (char* ptr = data.Path.FullString)
						{
							str.Append((byte*) ptr + resPathTypePlusProtocolLength, sizeof(char) * (ushort)(data.Path.FullString.Length - resPathTypePlusProtocolLength));
						}
					}

					var switchMap = !HasSingleton<ExecutingMapData>();
					if (!switchMap && !GetSingleton<ExecutingMapData>().Key.Equals(str))
						switchMap = true;
					
					if (switchMap)
						EntityManager.AddComponentData(EntityManager.CreateEntity(), new RequestMapLoad { Key = str });
				}
			}
			
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				EntityManager.SetEnabled(popupEntity, !EntityManager.GetEnabled(popupEntity));
			}
		}
	}
}