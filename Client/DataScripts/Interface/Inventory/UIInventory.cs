using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Inventory
{
	public abstract class UIInventory<TItem> : MonoBehaviour
	{
		[SerializeField]
		private GameObject prefabToSpawn;

		[SerializeField]
		private int2 displayedSizeTable = new int2(4);
		
		[SerializeField]
		private int2 fixedSizeTable = new int2(4);

		public Vector2   Size;
		public Transform ItemContainer;
		
		protected GameObject[,] spawnedColumn;
		
		private int2          cursor;
		private int2          view;

		protected abstract void OnSpawnGameObject(GameObject go);

		private void OnEnable()
		{
			spawnedColumn = new GameObject[displayedSizeTable.x, displayedSizeTable.y];
			for (var x = 0; x < displayedSizeTable.x; x++)
			for (var y = 0; y < displayedSizeTable.y; y++)
			{
				var clone = Instantiate(prefabToSpawn, ItemContainer == null ? transform : ItemContainer);
				clone.name = $"Row={x} Column={y}";
				if (clone.TryGetComponent(out RectTransform rt))
					rt.anchoredPosition = new Vector2(x * Size.x, y * Size.y);

				OnSpawnGameObject(clone);
				
				clone.SetActive(true);

				spawnedColumn[x, y] = clone;
			}
		}

		public GameObject GetAt(int2 position)
		{
			return spawnedColumn[position.x, position.y];
		}

		[field: SerializeField]
		public int2 ItemRange { get; set; }

		public int2 MaxItemPosition { get; set; }

		public int2 Cursor
		{
			get => cursor;
			set
			{
				cursor = value;
				if (cursor.x - displayedSizeTable.x >= 0) view.x++;
				if (cursor.x < 0)
				{
					cursor.x = fixedSizeTable.x - 1;
					cursor.y--;
					view.x--;
				}

				if (view.x < 0)
				{
					view.x = math.clamp((ItemRange.x > 0 ? ItemRange.x / displayedSizeTable.x : 0) - 1, 0, fixedSizeTable.x);
				}
				
				if (ViewAsTable.x > math.max(fixedSizeTable.x, ItemRange.x))
				{
					cursor.x = 0;
					cursor.y++;
					
					view.x = 0;
				}

				if (cursor.y - displayedSizeTable.y >= 0)
				{
					// Make sure that our Y cursor is at the fixed size
					cursor.y = fixedSizeTable.y - 1;
					view.y++;
				}

				var hasGoneIntoNegativeY = false;
				if (cursor.y < 0)
				{
					cursor.y = 0;
					view.y--;

					hasGoneIntoNegativeY = true;
				}

				if (view.y < 0)
				{
					view.y = math.max(ItemRange.y + 1, fixedSizeTable.y) - displayedSizeTable.y;
					if (hasGoneIntoNegativeY)
						cursor.y = fixedSizeTable.y - 1;
				}

				if (ViewAsTable.y - 1 > math.max(fixedSizeTable.y - 1, ItemRange.y))
				{
					cursor.y = 0;
					view.y   = 0;
				}
			}
		}

		public int2 AbsoluteCursor
		{
			get { return cursor + view; }
			set
			{
				var newView = value / displayedSizeTable;
				view = newView;

				Cursor = value % displayedSizeTable;
			}
		}

		public int2 View
		{
			get => view;
			set => view = value;
		}

		public int2 ViewAsTable => view + displayedSizeTable;

		protected abstract void OnAdded(TItem item, int2 position);

		public virtual void Clear()
		{
			ItemRange = default;
			Cursor    = Cursor;
		}

		public void Add(TItem item, int2 position)
		{
			ItemRange = math.max(ItemRange, position);
			OnAdded(item, position);
		}

		public abstract TItem Get(int2 position);

		public void AddLast(TItem item)
		{
			var position = MaxItemPosition;			
			Add(item, position);
			
			position.x++;
			if (position.x > fixedSizeTable.x)
			{
				position.x = 0;
				position.y++;
			}

			MaxItemPosition = position;
		}
	}
}