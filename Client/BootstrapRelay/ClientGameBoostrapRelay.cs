using PataNext.Client.DataScripts.Interface.Menu;
using StormiumTeam.GameBase.BaseSystems;

namespace PataNext.Client.BootstrapRelay
{
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