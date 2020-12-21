using System;
using PataNext.Client.Asset;
using PataNext.Client.Behaviors;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface
{
	public struct TestData
	{
		public int clickCount;
	}

	public class Asset : UIPresentation<TestData>
	{
		protected override void OnDataUpdate(TestData data)
		{
		}
	}
	
	public class TestUIPresentation : UIPresentation<TestData>
	{
		[SerializeField] private Button          button;
		[SerializeField] private TextMeshProUGUI label;

		[SerializeField] private GameObject assetReference;
		
		private IContainer<Asset> assetContainer;

		private void Awake()
		{
			assetContainer = ContainerPool.FromGameObject<Asset>(assetReference);
		}

		private void OnDestroy()
		{
			assetContainer.Dispose();
		}

		public override void OnBackendSet()
		{
			base.OnBackendSet();

			OnClick(button, () =>
			{
				var data = Data;
				data.clickCount++;

				Data = data;
			});
		}

		protected override void OnDataUpdate(TestData data)
		{
			label.text = data.clickCount.ToString();
		}

		public class Backend : UIBackend<TestData, TestUIPresentation>
		{
		}
	}
	
	public struct TestUIComponent : IComponentData {}
}