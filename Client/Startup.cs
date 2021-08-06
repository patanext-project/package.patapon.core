using StormiumTeam.GameBase.Utility.Rendering;
using Unity.Entities;
using UnityEngine.Rendering.Universal;

namespace PataNext.Client
{
	public class Startup : SystemBase
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			World.GetExistingSystem<ClientCreateCameraSystem>()
			     .Camera
			     .gameObject
			     .AddComponent<UniversalAdditionalCameraData>();
		}

		protected override void OnUpdate()
		{
			
		}
	}
}