using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase._Camera;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.DOTS.xCamera;
using StormiumTeam.GameBase.Utility.DOTS.xMonoBehaviour;
using StormiumTeam.GameBase.Utility.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PataNext.Client.Graphics.Camera
{
	public struct CurrentCameraStateSource : IComponentData
	{
		public Entity Value;
	}

	public class CameraModifyTargetSystemGroup : ComponentSystemGroup
	{

	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.UpdateCamera))]
	[AlwaysSynchronizeSystem]
	[AlwaysUpdateSystem]
	public class SynchronizeCameraStateSystem : SystemBase
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

		protected override void OnUpdate()
		{
			if (!m_CameraWithoutUpdateComp.IsEmptyIgnoreFilter) EntityManager.AddComponent(m_CameraWithoutUpdateComp, typeof(SystemData));

			Entity defaultCamera = default;
			if (HasSingleton<DefaultCamera>())
			{
				defaultCamera = GetSingletonEntity<DefaultCamera>();
				if (!EntityManager.HasComponent<ComputedCameraState>(defaultCamera))
					EntityManager.AddComponentData(defaultCamera, new ComputedCameraState());
			}

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
					if (cameraTargetFromEntity.HasComponent(entity)) camera = cameraTargetFromEntity[entity].Value;

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
				.WithAll<PlayerIsLocal>()
				.ForEach((Entity entity, in ServerCameraState cameraState) =>
				{
					var camera                                        = defaultCamera;
					if (cameraTargetFromEntity.HasComponent(entity)) camera = cameraTargetFromEntity[entity].Value;

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

			Entities.ForEach((ref ComputedCameraState computed, in SystemData systemData) =>
			{
				computed.UseModifier = true;

				computed.Focus       = systemData.Focus;
				computed.StateData   = systemData.StateData;
				computed.StateEntity = systemData.StateEntity;

				if (!math.all(computed.StateData.Offset.rot.value))
					computed.StateData.Offset.rot = quaternion.identity;
			}).Run();

			World.GetExistingSystem<CameraModifyTargetSystemGroup>().Update();

			var modifierFromEntity = GetComponentDataFromEntity<CameraModifierData>(true);
			Entities
				.ForEach((ref Translation translation, ref Rotation rotation, ref LocalToWorld ltw, ref ComputedCameraState computed) =>
				{
					var offset = computed.StateData.Offset;
					if (!math.all(offset.rot.value))
						offset.rot = quaternion.identity;

					CameraModifierData modifier = default;
					if (computed.UseModifier && modifierFromEntity.TryGet(computed.StateData.Target, out modifier))
					{
						computed.Focus = modifier.FieldOfView;
					}
					else
					{
						modifier.Rotation    = quaternion.identity;
						modifier.FieldOfView = 8;

						computed.Focus = modifier.FieldOfView;
					}

					translation.Value = modifier.Position + offset.pos;
					rotation.Value    = math.mul(modifier.Rotation, offset.rot);

					translation.Value.z = -100; // temporary for now

					ltw.Value = new float4x4(rotation.Value, translation.Value);
				}).WithReadOnly(modifierFromEntity).Run();
		}

		private struct SystemData : IComponentData
		{
			public CameraMode Mode;
			public int        Priority;

			public Entity      StateEntity;
			public CameraState StateData;

			public float Focus;
		}
	}
}