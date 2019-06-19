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
					var e = PostUpdateCommands.CreateEntity();
					PostUpdateCommands.AddComponent(e, new UnitDescription());

					var cpy = cameraState;
					cpy.Data.Target = e;
					PostUpdateCommands.SetComponent(entity, cpy);
				}
			});
		}
	}
}