using System;
using DataScripts.Interface.Menu.UIECS;
using DefaultNamespace;
using package.stormiumteam.shared.ecs;
using Patapon.Client.PoolingSystems;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataScripts.Interface.Menu.ServerRoom
{
	public struct ServerRoomUIButton : IComponentData
	{
	}

	public class ServerRoomButtonPresentation : RuntimeAssetPresentation<ServerRoomButtonPresentation>
	{
		public TextMeshProUGUI label;

		[NonSerialized] public bool HasPendingClickEvent;

		private void OnEnable()
		{
			GetComponent<Button>().onClick.AddListener(() => HasPendingClickEvent = true);
		}

		public override void OnBackendSet()
		{
			base.OnBackendSet();
			HasPendingClickEvent = false;
		}

		private void OnDisable()
		{
			GetComponent<Button>().onClick.RemoveAllListeners();
		}
	}

	public class ServerRoomButtonBackend : RuntimeAssetBackend<ServerRoomButtonPresentation>
	{
		[NonSerialized] public Transform LastParent;

		public override bool PresentationWorldTransformStayOnSpawn => false;

		public override void OnReset()
		{
			base.OnReset();
			LastParent = null;
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ServerRoomButtonPoolingSystem : PoolingSystem<ServerRoomButtonBackend, ServerRoomMenuPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("Menu")
			              .Folder("ServerRoom")
			              .GetFile("ServerRoom_Button.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(ServerRoomUIButton), typeof(UIButton), typeof(UIGridPosition));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			CanvasUtility.ExtendRectTransform(LastBackend.GetComponent<RectTransform>());
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class ServerRoomButtonRenderSystem : BaseRenderSystem<ServerRoomButtonPresentation>
	{
		private EntityQuery                m_MenuQuery;
		private ServerRoomMenuPresentation m_Menu;

		protected override void PrepareValues()
		{
			if (m_MenuQuery == null)
			{
				m_MenuQuery = GetEntityQuery(typeof(ServerRoomMenuPresentation));
			}

			if (!m_MenuQuery.IsEmptyIgnoreFilter)
				m_Menu = EntityManager.GetComponentObject<ServerRoomMenuPresentation>(m_MenuQuery.GetSingletonEntity());
		}

		protected override void Render(ServerRoomButtonPresentation definition)
		{
			var backend = (ServerRoomButtonBackend) definition.Backend;
			var entity  = definition.Backend.DstEntity;

			EntityManager.TryGetComponentData(entity, out UIGridPosition gridPosition);
			EntityManager.TryGetComponent(entity, out UIButtonText label);

			if (backend.LastParent != m_Menu.buttonBoard)
			{
				backend.LastParent = m_Menu.buttonBoard;
				backend.transform.SetParent(m_Menu.buttonBoard, false);
				
				backend.transform.SetSiblingIndex(gridPosition.Value.y);
			}

			if ((EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
			    && EntityManager.HasComponent<UIFirstSelected>(entity))
			{
				EventSystem.current.SetSelectedGameObject(null);
				EventSystem.current.SetSelectedGameObject(definition.gameObject);
			}

			if (definition.HasPendingClickEvent)
			{
				definition.HasPendingClickEvent = false;
				EntityManager.AddComponentData(entity, new UIButton.ClickedEvent());
			}

			definition.label.SetText(label.Value);
		}

		protected override void ClearValues()
		{
		}
	}
}