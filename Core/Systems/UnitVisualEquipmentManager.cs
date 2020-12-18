using System.Collections.Generic;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Systems
{
	public class UnitVisualEquipmentManager : ComponentSystem
	{
		private Dictionary<AssetPath, AsyncAssetPool<GameObject>> m_PoolByArchetype;

		public UnitVisualEquipmentManager()
		{
			m_PoolByArchetype = new Dictionary<AssetPath, AsyncAssetPool<GameObject>>();
		}
		
		protected override void OnCreate()
		{
			base.OnCreate();
			
			// TODO: Need to be dynamic in the future (search based in StreamingAssets)
			/*var builder = AddressBuilder.Client()
			                            .Folder("Models")
			                            .Folder("Equipments");

			var addr = AddressBuilder.Client()
			                         .Folder("equipments")
			                         .Folder("{0}")
			                         .GetFile("{1}");
			
			m_PoolByArchetype[string.Format(addr, "swords", "default_sword")]         = new AsyncAssetPool<GameObject>(builder.GetFile("Swords/default_sword.prefab"));
			m_PoolByArchetype[string.Format(addr, "spears", "default_spear")]         = new AsyncAssetPool<GameObject>(builder.GetFile("Spears/default_spear.prefab"));
			m_PoolByArchetype[string.Format(addr, "spears", "default_spear:small")]   = new AsyncAssetPool<GameObject>(builder.GetFile("Spears/default_spear_smaller.prefab"));
			m_PoolByArchetype[string.Format(addr, "bows", "default_bow")]   = new AsyncAssetPool<GameObject>(builder.GetFile("Spears/default_bow.prefab"));
			m_PoolByArchetype[string.Format(addr, "bows", "default_bow:small")]   = new AsyncAssetPool<GameObject>(builder.GetFile("Bows/default_bow_small.prefab"));
			m_PoolByArchetype[string.Format(addr, "shields", "default_shield")]       = new AsyncAssetPool<GameObject>(builder.GetFile("Shields/default_shield.prefab"));
			m_PoolByArchetype[string.Format(addr, "helmets", "default_helmet:small")] = new AsyncAssetPool<GameObject>(builder.GetFile("Helmets/default_helmet_small.prefab"));
			m_PoolByArchetype[string.Format(addr, "masks", "taterazay")]              = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_taterazay.prefab"));
			m_PoolByArchetype[string.Format(addr, "masks", "yarida")]                 = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_yarida.prefab"));
			m_PoolByArchetype[string.Format(addr, "masks", "kibadda")]                = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_kibadda.prefab"));
			m_PoolByArchetype[string.Format(addr, "masks", "guardira")]                = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_guardira.prefab"));*/
		}

		protected override void OnUpdate()
		{
		}

		public void AddPool(string masterserverId, AsyncAssetPool<GameObject> pool)
		{
			Debug.Log($"adding {masterserverId} -> {pool.AssetPath}");
			m_PoolByArchetype[new ResPath(masterserverId)] = pool;
		}

		public bool TryGetPool(string archetype, out AsyncAssetPool<GameObject> pool)
		{
			return m_PoolByArchetype.TryGetValue(new ResPath(archetype), out pool);
		}
	}
}