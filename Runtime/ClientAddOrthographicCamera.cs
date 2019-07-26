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

				PostUpdateCommands.AddComponent(e, new CameraTargetAnchor());
				PostUpdateCommands.AddComponent(e, new AnchorOrthographicCameraData());
				PostUpdateCommands.AddComponent(e, new AnchorOrthographicCameraOutput());
				PostUpdateCommands.AddComponent(e, new Component());
			});
		}
	}
}