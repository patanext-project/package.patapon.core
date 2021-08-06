using System;
using System.Collections.Generic;
using PataNext.Client.DataScripts.Interface.Inventory;
using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories.UIPresets
{
	public struct PresetItemInventory
	{
		public string Id;
		public string Name;
	}

	public class PresetInventory : UIInventory<PresetItemInventory>
	{
		protected override bool SwapAxis => true;

		private Dictionary<int, PresetItemInventory> map = new Dictionary<int, PresetItemInventory>();

		protected override void OnSpawnGameObject(GameObject go)
		{
			var comp = go.GetComponent<PresetInventoryRow>();
			comp.classSwitch.ForceActive(true, true);
			comp.nameSwitch.ForceActive(true, true);
			comp.selectionSwitch.ForceActive(false, true);
		}

		protected override void OnAdded(PresetItemInventory item, int2 position)
		{
			map[position.x] = item;
			Debug.LogError("added at " + position);
		}

		protected override void OnRemoved(int2 position)
		{
			map.Remove(position.x);
		}

		public override PresetItemInventory Get(int2 position)
		{
			if (map.TryGetValue(position.x, out var item))
				return item;

			return default;
		}

		private void LateUpdate()
		{
			for (var x = 0; x < spawnedColumn.GetLength(0); x++)
			{
				var go           = spawnedColumn[x, 0];
				var presentation = go.GetComponent<PresetInventoryRow>();

				var position = x + View.x;
				if (map.TryGetValue(position, out var item))
				{
					presentation.classSwitch.Active = false;
					presentation.nameSwitch.Active  = false;

					presentation.nameLabel.text = item.Name;
				}
				else
				{
					presentation.classSwitch.Active = true;
					presentation.nameSwitch.Active  = true;
				}

				presentation.selectionSwitch.Active = position == Cursor.x;
				if (presentation.selectionSwitch.Active)
					go.transform.SetAsLastSibling();
			}
		}
		
		public void MoveCursorDelta(int delta)
		{
			var previousPosition = Cursor;
			var previousView     = View;

			Cursor += new int2(delta, 0);

			if (!previousPosition.Equals(Cursor) || !previousView.Equals(View))
			{
				/*audioSource.clip  = resources.audioOnSelection;
				audioSource.pitch = Random.Range(0.925f, 1.125f);
				audioSource.Play();
				
				UpdateSelectedEquipmentData();*/
			}
		}
	}
}