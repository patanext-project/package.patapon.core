using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<UnitDescription>))]

namespace Patapon4TLB.Core
{
	public struct UnitDescription : IEntityDescription
	{
	}
}