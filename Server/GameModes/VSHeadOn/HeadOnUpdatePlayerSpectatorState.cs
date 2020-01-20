using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(GameModeSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[AlwaysSynchronizeSystem]
	public class HeadOnUpdatePlayerSpectatorState : JobGameBaseSystem
	{
		private EntityQuery m_Query;

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireSingletonForUpdate<MpVersusHeadOn>();

			m_Query = GetEntityQuery(typeof(UnitDescription));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			Entities.WithAll<HeadOnPlaying>().ForEach((Entity e, DynamicBuffer<OwnerChild> children, ref ServerCameraState cameraState, ref GamePlayer gmPlayer) =>
			{
				Entity targetChild = default;
				foreach (var elem in children)
				{
					var child = elem.Child;
					if (!EntityManager.HasComponent<UnitDescription>(child))
						continue;

					targetChild = child;
					break;
				}

				cameraState.Data.Target = targetChild;
				cameraState.Data.Mode   = CameraMode.Forced;
			}).WithoutBurst().Run();

			using (var unitEntities = m_Query.ToEntityArray(Allocator.TempJob))
			{
				var time = Time.ElapsedTime;
				Entities.ForEach((Entity e, ref ServerCameraState cameraState, ref GamePlayerCommand command, ref HeadOnSpectating spectating) =>
				{
					if (unitEntities.Length == 0)
						return;
					
					if (math.abs(command.Base.Panning) < 0.5f && EntityManager.Exists(cameraState.Target))
						return;

					if (!EntityManager.Exists(cameraState.Target))
						cameraState.Data.Target = default;

					spectating.UnitIndex += (int) math.sign(command.Base.Panning);
					spectating.UnitIndex %= unitEntities.Length - 1;

					for (var ent = 0; ent < unitEntities.Length; ent++)
					{
						var unitEntity = unitEntities[ent];
						if (cameraState.Target == default)
						{
							cameraState.Data.Target = unitEntity;
							break;
						}

						if (spectating.SwitchDelay < time && spectating.UnitIndex == ent)
						{
							spectating.SwitchDelay  = time + 0.25;
							cameraState.Data.Target = unitEntity;
							break;
						}
					}

					cameraState.Data.Mode = CameraMode.Forced;
				}).WithoutBurst().Run();
			}

			return default;
		}
	}
}