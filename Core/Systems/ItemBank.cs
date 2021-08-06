using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameHost.Core;
using Newtonsoft.Json;
using PataNext.UnityCore.Rpc;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Systems
{
	public class ItemBank : SystemBase
	{
		private ItemManager itemManager;
		private DentBank    dentBank;

		protected override void OnCreate()
		{
			base.OnCreate();

			itemManager = World.GetExistingSystem<ItemManager>();
			dentBank    = World.GetExistingSystem<DentBank>();
		}

		protected override void OnUpdate()
		{
		}
		
		public Task<Entity> CallAndStoreLater(DentEntity itemEntity, bool forceUpdate = false)
		{
			return dentBank.CallAndStoreLater(itemEntity, forceUpdate);
		}

		public bool TryGetItemDetails(DentEntity itemEntity, out ReadOnlyItemDetails details)
		{
			details = default;

			return dentBank.TryGetOutput(itemEntity, out var output)
			       && EntityManager.HasComponent<ItemTargetAssetIdComponent>(output)
			       && itemManager.TryGetDetails(EntityManager.GetSharedComponentData<ItemTargetAssetIdComponent>(output).Value.FullString, out details);
		}
	}

	public struct ItemTargetAssetIdComponent : ISharedComponentData, IEquatable<ItemTargetAssetIdComponent>
	{
		public ResPath Value;

		public bool Equals(ItemTargetAssetIdComponent other)
		{
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			return obj is ItemTargetAssetIdComponent other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}