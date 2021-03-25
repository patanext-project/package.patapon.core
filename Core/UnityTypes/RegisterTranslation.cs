using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Transforms;

namespace PataNext.Client.Core.UnityTypes
{
	public class RegisterTranslation : RegisterGameHostComponentData<Translation>
	{
		protected override string CustomComponentPath => "StormiumTeam.GameBase.Transform.Components::Position";

		protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<Translation>();
	}
}