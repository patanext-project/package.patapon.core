using Patapon.Mixed.RhythmEngine.Flow;
using Unity.Entities;

namespace Patapon.Mixed.RhythmEngine
{
	/// <summary>
	/// This component should only be used on simulated rhythm engines (eg: client owned rhythm engines)
	/// </summary>
	[InternalBufferCapacity(8)]
	public struct RhythmEngineCommandProgression : IBufferElementData
	{
		public FlowPressure Data;
	}

	/// <summary>
	/// This component should only be used on server
	/// </summary>
	[InternalBufferCapacity(8)]
	public struct RhythmEngineClientPredictedCommandProgression : IBufferElementData
	{
		public FlowPressure Data;
	}

	/// <summary>
	/// This component should only be used on server
	/// </summary>
	[InternalBufferCapacity(8)]
	public struct RhythmEngineClientRequestedCommandProgression : IBufferElementData
	{
		public FlowPressure Data;
	}
}