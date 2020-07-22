using System;
using DataScripts.Interface.Menu.TemporaryMenu;
using package.patapon.core.Animation;
using Patapon.Client.PoolingSystems;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine;

namespace DataScripts.Interface.Menu.ServerList
{
	public class ServerListMenuPresentation : RuntimeAssetPresentation<ServerListMenuPresentation>
	{
		public RectTransform selectionRoot;
		public ServerListGoBackButtonPresentation goBackButton;
	}

	public class ServerListMenuBackend : RuntimeAssetBackend<ServerListMenuPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;
	}

	public class ServerListMenu : ComponentSystem, IMenu, IMenuCallbacks
	{
		public struct IsActive : IComponentData
		{
		}

		protected override void OnUpdate()
		{

		}

		public void OnMenuSet(TargetAnimation current)
		{
			EntityManager.CreateEntity(typeof(IsActive));
		}

		public void OnMenuUnset(TargetAnimation current)
		{
			if (HasSingleton<IsActive>())
				EntityManager.DestroyEntity(GetSingletonEntity<IsActive>());
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ServerListMenuPoolingSystem : PoolingSystem<ServerListMenuBackend, ServerListMenuPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("Menu")
			              .Folder("ServerList")
			              .GetFile("ServerList.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(ServerListMenu.IsActive));
		}

		private Canvas m_Canvas;

		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
			{
				m_Canvas = CanvasUtility.Create(World, 0, "ServerList");
				CanvasUtility.DisableInteractionOnActivePopup(World, m_Canvas);
			}

			base.SpawnBackend(target);
			LastBackend.transform.SetParent(m_Canvas.transform, false);

			CanvasUtility.ExtendRectTransform(LastBackend.GetComponent<RectTransform>());
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ServerListMenuRenderSystem : BaseRenderSystem<ServerListMenuPresentation>
	{
		protected override void PrepareValues()
		{
			
		}

		protected override void Render(ServerListMenuPresentation definition)
		{
			if (definition.goBackButton.HasPendingClickEvent)
			{
				definition.goBackButton.HasPendingClickEvent = false;

				World.GetExistingSystem<ClientMenuSystem>()
				     .SetMenu<TempMenu>();
			}
		}

		protected override void ClearValues()
		{
			
		}
	}
	
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[AlwaysUpdateSystem]
	public class RefreshServerList : ComponentSystem
	{
		private EntityQuery m_PendingQuery;
		private EntityQuery m_CompletedQuery;

		private float m_Delay;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_PendingQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(RequestServerList)}
			});
			m_CompletedQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(RequestServerList.CompletionStatus)}
			});
		}

		protected override void OnUpdate()
		{
			if (!m_PendingQuery.IsEmptyIgnoreFilter)
				return;

			if (m_Delay > 0)
			{
				m_Delay -= Time.DeltaTime;
				return;
			}
			
			EntityManager.DestroyEntity(m_CompletedQuery);
			EntityManager.CreateEntity(typeof(RequestServerList));

			m_Delay = 2f;
		}
	}
}