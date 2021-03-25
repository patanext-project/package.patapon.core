using System;
using PataNext.Client.Asset;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories
{
	public struct CategoryViewData
	{
		public Action    QuitView;
		public Transform CategoryTransform;
	}

	public class UnitCategoryViewBackend : UIBackend<CategoryViewData, UnitCategoryView>
	{
	}

	public class UnitCategoryView : UIPresentation<CategoryViewData>
	{
		protected override void OnDataUpdate(CategoryViewData data)
		{
			Backend.transform.SetParent(data.CategoryTransform, false);
			Backend.transform.SetAsFirstSibling();
		}

		public class DefaultRenderSystem : BaseRenderSystem<UnitCategoryView>
		{
			private bool triggerQuit;

			protected override void PrepareValues()
			{
				var inputSystem = EventSystem.current.GetComponent<InputSystemUIInputModule>();
				triggerQuit = inputSystem.cancel.action.ReadValue<float>() > 0.1f;
			}

			protected override void Render(UnitCategoryView definition)
			{
				if (triggerQuit && definition.Data.QuitView is { } quitAction)
					quitAction();
			}

			protected override void ClearValues()
			{
				triggerQuit = false;
			}
		}
	}
}