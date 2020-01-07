using System;
using Modules;
using Patapon.Client.Graphics.Animation.Units;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;

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

	public interface IPlayableSystemData<TPlayable>
	{
		TPlayable Behaviour { get; set; }
	}

	public abstract class BaseAbilityAnimationSystem<TPlayable, TPlayableInit, TSystemData> : BaseAbilityAnimationSystem
		where TPlayable : BaseAbilityPlayable<TPlayableInit>, IPlayableBehaviour, new()
		where TSystemData : struct, IPlayableSystemData<TPlayable>
	{
		private TPlayableInit m_LastPlayableInit;

		protected void InjectAnimation(UnitVisualAnimation animation, TPlayableInit initData)
		{
			if (!animation.ContainsSystem(SystemType))
			{
				m_LastPlayableInit = initData;
				animation.InsertSystem<TSystemData>(SystemType, OnAnimationAdded, OnAnimationRemoved);
			}
		}

		protected virtual void OnAnimationAdded(ref VisualAnimation.ManageData data, ref TSystemData systemData)
		{
			var playable = ScriptPlayable<TPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(this, data.Graph, playable, data.Index, data.Behavior.RootMixer, m_LastPlayableInit);
			systemData.Behaviour = behavior;
			systemData.Behaviour.Visual = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		protected virtual void OnAnimationRemoved(VisualAnimation.ManageData data, TSystemData systemData)
		{

		}
	}

	public abstract class BaseAbilityAnimationSystem<TPlayable, TPlayableInit> : BaseAbilityAnimationSystem
	<
		TPlayable,
		TPlayableInit,
		BaseAbilityAnimationSystem<TPlayable, TPlayableInit>.DefaultSystemData
	>
		where TPlayable : BaseAbilityPlayable<TPlayableInit>, IPlayableBehaviour, new()
	{
		public struct DefaultSystemData : IPlayableSystemData<TPlayable>
		{
			public TPlayable Behaviour { get; set; }
		}
	}

	public class DefaultAsyncOperation
	{
		public static AsyncOperationModule.HandleDataPair<TComponent, THandleData> InvokeExecute<TComponent, THandleData>(AsyncOperationModule module, ref int index)
			where THandleData : struct
		{
			var handleDataPair = module.Get<TComponent, THandleData>(index);
			if (handleDataPair.Handle.Result == null)
				return null;

			module.Handles.RemoveAtSwapBack(index);
			index--;

			return handleDataPair;
		}
	}
}