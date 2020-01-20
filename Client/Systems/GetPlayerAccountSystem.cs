using Patapon4TLB.Core.MasterServer;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon.Client.Systems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class GetPlayerAccountSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.WithNone<RequestGetUserAccountData>().ForEach((Entity entity, ref GamePlayer player) =>
			{
				if (player.MasterServerId == 0)
					return;
				
				EntityManager.AddComponentData(entity, new RequestGetUserAccountData
				{
					UserId = player.MasterServerId
				});
			});
		}
	}
}