using System;
using Modules;
using Patapon.Client.Graphics.Animation.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.AddressableAssets;

namespace package.patapon.core.Animation.Units
{
	[AlwaysSynchronizeSystem]
	public abstract class BaseAnimationSystem : JobGameBaseSystem
	{
		protected AsyncOperationModule AsyncOp;
		protected Type                 SystemType;

		protected override void OnCreate()
		{
			base.OnCreate();

			SystemType = GetType();
			GetModule(out AsyncOp);
		}

		protected void LoadAssetAsync<TAsset, TData>(string address, TData data)
			where TData : struct
		{
			AsyncOp.Add(Addressables.LoadAssetAsync<TAsset>(address), data);
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			for (var i = 0; i != AsyncOp.Handles.Count; i++) OnAsyncOpUpdate(ref i);

			if (!OnBeforeForEach())
				return default;

			Entities.ForEach((UnitVisualBackend backend, UnitVisualAnimation animation) =>
			{
				// main
				OnUpdate(backend.DstEntity, backend, animation);
			}).WithoutBurst().Run();
			return default;
		}

		protected abstract void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation);

		protected virtual void OnAsyncOpUpdate(ref int index)
		{
		}

		protected virtual bool OnBeforeForEach()
		{
			return true;
		}
	}

	public abstract class BaseAbilityAnimationSystem : BaseAnimationSystem
	{
		protected AbilityFinderSystemModule AbilityFinder;
		protected EntityQuery               AbilityQuery { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out AbilityFinder);
			AbilityQuery        = GetAbilityQuery();
			AbilityFinder.Query = AbilityQuery;
		}

		protected abstract EntityQuery GetAbilityQuery();

		protected override bool OnBeforeForEach()
		{
			AbilityFinder.Update(default).Complete();
			return true;
		}
	}
}