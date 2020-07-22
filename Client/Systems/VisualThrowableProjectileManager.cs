using System;
using System.Collections.Generic;
using StormiumTeam.GameBase.Utility.Pooling;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Client.Systems
{
	public struct VisualThrowableDefinition : IComponentData
	{
		public bool IsValid => PoolId > 0;
		
		public uint PoolId;
	}

	public class VisualThrowableProjectileManager : ComponentSystem
	{
		private Dictionary<string, uint> m_DefinitionByAddress;
		private Dictionary<uint, AsyncAssetPool<GameObject>> m_PoolByDefinition;

		protected override void OnCreate()
		{
			base.OnCreate();
			
			m_DefinitionByAddress = new Dictionary<string, uint>();
			m_PoolByDefinition = new Dictionary<uint, AsyncAssetPool<GameObject>>();
		}

		protected override void OnUpdate()
		{
		}
		
		public VisualThrowableDefinition RegisterPool(GameObject prefab)
		{
			throw new NotImplementedException("RegisterPool(GameObject) not implemented");
		}

		public VisualThrowableDefinition Register(string addr)
		{
			if (m_DefinitionByAddress.TryGetValue(addr, out var definition))
				return new VisualThrowableDefinition {PoolId = definition};

			var index = (uint) (m_DefinitionByAddress.Count + 1);
			m_DefinitionByAddress[addr] = index;
			m_PoolByDefinition[index]   = new AsyncAssetPool<GameObject>(addr);

			return new VisualThrowableDefinition
			{
				PoolId = index
			};
		}
	}
}