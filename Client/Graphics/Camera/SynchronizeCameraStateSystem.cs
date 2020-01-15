using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using StormiumTeam.GameBase.Misc;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace package.patapon.core
{
	public struct CurrentCameraStateSource : IComponentData
	{
		public Entity Value;
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.UpdateCamera))]
	[AlwaysSynchronizeSystem]
	[AlwaysUpdateSystem]
	public class SynchronizeCameraStateSystem : JobComponentSystem
	{
		private EntityQuery m_CameraWithoutUpdateComp;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_CameraWithoutUpdateComp = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(GameCamera)},
				None = new ComponentType[] {typeof(SystemData)}
			});
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (!m_CameraWithoutUpdateComp.IsEmptyIgnoreFilter) EntityManager.AddComponent(m_CameraWithoutUpdateComp, typeof(SystemData));

			Entity defaultCamera                             = default;
			if (HasSingleton<DefaultCamera>()) defaultCamera = GetSingletonEntity<DefaultCamera>();

			Entities
				.WithAll<GameCamera>()
				.ForEach((Entity entity, ref SystemData update) =>
				{
					update.Mode     = CameraMode.Default;
					update.Priority = int.MinValue;
				})
				.Run();

			var cameraTargetFromEntity = GetComponentDataFromEntity<CameraStateCameraTarget>(true);
			var updateFromEntity       = GetComponentDataFromEntity<SystemData>();
			Entities
				.ForEach((Entity entity, in LocalCameraState cameraState) =>
				{
					var camera                                        = defaultCamera;
					if (cameraTargetFromEntity.Exists(entity)) camera = cameraTargetFromEntity[entity].Value;

					var updater = updateFromEntity.GetUpdater(camera);
					if (!updater.Out(out var cameraStateUpdate).possess)
						return;

					if (cameraStateUpdate.Mode > cameraState.Mode)
						return;

					cameraStateUpdate.Mode     = cameraState.Mode;
					cameraStateUpdate.Priority = 0;

					cameraStateUpdate.StateEntity = entity;
					cameraStateUpdate.StateData   = cameraState.Data;

					updater.Update(cameraStateUpdate);
				})
				.WithName("Check_LocalCameraState_AndReplaceSystemData")
				.WithReadOnly(cameraTargetFromEntity)
				.Run(); // use ScheduleSingle() when it will be out.

			Entities
				.WithAll<GamePlayerLocalTag>()
				.ForEach((Entity entity, in ServerCameraState cameraState) =>
				{
					var camera                                        = defaultCamera;
					if (cameraTargetFromEntity.Exists(entity)) camera = cameraTargetFromEntity[entity].Value;

					var updater = updateFromEntity.GetUpdater(camera);
					if (!updater.Out(out var cameraStateUpdate).possess)
						return;

					if (cameraStateUpdate.Mode > cameraState.Mode)
						return;

					cameraStateUpdate.Mode     = cameraState.Mode;
					cameraStateUpdate.Priority = 0;

					cameraStateUpdate.StateEntity = entity;
					cameraStateUpdate.StateData   = cameraState.Data;

					updater.Update(cameraStateUpdate);
				})
				.WithName("Check_ServerCameraState_AndReplaceSystemData")
				.WithReadOnly(cameraTargetFromEntity)
				.Run(); // use ScheduleSingle() when it will be out.

			var modifierFromEntity = GetComponentDataFromEntity<CameraModifierData>(true);
			Entities
				.ForEach((ref Translation translation, ref Rotation rotation, ref LocalToWorld ltw, ref SystemData systemData) =>
				{
					var offset = systemData.StateData.Offset;
					if (!math.all(offset.rot.value))
						offset.rot = quaternion.identity;

					var modifier = modifierFromEntity.TryGet(systemData.StateData.Target, out var hasModifier, default);
					if (!hasModifier)
						modifier.Rotation = quaternion.identity;

					translation.Value = modifier.Position + offset.pos;
					rotation.Value    = math.mul(modifier.Rotation, offset.rot);

					translation.Value.z = -100; // temporary for now

					ltw.Value = new float4x4(rotation.Value, translation.Value);

					systemData.Focus = hasModifier ? modifier.FieldOfView : 8;
				}).WithReadOnly(modifierFromEntity).Run();

			return default;
		}

		public struct SystemData : IComponentData
		{
			public CameraMode Mode;
			public int        Priority;

			public Entity      StateEntity;
			public CameraState StateData;

			public float Focus;
		}
	}
}