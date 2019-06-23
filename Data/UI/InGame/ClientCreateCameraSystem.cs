using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.UI.InGame
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class ClientCreateCameraSystem : GameBaseSystem
	{
		private Camera m_Camera;
		private bool   m_PreviousState;

		protected override void OnCreate()
		{
			base.OnCreate();

			var previousActiveWorld = World.Active;
			World.Active = World;
			var gameObject = new GameObject($"(World: {World.Name}) GameCamera",
				typeof(Camera),
				typeof(GameCamera),
				typeof(AudioListener),
				typeof(GameObjectEntity));
			m_Camera = gameObject.GetComponent<Camera>();
			World.Active = previousActiveWorld;

			gameObject.SetActive(false);
		}

		protected override void OnUpdate()
		{
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (m_Camera != null)
				GameObject.Destroy(m_Camera.gameObject);
			m_Camera = null;
		}

		internal void InternalSetActive(bool state)
		{
			if (state == m_PreviousState)
				return;

			m_PreviousState = state;

			var previousActiveWorld = World.Active;
			World.Active = World;
			m_Camera.gameObject.SetActive(state);
			World.Active = previousActiveWorld;
		}
	}

	[UpdateBefore(typeof(TickClientPresentationSystem))]
	[AlwaysUpdateSystem]
	public class ManageClientCameraSystem : GameBaseSystem
	{
		private EntityQuery m_GameCameraQuery;
		private Camera      m_Camera;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameCameraQuery = GetEntityQuery(typeof(GameCamera));
		}

		protected override void OnUpdate()
		{
			if (m_Camera == null && m_GameCameraQuery.CalculateLength() > 0)
			{
				var entity = m_GameCameraQuery.GetSingletonEntity();
				m_Camera = EntityManager.GetComponentObject<Camera>(entity);
			}

			if (ClientServerBootstrap.clientWorld == null
			    || ClientServerBootstrap.clientWorld.Length <= 0)
			{
				if (m_Camera != null)
				{
					m_Camera.gameObject.SetActive(true);
				}

				return;
			}

			foreach (var clientWorld in ClientServerBootstrap.clientWorld)
			{
				var presentationSystemGroup = clientWorld.GetExistingSystem<ClientPresentationSystemGroup>();
				var cameraSystem            = clientWorld.GetExistingSystem<ClientCreateCameraSystem>();
				cameraSystem.InternalSetActive(presentationSystemGroup.Enabled);
			}

			if (m_Camera != null)
			{
				m_Camera.gameObject.SetActive(false);
			}
		}
	}
}