using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	public interface IMasterServerRequest
	{
		bool error { get; }
	}
}