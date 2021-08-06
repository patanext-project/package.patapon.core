using System;
using System.Collections.Generic;
using UnityEngine;

namespace PataNext.Client.Core.Addressables
{
	[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/DefaultCityScenes", order = 1)]
	public class DefaultCityScenes : ScriptableObject
	{
		public static DefaultCityScenes Singleton { get; private set; }

		[Serializable]
		public struct Scene
		{
			public string     key;
			public GameObject prefab;
		}

		public List<Scene> scenes;

		private void OnEnable()
		{
			Singleton = this;
		}

		private void OnDisable()
		{
			if (Singleton == this)
				Singleton = null;
		}
	}
}