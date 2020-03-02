using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace DefaultNamespace
{
	[DisplayName("Yes")]
	public class AssetFileProvider : TextDataProvider
	{
		public override bool Initialize(string id, string data)
		{
			Debug.Log("Initialize Provider");
			m_BehaviourFlags = ProviderBehaviourFlags.CanProvideWithFailedDependencies;
			return base.Initialize(id, data);
		}

		public override bool CanProvide(Type t, IResourceLocation location)
		{
			Debug.Log("?");
			return true;
		}

		public override void Provide(ProvideHandle provideHandle)
		{
			Debug.Log($"=> {provideHandle.Location.PrimaryKey}");
		}
	}
}