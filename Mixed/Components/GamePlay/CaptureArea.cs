using Unity.Entities;

namespace Patapon.Mixed.GamePlay
{
	public enum CaptureAreaType
	{
		/// <summary>
		///     When a team pass on the area, it's captured instantly
		/// </summary>
		Instant,

		/// <summary>
		///     When a team pass on the area, it's capturing progressively
		/// </summary>
		Progressive
	}


	public struct CaptureAreaComponent : IComponentData
	{
		public CaptureAreaType CaptureType;
	}
}