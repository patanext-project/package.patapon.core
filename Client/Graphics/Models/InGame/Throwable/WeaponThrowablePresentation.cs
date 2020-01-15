using DefaultNamespace;
using Patapon.Mixed.GamePlay;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace package.patapon.core.Models.InGame.Throwable
{
	public class WeaponThrowablePresentation : RuntimeAssetPresentation<WeaponThrowablePresentation>
	{
		
	}

	public class WeaponThrowableBackend : RuntimeAssetBackend<WeaponThrowablePresentation>
	{
		
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class WeaponThrowablePoolingSystem : PoolingSystem<WeaponThrowableBackend, WeaponThrowablePresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("Equipments")
			              .Folder("Spears")
			              .GetFile("default_spear_throwable.prefab");
		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(Translation), typeof(UnitWeaponProjectile));
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	[UpdateAfter(typeof(WeaponThrowablePoolingSystem))]
	public class WeaponThrowableRenderSystem : BaseRenderSystem<WeaponThrowablePresentation>
	{
		protected override void PrepareValues()
		{
			
		}

		protected override void Render(WeaponThrowablePresentation definition)
		{
			var backend = definition.Backend;
			var target  = backend.DstEntity;

			backend.transform.position = EntityManager.GetComponentData<Translation>(target).Value;
			var dir   = EntityManager.GetComponentData<Velocity>(target).Value;
			var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
			backend.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		}

		protected override void ClearValues()
		{
			
		}
	}
}