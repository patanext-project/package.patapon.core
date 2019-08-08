using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using UnityEngine;

namespace package.patapon.core
{
	public class ClientAddOrthographicCamera : ComponentSystem
	{
		public struct Component : IComponentData
		{
		}

		private EntityQuery m_Query;

		protected override void OnCreate()
		{
			m_Query = GetEntityQuery(typeof(GameCamera), typeof(Camera), ComponentType.Exclude<Component>());
		}

		protected override void OnUpdate()
		{
			Entities.With(m_Query).ForEach((Entity e, Camera camera) =>
			{
				camera.orthographicSize = 10;
				camera.orthographic     = true;

				camera.transform.position = new Vector3(0, 0, -100);

				EntityManager.SetOrAddComponentData(e, new CameraTargetAnchor());
				EntityManager.SetOrAddComponentData(e, new AnchorOrthographicCameraData());
				EntityManager.SetOrAddComponentData(e, new AnchorOrthographicCameraOutput());
				EntityManager.SetOrAddComponentData(e, new Component());
			});
		}
	}
}