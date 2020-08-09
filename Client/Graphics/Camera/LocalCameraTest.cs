using StormiumTeam.GameBase._Camera;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Graphics.Camera
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
			var dt = Time.DeltaTime;
			Entities.WithAll<Local>().ForEach((ref LocalCameraState state) =>
			{
				state.Data.Offset.pos.x += Input.GetAxis("Horizontal") * dt;
			});
		}

		public struct Local : IComponentData
		{
		}
	}
}