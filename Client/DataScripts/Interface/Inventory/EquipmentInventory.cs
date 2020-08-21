using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PataNext.Client.DataScripts.Interface.Inventory
{
	public class EquipmentInventory : UIInventory<EquipmentInventoryItem>
	{
		private Dictionary<int2, EquipmentInventoryItem> itemMap = new Dictionary<int2, EquipmentInventoryItem>();

		private void Start()
		{
			for (var i = 0; i != 512; i++)
				AddLast(new EquipmentInventoryItem {level = i});
		}

		public override void Clear()
		{
			base.Clear();

			foreach (var go in spawnedColumn)
			{
				var presentation = go.GetComponent<EquipmentInventoryItemPresentation>();
				presentation.SetSprite(null);
			}
		}

		public override EquipmentInventoryItem Get(int2 position)
		{
			itemMap.TryGetValue(position, out var item);
			return item;
		}

		protected override void OnSpawnGameObject(GameObject go)
		{
			var presentation = go.GetComponent<EquipmentInventoryItemPresentation>();
			presentation.SetSprite(null);
		}

		private void LateUpdate()
		{
			if (Keyboard.current.numpad4Key.wasPressedThisFrame)
				AbsoluteCursor = new int2(2, 12);
			
			for (var x = 0; x < spawnedColumn.GetLength(0); x++)
			for (var y = 0; y < spawnedColumn.GetLength(1); y++)
			{
				var go           = spawnedColumn[x, y];
				var presentation = go.GetComponent<EquipmentInventoryItemPresentation>();
				
				var position = new int2(x, y) + View;
				if (itemMap.TryGetValue(position, out var item))
				{
					presentation.SetSprite(item.sprite);

					var sum = math.csum(position);
					presentation.graphic.color = new Color32((byte) ((sum * 4) % 255), (byte) ((position.y * 4) % 255), (byte) (item.level % 255), 255);
				}
				else
				{
					presentation.SetSprite(null);
					presentation.graphic.color = Color.clear;
				}
				
				presentation.SetSelected(math.all(new int2(x, y) == Cursor));
			}
		}

		protected override void OnAdded(EquipmentInventoryItem item, int2 position)
		{
			itemMap[position] = item;
		}
	}
}