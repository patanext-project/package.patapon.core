using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameHost.Transports.enet;
using JetBrains.Annotations;
using PataNext.Client.Asset;
using PataNext.Client.Behaviors;
using PataNext.Client.DataScripts.Interface.Inventory;
using PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories;
using PataNext.Client.DataScripts.Interface.Menu.__Barracks.Controls;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks
{
	[Serializable]
	public class UnitOverviewCategoryRow : IDisposable
	{
		public string id;

		public string                name;
		public AssetPathSerializable spritePath;
		public AssetPathSerializable viewPath;

		private IContainer<UnitCategoryView> viewContainer;

		public IContainer<UnitCategoryView> ViewContainer
		{
			get
			{
				if (viewContainer is null)
					throw new NullReferenceException(nameof(ViewContainer));

				return viewContainer;
			}
		}

		private Task<Sprite> spriteTask;
	
		[CanBeNull]
		public Sprite Sprite
		{
			get
			{
				if (spritePath.asset == null)
					return null;
				
				if (spriteTask == null)
				{
					spriteTask = AssetManager.LoadAssetAsync<Sprite>(spritePath)
					                         .AsTask();
				}

				return spriteTask.IsCompleted ? spriteTask.Result : null;
			}
		}

		public void SetViewContainer(Func<UnitOverviewCategoryRow, IContainer<UnitCategoryView>> container)
		{
			if (!(viewContainer is null))
			{
				throw new InvalidOperationException(nameof(viewContainer));
			}
			viewContainer = container(this);
		}

		public void Dispose()
		{
			viewContainer?.Dispose();
		}
	}

	public class UIUnitOverviewCategoryRows : UIInventory<UnitOverviewCategoryRow>
	{
		public sealed class Backend : UIBackend<UnitOverviewCategoryRow, UIUnitOverviewCategoryRows>
		{}
		
		private Dictionary<int, UnitOverviewCategoryRow> itemMap = new Dictionary<int, UnitOverviewCategoryRow>();

		public string CurrentlySelectedId { get; set; }

		public override void OnBackendSet()
		{
			base.OnBackendSet();

			var root = "Interface/Menu/__Barracks";
			
			AddLast(new UnitOverviewCategoryRow
			{
				id = "equip", name = "Equipment", 
				viewPath = new AssetPathSerializable
				{
					asset = $"{root}/Controls/UnitEquipment",
					bundle = "st.pn"
				},
				spritePath = new AssetPathSerializable
				{
					asset = $"{root}/Icons/equipment_icon",
					bundle = "st.pn"
				}
			});
			AddLast(new UnitOverviewCategoryRow {id = "role_tree", name = "Role Tree"});
			AddLast(new UnitOverviewCategoryRow
			{
				id = "abilities", name = "Abilities",
				viewPath = new AssetPathSerializable
				{
					asset  = $"{root}/Controls/UnitAbility",
					bundle = "st.pn"
				},
				spritePath = new AssetPathSerializable
				{
					asset  = $"{root}/Icons/ability_icon",
					bundle = "st.pn"
				}
				
			});
			AddLast(new UnitOverviewCategoryRow {id = "miracles", name  = "Miracles"});
			AddLast(new UnitOverviewCategoryRow {id = "visuals", name   = "Customize Visuals"});
		}

		public override void OnReset()
		{
			foreach (var item in itemMap.Values)
				item.Dispose();
			
			base.OnReset();
		}

		protected override void OnAdded(UnitOverviewCategoryRow item, int2 position)
		{
			itemMap[position.x] = item;

			item.SetViewContainer(row => ContainerPool.FromPresentation<UnitCategoryViewBackend, UnitCategoryView>(row.viewPath));
			item.ViewContainer.Warm();
			item.ViewContainer.onCollectionUpdate.AddListener(collection =>
			{
				foreach (var view in collection)
				{
					view.Data = new CategoryViewData
					{
						QuitView          = () => CurrentlySelectedId = null, 
						CategoryTransform = GetAt(position).transform
					};
				}
			});
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
					presentation.SetIcon(item.Sprite);
					presentation.SetName(item.name);
				}
				else
				{
					presentation.SetIcon(null);
					presentation.SetName(null);
				}

				var phase = CurrentlySelectedId == item.id && item.id != null
					? UIUnitOverviewCategoryButtonPresentation.EPhase.Active
					: math.all(new int2(x, y) == Cursor)
						? UIUnitOverviewCategoryButtonPresentation.EPhase.Selected
						: UIUnitOverviewCategoryButtonPresentation.EPhase.None;
				
				presentation.SetPhase(phase);

				if (phase == UIUnitOverviewCategoryButtonPresentation.EPhase.Active)
				{
					item.ViewContainer.SetSize(1);
				}
				else
				{
					item.ViewContainer.SetSize(0);
					go.SetActive(string.IsNullOrEmpty(CurrentlySelectedId));
				}
			}
		}
	}
}