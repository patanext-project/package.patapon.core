using DefaultNamespace;
using package.patapon.core.Models.InGame.Multiplayer;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon.Client.PoolingSystems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UIPlayerTargetCursorPoolingSystem : PoolingSystem<UIPlayerTargetCursorBackend, UIPlayerTargetCursorPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Multiplayer")
			              .GetFile("MpTargetCursor.prefab");

		protected override EntityQuery GetQuery()
		{
			// only display if there is a relative player...
			return GetEntityQuery(typeof(UnitTargetDescription), typeof(Relative<PlayerDescription>));
		}
	}
}