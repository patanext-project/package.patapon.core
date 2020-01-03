using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(GameModeSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class HeadOnUpdatePlayerSpectatorState : GameBaseSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			RequireSingletonForUpdate<MpVersusHeadOn>();
		}

		protected override void OnUpdate()
		{
			Entities.WithAll<GamePlayer, GamePlayerReadyTag>().ForEach((Entity e, DynamicBuffer<OwnerChild> children, ref ServerCameraState cameraState, ref VersusHeadOnPlayer gmPlayer) =>
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
			});
		}
	}
}