using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;

namespace PataNext.Client.Graphics.Camera.Follower
{
	[UpdateBefore(typeof(OrderGroup.Presentation.UpdateCamera))]
	public class UnitCameraFollowerSystem : AbsGameBaseSystem
	{
		protected override void OnUpdate()
		{
		}
	}
}