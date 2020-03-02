using DefaultNamespace;
using GameModes.VSHeadOn;
using package.patapon.core;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Patapon.Client.Systems
{
	[UpdateInGroup(typeof(CameraModifyTargetSystemGroup))]
	[AlwaysSynchronizeSystem]
	public class HeadOnSpectatingUpdateTargetSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			var dt  = Time.DeltaTime;
			var cmd = World.GetExistingSystem<GrabInputSystem>().LocalCommand;

			Entities
				.ForEach((ref ComputedCameraState computed) =>
				{
					if (!EntityManager.TryGetComponentData(computed.StateData.Target, out HeadOnSpectating spectating))
						return;

					var player = computed.StateData.Target;

					if (!EntityManager.HasComponent<CameraTargetAnchor>(player))
					{
						EntityManager.AddComponentData(player, new CameraTargetAnchor
						{
							Type  = AnchorType.Screen,
							Value = new float2(0, 0.7f),
						});
					}

					var offset = RigidTransform.identity;
					if (spectating.CurrentTarget == Entity.Null)
					{
						computed.UseModifier = false;

						spectating.Velocity += cmd.Panning * dt * 5;
						if (math.abs(cmd.Panning) < 0.1f)
							spectating.Velocity = math.lerp(spectating.Velocity, 0, dt * 3);
						if (math.abs(spectating.Velocity) > 15)
							spectating.Velocity = 15 * math.sign(spectating.Velocity);

						spectating.Position += spectating.Velocity * dt;

						offset.pos.x = spectating.Position;


						computed.Focus            = 7;
						computed.StateData.Target = player;
					}
					else
						computed.StateData.Target = spectating.CurrentTarget;

					computed.StateData.Offset = offset;

					EntityManager.SetComponentData(player, spectating);
				})
				.WithStructuralChanges().Run();
		}
	}
}