using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Components;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Modules;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;

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

		private DefaultAnimationProvider provider = new DefaultAnimationProvider(new Dictionary<string, Dictionary<string, Task<AnimationClip>>>());
		public IAnimationClipProvider GetCurrentClipProvider()
		{
			provider.Presentation = CurrentVisualAnimation.Presentation;
			return provider;
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
			handles     = new List<(Task, Action<Task> complete)>();
			systemCalls.OnInitialize(this);
		}

		public override void PrepareFrame(Playable playable, FrameData info)
		{
			for (var i = 0; i != handles.Count; i++)
			{
				if (handles[i].handle.IsCompleted)
				{
					try
					{
						handles[i].complete(handles[i].handle);
					}
					catch (Exception ex)
					{
						Debug.LogException(ex);
					}
					
					handles.RemoveAt(i--);
				}
			}

			systemCalls.PrepareFrame(this, playable, info);
		}

		private List<(Task handle, Action<Task> complete)> handles;

		public void AddAsyncOp(Task handle, Action<Task> complete)
		{
			handles.Add((handle, complete));
		}
	}

	public class DefaultAnimationProvider : IAnimationClipProvider
	{
		public UnitVisualPresentation Presentation;
		
		public readonly Dictionary<string, Dictionary<string, Task<AnimationClip>>> CacheClipMap;

		public DefaultAnimationProvider(Dictionary<string, Dictionary<string, Task<AnimationClip>>> cacheClipMap)
		{
			CacheClipMap = cacheClipMap;
		}

		public Task<AnimationClip> Provide(string key)
		{
			Task<AnimationClip> clipHandle;

			var canAccessToCache = !string.IsNullOrEmpty(Presentation.animationCacheId);

			var overrides = Presentation.GetComponents<OverrideObjectComponent>();
			foreach (var overrideComponent in overrides)
			{
				if (overrideComponent.TryGetPresentationObject(key, out AnimationClip clip))
					return Task.FromResult(clip);
			}

			if (canAccessToCache)
			{
				if (CacheClipMap.TryGetValue(Presentation.animationCacheId, out var clipMap))
				{
					Console.WriteLine($"{clipMap.TryGetValue(key, out clipHandle)} - {key}");
					if (clipMap.TryGetValue(key, out clipHandle))
						return clipHandle;
				}
				else
					CacheClipMap[Presentation.animationCacheId] = new Dictionary<string, Task<AnimationClip>>();
			}

			var computedFolders = new AssetPath[Presentation.animationAssetFolders.Length];
			if (computedFolders.Length == 0)
				return Task.FromException<AnimationClip>(new KeyNotFoundException("(empty folders) no clip found for key=" + key));

			for (var i = 0; i != computedFolders.Length; i++)
			{
				var path = Presentation.animationAssetFolders[i];
				computedFolders[i] = new AssetPath(path.bundle, path.asset + "/" + key);
			}

			// TODO: Cache?
			if (computedFolders.Length == 1)
			{
				var task = AssetManager.LoadAssetAsync<AnimationClip>(computedFolders[0]).AsTask();
				if (canAccessToCache)
				{
					CacheClipMap[Presentation.animationCacheId][key] = task;
				}

				return task;
			}

			// TODO: Use async in the origin method instead?
			return UniTask.RunOnThreadPool(async () =>
			{
				await UniTask.SwitchToMainThread();

				foreach (var assetPath in computedFolders)
				{
					var task   = AssetManager.LoadAssetAsync<AnimationClip>(assetPath).AsTask();
					var result = await task;

					if (result != null)
					{
						if (canAccessToCache)
							CacheClipMap[Presentation.animationCacheId][key] = task;

						return result;
					}
				}

				throw new KeyNotFoundException("(multiple folders) no clip found for key=" + key);
			}).AsTask();
		}
	}
}