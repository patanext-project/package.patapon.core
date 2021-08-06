using System;
using PataNext.Client.Asset;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Categories
{
	public struct ModuleViewData
	{
		public Action    QuitView;
		public Transform CategoryTransform;

		public Transform RootTransform;
		public Entity    Entity;
	}

	public class UIOverviewModuleBackend : UIBackend<ModuleViewData, UIOverviewModuleBase>
	{
		
	}
	
	public abstract class UIOverviewModuleBase : UIPresentation<ModuleViewData>
	{
		public bool IsActive { get; internal set; }

		public virtual bool OnActiveHideUnitModule => false;
		
		protected override void OnDataUpdate(ModuleViewData data)
		{
			Backend.transform.SetParent(data.CategoryTransform, false);

			var ourRt     = GetComponent<RectTransform>();
			var backendRt = Backend.GetComponent<RectTransform>();
			backendRt.anchorMin = ourRt.anchorMin;
			backendRt.anchorMax = ourRt.anchorMax;
			backendRt.sizeDelta = ourRt.sizeDelta;
		}

		public override void OnReset()
		{
			OnSetSelected(false, true);
			
			base.OnReset();
		}

		public abstract bool Enter();
		public abstract void ForceExit();

		private bool isSelected;

		[SerializeField]
		private GameObject[] selectedGameObjects;

		public bool SetSelected(bool selected)
		{
			if (isSelected != selected)
			{
				isSelected = selected;
				OnSetSelected(selected, true);
				return true;
			}

			OnSetSelected(selected, false);
			return false;
		}

		protected virtual void OnSetSelected(bool isSelected, bool isStateUpdate)
		{
			Console.WriteLine(isSelected + ", " + isStateUpdate);
			if (isStateUpdate)
				foreach (var go in selectedGameObjects)
					go.SetActive(isSelected);
		}
	}
}