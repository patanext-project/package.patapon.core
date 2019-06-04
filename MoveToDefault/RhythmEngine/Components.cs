using package.patapon.core;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct DefaultRhythmEngineData
	{
		public struct Settings : IComponentData
		{
			public int MaxBeats;
		}
		
		public struct Predicted : IComponentData, IPredictable<Predicted>
		{
			public int Beat;
			public bool CheckNewCommand;
			
			public bool VerifyPrediction(in Predicted real)
			{
				return false;
			}
		}
	}

	public struct PressureEvent : IComponentData
	{
		public Entity Engine;
		public int Key;
	}

	[InternalBufferCapacity(8)]
	public struct DefaultRhythmEngineCurrentCommand : IBufferElementData
	{
		public FlowRhythmPressureData Data;
	}
	
	public struct DefaultRhythmCommand : IComponentData
	{

	}

	public struct MarchCommand : IComponentData
	{
	}

	public struct AttackCommand : IComponentData
	{
	}

	public struct DefendCommand : IComponentData
	{
	}

	public struct ChargeCommand : IComponentData
	{
	}

	public struct RetreatCommand : IComponentData
	{
	}

	public struct JumpCommand : IComponentData
	{
	}

	public struct PartyCommand : IComponentData
	{
	}

	public struct SummonCommand : IComponentData
	{
	}

	public struct BackwardCommand : IComponentData
	{
	}
}