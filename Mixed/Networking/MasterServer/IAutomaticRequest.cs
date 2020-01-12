using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	/// <summary>
	/// This component is used in case you need to have an entity that automatically refresh it's own events
	/// </summary>
	public struct TrackedAutomatedRequest : IBufferElementData
	{
		public Entity Value;
	}

	public interface IAutomaticRequestComponent<TOriginal> : IComponentData
	{
		void SetRequest(ref TOriginal original);
	}

	/// <summary>
	/// Component to indicate that this entity was spawned as an automatic request... 
	/// </summary>
	public struct SpawnedAsAutomaticRequest : IComponentData
	{
		public Entity Origin;
	}
}