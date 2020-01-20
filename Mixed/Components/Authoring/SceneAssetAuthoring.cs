using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Default.Authoring
{
	public class SceneAssetAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			
		}
	}
}