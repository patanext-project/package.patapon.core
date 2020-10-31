using System;
using GameHost.Native;
using PataNext.Client.DataScripts.Models.Equipments;
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
		public CharBuffer64 AttachmentId, UpdatedEquipmentId;
	}
}