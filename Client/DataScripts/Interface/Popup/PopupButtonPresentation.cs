using System;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Core.DOTSxUI.Components;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.Popup
{
	public class PopupButtonPresentation : RuntimeAssetPresentation<PopupButtonPresentation>
	{
		public bool PendingClickEvent { get; set; }
		
		public Button button;
		public TextMeshProUGUI label;

		private void OnEnable()
		{
			button.onClick.AddListener(() => PendingClickEvent = true);
		}

		private void OnDisable()
		{
			button.onClick.RemoveAllListeners();
		}
	}

	public class PopupButtonBackend : RuntimeAssetBackend<PopupButtonPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;

		public Transform LastParent { get; set; }

		public override void OnReset()
		{
			base.OnReset();
			LastParent = null;
		}

		public override void OnPresentationSet()
		{
			base.OnPresentationSet();

			if (Presentation.TryGetComponent(out LayoutElement presentationLayout))
			{
				var layout = GetComponent<LayoutElement>();
				layout.preferredHeight = presentationLayout.preferredHeight;
				layout.minWidth        = presentationLayout.minWidth;
				layout.minHeight       = presentationLayout.minHeight;
				layout.flexibleWidth   = presentationLayout.flexibleWidth;
			}
		}

		public void ComputeLayout()
		{
			if (Presentation.TryGetComponent(out LayoutElement presentationLayout))
			{
				var layout = GetComponent<LayoutElement>();
				layout.preferredHeight = presentationLayout.preferredHeight;
				layout.minWidth        = presentationLayout.minWidth + Presentation.label.preferredWidth + 5;
				layout.minHeight       = presentationLayout.minHeight;
				layout.flexibleWidth   = presentationLayout.flexibleWidth;
			}
		}
	}
	
	public class PopupButtonPoolingSystem : PoolingSystem<PopupButtonBackend, PopupButtonPresentation, PopupButtonPoolingSystem.CheckValidity>
	{
		public struct CheckValidity : ICheckValidity
		{
			[ReadOnly]
			public ComponentDataFromEntity<Disabled> DisabledFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<Relative<PopupDescription>> PopupFromEntity;
			
			public void OnSetup(ComponentSystemBase system)
			{
				DisabledFromEntity = system.GetComponentDataFromEntity<Disabled>(true);
				PopupFromEntity = system.GetComponentDataFromEntity<Relative<PopupDescription>>(true);
			}

			public bool IsValid(Entity target)
			{
				return !DisabledFromEntity.HasComponent(target) && !DisabledFromEntity.HasComponent(PopupFromEntity[target].Target);
			}
		}
		
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("Popup")
			              .GetFile("DarkStylePopupButton.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform), typeof(LayoutElement)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(UIButton), typeof(UIButtonText), typeof(Relative<PopupDescription>), ComponentType.Exclude<Disabled>());
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			var rt = LastBackend.GetComponent<RectTransform>();
			CanvasUtility.ExtendRectTransform(rt);

			var layout = LastBackend.GetComponent<LayoutElement>();
			layout.preferredHeight = 60;
			layout.minWidth        = 125;
			layout.minHeight       = 60;
			layout.flexibleWidth   = 1;
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class PopupButtonRenderSystem : BaseRenderSystem<PopupButtonPresentation>
	{
		protected override void PrepareValues()
		{
			
		}

		protected override void Render(PopupButtonPresentation definition)
		{
			var backend = (PopupButtonBackend) definition.Backend;
			var entity  = backend.DstEntity;

			if (!EntityManager.TryGetComponentData(entity, out Relative<PopupDescription> relativePopup)
			    || !EntityManager.Exists(relativePopup.Target))
			{
				Debug.Log("no popup relative or does not exist: " + relativePopup.Target + ", from: " + entity);
				return;
			}

			if (!EntityManager.TryGetComponent(relativePopup.Target, out UIPopup popupData) && popupData.Board == null)
			{
				Debug.Log("no popup board");
				return;
			}

			if (backend.LastParent != popupData.Board)
			{
				backend.LastParent = popupData.Board;
				backend.transform.SetParent(popupData.Board, false);
			}

			var targetText = EntityManager.GetComponentData<UIButtonText>(entity).Value;
			if (definition.label.text != targetText)
			{
				definition.label.SetText(targetText);
			}

			if (definition.PendingClickEvent)
			{
				definition.PendingClickEvent = false;
				EntityManager.AddComponent(entity, typeof(UIButton.ClickedEvent));
			}

			if (EntityManager.TryGetComponentData(entity, out UIGridPosition gridPosition))
				backend.transform.SetSiblingIndex(gridPosition.Value.y + 1);

			if ((EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy 
			                                                           || !EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().IsInteractable())
			    && EntityManager.HasComponent<UIFirstSelected>(entity))
			{
				EventSystem.current.SetSelectedGameObject(null);
				EventSystem.current.SetSelectedGameObject(definition.gameObject);
			}

			backend.ComputeLayout();
		}

		protected override void ClearValues()
		{
			
		}
	}
}