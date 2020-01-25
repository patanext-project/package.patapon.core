using Revolution;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Patapon.Mixed.GameModes.Training
{
	public struct SoloTraining : IComponentData, IGameMode
	{
		public int foo;
		
		public class EmptySynchronize : ComponentSnapshotSystemTag<SoloTraining>
		{}
	}
}