using System;
using System.Collections.Generic;
using GameHost;
using GameHost.Core;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Components.Archetypes;
using PataNext.Client.DataScripts.Interface.Inventory;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Rpc;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.Units;
using PataNext.UnityCore.Rpc;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories
{
	public class UIOverviewEquipmentModule : UIOverviewModuleBase
	{
		[SerializeField]
		private GameObject fullViewPrefab;

		private UIFullViewEquipment fullView;
		private bool                spawnFrame;

		public override bool OnActiveHideUnitModule => true;

		public override bool Enter()
		{
			fullView = Instantiate(fullViewPrefab, Data.RootTransform, false).GetComponent<UIFullViewEquipment>();
			if (!fullView)
				throw new InvalidOperationException("FullView gameObject has no UIFullViewEquipment");

			fullView.ExitAction = exit;
			spawnFrame          = true;
			
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
		public class RenderSystem : BaseRenderSystem<UIOverviewEquipmentModule>
		{
			private ItemManager itemManager;
			private ItemBank    dentBank;
			
			private GameResourceManager gameResourceMgr;

			private GameHostConnector connector;

			protected override void OnCreate()
			{
				base.OnCreate();

				itemManager     = World.GetExistingSystem<ItemManager>();
				dentBank        = World.GetExistingSystem<ItemBank>();
				gameResourceMgr = World.GetExistingSystem<GameResourceManager>();

				connector = World.GetExistingSystem<GameHostConnector>();
			}

			private bool  exitRequested;
			
			private int2  movInput;
			private bool2 inputUpdate;

			private bool enterInput;
			private bool enterInputDown;

			protected override void PrepareValues()
			{
				var inputSystem = EventSystem.current.GetComponent<StandaloneInputModule>();
				exitRequested = Input.GetButtonDown(inputSystem.cancelButton);
				
				var moveInput = new Vector2(Input.GetAxisRaw(inputSystem.horizontalAxis), Input.GetAxisRaw(inputSystem.verticalAxis));

				var nextInput = new int2((int) math.sign(moveInput.x), (int) math.sign(moveInput.y));
				inputUpdate = movInput != nextInput;
				movInput    = nextInput;

				enterInput     = Input.GetButton(inputSystem.submitButton);
				enterInputDown = Input.GetButtonDown(inputSystem.submitButton);
			}

			private string currentItemPrefix;

			protected override void Render(UIOverviewEquipmentModule definition)
			{
				if (definition.fullView == null)
					return;

				var fullView = definition.fullView;
				if (!EntityManager.TryGetBuffer(definition.Data.Entity, out DynamicBuffer<UnitDefinedEquipments> buffer))
					return;

				if (EntityManager.TryGetComponentData(definition.Data.Entity, out UnitVisualSourceBackend sourceBackend))
				{
					var backend = EntityManager.GetComponentObject<UnitVisualBackend>(sourceBackend.Backend);
					if (backend.Presentation != null && backend.Presentation.TryGetComponent(out ArchetypeDefaultEquipmentRoot defaultEquipmentRoot))
						currentItemPrefix = defaultEquipmentRoot.suffix;
				}

				if (definition.spawnFrame)
				{
					exitRequested         = false;
					movInput              = default;
					inputUpdate           = default;
					enterInput            = false;
					enterInputDown        = false;
					definition.spawnFrame = false;
				}

				// todo: sort equipment based on attachment priority?
				// as for now it's sorted on the backend and masterserver, but maybe we should also enforce it here?
				using var equipments = buffer.ToNativeArray(Allocator.Temp);
				
				if (fullView.ModifyIndex >= 0)
					showSelection(equipments, definition.Data.Entity, fullView);
				else
					showCarousel(equipments, definition.Data.Entity, fullView);
			}

			private void showCarousel(NativeArray<UnitDefinedEquipments> equipments, Entity entity, UIFullViewEquipment fullView)
			{
				if (exitRequested)
				{
					fullView.ExitAction();
					return;
				}
				
				if (movInput.y != 0 && (fullView.TimeBeforeNextItemInput <= 0 || inputUpdate.y))
				{
					fullView.TimeBeforeNextItemInput =  0.2f;
					fullView.FocusedIndex            += -movInput.y;
				}
				
				if (fullView.FocusedIndex < 0)
					fullView.FocusedIndex = equipments.Length - 1;
				else if (fullView.FocusedIndex >= equipments.Length)
					fullView.FocusedIndex = 0;

				for (var i = 0; i < fullView.rows.Length; i++)
				{
					var row = fullView.rows[i];
					if (i >= equipments.Length)
					{
						row.gameObject.SetActive(false);
						continue;
					}

					row.gameObject.SetActive(true);
					row.SetFocus(fullView.FocusedIndex == i);

					var equipment = equipments[i];
					
					gameResourceMgr.TryGetResource(equipment.Attachment, out var attachRes);
					if (!dentBank.TryGetItemDetails(equipment.Item, out var details))
					{
						dentBank.CallAndStoreLater(equipment.Item);
						
						details = new ReadOnlyItemDetails
						{
							MasterServerId           = "null",
							DisplayNameFallback      = $"not found ({equipment.Item.ToString()})",
							DisplayNameTranslationId = $"not found ({equipment.Item.ToString()})",
							DescriptionFallback      = $"not found ({equipment.Item.ToString()})",
							DescriptionTranslationId = $"not found ({equipment.Item.ToString()})",
						};
					}

					row.SetName(details.DisplayNameFallback);
					row.equipmentIcon.sprite = itemManager.GetSpriteOf(details.MasterServerId, currentItemPrefix);

					if (fullView.FocusedIndex == i && enterInputDown)
					{
						fullView.ModifyIndex = i;

						fullView.InventoryTask = connector.RpcClient.SendRequest
							<UnitOverviewGetRestrictedItemInventory, UnitOverviewGetRestrictedItemInventory.Response>
							(new UnitOverviewGetRestrictedItemInventory
							{
								EntityTarget     = GetComponent<ReplicatedGameEntity>(entity).Source,
								AttachmentTarget = attachRes.Value.ToString()
							});
					}
				}
			}

			private void showSelection(NativeArray<UnitDefinedEquipments> equipments, Entity entity, UIFullViewEquipment fullView)
			{
				exit:
				if (exitRequested)
				{
					for (var i = 0; i < fullView.rows.Length; i++)
					{
						var row = fullView.rows[i];
						if (i == fullView.ModifyIndex && i != fullView.FocusedIndex)
						{
							row.SetFocus(false);
							row.animator.Update(1);
						}
						else if (i == fullView.FocusedIndex)
						{
							row.gameObject.SetActive(true);

							row.SetFocus(true);
							row.animator.Update(1);
						}

						row.SetSelected(false);
					}

					fullView.ModifyIndex = -1;
					return;
				}

				for (var i = 0; i < fullView.rows.Length; i++)
				{
					var row = fullView.rows[i];
					if (i != 0)
					{
						row.SetFocus(false);
						row.animator.Update(1);

						row.gameObject.SetActive(false);
						continue;
					}

					row.gameObject.SetActive(true);
					row.SetFocus(true);
					row.SetSelected(true);

					var equipment = equipments[fullView.ModifyIndex];

					gameResourceMgr.TryGetResource(equipment.Attachment, out var attachRes);
					if (!dentBank.TryGetItemDetails(equipment.Item, out var details))
					{
						details = new ReadOnlyItemDetails
						{
							MasterServerId           = "null",
							DisplayNameFallback      = $"not found ({equipment.Item.ToString()})",
							DisplayNameTranslationId = $"not found ({equipment.Item.ToString()})",
							DescriptionFallback      = $"not found ({equipment.Item.ToString()})",
							DescriptionTranslationId = $"not found ({equipment.Item.ToString()})",
						};
					}

					row.SetName(details.DisplayNameFallback);
					row.equipmentIcon.sprite = itemManager.GetSpriteOf(details.MasterServerId, currentItemPrefix);

					var currentSelectedItem = row.inventory.Get(row.inventory.AbsoluteCursor);
					if (currentSelectedItem.itemEntity.Equals(default) == false && enterInputDown)
					{
						var targets = new Dictionary<string, DentEntity>()
						{
							{ attachRes.Value.ToString(), currentSelectedItem.itemEntity }
						};
						
						connector.RpcClient.SendNotification(new SetEquipmentUnit
						{
							UnitEntity = GetComponent<ReplicatedGameEntity>(entity).Source,
							Targets = targets
						});
						
						exitRequested = true;
						goto exit;
					}

					if (fullView.InventoryTask is { IsCompleted: true } inventoryTask)
					{
						fullView.InventoryTask = null;

						var result = inventoryTask.Result;
						row.inventory.Clear();

						result.Items ??= Array.Empty<UnitOverviewGetRestrictedItemInventory.Response.Item>();

						foreach (var item in result.Items)
						{
							var name        = item.Name;
							var description = item.Description;
							if (itemManager.TryGetDetails(item.AssetResPath.FullString, out details))
							{
								name        = details.DisplayNameFallback;
								description = details.DescriptionFallback;
							}

							row.inventory.AddLast(new EquipmentInventoryItem
							{
								itemEntity  = item.Id,
								level       = 0,
								sprite      = itemManager.GetSpriteOf(item.AssetResPath.FullString, currentItemPrefix),
								customName  = name,
								description = description
							});

							dentBank.CallAndStoreLater(item.Id);
						}

						row.inventory.CurrentEquippedItemId = equipment.Item;
					}
				}
			}

			protected override void ClearValues()
			{
			}
		}
	}
}