using System;
using System.Collections.Generic;
using PataNext.Client.DataScripts.Interface.Inventory;
using PataNext.Client.DataScripts.Interface.Menu.__Barracks.Controls;
using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks
{
	[Serializable]
	public struct UnitOverviewCategoryRow
	{
		public string id;
		
		public string name;
		public Sprite sprite;
	}

	public class UIUnitOverviewCategoryRows : UIInventory<UnitOverviewCategoryRow>
	{
		private Dictionary<int, UnitOverviewCategoryRow> itemMap = new Dictionary<int, UnitOverviewCategoryRow>();

		public string CurrentlySelectedId { get; set; }

		private void Start()
		{
			AddLast(new UnitOverviewCategoryRow {id = "equip", name = "Equipment"});
			AddLast(new UnitOverviewCategoryRow {id = "role_tree", name = "Role Tree"});
			AddLast(new UnitOverviewCategoryRow {id = "abilities", name = "Abilities"});
			AddLast(new UnitOverviewCategoryRow {id = "miracles", name = "Miracles"});
			AddLast(new UnitOverviewCategoryRow {id = "visuals", name = "Customize Visuals"});
		}
		
		protected override void OnAdded(UnitOverviewCategoryRow item, int2 position)
		{
			itemMap[position.x] = item;
		}

		public override void Clear()
		{
			base.Clear();

			foreach (var go in spawnedColumn)
			{
				var presentation = go.GetComponent<UIUnitOverviewCategoryButtonPresentation>();
				presentation.SetIcon(null);
				presentation.SetPhase(UIUnitOverviewCategoryButtonPresentation.EPhase.None);
				presentation.SetName(null);
			}
		}

		public override UnitOverviewCategoryRow Get(int2 position)
		{
			itemMap.TryGetValue(position.x, out var item);
			return item;
		}

		protected override void OnSpawnGameObject(GameObject go)
		{
			var presentation = go.GetComponent<UIUnitOverviewCategoryButtonPresentation>();
			presentation.SetIcon(null);
			presentation.SetPhase(UIUnitOverviewCategoryButtonPresentation.EPhase.None);
			presentation.SetName(null);
		}

		private void Update()
		{
			for (var x = 0; x < spawnedColumn.GetLength(0); x++)
			for (var y = 0; y < spawnedColumn.GetLength(1); y++)
			{
				var go           = spawnedColumn[x, y];
				var presentation = go.GetComponent<UIUnitOverviewCategoryButtonPresentation>();

				var position = new int2(x, y) + View;
				if (itemMap.TryGetValue(position.x, out var item))
				{
					presentation.SetIcon(item.sprite);
					presentation.SetName(item.name);
				}
				else
				{
					presentation.SetIcon(null);
					presentation.SetName(null);
				}
				
				presentation.SetPhase(CurrentlySelectedId == item.id && item.id != null
					? UIUnitOverviewCategoryButtonPresentation.EPhase.Selected
					: math.all(new int2(x, y) == Cursor)
						? UIUnitOverviewCategoryButtonPresentation.EPhase.Active
						: UIUnitOverviewCategoryButtonPresentation.EPhase.None);
			}
		}
	}
}