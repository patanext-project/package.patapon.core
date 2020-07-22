using System;
using DataScripts.Interface.Menu.ServerRoom;
using Patapon.Client.PoolingSystems;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace DataScripts.Interface.Menu.ServerList
{
	public class ServerListMenuSelectionPresentation : RuntimeAssetPresentation<ServerListMenuSelectionPresentation>
	{
		public TextMeshProUGUI nameLabel;
		public Button          button;

		public bool IsButtonActive { get; set; }

		private void OnEnable()
		{
			button.onClick.AddListener(OnClick);
		}

		public override void OnBackendSet()
		{
			base.OnBackendSet();
			GetComponent<RectTransform>().anchoredPosition = default;
		}

		private void OnDisable()
		{
			button.onClick.RemoveAllListeners();
		}

		private void OnClick()
		{
			IsButtonActive = true;
		}
	}

	public class ServerListMenuSelectionBackend : RuntimeAssetBackend<ServerListMenuSelectionPresentation>
	{
		public Transform m_LastRoot;

		public override bool PresentationWorldTransformStayOnSpawn => false;
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ServerListMenuSelectionPoolingSystem : PoolingSystem<ServerListMenuSelectionBackend, ServerListMenuSelectionPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("Menu")
			              .Folder("ServerList")
			              .GetFile("Selection.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			RequireSingletonForUpdate<ServerListMenu.IsActive>();
			
			return GetEntityQuery(typeof(FoundServer));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			var rt = LastBackend.GetComponent<RectTransform>();
			CanvasUtility.ExtendRectTransform(rt);
			rt.anchorMin = new Vector2(0, 1);
			rt.sizeDelta = new Vector2(0, 60);
			rt.pivot     = new Vector2(0.5f, 1);
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class ServerListMenuSelectionRenderSystem : BaseRenderSystem<ServerListMenuSelectionPresentation>
	{
		private EntityQuery m_InterfaceQuery;
		private Transform   m_ListRoot;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_InterfaceQuery = GetEntityQuery(typeof(ServerListMenuPresentation));
		}

		protected override void PrepareValues()
		{
			if (m_InterfaceQuery.CalculateEntityCount() > 0)
			{
				m_ListRoot = EntityManager.GetComponentObject<ServerListMenuPresentation>(m_InterfaceQuery.GetSingletonEntity())
				                          .selectionRoot;
			}
		}

		protected override void Render(ServerListMenuSelectionPresentation definition)
		{
			var backend      = (ServerListMenuSelectionBackend) definition.Backend;
			var targetEntity = backend.DstEntity;
			if (backend.m_LastRoot != m_ListRoot)
			{
				backend.m_LastRoot = m_ListRoot;
				backend.transform.SetParent(m_ListRoot, false);
			}

			if (!EntityManager.Exists(targetEntity))
				return;

			var server = EntityManager.GetComponentData<FoundServer>(targetEntity);

			definition.nameLabel.text = server.Name;

			var rt = backend.GetComponent<RectTransform>();
			rt.anchoredPosition = new Vector2(0, -(rt.sizeDelta.y * server.Index));

			if (definition.IsButtonActive)
			{
				definition.IsButtonActive = false;

				var ent = EntityManager.CreateEntity(typeof(RequestConnectToServer));
				EntityManager.SetComponentData(ent, new RequestConnectToServer {ServerUserId = server.Id});

				World.GetExistingSystem<ClientMenuSystem>()
				     .SetMenu<ServerRoomMenu>();
			}
		}

		protected override void ClearValues()
		{

		}
	}
}