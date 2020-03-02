using Bootstraps.Full;
using DataScripts.Interface.Menu;
using DataScripts.Interface.Menu.TemporaryMenu;
using StormiumTeam.GameBase;
using Unity.NetCode;

namespace BootstrapRelay
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class ClientGameBoostrapRelay : AbsGameBaseSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			RequireSingletonForUpdate<GameBootstrap.IsActive>();
		}

		protected override void OnStartRunning()
		{
			/*World.GetOrCreateSystem<ClientMenuSystem>()
			     .SetMenu<ConnectionMenu>();*/
			World.GetOrCreateSystem<ClientMenuSystem>()
			     .SetMenu<TempMenu>();
		}

		protected override void OnUpdate()
		{

		}
	}
}