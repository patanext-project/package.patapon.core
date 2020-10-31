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
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PataNext.Client.Graphics.Animation.Base
{
	[AlwaysSynchronizeSystem]
	public abstract class BaseAnimationSystem : AbsGameBaseSystem
	{
		protected Type SystemType;

		protected UnitVisualAnimation CurrentVisualAnimation;

		protected AnimationMap AnimationMap { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();
			
			SystemType   = GetType();
			AnimationMap = GetAnimationMap();
		}

		protected abstract AnimationMap GetAnimationMap();

		protected override void OnUpdate()
		{
			if (!OnBeforeForEach())
				return;

			Entities.ForEach((UnitVisualBackend backend, UnitVisualAnimation animation) =>
			{
				if (backend.Presentation == null || !EntityManager.Exists(backend.DstEntity))
					return;

				// main
				CurrentVisualAnimation = animation;
				OnUpdate(backend.DstEntity, backend, animation);
			}).WithStructuralChanges().Run();
		}

		protected abstract void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation);

		protected void ResetIdleTime(Entity targetEntity)
		{
			if (EntityManager.TryGetComponentData(targetEntity, out AnimationIdleTime idleTime))
				EntityManager.SetComponentData(targetEntity, default(AnimationIdleTime));
		}

		protected virtual bool OnBeforeForEach()
		{
			return true;
		}
		
		protected virtual ScriptPlayable<PlayableInnerCall> GetNewPlayable(ref VisualAnimation.ManageData data)
		{
			return ScriptPlayable<PlayableInnerCall>.Create(data.Graph);
		}

		// this should return itself
		protected abstract IAbilityPlayableSystemCalls GetPlayableCalls();

		public IAnimationClipProvider GetCurrentClipProvider()
		{
			return new DefaultAnimationProvider(CurrentVisualAnimation.Presentation, null);
		}

		protected bool InjectAnimationWithSystemData<TSystemData>(VisualAnimation.AddSystem<TSystemData> add = null, VisualAnimation.RemoveSystem<TSystemData> remove = null)
			where TSystemData : struct
		{
			if (!CurrentVisualAnimation.ContainsSystem(SystemType))
			{
				CurrentVisualAnimation.InsertSystem(SystemType,
					add ?? ((ref VisualAnimation.ManageData data, ref TSystemData systemData) =>
					{
						var playable = GetNewPlayable(ref data);
						var behavior = playable.GetBehaviour();

						behavior.Visual = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
						behavior.Initialize(this, data.Graph, playable, data.Index, data.Behavior.RootMixer, GetPlayableCalls());
					}),
					remove ?? ((data, systemData) => { }));
				return true;
			}

			return false;
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

	public interface IAbilityPlayableSystemCalls
	{
		void OnInitialize(PlayableInnerCall behavior);
		void PrepareFrame(PlayableInnerCall behavior, Playable playable, FrameData info);
	}

	public class PlayableInnerCall : BaseAbilityPlayable<IAbilityPlayableSystemCalls>
	{
		private IAbilityPlayableSystemCalls systemCalls;

		protected override void OnInitialize(IAbilityPlayableSystemCalls init)
		{
			systemCalls = init;
			handles     = new List<(AsyncOperationHandle, Action<AsyncOperationHandle> complete)>();
			systemCalls.OnInitialize(this);
		}

		public override void PrepareFrame(Playable playable, FrameData info)
		{
			for (var i = 0; i != handles.Count; i++)
			{
				if (handles[i].handle.IsDone)
				{
					handles[i].complete(handles[i].handle);
					handles.RemoveAt(i--);
				}
			}
			systemCalls.PrepareFrame(this, playable, info);
		}

		private List<(AsyncOperationHandle handle, Action<AsyncOperationHandle> complete)> handles;
		public void AddAsyncOp(AsyncOperationHandle handle, Action<AsyncOperationHandle> complete)
		{
			handles.Add((handle, complete));
		}
	}

	public struct DefaultAnimationProvider : IAnimationClipProvider
	{
		public readonly UnitVisualPresentation                                                      Presentation;
		public readonly Dictionary<string, Dictionary<string, AsyncOperationHandle<AnimationClip>>> CacheClipMap;

		public DefaultAnimationProvider(UnitVisualPresentation presentation, Dictionary<string, Dictionary<string, AsyncOperationHandle<AnimationClip>>> cacheClipMap)
		{
			Presentation = presentation;
			CacheClipMap = cacheClipMap;
		}

		public AsyncOperationHandle<AnimationClip> Provide(string key)
		{
			AsyncOperationHandle<AnimationClip> clipHandle;

			var overrides = Presentation.GetComponents<OverrideObjectComponent>();
			foreach (var overrideComponent in overrides)
			{
				if (overrideComponent.TryGetPresentationObject(key, out AnimationClip clip))
					return Addressables.ResourceManager.CreateCompletedOperation(clip, null);
			}

			if (!string.IsNullOrEmpty(Presentation.animationCacheId))
			{
				if (CacheClipMap.TryGetValue(Presentation.animationCacheId, out var clipMap))
				{
					if (clipMap.TryGetValue(key, out clipHandle)) 
						return clipHandle;
				}
				else
					CacheClipMap[key] = new Dictionary<string, AsyncOperationHandle<AnimationClip>>();
			}
			
			// HOW
			/*foreach (var label in Presentation.animationAssetLabels)
			{
				Addressables.LoadAssetAsync<>()
			}*/

			return Addressables.ResourceManager.CreateCompletedOperation(default(AnimationClip), $"No Animation found with key={key}");
		}
	}
}