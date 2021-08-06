using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;

namespace PataNext.Client.Behaviors
{
	public class RootContainer<T> : IContainer<T>
		where T : Component
	{
		public          bool          DisposeParent;
		
		public readonly IContainer<T> Source;
		public readonly Transform          Root;

		public RootContainer(IContainer<T> source, Transform root, bool disposeParent = true)
		{
			DisposeParent = disposeParent;
			Source        = source;
			Root          = root;

			source.onAdded.AddListener(OnAdded);
		}

		private void OnAdded((T element, int index) args)
		{
			var (element, index) = args;

			var tr = element.transform;
			tr.SetParent(Root, false);
			tr.SetSiblingIndex(index);
		}

		public void SetSize(int size)
		{
			Source.SetSize(size);
		}

		public UniTask Warm()
		{
			return Source.Warm();
		}

		public UniTask<(T element, int index)> Add()
		{
			return Source.Add();
		}

		public UnityEvent<(T element, int index)>             onAdded            => Source.onAdded;
		public UnityEvent<World.NoAllocReadOnlyCollection<T>> onCollectionUpdate => Source.onCollectionUpdate;
		public UnityEvent<(T element, int index)>             onRemoved          => Source.onRemoved;

		public World.NoAllocReadOnlyCollection<T> GetList()
		{
			return Source.GetList();
		}

		public void Dispose()
		{
			if (DisposeParent)
				Source.Dispose();
		}
	}

	public static class GridWithContainerExtensions
	{
		public static RootContainer<T> WithTransformRoot<T>(this IContainer<T> source, Transform root, bool disposeParent = true)
			where T : Component
		{
			return new RootContainer<T>(source, root, disposeParent);
		}
	}
}