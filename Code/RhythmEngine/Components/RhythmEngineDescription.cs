using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<RhythmEngineDescription>))]

namespace Patapon4TLB.Default
{
	public struct RhythmEngineDescription : IEntityDescription
	{
	}
}