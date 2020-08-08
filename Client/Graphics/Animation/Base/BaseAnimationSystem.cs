using System;
using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Components;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Modules;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Modules;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Base
{
	[AlwaysSynchronizeSystem]
	public abstract class BaseAnimationSystem : AbsGameBaseSystem
	{
		protected AsyncOperationModule AsyncOp;
		protected Type                 SystemType;

		protected UnitVisualPresentation CurrentPresentation;

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

		protected override void OnUpdate()
		{
			for (var i = 0; i != AsyncOp.Handles.Count; i++) OnAsyncOpUpdate(ref i);

			if (!OnBeforeForEach())
				return;

			Entities.ForEach((UnitVisualBackend backend, UnitVisualAnimation animation) =>
			{
				if (backend.Presentation == null || !EntityManager.Exists(backend.DstEntity))
					return;

				// main
				CurrentPresentation = backend.Presentation;
				OnUpdate(backend.DstEntity, backend, animation);
			}).WithStructuralChanges().Run();
			return;
		}

		protected abstract void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation);

		protected void ResetIdleTime(Entity targetEntity)
		{
			if (EntityManager.TryGetComponentData(targetEntity, out AnimationIdleTime idleTime))
				EntityManager.SetComponentData(targetEntity, default(AnimationIdleTime));
		}

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
			AbilityFinder.Update();
			return true;
		}

		public void SetAnimOverride<TKey>(OverrideObjectComponent overrides, in Dictionary<TKey, AnimationClip> origin, Action<TKey, AnimationClip> onResult)
			where TKey : IAbilityAnimClip
		{
			foreach (var pair in origin)
			{
				if (overrides != null)
				{
					overrides.TryGetPresentationObject(pair.Key.Key, out var clip, pair.Value);
					onResult(pair.Key, clip);
				}
				else
					onResult(pair.Key, pair.Value);
			}
		}

		public void SetAnimOverride<TKey>(OverrideObjectComponent overrides, Dictionary<TKey, AnimationClip> map, Dictionary<TKey, AnimationClip> destination)
			where TKey : IAbilityAnimClip
		{
			var originalDest = destination;
			if (map == destination)
				destination = new Dictionary<TKey, AnimationClip>(map);

			foreach (var pair in map)
			{
				if (overrides != null)
				{
					overrides.TryGetPresentationObject(pair.Key.Key, out var clip, pair.Value);
					destination[pair.Key] = clip;
				}
				else
					destination[pair.Key] = pair.Value;
			}

			if (originalDest == destination)
				return;

			originalDest.Clear();
			foreach (var pair in destination)
				originalDest.Add(pair.Key, pair.Value);
		}

		private           bool useAutomaticAsyncOp;
		protected virtual bool AutomaticAsyncOperation => useAutomaticAsyncOp;

		protected void PreLoadAnimationAsset<THandle>(string path, THandle handle)
			where THandle : struct, IAbilityAnimationKey
		{
			LoadAssetAsync<AnimationClip, THandle>(path, handle);
			useAutomaticAsyncOp = true;
		}
	}

	public interface IPlayableSystemData<TPlayable>
	{
		TPlayable Behaviour { get; set; }
	}

	public interface IAbilityAnimationKey
	{
		string Key { get; set; }
	}
	
	public interface IAbilityAnimClip : IAbilityAnimationKey
	{
		AnimationClip Clip { get; set; }
	}

	public abstract class BaseAbilityAnimationSystem<TPlayable, TPlayableInit, TSystemData, THandleData, TClip> : BaseAbilityAnimationSystem
		where TPlayable : BaseAbilityPlayable<TPlayableInit>, IPlayableBehaviour, new()
		where TSystemData : struct, IPlayableSystemData<TPlayable>
		where THandleData : struct, IAbilityAnimationKey
		where TClip : struct, IAbilityAnimClip
	{
		private TPlayableInit m_LastPlayableInit;

		protected void SetInit(TPlayableInit init)
		{
			m_LastPlayableInit = init;
		}

		protected abstract void OnAnimationInject(UnitVisualAnimation animation, ref TPlayableInit initData);

		protected void InjectAnimation(UnitVisualAnimation animation, TPlayableInit initData)
		{
			if (!animation.ContainsSystem(SystemType))
			{
				OnAnimationInject(animation, ref initData);

				m_LastPlayableInit = initData;
				animation.InsertSystem<TSystemData>(SystemType, OnAnimationAdded, OnAnimationRemoved);
			}
		}

		protected virtual ScriptPlayable<TPlayable> GetNewPlayable(ref VisualAnimation.ManageData data, ref TSystemData systemData)
		{
			return ScriptPlayable<TPlayable>.Create(data.Graph);
		}

		protected virtual void OnAnimationAdded(ref VisualAnimation.ManageData data, ref TSystemData systemData)
		{
			var playable = GetNewPlayable(ref data, ref systemData);
			var behavior = playable.GetBehaviour();

			behavior.Visual = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
			behavior.Initialize(this, data.Graph, playable, data.Index, data.Behavior.RootMixer, m_LastPlayableInit);
			systemData.Behaviour = behavior;
		}

		protected virtual void OnAnimationRemoved(VisualAnimation.ManageData data, TSystemData systemData)
		{

		}
		
		protected override void OnAsyncOpUpdate(ref int index)
		{
			if (!AutomaticAsyncOperation)
				return;
			
			var (handle, data) = DefaultAsyncOperation.InvokeExecute<AnimationClip, THandleData>(AsyncOp, ref index);
			if (!handle.IsValid() || handle.Result == null)
				return;
			
			OnAsyncOpElement(data, new TClip {Clip = handle.Result, Key = data.Key});
		}

		protected virtual void OnAsyncOpElement(THandleData handle, TClip result)
		{
			
		}

		protected override bool OnBeforeForEach()
		{
			return base.OnBeforeForEach() && AsyncOp.Handles.Count == 0;
		}
	}

	public class DefaultAsyncOperation
	{
		public static AsyncOperationModule.HandleDataPair<TComponent, THandleData> InvokeExecute<TComponent, THandleData>(AsyncOperationModule module, ref int index)
			where THandleData : struct
		{
			var handleDataPair = module.Get<TComponent, THandleData>(index);
			if (handleDataPair == null || !handleDataPair.Handle.IsValid() || !handleDataPair.Handle.IsDone)
			{
				if (handleDataPair != null && handleDataPair.Handle.IsValid() == false)
				{
					Debug.LogError(handleDataPair.Handle.OperationException);
					Debug.LogError(handleDataPair.Handle.Status);
				}
				
				return new AsyncOperationModule.HandleDataPair<TComponent, THandleData>();
			}
			
			module.Handles.RemoveAtSwapBack(index);
			index--;

			return handleDataPair;
		}
	}
}