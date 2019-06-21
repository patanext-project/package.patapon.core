using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.Core.Tests
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class TestUI : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Entity entity, ref GamePlayer gamePlayer, ref LocalCameraState cameraState) =>
			{
				cameraState.Data.Mode = CameraMode.Forced;
				if (cameraState.Target == default)
				{
					var team = PostUpdateCommands.CreateEntity();
					PostUpdateCommands.AddComponent(team, new TeamDescription());

					var firstEntity = default(Entity);
					for (var i = 0; i != 4; i++)
					{
						var e = PostUpdateCommands.CreateEntity();
						PostUpdateCommands.AddComponent(e, new UnitDescription());
						PostUpdateCommands.AddComponent(e, new Relative<TeamDescription> {Target = team});

						if (firstEntity == default)
							firstEntity = e;
					}

					var cpy = cameraState;
					cpy.Data.Target = firstEntity;
					PostUpdateCommands.SetComponent(entity, cpy);
				}
			});
		}
	}
}