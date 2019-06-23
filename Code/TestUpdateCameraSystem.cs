using package.patapon.core;
using package.stormiumteam.shared;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Core
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateBefore(typeof(ClientPresentationTransformSystemGroup))]
	public class TestUpdateCameraSystem : ComponentSystem
	{
		private void ForEachCameraState(ref ServerCameraState cameraState)
		{
		}

		private Entity m_CameraTarget;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_CameraTarget = EntityManager.CreateEntity(typeof(CameraTargetData), typeof(CameraTargetAnchor), typeof(CameraTargetPosition));

			EntityManager.SetComponentData(m_CameraTarget, new CameraTargetAnchor(AnchorType.Screen, new float2(0, 0.7f)));
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((ref CameraTargetAnchor anchor) =>
			{
				var prev = anchor.Value.y;
				
				if (Input.GetKeyDown(KeyCode.PageUp))
					anchor.Value.y += 0.1f;
				if (Input.GetKeyDown(KeyCode.PageDown))
					anchor.Value.y -= 0.1f;
				
				if (prev != anchor.Value.y)
					Debug.Log("New: " + anchor.Value.y);
			});
			
			Entities.ForEach((Camera camera) =>
			{
				if (Input.GetKeyDown(KeyCode.KeypadPlus))
					camera.orthographicSize += 1;
				if (Input.GetKeyDown(KeyCode.KeypadMinus))
					camera.orthographicSize -= 1;
			});

			World.GetOrCreateSystem<AnchorOrthographicCameraSystem>().Update();
		}
	}
}