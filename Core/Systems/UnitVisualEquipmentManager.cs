using System.Collections.Generic;
using PataNext.Client.Core.Addressables;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Systems
{
	public class UnitVisualEquipmentManager : ComponentSystem
	{
		private Dictionary<string, AsyncAssetPool<GameObject>> m_PoolByArchetype;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PoolByArchetype = new Dictionary<string, AsyncAssetPool<GameObject>>();
			// TODO: Need to be dynamic in the future (search based in StreamingAssets)
			var builder = AddressBuilder.Client()
			                            .Folder("Models")
			                            .Folder("Equipments");

			var addr = AddressBuilder.Client()
			                         .Folder("equipments")
			                         .Folder("{0}")
			                         .GetFile("{1}");
			
			m_PoolByArchetype[string.Format(addr, "swords", "default_sword")]         = new AsyncAssetPool<GameObject>(builder.GetFile("Swords/default_sword.prefab"));
			m_PoolByArchetype[string.Format(addr, "spears", "default_spear")]         = new AsyncAssetPool<GameObject>(builder.GetFile("Spears/default_spear.prefab"));
			m_PoolByArchetype[string.Format(addr, "spears", "default_spear:small")]   = new AsyncAssetPool<GameObject>(builder.GetFile("Spears/default_spear_smaller.prefab"));
			m_PoolByArchetype[string.Format(addr, "shields", "default_shield")]       = new AsyncAssetPool<GameObject>(builder.GetFile("Shields/default_shield.prefab"));
			m_PoolByArchetype[string.Format(addr, "helmets", "default_helmet:small")] = new AsyncAssetPool<GameObject>(builder.GetFile("Helmets/default_helmet_small.prefab"));
			m_PoolByArchetype[string.Format(addr, "masks", "taterazay")]              = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_taterazay.prefab"));
			m_PoolByArchetype[string.Format(addr, "masks", "yarida")]                 = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_yarida.prefab"));
			m_PoolByArchetype[string.Format(addr, "masks", "kibadda")]                = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_kibadda.prefab"));
		}

		protected override void OnUpdate()
		{
		}

		public bool TryGetPool(string archetype, out AsyncAssetPool<GameObject> pool)
		{
			if (archetype.StartsWith("ms://"))
				archetype = archetype.Replace("ms://", "cr://");

			return m_PoolByArchetype.TryGetValue(archetype, out pool);
		}
	}
}