using PataNext.Client.Graphics.UI;
using StormiumTeam.GameBase.Utility.Rendering;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Bubble
{
	public class UISpeechBubblePresentation : UIBubblePresentation
	{
		public Transform[]       BubbleTransforms;
		public UITrianglePoint[] TrianglePoints;

		public TextMeshProUGUI[] Labels;
	}

	public class UISpeechBubbleRenderSystem : BaseRenderSystem<UISpeechBubblePresentation>
	{
		private ClientCreateCameraSystem clientCreateCameraSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			clientCreateCameraSystem = World.GetExistingSystem<ClientCreateCameraSystem>();
		}

		private float3 camPos;

		protected override void PrepareValues()
		{
			if (clientCreateCameraSystem.Camera != null)
			{
				camPos = clientCreateCameraSystem.Camera.transform.position;
			}
		}

		protected override void Render(UISpeechBubblePresentation definition)
		{
			var backend = definition.Backend;
			var entity  = backend.DstEntity;

			var speechBubble = GetComponent<SpeechBubble>(entity);
			if (!EntityManager.HasComponent<SpeechBubbleText>(entity))
				speechBubble.IsEnabled = false;

			if (!speechBubble.IsEnabled)
			{
				definition.transform.localScale = Vector3.Lerp(definition.transform.localScale, Vector3.zero, Time.DeltaTime * 15f);
				return;
			}

			definition.transform.localScale = Vector3.Lerp(definition.transform.localScale, Vector3.one, Time.DeltaTime * 15f);

			var textTarget            = EntityManager.GetComponentData<SpeechBubbleText>(entity);
			var mainTextDesiredWidth  = 0f;
			var mainTextDesiredHeight = 0f;
			foreach (var label in definition.Labels)
			{
				label.text            = textTarget.Value;
				mainTextDesiredWidth  = math.max(label.preferredWidth, mainTextDesiredWidth);
				mainTextDesiredHeight = math.max(label.preferredHeight, mainTextDesiredHeight);
			}

			var translation = GetComponent<Translation>(entity);
			
			var difference = math.clamp(math.distance(camPos, definition.transform.position), 0, 0.5f) * 0.1f;
			foreach (var triangle in definition.TrianglePoints)
			{
				var invTr = triangle.transform.InverseTransformPoint(translation.Value);
				
				triangle.PointA = new Vector2(-(4 + difference), 50);
				triangle.PointB = new Vector2(+(13 + difference), 50);
				triangle.PointC = new Vector2(invTr.x, invTr.y);
			}

			foreach (var bubble in definition.BubbleTransforms)
			{
				bubble.GetComponent<RectTransform>().sizeDelta = new Vector2(mainTextDesiredWidth + 25, mainTextDesiredHeight + 18);
			}

			definition.transform.position = math.lerp(definition.transform.position, translation.Value, Time.DeltaTime * 10f);
		}

		protected override void ClearValues()
		{
			
		}
	}
}