using System;
using PataNext.Client.DataScripts.Models.Equipments;
using Unity.Collections;
using UnityEngine;

namespace PataNext.Client.Components.Archetypes
{
	public interface IEquipmentRoot
	{
		EquipmentRootData GetRoot(Transform fromTransform);
	}
	
	[Serializable]
	public class EquipmentRootData
	{
		public Transform transform;

		[NonSerialized]
		public UnitEquipmentBackend UnitEquipmentBackend;

		[NonSerialized]
		public FixedString64 EquipmentId;
	}
}