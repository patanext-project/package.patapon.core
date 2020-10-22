using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PataNext.Client.Graphics.Animation
{
	public abstract class AnimationMap : IEnumerable<string>
	{
		public readonly string Prefix;

		public AnimationMap(string prefix)
		{
			Prefix = prefix;
		}

		public abstract IEnumerator<string> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public abstract AsyncOperationHandle<AnimationClip> Resolve<TProvider>(string key, TProvider provider)
			where TProvider : IAnimationClipProvider;
	}

	public class AnimationMap<T> : AnimationMap
	{
		public readonly Dictionary<string, T> KeyDataMap;

		public AnimationMap(string prefix) : base(prefix)
		{
			KeyDataMap = new Dictionary<string, T>();
		}

		public override IEnumerator<string> GetEnumerator()
		{
			return KeyDataMap.Keys.GetEnumerator();
		}

		public override AsyncOperationHandle<AnimationClip> Resolve<TProvider>(string key, TProvider provider)
		{
			return provider.Provide(Prefix + key);
		}

		public void Add(string key)
		{
			KeyDataMap.Add(key, default);
		}

		public void Add(string key, T data)
		{
			KeyDataMap.Add(key, data);
		}
	}

	public interface IAnimationClipProvider
	{
		AsyncOperationHandle<AnimationClip> Provide(string key);
	}
}