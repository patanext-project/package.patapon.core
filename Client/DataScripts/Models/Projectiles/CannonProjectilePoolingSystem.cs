using System;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine.Rendering;

namespace PataNext.Client.DataScripts.Models.Projectiles
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class CannonProjectilePoolingSystem : PoolingSystem<DefaultProjectileBackend, BaseProjectilePresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Projectiles")
			              .Folder("Cannon")
			              .GetFile("CannonProjectile.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(SortingGroup)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(CannonProjectile));
		}

		protected override void ReturnBackend(DefaultProjectileBackend backend)
		{
			if (backend.Presentation is DefaultProjectilePresentation def
			    && !def.CanBePooled)
				return;

			base.ReturnBackend(backend);
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);
			
			LastBackend.GetComponent<SortingGroup>()
			           .sortingLayerName = "BattlegroundEffects";
		}
	}
}