using Unity.Entities;
using UnityEngine;

namespace Graphics.Camera
{
	public class LocalCameraTest : ComponentSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			EntityManager.CreateEntity(typeof(Local), typeof(LocalCameraState));
		}

		protected override void OnUpdate()
		{
			Entities.WithAll<Local>().ForEach((ref LocalCameraState state) => { state.Data.Offset.pos.x += Input.GetAxis("Horizontal"); });
		}

		public struct Local : IComponentData
		{
		}
	}
}