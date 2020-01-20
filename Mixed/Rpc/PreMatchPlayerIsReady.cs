using Revolution;
using Unity.Entities;

namespace Patapon.Server.GameModes
{
	public struct PreMatchPlayerIsReady : IComponentData
	{
		public class NetSynchronize : ComponentSnapshotSystemTag<PreMatchPlayerIsReady>
		{
		}
	}
}