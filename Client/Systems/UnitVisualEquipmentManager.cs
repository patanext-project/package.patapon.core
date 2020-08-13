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

			m_PoolByArchetype["Swords/default_sword"]   = new AsyncAssetPool<GameObject>(builder.GetFile("Swords/default_sword.prefab"));
			m_PoolByArchetype["Spears/default_spear"]   = new AsyncAssetPool<GameObject>(builder.GetFile("Spears/default_spear.prefab"));
			m_PoolByArchetype["Shields/default_shield"] = new AsyncAssetPool<GameObject>(builder.GetFile("Shields/default_shield.prefab"));
			m_PoolByArchetype["Masks/n_taterazay"]      = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_taterazay.prefab"));
			m_PoolByArchetype["Masks/n_yarida"]         = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_yarida.prefab"));
			m_PoolByArchetype["Masks/n_kibadda"]         = new AsyncAssetPool<GameObject>(builder.GetFile("Masks/n_kibadda.prefab"));
		} 

		protected override void OnUpdate()
		{
		}

		public bool TryGetPool(string archetype, out AsyncAssetPool<GameObject> pool)
		{
			return m_PoolByArchetype.TryGetValue(archetype, out pool);
		}
	}
}