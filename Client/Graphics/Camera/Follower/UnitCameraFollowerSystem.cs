using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Graphics.Camera.Follower
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateBefore(typeof(OrderGroup.Presentation.UpdateCamera))]
	public class UnitCameraFollowerSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			
		}
	}
}