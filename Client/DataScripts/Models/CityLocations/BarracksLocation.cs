using System;
using package.stormiumteam.shared.ecs;
using PataNext.Client.DataScripts.Interface.Menu.__Barracks;
using PataNext.Client.DataScripts.Models.Projectiles.City.Scenes;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Graphics.Camera;
using PataNext.Client.PoolingSystems;
using PataNext.Module.Simulation.Components.Army;
using PataNext.Module.Simulation.Components.Hideout;
using PataNext.UnityCore.Utilities;
using StormiumTeam.GameBase._Camera;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Utility.DOTS.xCamera;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PataNext.Client.DataScripts.Models.CityLocations
{
	public class BarracksLocation : CityScenePresentation
	{
		[SerializeField]
		private GameObject fullViewPrefab;
		
		private BarracksFullView fullView;
		
		private EntityManager entityManager;
		private Entity        followEntity;
		
		protected override void OnEnter()
		{
			fullView               = Instantiate(fullViewPrefab).GetComponent<BarracksFullView>();
			fullView.SelectedIndex = -1;
			fullView.FocusIndex    = 0;
			
			entityManager = Backend.DstEntityManager;
			followEntity  = entityManager.CreateEntity();

			entityManager.AddComponentData(followEntity, new CameraModifierData
			{
				Position    = fullView.centerTransform.position,
				Rotation    = Quaternion.identity,
				FieldOfView = 7f
			});
			entityManager.AddComponentData(followEntity, new CameraTargetAnchor(AnchorType.Screen, new float2(0, 0.7f)));

			entityManager.AddComponentData(followEntity, new LocalCameraState
			{
				Data = new CameraState()
				{
					Target = followEntity,
					Mode   = CameraMode.Forced
				}
			});
		}

		protected override void OnExit()
		{
			if (entityManager != null)
			{
				entityManager.DestroyEntity(followEntity);
			}

			Destroy(fullView.gameObject);
		}

		public class RenderSystem : BaseRenderSystem<BarracksLocation>
		{
			internal bool exitDown;
			internal bool enterDown;
			
			internal int2  movInput;
			internal bool2 inputUpdate;

			public Entity                 FollowEntity;
			public GameObjectSwitchEnable FocusUnitSwitch;
			public GameObjectSwitchEnable FocusArmySwitch;

			protected override void PrepareValues()
			{
				var inputSystem = EventSystem.current.GetComponent<StandaloneInputModule>();

				exitDown  = Input.GetButtonDown(inputSystem.cancelButton);
				enterDown = Input.GetButtonDown(inputSystem.submitButton);
				
				var moveInput = new Vector2(Input.GetAxisRaw(inputSystem.horizontalAxis), Input.GetAxisRaw(inputSystem.verticalAxis));

				var nextInput = new int2((int) math.sign(moveInput.x), (int) math.sign(moveInput.y));
				inputUpdate = movInput != nextInput;
				movInput    = nextInput;
			}

			protected override void Render(BarracksLocation definition)
			{
				if (exitDown && definition.fullView != null && definition.fullView.SelectedIndex < 0)
					definition.OnRequestExit();

				FollowEntity    = definition.followEntity;

				if (definition.fullView != null)
				{
					FocusUnitSwitch                 = definition.fullView.focusUnitSwitch;
					FocusUnitSwitch.LastActiveState = false;

					FocusArmySwitch                 = definition.fullView.focusArmySwitch;
					FocusArmySwitch.LastActiveState = false;

					var fullView = definition.fullView;

					BarracksSquadView currentSelectedSquad = null;
					if (fullView.SelectedIndex >= 0 && fullView.squads.Length > fullView.SelectedIndex)
					{
						currentSelectedSquad = fullView.squads[fullView.SelectedIndex];
						if (currentSelectedSquad.WantToQuit)
						{
							currentSelectedSquad   = null;
							fullView.SelectedIndex = -1;
							
							EntityManager.RemoveComponent<CurrentUnitOverview>(definition.followEntity);
							EntityManager.RemoveComponent<CurrentUnitOverview.RequestToQuit>(definition.followEntity);
							
							Debug.LogError("quit requested");
						}
					}

					var cameraData     = GetComponent<CameraModifierData>(definition.followEntity);
					var targetPosition = fullView.centerTransform.position;

					if (inputUpdate.x && !currentSelectedSquad)
						fullView.FocusIndex -= movInput.x;

					if (fullView.FocusIndex < 0)
						fullView.FocusIndex = fullView.squads.Length - 1;
					else if (fullView.FocusIndex >= fullView.squads.Length)
						fullView.FocusIndex = 0;

					var currentFocusedSquad = fullView.squads[fullView.FocusIndex];
					foreach (var squad in fullView.squads)
						squad.SetFocus(currentFocusedSquad == squad);

					if (enterDown && currentFocusedSquad != null && currentSelectedSquad == null)
					{
						fullView.SelectedIndex = fullView.FocusIndex;
						
						currentSelectedSquad = currentFocusedSquad;
						currentSelectedSquad.Enter(this);

						enterDown = false;
					}

					if (currentFocusedSquad != null && currentSelectedSquad == null)
					{
						FocusArmySwitch.Active                    = true;
						FocusArmySwitch.target.transform.position = currentFocusedSquad.transform.position + new Vector3(0, 4, 0);
					}

					if (currentSelectedSquad == null)
					{
						targetPosition.x += currentFocusedSquad.transform.localPosition.x * 0.08f;

						cameraData.Position = math.lerp(cameraData.Position, targetPosition, Time.DeltaTime * 2);
						cameraData.FieldOfView = math.lerp(cameraData.FieldOfView, 7f, Time.DeltaTime * 5);
					}
					else
					{
						cameraData = currentSelectedSquad.GetCameraData(cameraData);
					}

					SetComponent(definition.followEntity, cameraData);
										
					/*Entities.WithAll<IsBarracksUnitViewTag>().ForEach((UnitVisualBackend backend) =>
					{
						backend.transform.position   = Vector3.zero;
						backend.transform.localScale = Vector3.zero;
					}).WithStructuralChanges().Run();*/

					Entities.WithAll<ArmyFormationDescription>().ForEach((Entity entity) =>
					{
						// can't put this type as the query argument since it would make unity CRASH
						if (!EntityManager.TryGetBuffer<OwnedRelative<ArmySquadDescription>>(entity, out var ownedSquads))
							return;
						
						foreach (var squad in ownedSquads.ToNativeArray(Allocator.Temp))
						{
							if (!EntityManager.TryGetBuffer<OwnedRelative<ArmyUnitDescription>>(squad.Target, out var ownedUnitsBuffer))
								continue;

							if (!EntityManager.TryGetComponentData(squad.Target, out HideoutSquadIndex squadIndex)
							    || squadIndex.Value >= fullView.squads.Length)
								continue;

							var ownedUnits = ownedUnitsBuffer.Reinterpret<Entity>()
							                                 .ToNativeArray(Allocator.Temp);
							if (squadIndex.Value >= 0)
							{
								var shouldHide = fullView.SelectedIndex >= 0 && squadIndex.Value != fullView.SelectedIndex;
								
								// Leader
								if (EntityManager.TryGetComponentData(squad.Target, out HideoutLeaderSquad leaderSquad))
								{
									fullView.squads[squadIndex.Value].SetLeaderSquad(this, shouldHide, squad.Target, leaderSquad.Leader, ownedUnits);
								}
								// Uberhero/player
								else if (ownedUnits.Length == 1)
								{
									fullView.squads[squadIndex.Value].SetPlayerSquad(this, shouldHide, squad.Target, ownedUnits[0]);
								}
							}
						}
					}).WithStructuralChanges().Run();
					
					if (FocusUnitSwitch.LastActiveState == false) FocusUnitSwitch.Active = false;
					if (FocusArmySwitch.LastActiveState == false) FocusArmySwitch.Active = false;
				}
			}

			protected override void ClearValues()
			{
			}
		}
	}
}