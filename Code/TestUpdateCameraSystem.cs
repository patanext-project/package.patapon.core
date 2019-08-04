using package.patapon.core;
using package.stormiumteam.shared;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateBefore(typeof(ClientPresentationTransformSystemGroup))]
	public class TestUpdateCameraSystem : ComponentSystem
	{
		public int OrthographicSize = 8;

		private void ForEachCameraState(ref ServerCameraState cameraState)
		{
			if (cameraState.Target == default)
				return;
			
			var translation = EntityManager.GetComponentData<Translation>(cameraState.Target);

			translation.Value.x += World.GetExistingSystem<CameraInputSystem>().CurrentPanning * (OrthographicSize + 2.5f);
			translation.Value.x += OrthographicSize * 0.25f;

			var previousTarget = EntityManager.GetComponentData<CameraTargetPosition>(m_CameraTarget).Value.x;
			var result         = math.lerp(previousTarget, translation.Value.x, Time.deltaTime * 2.5f);

			EntityManager.SetComponentData(m_CameraTarget, new CameraTargetPosition
			{
				Value = new float3(result, 0, 0)
			});
		}

		private EntityQueryBuilder.F_D<ServerCameraState> m_ForEach;
		private EntityQuery                               m_CameraQuery;
		private Entity                                    m_CameraTarget;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ForEach = ForEachCameraState;

			m_CameraQuery  = GetEntityQuery(typeof(GameCamera));
			m_CameraTarget = EntityManager.CreateEntity(typeof(CameraTargetData), typeof(CameraTargetAnchor), typeof(CameraTargetPosition));

			EntityManager.SetComponentData(m_CameraTarget, new CameraTargetAnchor(AnchorType.Screen, new float2(0, 0.7f)));
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((ref CameraTargetAnchor anchor) =>
			{
				if (Input.GetKeyDown(KeyCode.PageUp))
					anchor.Value.y += 0.1f;
				if (Input.GetKeyDown(KeyCode.PageDown))
					anchor.Value.y -= 0.1f;
			});

			var gameCamera = m_CameraQuery.GetSingletonEntity();
			var camera     = EntityManager.GetComponentObject<Camera>(gameCamera);

			if (Input.GetKeyDown(KeyCode.KeypadPlus))
				OrthographicSize++;
			if (Input.GetKeyDown(KeyCode.KeypadMinus))
				OrthographicSize--;

			camera.orthographicSize = math.lerp(camera.orthographicSize, OrthographicSize, Time.deltaTime * 5);

			Entities.WithAll<GamePlayerLocalTag>().ForEach(m_ForEach);

			World.GetOrCreateSystem<AnchorOrthographicCameraSystem>().Update();
		}
	}
}