using System;
using System.Collections.Generic;
using PataNext.Client.Systems;
using TMPro;
using Unity.Mathematics;
using Unity.VectorGraphics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PataNext.Client.DataScripts.Interface.Inventory
{
	public class EquipmentInventory : UIInventory<EquipmentInventoryItem>
	{
		[Serializable]
		public struct SelectedEquipment
		{
			public RectTransform   indicator;
			public RectTransform   quadBackground;
			public TextMeshProUGUI label;
			public TextMeshProUGUI description;
			public SVGImage        icon;
		}

		[Serializable]
		public struct Resources
		{
			public AudioClip         audioOnSelection;
			public SelectedEquipment selectedEquipment;
		}
		
		private Dictionary<int2, EquipmentInventoryItem> itemMap = new Dictionary<int2, EquipmentInventoryItem>();

		public Resources resources;

		private AudioSource audioSource;
		
		private void Start()
		{
			audioSource = gameObject.AddComponent<AudioSource>();
			
			/*AddLast(new EquipmentInventoryItem {masterServerId = 1, customName = "Standard Bow"});
			AddLast(new EquipmentInventoryItem {masterServerId = 4, customName = "Iron Bow"});
			AddLast(new EquipmentInventoryItem {masterServerId = 5, customName = "Iron Bow"});
			AddLast(new EquipmentInventoryItem {masterServerId = 6, customName = "Magilian's Aftermath"});*/
		}

		public DentEntity CurrentEquippedItemId { get; set; }

		public override void Clear()
		{
			base.Clear();

			itemMap.Clear();
			foreach (var go in spawnedColumn)
			{
				var presentation = go.GetComponent<EquipmentInventoryItemPresentation>();
				presentation.SetSprite(null);
			}

			UpdateSelectedEquipmentData();
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
		
		private void Update()
		{
			var c = default(int2);
			// TODO: fix
			if (Input.GetKeyDown(KeyCode.LeftArrow))
				c.x--;
			if (Input.GetKeyDown(KeyCode.RightArrow))
				c.x++;
			if (Input.GetKeyDown(KeyCode.UpArrow))
				c.y--;
			if (Input.GetKeyDown(KeyCode.DownArrow))
				c.y++;
			if (c.x != 0 || c.y != 0)
				MoveCursorDelta(c);
		}

		private void LateUpdate()
		{
			resources.selectedEquipment.label.transform.localScale = Vector3.MoveTowards(resources.selectedEquipment.label.transform.localScale, Vector3.one, Time.deltaTime);
			resources.selectedEquipment.indicator.transform.localScale = Vector3.MoveTowards(resources.selectedEquipment.indicator.transform.localScale, Vector3.one, Time.deltaTime);
			
			for (var x = 0; x < spawnedColumn.GetLength(0); x++)
			for (var y = 0; y < spawnedColumn.GetLength(1); y++)
			{
				var go           = spawnedColumn[x, y];
				var presentation = go.GetComponent<EquipmentInventoryItemPresentation>();

				var position = new int2(x, y) + View;
				if (itemMap.TryGetValue(position, out var item))
				{
					presentation.SetSprite(item.sprite);
					presentation.graphic.color    = item.sprite == null ? Color.clear : Color.white;
					presentation.background.color = Color.white;

					presentation.SetEquipped(item.itemEntity.Equals(CurrentEquippedItemId));
				}
				else
				{
					presentation.SetSprite(null);
					presentation.graphic.color    = Color.clear;
					presentation.background.color = new Color32(33, 33, 33, 255);
					
					presentation.SetEquipped(false);
				}

				presentation.SetSelected(math.all(new int2(x, y) == Cursor));
			}
		}


		public void MoveCursorDelta(int2 delta)
		{
			var previousPosition = Cursor;
			var previousView = View;

			Cursor += delta;

			if (!previousPosition.Equals(Cursor) || !previousView.Equals(View))
			{
				audioSource.clip  = resources.audioOnSelection;
				audioSource.pitch = Random.Range(0.925f, 1.125f);
				audioSource.Play();
				
				UpdateSelectedEquipmentData();
			}
		}

		private void UpdateSelectedEquipmentData()
		{
			var se = resources.selectedEquipment;
			
			if (itemMap.TryGetValue(AbsoluteCursor, out var item))
			{
				// TODO: if customName is null, use the name from masterserver resources
				se.label.text = item.customName;
				se.description.text = item.description;
				se.indicator.gameObject.SetActive(true);
				se.quadBackground.sizeDelta = new Vector2(se.label.preferredWidth + 15, se.quadBackground.sizeDelta.y);

				se.icon.sprite = item.sprite;
				se.icon.color  = Color.white;
				
				se.label.transform.localScale     = Vector3.one * 1.1f;
				se.indicator.transform.localScale = Vector3.one * 1.1f;
			}
			else
			{
				se.label.text = null;
				se.indicator.gameObject.SetActive(false);

				se.description.text = string.Empty;
				se.icon.color = Color.clear;
			}
		}

		protected override void OnAdded(EquipmentInventoryItem item, int2 position)
		{
			itemMap[position] = item;
			
			if (position.Equals(AbsoluteCursor))
				UpdateSelectedEquipmentData();
		}

		protected override void OnRemoved(int2 position)
		{
			itemMap.Remove(position);
			
			if (position.Equals(AbsoluteCursor))
				UpdateSelectedEquipmentData();
		}
	}
}