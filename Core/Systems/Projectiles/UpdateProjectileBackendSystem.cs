using PataNext.Client.DataScripts.Models.Projectiles;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.Systems.Projectiles
{
	public class UpdateProjectileBackendSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Transform transform, ProjectileBackend backend) =>
			{
				if (!backend.letPresentationUpdateTransform)
					transform.localPosition = GetComponent<Translation>(backend.DstEntity).Value;

				if (backend.Presentation != null)
					backend.Presentation.OnSystemUpdate();
			}).WithStructuralChanges().Run();
		}
	}
}