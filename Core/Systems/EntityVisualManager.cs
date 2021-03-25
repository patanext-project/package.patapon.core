using System.Collections.Generic;
using JetBrains.Annotations;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Systems
{
	public struct EntityVisualDefinition : IComponentData
	{
		public bool IsValid => PoolId > 0;

		public uint PoolId;
	}

	public class EntityVisualManager : ComponentSystem
	{
		private Dictionary<AssetPath, uint>                  m_DefinitionByAddress;
		private Dictionary<uint, AsyncAssetPool<GameObject>> m_PoolByDefinition;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_DefinitionByAddress = new Dictionary<AssetPath, uint>();
			m_PoolByDefinition    = new Dictionary<uint, AsyncAssetPool<GameObject>>();
		}

		protected override void OnUpdate()
		{
		}

		public EntityVisualDefinition Register(GameObject prefab)
		{
			AssetPath assetPath = ("internal", prefab.GetInstanceID().ToString());

			if (m_DefinitionByAddress.TryGetValue(assetPath, out var definition))
				return new EntityVisualDefinition {PoolId = definition};

			var index = (uint) (m_DefinitionByAddress.Count + 1);
			m_DefinitionByAddress[assetPath] = index;
			m_PoolByDefinition[index]        = new AsyncAssetPool<GameObject>(prefab);

			return new EntityVisualDefinition
			{
				PoolId = index
			};
		}

		public EntityVisualDefinition Register(AssetPath assetPath)
		{
			if (m_DefinitionByAddress.TryGetValue(assetPath, out var definition))
				return new EntityVisualDefinition {PoolId = definition};

			var index = (uint) (m_DefinitionByAddress.Count + 1);
			m_DefinitionByAddress[assetPath] = index;
			m_PoolByDefinition[index]        = new AsyncAssetPool<GameObject>(assetPath);

			return new EntityVisualDefinition
			{
				PoolId = index
			};
		}

		[CanBeNull]
		public AsyncAssetPool<GameObject> GetPool(EntityVisualDefinition definition)
		{
			m_PoolByDefinition.TryGetValue(definition.PoolId, out var pool);
			return pool;
		}
	}
}