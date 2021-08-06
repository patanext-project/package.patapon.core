using System;
using System.Threading.Tasks;
using GameHost.Core;
using package.stormiumteam.shared.ecs;
using PataNext.Module.Simulation.Components.Network;
using PataNext.Simulation.Client.Rpc;
using PataNext.UnityCore.Rpc;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories.UIPresets
{
	public class UIOverviewPresetModule : UIOverviewModuleBase
	{
		[SerializeField]
		private GameObject fullViewPrefab;
		
		private UIFullViewPreset fullView;

		private bool spawnFrame;
		
		public override bool OnActiveHideUnitModule => true;
		
		public override bool Enter()
		{
			fullView = Instantiate(fullViewPrefab, Data.RootTransform, false).GetComponent<UIFullViewPreset>();
			if (!fullView)
				throw new InvalidOperationException("FullView gameObject has no UIFullViewPreset");

			fullView.ExitAction    = exit;
			fullView.launchRequest = true;
			spawnFrame             = true;
			
			return true;
		}

		public override void ForceExit()
		{
			exit();
		}

		private void exit()
		{
			Destroy(fullView.gameObject);
			
			Data.QuitView();
		}

		[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
		public class RenderSystem : BaseRenderSystem<UIOverviewPresetModule>
		{
			private GameHostConnector connector;

			protected override void OnCreate()
			{
				base.OnCreate();
				
				connector = World.GetExistingSystem<GameHostConnector>();
			}

			private bool exitRequested;

			private int2  movInput;
			private bool2 inputUpdate;

			private bool enterInput;
			private bool enterInputDown;

			protected override void PrepareValues()
			{
				var inputSystem = EventSystem.current.GetComponent<StandaloneInputModule>();
				exitRequested = Input.GetButtonDown(inputSystem.cancelButton);

				var moveInput = new Vector2(Input.GetAxisRaw(inputSystem.horizontalAxis), Input.GetAxisRaw(inputSystem.verticalAxis));

				var nextInput = new int2((int)math.sign(moveInput.x), (int)math.sign(moveInput.y));
				inputUpdate = movInput != nextInput;
				movInput    = nextInput;

				enterInput     = Input.GetButton(inputSystem.submitButton);
				enterInputDown = Input.GetButtonDown(inputSystem.submitButton);
			}

			protected override void Render(UIOverviewPresetModule definition)
			{
				if (definition.spawnFrame)
				{
					definition.spawnFrame = false;
					
					enterInput     = false;
					enterInputDown = false;
					movInput       = default;
				}

				if (exitRequested)
					definition.fullView.ExitAction();

				if (definition.fullView == null)
					return;

				if (definition.fullView.launchRequest)
				{
					definition.fullView.Request       = connector.RpcClient.SendRequest<GetSavePresetsRpc, GetSavePresetsRpc.Response>(default);
					definition.fullView.launchRequest = false;
				}
				else if (definition.fullView.Request is { IsCompleted: true })
				{
					var inventory = definition.fullView.inventory;
					inventory.Clear();
					foreach (var item in definition.fullView.Request.Result.Presets)
					{
						inventory.AddLast(new PresetItemInventory
						{
							Id   = item.Id.Value,
							Name = item.Name
						});
						
						Debug.LogError($"Added Preset {item.Id.Value} : {item.Name}");
					}

					definition.fullView.Request = null;
				}

				if (inputUpdate.y)
					definition.fullView.inventory.MoveCursorDelta(-movInput.y);

				var currentItem = definition.fullView.inventory.Get(new int2(definition.fullView.inventory.AbsoluteCursor.x, 0));
				if (enterInputDown && !string.IsNullOrEmpty(currentItem.Id) && EntityManager.TryGetComponentData(definition.Data.Entity, out MasterServerControlledUnitData controlledUnitData))
				{
					connector.RpcClient.SendNotification(new CopyPresetToUnitRpc
					{
						Preset = new MasterServerUnitPresetId(currentItem.Id),
						Unit   = new MasterServerUnitId(controlledUnitData.UnitGuid.ToString())
					});
				}
			}

			protected override void ClearValues()
			{
				
			}
		}
	}
}