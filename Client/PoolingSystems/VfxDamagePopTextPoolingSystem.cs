using DefaultNamespace;
using package.patapon.core.Models.InGame.VFXDamage;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Client.PoolingSystems
{
	public class VfxDamagePopTextPoolingSystem : PoolingSystem<VfxDamagePopTextBackend, VfxDamagePopTextPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Folder("InGame")
			              .Folder("Effects")
			              .Folder("VfxDamage")
			              .GetFile("VfxDamagePopTextDefault.prefab");

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(TargetDamageEvent));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			LastBackend.Play(EntityManager.GetComponentData<TargetDamageEvent>(LastBackend.DstEntity));
			LastBackend.setToPoolAt = Time.ElapsedTime + 2f;
			LastBackend.transform.localScale = Vector3.one * 0.5f;
		}

		protected override void ReturnBackend(VfxDamagePopTextBackend backend)
		{
			if (backend.setToPoolAt > Time.ElapsedTime)
				return;
			base.ReturnBackend(backend);
		}
	}
}