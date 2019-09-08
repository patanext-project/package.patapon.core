using package.patapon.core;
using package.stormiumteam.shared;
using Patapon4TLB.Default;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Mathematics;
using Revolution.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateBefore(typeof(ClientPresentationTransformSystemGroup))]
	public class TestUpdateCameraSystem : ComponentSystem
	{
		public int OrthographicSize = 7;
		public bool Seek;

		private void ForEachCameraState(ref ServerCameraState cameraState)
		{
			if (cameraState.Target == default)
				return;

			UnitDirection direction = default;
			
			var translation = EntityManager.GetComponentData<Translation>(cameraState.Target);
			if (EntityManager.HasComponent<UnitDirection>(cameraState.Target))
			{
				direction = EntityManager.GetComponentData<UnitDirection>(cameraState.Target);
			}

			var orthoSize = OrthographicSize;
			var target = cameraState.Target;
			Entities.WithAll<UnitDescription>().ForEach((Entity e, ref Translation otherTr) =>
			{
				if (e == target || Seek)
					return;

				if (math.abs(otherTr.Value.x - translation.Value.x) < 20)
				{
					orthoSize += 2;
					Seek = true;
				}
			});

			translation.Value.x += World.GetExistingSystem<CameraInputSystem>().CurrentPanning * (orthoSize + 2.5f * direction.Value);
			translation.Value.x += orthoSize * 0.25f * direction.Value;

			var previousTarget = EntityManager.GetComponentData<CameraTargetPosition>(m_CameraTarget).Value.x;
			var result         = math.lerp(previousTarget, translation.Value.x, Time.deltaTime * 2.5f);
			
			if (math.isnan(result) || math.abs(result) > 4000.0f)
			{
				result = 0;
			}

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

			Entities.WithAll<GamePlayerLocalTag>().ForEach(m_ForEach);
			camera.orthographicSize = math.lerp(camera.orthographicSize, OrthographicSize + (Seek ? 2 : 0), Time.deltaTime * 2.5f);

			World.GetOrCreateSystem<AnchorOrthographicCameraSystem>().Update();

			Seek = false;
		}
	}
}