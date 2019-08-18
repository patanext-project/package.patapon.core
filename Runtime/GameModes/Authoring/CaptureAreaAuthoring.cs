using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.GameModes.Authoring
{
	public enum CaptureAreaType
	{
		/// <summary>
		/// When a team pass on the area, it's captured instantly
		/// </summary>
		Instant,
		/// <summary>
		/// When a team pass on the area, it's capturing progressively
		/// </summary>
		Progressive
	}

	public class CaptureAreaAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public CaptureAreaType CaptureType;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new CaptureAreaComponent
			{
				CaptureType = CaptureType
			});
		}
	}

	public struct CaptureAreaComponent : IComponentData
	{
		public CaptureAreaType CaptureType;
	}
}