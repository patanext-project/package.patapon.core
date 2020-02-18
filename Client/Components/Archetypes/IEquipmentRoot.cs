using System;
using DataScripts.Models.Equipments;
using Unity.Collections;
using UnityEngine;

namespace Components.Archetypes
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
		public NativeString64 EquipmentId;
	}
}