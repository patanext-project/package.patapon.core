using System;
using System.Globalization;
using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using GameHost.Core;
using Newtonsoft.Json;
using PataNext.Client.Asset;
using PataNext.Client.Behaviors;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Core.DOTSxUI.Components;
using PataNext.Client.Systems;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.Menu.Screens
{
	public class TestHomeScreen : UIPresentation<ValueTuple>
	{
		public class BackendBase : UIBackend<ValueTuple, TestHomeScreen>
		{}

		public bool IsHidden;
		
		[SerializeField] private GameObject screenStackRoot;
		
		[SerializeField] private GameObject settingsReference;

		[SerializeField] private Button         playButton;
		[SerializeField] private TMP_InputField inputField;
		
		[SerializeField] private Button settingsButton;
		[SerializeField] private Button quitButton;

		private IContainer<SettingsScreen> settingsContainer;

		private void Awake()
		{
			settingsContainer = ContainerPool.FromPresentation<SettingsScreen.BackendBase, SettingsScreen>(settingsReference)
			                                 .WithTransformRoot(screenStackRoot.transform);
			settingsContainer.Add().ContinueWith(args =>
			{
				var backend = args.element.Backend;
				backend.DstEntityManager.AddComponent<UIScreen>(backend.BackendEntity);
				
				args.element.gameObject.SetActive(false);
				
				var folder = AddressBuilder.Client()
				                           .Interface()
				                           .Menu()
				                           .Folder("Screens")
				                           .Folder("SettingsScreen");
				args.element.Data = new SettingsScreenData
				{
					Panels = new[]
					{
						folder.GetAsset("GraphicSettingsPanel"),
						folder.GetAsset("AudioSettingsPanel")
					}
				};
			});
		}
		
		// Handles IPv4 and IPv6 notation.
		public static IPEndPoint CreateIPEndPoint(string endPoint)
		{
			string[] ep = endPoint.Split(':');
			if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
			IPAddress ip;
			if (ep.Length > 2)
			{
				if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
				{
					throw new FormatException("Invalid ip-adress");
				}
			}
			else
			{
				if (!IPAddress.TryParse(ep[0], out ip))
				{
					throw new FormatException("Invalid ip-adress");
				}
			}
			int port;
			if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
			{
				throw new FormatException("Invalid port");
			}
			return new IPEndPoint(ip, port);
		}

		private NoticeRpc noticeRpc;
		public override void OnBackendSet()
		{
			base.OnBackendSet();

			noticeRpc = Backend.DstEntityManager.World
			                   .GetExistingSystem<NoticeRpc>();
			
			Attach(noticeRpc.OnNoticeReceived, () =>
			{
				playButton.GetComponentInChildren<TextMeshProUGUI>().text = noticeRpc.Notice.IsConnectedToServer ? "Disconnect" : "Connect";
			});

			OnClick(playButton, async () =>
			{
				var gameHostConnector = Backend.DstEntityManager.World
				                               .GetExistingSystem<GameHostConnector>();

				using var buffer = new DataBufferWriter(128, Allocator.Temp);
				if (noticeRpc.Notice.IsConnectedToServer)
				{
					gameHostConnector.BroadcastRequest("disconnect_from_server", buffer);
				}
				else
				{
					IsHidden = true;
					
					var endPoint = CreateIPEndPoint(inputField.text);

					buffer.WriteStaticString(JsonConvert.SerializeObject(new
					{
						Host = endPoint.Address.ToString(),
						Port = (ushort) endPoint.Port
					}));

					gameHostConnector.BroadcastRequest("connect_to_server", buffer);
				}
			});
			OnClick(settingsButton, async () =>
			{
				Debug.LogError("settings!");
				
				await settingsContainer.Warm();

				var screen = settingsContainer.GetList()[0];
				screen.gameObject.SetActive(true);
				screen.transform.SetAsLastSibling();


			});
			OnClick(quitButton, async () =>
			{
				Application.Quit(0);
			});
		}

		protected override void OnDataUpdate(ValueTuple data)
		{
		}

		private void OnDestroy()
		{
			settingsContainer.Dispose();
		}

		public class RenderSystem : BaseRenderSystem<TestHomeScreen>
		{
			protected override void PrepareValues()
			{
			}

			private float delaySend;

			protected override void Render(TestHomeScreen definition)
			{
				if (Keyboard.current.escapeKey.wasPressedThisFrame)
				{
					definition.IsHidden = !definition.IsHidden;
				}
				
				definition.screenStackRoot.SetActive(!definition.IsHidden);
				
				var settingsCollection = definition.settingsContainer.GetList();
				if (settingsCollection.Count > 0)
				{
					var screen = settingsCollection[0];
					if (HasComponent<UIScreen.WantToQuit>(screen.Backend.BackendEntity))
					{
						Debug.LogError("remove settings");

						EntityManager.RemoveComponent<UIScreen.WantToQuit>(screen.Backend.BackendEntity);

						screen.gameObject.SetActive(false);
					}
				}

				delaySend -= Time.DeltaTime;

				if (delaySend < 0)
				{
					definition.noticeRpc.Send();
					delaySend = 0.1f;
				}
			}

			protected override void ClearValues()
			{
			}
		}
	}
}