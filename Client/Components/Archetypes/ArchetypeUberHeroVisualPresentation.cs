using System;
using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using PataNext.Client.DataScripts.Models.Equipments;
using PataNext.Client.Graphics;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.Components.Archetypes
{
	public class ArchetypeUberHeroVisualPresentation : UnitVisualPresentation, IEquipmentRoot
	{
		public enum RootType
		{
			Mask,
			LeftEquipment,
			RightEquipment,
			Hair
		}
		
		public EquipmentRootData maskRoot;
		public EquipmentRootData leftWeaponRoot;
		public EquipmentRootData rightWeaponRoot;
		public EquipmentRootData hairRoot;

		[NonSerialized]
		public Dictionary<RootType, EquipmentRootData> RootHashMap;

		[NonSerialized]
		public Dictionary<Transform, EquipmentRootData> TransformToRootMap;

		public EquipmentRootData GetRoot(RootType rootType)
		{
			switch (rootType)
			{
				case RootType.Mask:
					return maskRoot;
				case RootType.LeftEquipment:
					return leftWeaponRoot;
				case RootType.RightEquipment:
					return rightWeaponRoot;
				case RootType.Hair:
					return hairRoot;
			}

			throw new NullReferenceException("No transform found with rootType: " + rootType);
		}

		public EquipmentRootData GetRoot(Transform fromTransform)
		{
			return TransformToRootMap[fromTransform];
		}

		private void CreateEquipmentBackend()
		{
			foreach (var kvp in RootHashMap)
			{
				var root = kvp.Value;
				if (root.UnitEquipmentBackend != null)
				{
					root.UnitEquipmentBackend.gameObject.SetActive(true);
					continue;
				}

				var backendGameObject = new GameObject($"'{kvp.Key}' Equipment backend", typeof(UnitEquipmentBackend), typeof(GameObjectEntity));
				root.UnitEquipmentBackend = backendGameObject.GetComponent<UnitEquipmentBackend>();
				root.UnitEquipmentBackend.transform.SetParent(root.transform, false);
			}
		}
		
		private void OnEnable()
		{
			RootHashMap = new Dictionary<RootType, EquipmentRootData>(3)
			{
				[RootType.Mask]        = maskRoot,
				[RootType.LeftEquipment]  = leftWeaponRoot,
				[RootType.RightEquipment] = rightWeaponRoot
			};
			TransformToRootMap = new Dictionary<Transform, EquipmentRootData>(RootHashMap.Count);
			foreach (var kvp in RootHashMap)
			{
				TransformToRootMap[kvp.Value.transform] = kvp.Value;
			}

			// create backend
			CreateEquipmentBackend();
		}

		public override void OnBackendSet()
		{
			// create backend
			CreateEquipmentBackend();
			
			foreach (var kvp in RootHashMap)
			{
				var root = kvp.Value;
				root.UnitEquipmentBackend.SetTarget(Backend.DstEntityManager, Backend.DstEntity);
			}
			
			base.OnBackendSet();
		}

		public override void UpdateData()
		{
		}

		private float m_HeroModeScaling;
		
		[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
		public class LocalSystem : AbsGameBaseSystem
		{
			private UnitVisualEquipmentManager m_EquipmentManager;

			protected override void OnCreate()
			{
				base.OnCreate();
				m_EquipmentManager = World.GetOrCreateSystem<UnitVisualEquipmentManager>();
			}

			protected override void OnUpdate()
			{
				Entities.ForEach((ArchetypeUberHeroVisualPresentation presentation) =>
				{
					var backend = presentation.Backend;
					if (!EntityManager.Exists(backend.DstEntity))
						return;

					if (!EntityManager.TryGetComponentData(backend.DstEntity, out UnitDisplayedEquipment displayedEquipment))
						return;

					foreach (var kvp in presentation.RootHashMap)
					{
						var type = kvp.Key;
						var root = kvp.Value;

						var equipmentTarget = default(NativeString64);
						switch (type)
						{
							case RootType.Mask:
								equipmentTarget = displayedEquipment.Mask;
								break;
							case RootType.LeftEquipment:
								equipmentTarget = displayedEquipment.LeftEquipment;
								break;
							case RootType.RightEquipment:
								equipmentTarget = displayedEquipment.RightEquipment;
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						if (!root.EquipmentId.Equals(equipmentTarget) || !root.UnitEquipmentBackend.HasIncomingPresentation)
						{
							root.EquipmentId = equipmentTarget;
							
							root.UnitEquipmentBackend.ReturnPresentation();
							if (m_EquipmentManager.TryGetPool(equipmentTarget.ToString(), out var pool))
							{
								root.UnitEquipmentBackend.SetPresentationFromPool(pool);
							}
						}
					}

					var scale = 1f;
					if (EntityManager.TryGetComponentData(backend.DstEntity, out LivableHealth health) && health.IsDead)
					{
						scale = 0;
					}

					ref var heroModeScaling = ref presentation.m_HeroModeScaling;
					if (EntityManager.TryGetComponentData(backend.DstEntity, out OwnerActiveAbility ownerAbility)
					    && EntityManager.TryGetComponentData(ownerAbility.Active, out AbilityActivation activation)
					    && activation.Type == EActivationType.HeroMode)
					{
						heroModeScaling = 1.325f;
					}
					else
					{
						heroModeScaling = math.lerp(heroModeScaling, 1, Time.DeltaTime * 1.75f);
						heroModeScaling = math.lerp(heroModeScaling, 1, Time.DeltaTime * 1.25f);
						heroModeScaling = math.clamp(heroModeScaling, 1, 1.325f);
					}

					presentation.transform.localScale = Vector3.one * (scale * presentation.m_HeroModeScaling);
					presentation.OnSystemUpdate();
				}).WithStructuralChanges().Run();
			}
		}
	}
}