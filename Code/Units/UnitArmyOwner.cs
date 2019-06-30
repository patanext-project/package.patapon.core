using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<ArmyDescription>))]
[assembly: RegisterGenericComponentType(typeof(GhostRelative<ArmyDescription>))]

namespace Patapon4TLB.Core
{
	public struct ArmyDescription : IEntityDescription
	{
	}

	public struct ArmyControlledBy : IComponentData
	{
		public Entity Target;
	}
}