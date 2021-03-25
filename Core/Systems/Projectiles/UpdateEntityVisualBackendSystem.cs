using PataNext.Client.DataScripts.Models.Projectiles;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.Systems.Projectiles
{
	public class UpdateEntityVisualBackendSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Transform transform, EntityVisualBackend backend) =>
			{
				if (backend.Presentation != null)
					backend.Presentation.OnSystemUpdate();
				
				if (!EntityManager.Exists(backend.DstEntity))
					return;

				if (!backend.letPresentationUpdateTransform)
					transform.localPosition = GetComponent<Translation>(backend.DstEntity).Value;
			}).WithStructuralChanges().Run();
		}
	}
}