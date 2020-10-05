using System;
using System.Collections.Generic;
using UnityEngine;

namespace PataNext.Client.Graphics.Models
{
	[ExecuteAlways]
	public class PataponBodyController : MonoBehaviour
	{
		private static readonly int BodyDirectionId = Shader.PropertyToID("_BodyDirection");
		private static readonly int BodyColorId = Shader.PropertyToID("_BodyColor");
		
		private static readonly int EyeDirectionId  = Shader.PropertyToID("_EyeDirection");
		private static readonly int EyeColorId  = Shader.PropertyToID("_EyeColor");
		private static readonly int EyeSizeId  = Shader.PropertyToID("_EyeSize");
		
		private static readonly int EyelidTextureId  = Shader.PropertyToID("_EyeTexture");
		private static readonly int EyelidTextureStepId  = Shader.PropertyToID("_EyeTextureStep");
		
		private static readonly int IrisColorId = Shader.PropertyToID("_IrisColor");
		private static readonly int IrisSizeId = Shader.PropertyToID("_IrisSize");

		[Header("Properties")]
		public List<Renderer> rendererArray;

		[Header("Body Settings")]
		//

		public Transform bodyDirection;

		public Color bodyColor = Color.black;

		[Header("Eye Settings")]
		//

		public Transform eyeDirection;

		public Color     eyeColor = Color.white;
		public float     eyeSize  = 0.7f;
		public Texture2D eyelidTexture;
		
		[Range(0f, 1f)]
		public float     eyelidTextureStep;

		[Header("Iris Settings")]
		//

		public Color irisColor = Color.black;

		public float irisSize = 0.45f;

		public MaterialPropertyBlock mpb;

		private void OnEnable()
		{
			mpb = new MaterialPropertyBlock();
		}

		private void LateUpdate()
		{
#if UNITY_EDITOR
			if (bodyDirection == null || eyeDirection == null)
				return;
#endif

			var bFwd = bodyDirection.forward;
			var eFwd = eyeDirection.forward;
			bFwd *= -1;
			eFwd *= -1;

			foreach (var r in rendererArray)
			{
				r.GetPropertyBlock(mpb);
				{
					mpb.SetVector(BodyDirectionId, bFwd);
					mpb.SetColor(BodyColorId, bodyColor);

					mpb.SetVector(EyeDirectionId, eFwd);
					mpb.SetColor(EyeColorId, eyeColor);
					mpb.SetFloat(EyeSizeId, eyeSize);
					mpb.SetTexture(EyelidTextureId, eyelidTexture == null ? Texture2D.blackTexture : eyelidTexture);
					mpb.SetFloat(EyelidTextureStepId, eyelidTextureStep);

					mpb.SetColor(IrisColorId, irisColor);
					mpb.SetFloat(IrisSizeId, irisSize);
				}
				r.SetPropertyBlock(mpb);
			}
		}

#if UNITY_EDITOR
		[UnityEditor.DrawGizmo(UnityEditor.GizmoType.NonSelected | UnityEditor.GizmoType.Selected, typeof(PataponBodyController))]
		public static void Gizmos(Component component, UnityEditor.GizmoType gizmoType)
		{
			var t = (PataponBodyController) component;
			if (UnityEditor.Selection.activeTransform == component.transform)
			{
				var bFwd = t.bodyDirection.forward;
				var eFwd = t.eyeDirection.forward;
				bFwd *= -1;
				eFwd *= -1;

				UnityEngine.Gizmos.color = Color.yellow;
				UnityEngine.Gizmos.DrawRay(t.bodyDirection.position, bFwd);
				UnityEngine.Gizmos.DrawSphere(t.bodyDirection.position + bFwd, 0.1f);
				UnityEngine.Gizmos.color = Color.green;
				UnityEngine.Gizmos.DrawRay(t.eyeDirection.position, eFwd);
				UnityEngine.Gizmos.DrawSphere(t.eyeDirection.position + eFwd, 0.1f);
			}
			else if (UnityEditor.Selection.activeTransform == t.bodyDirection
			         || UnityEditor.Selection.activeTransform == t.eyeDirection)
			{
				var fwd = UnityEditor.Selection.activeTransform.forward;
				fwd *= -1;

				UnityEngine.Gizmos.DrawRay(UnityEditor.Selection.activeTransform.position, fwd);
				UnityEngine.Gizmos.DrawSphere(UnityEditor.Selection.activeTransform.position + fwd, 0.1f);
			}
		}
#endif
	}
}