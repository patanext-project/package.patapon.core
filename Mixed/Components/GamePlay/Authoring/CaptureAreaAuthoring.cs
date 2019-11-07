using Unity.Entities;
using UnityEngine;

namespace Patapon.Mixed.GamePlay.Authoring
{
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
}