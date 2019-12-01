using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Mixed.RhythmEngine.Definitions
{
	public struct RhythmCommandDefinitionSequence : IBufferElementData
	{
		public RangeInt BeatRange;
		public int      Key;

		public RhythmCommandDefinitionSequence(int beatFract, int key)
		{
			BeatRange = new RangeInt(beatFract, 0);
			Key       = key;
		}

		public RhythmCommandDefinitionSequence(int beatFract, int beatFractLength, int key)
		{
			BeatRange = new RangeInt(beatFract, beatFractLength);
			Key       = key;
		}

		public int BeatEnd => BeatRange.end;
	}

	public struct RhythmCommandDefinition : IComponentData
	{
		public NativeString64 Identifier;
		public int            BeatLength;
	}
}