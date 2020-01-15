using System;
using System.Collections.Generic;
using DataScripts.Models.Equipments;
using package.stormiumteam.shared.ecs;
using Patapon.Client.Graphics.Animation.Units;
using Patapon.Client.Systems;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Components.Archetypes
{
	public class ArchetypeUberHeroVisualPresentation : UnitVisualPresentation
	{
		[Serializable]
		public class Root
		{
			public Transform transform;

			[NonSerialized]
			public UnitEquipmentBackend UnitEquipmentBackend;

			[NonSerialized]
			public NativeString64 EquipmentId;
		}

		public enum RootType
		{
			Mask,
			LeftEquipment,
			RightEquipment
		}
		
		public Root maskRoot;
		public Root leftWeaponRoot;
		public Root rightWeaponRoot;

		[NonSerialized]
		public Dictionary<RootType, Root> RootHashMap;

		public Root GetRoot(RootType rootType)
		{
			switch (rootType)
			{
				case RootType.Mask:
					return maskRoot;
				case RootType.LeftEquipment:
					return leftWeaponRoot;
				case RootType.RightEquipment:
					return rightWeaponRoot;
			}

			throw new NullReferenceException("No transform found with rootType: " + rootType);
		}

		private void CreateEquipmentBackend()
		{
			foreach (var kvp in RootHashMap)
			{
				var root = kvp.Value;
				if (root.UnitEquipmentBackend != null)
					continue;
				
				var backendGameObject = new GameObject($"'{kvp.Key}' Equipment backend", typeof(UnitEquipmentBackend), typeof(GameObjectEntity));
				root.UnitEquipmentBackend = backendGameObject.GetComponent<UnitEquipmentBackend>();
				root.UnitEquipmentBackend.transform.SetParent(root.transform, false);
			}
		}
		
		private void OnEnable()
		{
			RootHashMap = new Dictionary<RootType, Root>(3)
			{
				[RootType.Mask]        = maskRoot,
				[RootType.LeftEquipment]  = leftWeaponRoot,
				[RootType.RightEquipment] = rightWeaponRoot
			};
			
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
		}

		public override void UpdateData()
		{
		}
		
		[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
		public class LocalSystem : GameBaseSystem
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

						if (!root.EquipmentId.Equals(equipmentTarget))
						{
							root.EquipmentId = equipmentTarget;
							if (m_EquipmentManager.TryGetPool(equipmentTarget.ToString(), out var pool))
								root.UnitEquipmentBackend.SetPresentationFromPool(pool);
						}
					}
				});
			}
		}
	}
}