using System;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Inventory
{
	[Serializable]
	public struct EquipmentInventoryItem
	{
		public long masterServerId;

		public int    level;
		public Sprite sprite;

		public string customName;
	}
}