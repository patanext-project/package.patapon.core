using System;
using System.Threading.Tasks;
using PataNext.Client.Rpc;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories
{
	public class UIFullViewEquipment : MonoBehaviour
	{
		public Action ExitAction;

		public EquipmentRow[] rows;

		public int   ModifyIndex             { get; set; } = -1;
		public float TimeBeforeNextItemInput { get; set; }
		public int   FocusedIndex            { get; set; }

		public Task<UnitOverviewGetRestrictedItemInventory.Response> InventoryTask { get; set; }
	}
}