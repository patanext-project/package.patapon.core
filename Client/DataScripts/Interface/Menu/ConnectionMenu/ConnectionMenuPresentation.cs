using System;
using DataScripts.Interface.Menu.ServerList;
using DefaultNamespace;
using EcsComponents.MasterServer;
using package.patapon.core.Animation;
using Patapon.Client.PoolingSystems;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.External.Discord;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace DataScripts.Interface.Menu
{
	public class ConnectionMenuPresentation : RuntimeAssetPresentation<ConnectionMenuPresentation>
	{
		public TextMeshProUGUI connectionLabel;
	}

	public class ConnectionMenuBackend : RuntimeAssetBackend<ConnectionMenuPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ConnectionMenu : ComponentSystem, IMenu, IMenuCallbacks
	{
		public struct IsActive : IComponentData
		{
		}

		protected override void OnUpdate()
		{
		}

		public void OnMenuSet(TargetAnimation current)
		{
			Debug.Log("OnMenuSet");
			EntityManager.CreateEntity(typeof(IsActive));
		}

		public void OnMenuUnset(TargetAnimation current)
		{
			Debug.Log("OnMenuUnset");
			if (HasSingleton<IsActive>())
				EntityManager.DestroyEntity(GetSingletonEntity<IsActive>());
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ConnectionMenuPoolingSystem : PoolingSystem<ConnectionMenuBackend, ConnectionMenuPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("Menu")
			              .Folder("ConnectionMenu")
			              .GetFile("ConnectionMenu.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(ConnectionMenu.IsActive));
		}

		private Canvas m_Canvas;

		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
			{
				m_Canvas = CanvasUtility.Create(World, 0, "ConnectionMenu");
			}

			base.SpawnBackend(target);

			LastBackend.transform.SetParent(m_Canvas.transform, false);
			CanvasUtility.ExtendRectTransform(LastBackend.GetComponent<RectTransform>());
		}
	}
	
	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	[AlwaysUpdateSystem]
	public class ConnectionMenuRenderSystem : BaseRenderSystem<ConnectionMenuPresentation>
	{
		public bool IsDiscordConnecting, IsMasterServerConnecting, IsConnected;

		private P4ConnectToMasterServerFromDiscord m_ConnectSystem;
		private EntityQuery                        m_ConnectionOrPendingQuery;
		private EntityQuery                        m_CompletionQuery;

		public double NextMenuAt;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_ConnectSystem = World.GetOrCreateSystem<P4ConnectToMasterServerFromDiscord>();
			m_ConnectionOrPendingQuery = GetEntityQuery(new EntityQueryDesc
			{
				Any = new ComponentType[] {typeof(RequestUserLogin.Processing), typeof(RequestUserLogin.CompletionStatus)}
			});
			m_CompletionQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(RequestUserLogin.CompletionStatus)}
			});
		}

		protected override void PrepareValues()
		{
			if (!HasAnyDefinition)
				return;
			if (!(BaseDiscordSystem.Instance is P4DiscordSystem discordSystem))
				return;
			if (!discordSystem.HasSingleton<DiscordLocalUser>())
				return;

			if (!m_ConnectSystem.IsCurrentlyRequesting && m_ConnectionOrPendingQuery.CalculateEntityCount() == 0)
			{
				m_ConnectSystem.Request();
			}

			IsDiscordConnecting      = m_ConnectSystem.IsCurrentlyRequesting;
			IsMasterServerConnecting = !m_ConnectionOrPendingQuery.IsEmptyIgnoreFilter;
			IsConnected              = HasSingleton<ConnectedMasterServerClient>();
			if (IsConnected && NextMenuAt <= 0)
				NextMenuAt = Time.ElapsedTime + 0.5;
		}

		protected override void Render(ConnectionMenuPresentation definition)
		{
			if (IsConnected)
				definition.connectionLabel.text = "Connected";
			else
			{
				if (IsDiscordConnecting)
					definition.connectionLabel.text = "Connecting 1/2 ...";
				else if (IsMasterServerConnecting)
					definition.connectionLabel.text = "Connecting 2/2 ...";
				else
					definition.connectionLabel.text = string.Empty;
			}
		}

		protected override void ClearValues()
		{
			IsDiscordConnecting      = false;
			IsMasterServerConnecting = false;
			IsConnected              = false;

			if (NextMenuAt > 0 && NextMenuAt < Time.ElapsedTime)
			{
				NextMenuAt = -1;
				World.GetExistingSystem<ClientMenuSystem>()
				     .SetMenu<ServerListMenu>();
			}
		}
	}
}