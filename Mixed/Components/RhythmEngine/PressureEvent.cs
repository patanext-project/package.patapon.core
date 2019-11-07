using Unity.Entities;

namespace Patapon.Mixed.RhythmEngine
{
	public struct PressureEvent : IComponentData
	{
		public Entity Engine;
		public int    Key;
		public long   TimeMs;
		public int    RenderBeat;
		public float  Score;
	}
}