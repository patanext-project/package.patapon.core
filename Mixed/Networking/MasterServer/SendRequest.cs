using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	public interface IRequestCompletionStatus : IComponentData
	{
		bool error { get; }
	}
}