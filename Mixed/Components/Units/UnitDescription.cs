using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<UnitDescription>))]

namespace Patapon.Mixed.Units
{
	public struct UnitDescription : IEntityDescription
	{
	}
}