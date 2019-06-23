using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<RhythmEngineDescription>))]
[assembly: RegisterGenericComponentType(typeof(GhostRelative<RhythmEngineDescription>))]

namespace Patapon4TLB.Default
{
	public struct RhythmEngineDescription : IEntityDescription
	{
	}
}