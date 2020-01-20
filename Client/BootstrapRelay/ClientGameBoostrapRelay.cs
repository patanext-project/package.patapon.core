using Bootstraps.Full;
using DataScripts.Interface.Menu;
using StormiumTeam.GameBase;
using Unity.NetCode;

namespace BootstrapRelay
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ClientGameBoostrapRelay : GameBaseSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			RequireSingletonForUpdate<GameBootstrap.IsActive>();
		}

		protected override void OnStartRunning()
		{
			World.GetOrCreateSystem<ClientMenuSystem>()
			     .SetMenu<ConnectionMenu>();
		}

		protected override void OnUpdate()
		{

		}
	}
}