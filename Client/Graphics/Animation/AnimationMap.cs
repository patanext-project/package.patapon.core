using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PataNext.Client.Graphics.Animation
{
	public class AnimationMap<T> : IEnumerable<string>
	{
		public readonly string                Prefix;
		public readonly Dictionary<string, T> KeyDataMap;

		public AnimationMap(string prefix)
		{
			KeyDataMap = new Dictionary<string, T>();
		}

		public T Resolve(IAnimationClipProvider provider)
		{

		}

		public IEnumerator<string> GetEnumerator()
		{
			return KeyDataMap.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
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