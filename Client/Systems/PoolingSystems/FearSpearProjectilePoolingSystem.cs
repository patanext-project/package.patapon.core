using System;
using PataNext.Client.Core.Addressables;
using PataNext.Client.DataScripts.Models.Projectiles;
using PataNext.CoreAbilities.Server;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine.Rendering;

namespace PataNext.Client.PoolingSystems
{
	public class FearSpearProjectileBackend : EntityVisualBackend
	{
	}

	// Fear spear has two projectiles:
	// - The weapon
	// - The effect
	// Which is why we create this pooling system.
	public class FearSpearProjectilePoolingSystem : PoolingSystem<FearSpearProjectileBackend, DefaultProjectilePresentation>
	{
		protected override AssetPath AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Effects")
			              .Folder("FearSpear")
			              .GetAsset("FearSpearProjectile");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(SortingGroup)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(FearSpearProjectile));
		}

		protected override void ReturnBackend(FearSpearProjectileBackend backend)
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