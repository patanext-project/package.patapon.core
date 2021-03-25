using System;
using System.Collections.Generic;
using System.IO;
using PataNext.Client.Asset;
using PataNext.Client.Core.Addressables;
using PataNext.Client.DataScripts.Interface;
using PataNext.Client.DataScripts.Interface.Menu.Screens;
using PataNext.Client.DataScripts.Interface.Menu.Settings;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using StormiumTeam.GameBase.Utility.Rendering;
using StormiumTeam.GameBase.Utility.uGUI.Systems;
using UIForia;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Application = UIForia.Application;

namespace PataNext.Client.Systems
{
	public class TestUIForia : SystemBase
	{
		private Application app;
		
		protected override void OnStartRunning()
		{
			/*TemplateSettings settings = new TemplateSettings();
			settings.rootType                   = typeof(Test);
			settings.applicationName            = "PataNext";
			settings.assemblyName               = "package.patapon.client";
			settings.outputPath                 = Path.Combine(UnityEngine.Application.dataPath, "UIForiaGenerated2");
			settings.codeFileExtension          = "generated.cs";
			settings.templateResolutionBasePath = "Packages/package.patapon.core/AddressableData/Client/Interface";
			//settings.dynamicallyCreatedTypes    = new List<Type>() {typeof(Test)};

			app = GameApplication.CreateFromRuntimeTemplates(settings, World.GetExistingSystem<ClientCreateCameraSystem>().UICamera, element => {});*/
		}

		protected override void OnUpdate()
		{
			if (app == null)
				return;
		
			app.Update();
			if (Input.GetKeyDown(KeyCode.R))
				app.Refresh();
		}
	}
	
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