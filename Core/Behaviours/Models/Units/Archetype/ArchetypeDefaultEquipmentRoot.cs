using System;
using System.Collections.Generic;
using GameHost.Native;
using GameHost.Simulation.Utility.Resource.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.DataScripts.Models.Equipments;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Resources;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Entities;
using UnityEngine;
using Utility.GameResources;

namespace PataNext.Client.Components.Archetypes
{
	public partial class ArchetypeDefaultEquipmentRoot : MonoBehaviour, IEquipmentRoot, IBackendReceiver
	{
		public enum KnownTypes
		{
			Custom,
			Shoes,
			LeftEquipment,
			RightEquipment,
			Cape,
			Shoulder,
			Helmet,
			Hair,
			Mask,
		}

		[Serializable]
		private struct KeyValue
		{
			public KnownTypes        known;
			public string            id;
			public EquipmentRootData root;
		}

		[SerializeField]
		private List<KeyValue> values;

		[NonSerialized]
		public Dictionary<Transform, EquipmentRootData> TransformToRootMap;

		public EquipmentRootData GetRoot(Transform fromTransform)
		{
			return TransformToRootMap[fromTransform];
		}

		private void CreateEquipmentBackend()
		{
			foreach (var kvp in values)
			{
				var root = kvp.root;
				if (root.UnitEquipmentBackend != null)
				{
					root.UnitEquipmentBackend.gameObject.SetActive(true);
					continue;
				}

				kvp.root.AttachmentId = kvp.known switch
				{
					KnownTypes.Custom => string.IsNullOrEmpty(kvp.id) ? throw new InvalidOperationException("empty equipId") : kvp.id,
					KnownTypes.Shoes => "ms://st.pn/equip_root/shoes",
					KnownTypes.LeftEquipment => "ms://st.pn/equip_root/l_eq",
					KnownTypes.RightEquipment => "ms://st.pn/equip_root/r_eq",
					KnownTypes.Cape => "ms://st.pn/equip_root/cape",
					KnownTypes.Shoulder => "ms://st.pn/equip_root/shoulder",
					KnownTypes.Helmet => "ms://st.pn/equip_root/helmet",
					KnownTypes.Hair => "ms://st.pn/equip_root/hair",
					KnownTypes.Mask => "ms://st.pn/equip_root/mask",
					_ => throw new NotImplementedException(kvp.known + " was not implemented")
				};

				var backendGameObject = new GameObject($"'{kvp.known}' Equipment backend", typeof(UnitEquipmentBackend), typeof(GameObjectEntity));
				root.UnitEquipmentBackend = backendGameObject.GetComponent<UnitEquipmentBackend>();
				root.UnitEquipmentBackend.transform.SetParent(root.transform, false);
			}
		}

		private void OnEnable()
		{
			TransformToRootMap = new Dictionary<Transform, EquipmentRootData>(values.Count);
			foreach (var kvp in values)
			{
				TransformToRootMap[kvp.root.transform] = kvp.root;
			}

			// create backend
			CreateEquipmentBackend();
		}
	}

	public partial class ArchetypeDefaultEquipmentRoot
	{
		public RuntimeAssetBackendBase Backend { get; set; }

		public void OnBackendSet()
		{
			// create backend
			CreateEquipmentBackend();

			foreach (var kvp in values)
			{
				kvp.root.UnitEquipmentBackend.SetTarget(Backend.DstEntityManager, Backend.DstEntity);
			}

			localUpdate = Backend.DstEntityManager.World.GetOrCreateSystem<LocalUpdate>();
		}

		private LocalUpdate localUpdate;

		public void OnPresentationSystemUpdate()
		{
			localUpdate.ForceUpdate(this);
		}

		private class LocalUpdate : AbsGameBaseSystem
		{
			private UnitVisualEquipmentManager                                            m_EquipmentManager;
			private GameResourceModule<UnitAttachmentResource> attachmentModule;
			private GameResourceModule<EquipmentResource>           equipmentModule;

			private GameResourceManager resourceMgr;

			protected override void OnCreate()
			{
				base.OnCreate();
				Enabled = false;

				m_EquipmentManager = World.GetOrCreateSystem<UnitVisualEquipmentManager>();
				resourceMgr        = World.GetOrCreateSystem<GameResourceManager>();

				GetModule(out attachmentModule);
				GetModule(out equipmentModule);
			}

			public void ForceUpdate(ArchetypeDefaultEquipmentRoot component)
			{
				var backend = component.Backend;
				if (!EntityManager.TryGetBuffer(backend.DstEntity, out DynamicBuffer<UnitDisplayedEquipment> displayedEquipment))
					return;

				foreach (var kvp in component.TransformToRootMap)
				{
					var root = kvp.Value;

					var equipmentTarget = default(CharBuffer64);
					foreach (var equipment in displayedEquipment)
					{
						EquipmentResource key;

						//Console.WriteLine($"({attachmentModule.GetResourceOrDefault(root.AttachmentId).Entity} == {equipment.Attachment.Entity}) {root.AttachmentId} == {equipment.Attachment.TryGet(resourceMgr, out var res)} {res.Value} ({equipment.Resource.TryGet(resourceMgr, out var ok)} {ok.Value})");
						if (equipment.Attachment == attachmentModule.GetResourceOrDefault(root.AttachmentId)
						    && equipment.Resource.TryGet(resourceMgr, out key))
						{
							equipmentTarget = key.Value;
							break;
						}
					}

					//Console.WriteLine($"{root.AttachmentId} -> {equipmentTarget}");
					if (!root.UpdatedEquipmentId.Equals(equipmentTarget) || !root.UnitEquipmentBackend.HasIncomingPresentation)
					{
						root.UpdatedEquipmentId = equipmentTarget;

						root.UnitEquipmentBackend.ReturnPresentation();
						if (m_EquipmentManager.TryGetPool(equipmentTarget.ToString(), out var pool))
						{
							root.UnitEquipmentBackend.SetPresentationFromPool(pool);
						}

						displayedEquipment = EntityManager.GetBuffer<UnitDisplayedEquipment>(backend.DstEntity);
					}
				}
			}

			protected override void OnUpdate()
			{
			}
		}
	}
}