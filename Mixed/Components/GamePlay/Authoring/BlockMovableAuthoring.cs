using Patapon.Mixed.GamePlay.Team;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Mixed.GamePlay.Authoring
{
	public class BlockMovableAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public float Center, Size;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new TeamAgainstMovable
			{
				Center = Center,
				Size   = Size
			});
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;

			var left  = transform.position + Vector3.right * (Center - Size);
			var right = transform.position + Vector3.right * (Center + Size);

			Gizmos.DrawLine(left, left + Vector3.up * 25);
			Gizmos.DrawLine(right, right + Vector3.up * 25);
		}
	}
}