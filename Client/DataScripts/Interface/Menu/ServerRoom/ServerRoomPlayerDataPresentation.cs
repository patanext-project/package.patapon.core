using System;
using System.Collections.Generic;
using DefaultNamespace;
using Discord;
using package.stormiumteam.shared.ecs;
using Patapon.Client.PoolingSystems;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units.Statistics;
using Patapon.Server.GameModes;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.External.Discord;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace DataScripts.Interface.Menu.ServerRoom
{
	public class ServerRoomPlayerDataPresentation : RuntimeAssetPresentation<ServerRoomPlayerDataPresentation>
	{
		public struct IconData
		{
			public string kit;
			public Sprite icon;
		}

		public TextMeshProUGUI nameLabel;
		public Image           backgroundQuad;
		public Image           kitQuad;

		public Color unreadyColor;
		public Color readyColor;

		public IconData[] icons;

		public Sprite GetIconSprite(string kit)
		{
			foreach (var icon in icons)
				if (icon.kit == kit)
					return icon.icon;
			return null;
		}
	}

	public class ServerRoomPlayerDataBackend : RuntimeAssetBackend<ServerRoomPlayerDataPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;

		public Transform LastParent { get; set; }

		public LoginPhase Phase;
		public enum LoginPhase
		{
			None,
			MsId,
			DiscordName
		}

		public override void OnReset()
		{
			base.OnReset();
			LastParent = null;
			Phase = LoginPhase.None;
			Debug.Log("reset button");
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ServerRoomPlayerDataPoolingSystem : PoolingSystem<ServerRoomPlayerDataBackend, ServerRoomPlayerDataPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("Menu")
			              .Folder("ServerRoom")
			              .GetFile("ServerRoom_PlayerData.prefab");

		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(RectTransform), typeof(LayoutElement)};

		protected override EntityQuery GetQuery()
		{
			RequireSingletonForUpdate<ServerRoomMenu.IsActive>();
			return GetEntityQuery(typeof(GamePlayer), typeof(Relative<TeamDescription>));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			var rt = LastBackend.GetComponent<RectTransform>();
			CanvasUtility.ExtendRectTransform(rt);
			rt.sizeDelta = new Vector2(0, 60);

			var layout = LastBackend.GetComponent<LayoutElement>();
			layout.preferredHeight = 60;
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class ServerRoomPlayerDataRenderSystem : BaseRenderSystem<ServerRoomPlayerDataPresentation>
	{
		private EntityQuery                m_MenuQuery;
		private EntityQuery m_UnitQuery;

		private Dictionary<Entity, NativeString64> m_PlayerToKit = new Dictionary<Entity, NativeString64>();
		
		private ServerRoomMenuPresentation m_Menu;

		protected override void PrepareValues()
		{
			if (m_MenuQuery == null)
			{
				m_MenuQuery = GetEntityQuery(typeof(ServerRoomMenuPresentation));
				m_UnitQuery = GetEntityQuery(typeof(UnitFormation), typeof(UnitCurrentKit), typeof(Relative<PlayerDescription>));
			}

			if (!m_MenuQuery.IsEmptyIgnoreFilter)
				m_Menu = EntityManager.GetComponentObject<ServerRoomMenuPresentation>(m_MenuQuery.GetSingletonEntity());
			
			m_PlayerToKit.Clear();
			using (var kitArray = m_UnitQuery.ToComponentDataArray<UnitCurrentKit>(Allocator.TempJob))
			using (var relativeArray = m_UnitQuery.ToComponentDataArray<Relative<PlayerDescription>>(Allocator.TempJob))
			{
				for (var i = 0; i != kitArray.Length; i++)
					m_PlayerToKit[relativeArray[i].Target] = kitArray[i].Value;
			}
		}

		protected override void Render(ServerRoomPlayerDataPresentation definition)
		{
			var backend = (ServerRoomPlayerDataBackend) definition.Backend;
			var entity  = backend.DstEntity;
			if (!EntityManager.TryGetComponentData(entity, out Relative<TeamDescription> playerTeam) || !HasSingleton<MpVersusHeadOn>())
			{
				backend.LastParent = null;
				backend.transform.SetParent(null, false);
				return;
			}

			var gmData = GetSingleton<MpVersusHeadOn>();
			var index  = -1;
			if (playerTeam.Target == gmData.Team0)
				index = 0;
			else if (playerTeam.Target == gmData.Team1)
				index = 1;

			if (index == -1)
			{
				backend.LastParent = null;
				backend.transform.SetParent(null, false);
				return;
			}

			GamePlayer               gp;
			ResultGetUserAccountData getUserData;

			var column = m_Menu.teamColumns[index];
			if (backend.LastParent != column.board)
			{
				backend.LastParent = column.board;
				backend.transform.SetParent(column.board, false);

				backend.GetComponent<RectTransform>()
				       .sizeDelta = new Vector2(0, 60);

				/*backend.Phase = ServerRoomPlayerDataBackend.LoginPhase.None;
				if (EntityManager.TryGetComponentData(entity, out gp))
				{
					backend.Phase = ServerRoomPlayerDataBackend.LoginPhase.MsId;
					if (EntityManager.TryGetComponentData(entity, out getUserData))
					{
						var id = getUserData.Login.ToString().Replace("DISCORD_", string.Empty);
						if (long.TryParse(id, out var longId))
						{
							backend.Phase = ServerRoomPlayerDataBackend.LoginPhase.DiscordName;
							BaseDiscordSystem.Instance.GetUser(longId, (Result result, ref User user) => { definition.nameLabel.text = user.Username; });
						}
					}
					else
						definition.nameLabel.text = "MasterServer#" + gp.MasterServerId;
				}
				else
					definition.nameLabel.text = "Unknown";*/
			}

			/*if (backend.LastParent != null && backend.Phase != ServerRoomPlayerDataBackend.LoginPhase.DiscordName
			                               && EntityManager.TryGetComponentData(entity, out gp)
			                               && EntityManager.TryGetComponentData(entity, out getUserData))
			{
				var id = getUserData.Login.ToString().Replace("DISCORD_", string.Empty);
				if (long.TryParse(id, out var longId))
				{
					backend.Phase = ServerRoomPlayerDataBackend.LoginPhase.DiscordName;
					BaseDiscordSystem.Instance.GetUser(longId, (Result result, ref User user) => { definition.nameLabel.text = user.Username; });
				}
			}*/

			if (EntityManager.TryGetComponent(entity, out PlayerName pn))
				definition.nameLabel.text = pn.Value;

			var isReady = EntityManager.HasComponent<PreMatchPlayerIsReady>(entity);
			definition.backgroundQuad.color = isReady ? definition.readyColor : definition.unreadyColor;

			if (m_PlayerToKit.TryGetValue(entity, out var kit))
			{
				if (kit.Equals(UnitKnownTypes.Taterazay))
					definition.kitQuad.color = Color.yellow;
				if (kit.Equals(UnitKnownTypes.Yarida))
					definition.kitQuad.color = Color.blue;
			}
		}

		protected override void ClearValues()
		{

		}
	}
}