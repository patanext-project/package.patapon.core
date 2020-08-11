using System;
using System.Collections.Generic;
using GameHost.Core.Native.xUnity;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.DataScripts.Models.Equipments;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Resources;
using PataNext.Module.Simulation.Resources.Keys;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Utility.GameResources;

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

		private float m_HeroModeScaling = 1;
		
		[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
		public class LocalSystem : AbsGameBaseSystem
		{
			private UnitVisualEquipmentManager m_EquipmentManager;
			private GameResourceModule<UnitAttachmentResource, UnitAttachmentResourceKey> attachmentModule;
			private GameResourceModule<EquipmentResource, EquipmentResourceKey> equipmentModule;

			private GameResourceManager resourceMgr;

			protected override void OnCreate()
			{
				base.OnCreate();
				m_EquipmentManager = World.GetOrCreateSystem<UnitVisualEquipmentManager>();
				resourceMgr        = World.GetOrCreateSystem<GameResourceManager>();
				
				GetModule(out attachmentModule);
				GetModule(out equipmentModule);
			}

			protected override void OnUpdate()
			{
				var (maskAttachResource, maskKey) = attachmentModule.GetResourceTuple(nameof(RootType.Mask));
				var (leftEquipResource, leftEquipKey) = attachmentModule.GetResourceTuple(nameof(RootType.LeftEquipment));
				var (rightEquipResource, rightEquipKey) = attachmentModule.GetResourceTuple(nameof(RootType.RightEquipment));

				Entities.ForEach((ArchetypeUberHeroVisualPresentation presentation) =>
				{
					var backend = presentation.Backend;
					if (!EntityManager.TryGetBuffer(backend.DstEntity, out DynamicBuffer<UnitDisplayedEquipment> displayedEquipment))
						return;
					
					foreach (var kvp in presentation.RootHashMap)
					{
						var type = kvp.Key;
						var root = kvp.Value;

						var equipmentTarget = default(FixedString64);
						foreach (var equipment in displayedEquipment)
						{
							EquipmentResourceKey key;
							switch (equipment.Attachment)
							{
								case { } resource when resource == maskAttachResource && type == RootType.Mask:
									if (resourceMgr.TryGetResource(equipment.Resource, out key))
										key.Value.CopyToNativeString(ref equipmentTarget);
									break;
								case { } resource when resource == leftEquipResource && type == RootType.LeftEquipment:
									if (resourceMgr.TryGetResource(equipment.Resource, out key))
										key.Value.CopyToNativeString(ref equipmentTarget);
									break;
								case { } resource when resource == rightEquipResource && type == RootType.RightEquipment:
									if (resourceMgr.TryGetResource(equipment.Resource, out key))
										key.Value.CopyToNativeString(ref equipmentTarget);
									break;
							}

							if (equipmentTarget.Length > 0)
								break;
						}

						if (!root.EquipmentId.Equals(equipmentTarget) || !root.UnitEquipmentBackend.HasIncomingPresentation)
						{
							root.EquipmentId = equipmentTarget;
							
							root.UnitEquipmentBackend.ReturnPresentation();
							if (m_EquipmentManager.TryGetPool(equipmentTarget.ToString(), out var pool))
							{
								root.UnitEquipmentBackend.SetPresentationFromPool(pool);
								Console.WriteLine($"SetPresnetationFromPool {equipmentTarget.ToString()}");
							}

							displayedEquipment = EntityManager.GetBuffer<UnitDisplayedEquipment>(backend.DstEntity);
						}
					}

					var scale = 1f;
					// Scaling in general should be done in another system...
					/*if (EntityManager.TryGetComponentData(backend.DstEntity, out LivableHealth health) && health.IsDead)
					{
						scale = 0;
					}*/

					ref var heroModeScaling = ref presentation.m_HeroModeScaling;
					// Hero mode scaling shouldn't be done here.
					/*if (EntityManager.TryGetComponentData(backend.DstEntity, out OwnerActiveAbility ownerAbility)
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
					}*/

					presentation.transform.localScale = Vector3.one * (scale * presentation.m_HeroModeScaling);
					presentation.OnSystemUpdate();
				}).WithStructuralChanges().Run();
			}
		}
	}
}