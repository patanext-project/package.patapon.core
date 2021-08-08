using System;
using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using PataNext.Client.DataScripts.Interface.Menu.__Barracks;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.Army;
using PataNext.Module.Simulation.Components.GamePlay.Units;
using PataNext.UnityCore.Utilities;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Utility.DOTS.xCamera;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace PataNext.Client.DataScripts.Models.CityLocations
{
	public class BarracksSquadView : MonoBehaviour
	{
		public enum ECurrentType
		{
			LeaderSquad,
			Player
		}

		[Serializable]
		public struct CurrentViewLeaderSquad
		{
			public bool    IsViewingUnitDetails;
			public Vector3 Position;
			
			public int  FocusIndex;

			public float isInTime;
			public float hideTime;
		}

		private CurrentViewLeaderSquad viewLeaderSquad;

		public struct CurrentViewPlayerSquad
		{
			public float hideTime;
		}

		private CurrentViewPlayerSquad viewPlayerSquad;

		private ECurrentType currentType;

		[SerializeField] private GameObjectSwitchEnable squadViewSwitch;
		[SerializeField] private GameObjectSwitchEnable playerViewSwitch;

		private Animator animator;

		private bool tryGetBackend(ComponentSystemBase system, Entity entity, out UnitVisualBackend backend)
		{
			if (system.EntityManager.TryGetComponentData(entity, out UnitVisualSourceBackend sourceBackend))
			{
				backend = system.EntityManager.GetComponentObject<UnitVisualBackend>(sourceBackend.Backend);
				return true;
			}

			backend = null;
			return false;
		}

		public static float CenterComputeV1(int i, int size, float space)
		{
			if (size == 1 && i == 0)
				return 0;

			return (i - (size - 1) / 2) * space + space / 2f;
		}

		private bool isFocused;

		private void OnEnable()
		{
			isFocused = false;
			animator  = GetComponent<Animator>();
		}

		public void SetFocus(bool focused)
		{
			if (isFocused == focused)
				return;

			isFocused = focused;
			animator.SetBool("Active", focused);
		}

		private bool isIn;

		public void SetLeaderSquad(BarracksLocation.RenderSystem system, bool shouldHide, Entity squadTarget, Entity leaderSquadLeader, NativeArray<Entity> entities)
		{
			currentType = ECurrentType.LeaderSquad;

			squadViewSwitch.Active  = true;
			playerViewSwitch.Active = false;

			if (shouldHide)
			{
				viewLeaderSquad.hideTime += Time.deltaTime;

				foreach (var ent in entities)
				{
					if (!tryGetBackend(system, ent, out var backend))
						continue;

					var prevPosition = backend.transform.position;
					var prevScale    = backend.transform.localScale;

					backend.transform.position   = Vector3.Lerp(prevPosition, transform.position + new Vector3 { z = 1 + prevPosition.z }, viewLeaderSquad.hideTime * 4.5f);
					backend.transform.localScale = Vector3.Lerp(prevScale, Vector3.zero, viewLeaderSquad.hideTime * 4.5f);
				}

				return;
			}

			viewLeaderSquad.hideTime = 0;

			if (tryGetBackend(system, leaderSquadLeader, out var leaderBackend))
			{
				leaderBackend.transform.position   = transform.position;
				leaderBackend.transform.localScale = Vector3.one;
			}

			using var list = new NativeList<(int idx, Entity ent)>(Allocator.Temp);
			if (!isIn)
			{
				viewLeaderSquad.isInTime = 0;
				
				foreach (var ent in entities)
				{
					if (ent == leaderSquadLeader)
						continue;
					
					if (!tryGetBackend(system, ent, out var backend))
						continue;

					var prevPosition = backend.transform.position;
					var prevScale    = backend.transform.localScale;

					backend.transform.position   = Vector3.Lerp(prevPosition, transform.position + new Vector3 { z = 1 + prevPosition.z }, Time.deltaTime * 10);
					backend.transform.localScale = Vector3.Lerp(prevScale, Vector3.zero, Time.deltaTime * 10);
				}
			}

			if (isIn)
			{
				viewLeaderSquad.isInTime += Time.deltaTime;
				
				foreach (var ent in entities)
				{
					if (!system.EntityManager.TryGetComponentData(ent, out UnitIndexInSquad indexInSquad))
						continue;
					
					list.Add((indexInSquad.Value, ent));
					
					if (!tryGetBackend(system, ent, out var backend))
						continue;
					
					if (backend.TryGetComponent(out SortingGroup sortingGroup))
						sortingGroup.sortingOrder = 0;
					
					if (ent == leaderSquadLeader)
						continue;

					backend.transform.position   = transform.position + new Vector3(Mathf.Lerp(0, -(indexInSquad.Value * 1.25f) - 0.5f, viewLeaderSquad.isInTime * 5f), 0, 1 + indexInSquad.Value * 5);
					backend.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 1, viewLeaderSquad.isInTime * 5f);
				}
			}
			
			list.Sort(new SortTuple());

			if (isIn)
			{
				var inputSystem = EventSystem.current.GetComponent<StandaloneInputModule>();
				if (!viewLeaderSquad.IsViewingUnitDetails)
				{
					if (system.inputUpdate.x)
					{
						viewLeaderSquad.FocusIndex -= system.movInput.x;
					}
					
					if (system.enterDown)
					{
						viewLeaderSquad.IsViewingUnitDetails = true;
					}
				}

				if (viewLeaderSquad.FocusIndex < 0)
					viewLeaderSquad.FocusIndex = list.Length - 1;
				else if (viewLeaderSquad.FocusIndex >= list.Length)
					viewLeaderSquad.FocusIndex = 0;

				if (tryGetBackend(system, list[viewLeaderSquad.FocusIndex].ent, out var backend))
				{
					var position = backend.transform.position;
					backend.transform.position = new Vector3(position.x, position.y, -2);
					
					system.FocusUnitSwitch.Active                    = true;
					system.FocusUnitSwitch.target.transform.position = backend.transform.position + new Vector3(0, 0, 1);

					if (backend.TryGetComponent(out SortingGroup sortingGroup))
						sortingGroup.sortingOrder = 10;
					
					backend.transform.SetAsFirstSibling();

					viewLeaderSquad.Position = backend.transform.position;
				}

				if (viewLeaderSquad.IsViewingUnitDetails && list.Length > 0)
				{
					system.EntityManager.AddComponentData(system.FollowEntity, new CurrentUnitOverview(list[viewLeaderSquad.FocusIndex].ent));

					if (system.EntityManager.HasComponent<CurrentUnitOverview.RequestToQuit>(system.FollowEntity))
					{
						viewLeaderSquad.IsViewingUnitDetails = false;
						
						system.EntityManager.RemoveComponent<CurrentUnitOverview>(system.FollowEntity);
						system.EntityManager.RemoveComponent<CurrentUnitOverview.RequestToQuit>(system.FollowEntity);
					}
				}
				else
				{
					if (Input.GetButtonDown(inputSystem.cancelButton))
					{
						isIn       = false;
						WantToQuit = true;
					}
					
					system.FocusArmySwitch.Active                    = true;
					system.FocusArmySwitch.target.transform.position = viewLeaderSquad.Position + new Vector3(0, 3, 0);
				}
			}
		}

		public void SetPlayerSquad(BarracksLocation.RenderSystem system, bool shouldHide, Entity squadTarget, Entity entity)
		{
			currentType = ECurrentType.Player;

			squadViewSwitch.Active  = false;
			playerViewSwitch.Active = true;

			if (tryGetBackend(system, entity, out var backend))
			{
				if (shouldHide)
				{
					viewPlayerSquad.hideTime += Time.deltaTime;
					
					var prevPosition = backend.transform.position;
					var prevScale    = backend.transform.localScale;
					backend.transform.position   = Vector3.Lerp(prevPosition, transform.position + new Vector3 { z = 1 + prevPosition.z }, viewPlayerSquad.hideTime * 4.5f);
					backend.transform.localScale = Vector3.Lerp(prevScale, Vector3.zero, viewPlayerSquad.hideTime * 4.5f);

					return;
				}
				
				backend.transform.position   = transform.position;
				backend.transform.localScale = Vector3.one;
				
				if (backend.TryGetComponent(out SortingGroup sortingGroup))
					sortingGroup.sortingOrder = 10;
			}
			
			viewPlayerSquad.hideTime = 0;

			if (isIn)
			{
				system.EntityManager.AddComponentData(system.FollowEntity, new CurrentUnitOverview(entity));
				system.FocusUnitSwitch.Active                    = true;
				system.FocusUnitSwitch.target.transform.position = transform.position + new Vector3(0, 0, 10);
				
				if (system.EntityManager.HasComponent<CurrentUnitOverview.RequestToQuit>(system.FollowEntity))
				{
					isIn       = false;
					WantToQuit = true;
				}
			}
		}

		public bool WantToQuit { get; private set; }

		public void Enter(BarracksLocation.RenderSystem renderSystem)
		{
			isIn       = true;
			WantToQuit = false;
		}

		public CameraModifierData GetCameraData(CameraModifierData prev)
		{
			var next = prev;
			switch (currentType)
			{
				case ECurrentType.LeaderSquad:
				{
					if (viewLeaderSquad.IsViewingUnitDetails)
					{
						next.FieldOfView = math.lerp(prev.FieldOfView, 2.5f, Time.deltaTime * 10);
						next.Position    = math.lerp(prev.Position, viewLeaderSquad.Position - new Vector3(1f, 0), Time.deltaTime * 10);
					}
					else
					{
						next.FieldOfView = math.lerp(prev.FieldOfView, 3.9f, Time.deltaTime * 7);
						next.Position    = math.lerp(prev.Position, transform.position + new Vector3(-2, 0, 0), Time.deltaTime * 7);
					}

					break;
				}
				case ECurrentType.Player:
				{
					next.FieldOfView = math.lerp(prev.FieldOfView, 2.5f, Time.deltaTime * 10);
					next.Position    = math.lerp(prev.Position, transform.position - new Vector3(1f, 0), Time.deltaTime * 10);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			return next;
		}
	}

	public struct SortTuple : IComparer<(int idx, Entity ent)>
	{
		public int Compare((int idx, Entity ent) x, (int idx, Entity ent) y)
		{
			var item1Comparison = x.Item1.CompareTo(y.Item1);
			return item1Comparison != 0 ? item1Comparison : x.Item2.CompareTo(y.Item2);
		}
	}
}