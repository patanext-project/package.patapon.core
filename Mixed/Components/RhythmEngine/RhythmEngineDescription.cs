using Patapon.Mixed.RhythmEngine;
using StormiumTeam.GameBase;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<RhythmEngineDescription>))]

namespace Patapon.Mixed.RhythmEngine
{
	public struct RhythmEngineDescription : IEntityDescription
	{
		public class Synchronize : RelativeSynchronize<RhythmEngineDescription>
		{
		}
	}
}