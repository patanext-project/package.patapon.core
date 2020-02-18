using System;
using Systems.GamePlay.CYari;
using DataScripts.Models.Units.Projectiles;
using DefaultNamespace;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.Rendering;

namespace DataScripts.Models.Units.Effects.FearSpear
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class FearSpearProjectilePoolingSystem : PoolingSystem<DefaultProjectileBackend, BaseProjectilePresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Effects")
			              .Folder("FearSpear")
			              .GetFile("FearSpearProjectile.prefab");

		protected override Type[] AdditionalBackendComponents => new[] {typeof(SortingGroup)};

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(FearSpearProjectile));
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