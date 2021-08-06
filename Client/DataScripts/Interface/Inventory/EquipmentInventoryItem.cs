using System;
using PataNext.Client.Systems;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Inventory
{
	[Serializable]
	public struct EquipmentInventoryItem
	{
		public DentEntity itemEntity;

		public int    level;
		public Sprite sprite;

		public string customName;
		public string description;
	}
}