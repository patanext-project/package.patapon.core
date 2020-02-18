using System;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace package.patapon.core
{
	[UpdateInGroup(typeof(OrderGroup.Presentation.UpdateCamera))]
	[UpdateAfter(typeof(SynchronizeCameraStateSystem))]
	// Update after animators
	public class AnchorOrthographicCameraSystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			Entities.ForEach((Camera camera, ref Translation tr, ref AnchorOrthographicCameraData data, in ComputedCameraState computed) =>
			{
				camera.orthographicSize = computed.Focus;

				data.Height = camera.orthographicSize;
				data.Width  = camera.aspect * data.Height;
			}).WithoutBurst().Run();

			var targetAnchorFromEntity = GetComponentDataFromEntity<CameraTargetAnchor>(true);
			inputDeps = Entities.ForEach((ref Translation translation, in AnchorOrthographicCameraData cameraData, in ComputedCameraState computed) =>
			{
				if (!targetAnchorFromEntity.Exists(computed.StateData.Target))
					return;

				var anchor    = targetAnchorFromEntity[computed.StateData.Target];
				var camSize   = new float2(cameraData.Width, cameraData.Height);
				var anchorPos = new float2(anchor.Value.x, anchor.Value.y);
				if (anchor.Type == AnchorType.World)
					// todo: bla bla... world to screen point...
					throw new NotImplementedException();

				var left = math.float2(1, 0) * (anchorPos.x * camSize.x);
				var up   = math.float2(0, 1) * (anchorPos.y * camSize.y);

				translation.Value = math.float3(translation.Value.xy + left + up, -100);
			}).WithReadOnly(targetAnchorFromEntity).Schedule(inputDeps);

			return inputDeps;
		}
	}
}