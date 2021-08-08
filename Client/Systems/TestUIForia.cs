using System;
using PataNext.Client.Core.Addressables;
using PataNext.Client.DataScripts.Interface.Menu.Screens;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Systems
{
	struct TestSettings : IComponentData {}
	struct TestHomeScreenSpawn : IComponentData {}
	
	public class TestHomeScreenPoolingSystem : PoolingSystem<TestHomeScreen.BackendBase, SettingsScreen>
	{
		protected override AssetPath AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Menu()
			              .Folder("Screens")
			              .GetAsset("HomeScreenBuildTest");
		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(TestHomeScreenSpawn));
		}

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		private Canvas m_Canvas;
		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
			{
				m_Canvas = CanvasUtility.Create(World, 0, "ServerRoom", defaultAddRaycaster: true);
			}

			base.SpawnBackend(target);
			var rt = LastBackend.GetComponent<RectTransform>();
			rt.SetParent(m_Canvas.transform, false);

			CanvasUtility.ExtendRectTransform(rt);
		}
	}
	
	public class TestSettingsPoolingSystem : PoolingSystem<SettingsScreen.BackendBase, SettingsScreen>
	{
		protected override AssetPath AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .Menu()
			              .Folder("Screens")
			              .GetAsset("SettingsScreen");
		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(TestSettings));
		}

		protected override Type[] AdditionalBackendComponents => new[] {typeof(RectTransform)};

		private Canvas m_Canvas;
		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
			{
				m_Canvas = CanvasUtility.Create(World, 0, "ServerRoom", defaultAddRaycaster: true);
				//CanvasUtility.DisableInteractionOnActivePopup(World, m_Canvas);
			}

			base.SpawnBackend(target);
			var rt = LastBackend.GetComponent<RectTransform>();
			rt.SetParent(m_Canvas.transform, false);

			var folder = AddressBuilder.Client()
			                           .Interface()
			                           .Menu()
			                           .Folder("Screens")
			                           .Folder("SettingsScreen");
			LastBackend.Data = new SettingsScreenData
			{
				Panels = new []
				{
					folder.GetAsset("GraphicSettingsPanel"),
					folder.GetAsset("AudioSettingsPanel")
				}
			};

			CanvasUtility.ExtendRectTransform(rt);
		}
	}
}