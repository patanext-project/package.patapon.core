using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

namespace package.patapon.core.Models.InGame.VFXDamage
{
	public class VfxDamagePopTextPresentation : RuntimeAssetPresentation<VfxDamagePopTextPresentation>
	{
		public TextMeshPro[] damageLabels;
		public Animator      animator;

		public Color damageColor;
		public Color healColor;
	}

	public class VfxDamagePopTextBackend : RuntimeAssetBackend<VfxDamagePopTextPresentation>
	{
		public bool isPlayQueued;

		public TargetDamageEvent eventData;

		public double startTime;
		public double setToPoolAt;

		public void Play(TargetDamageEvent damageEvent)
		{
			isPlayQueued = true;
			eventData       = damageEvent;
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class VfxDamagePopTextRenderSystem : BaseRenderSystem<VfxDamagePopTextPresentation>
	{
		protected override void PrepareValues()
		{

		}

		protected override void Render(VfxDamagePopTextPresentation definition)
		{
			var backend = (VfxDamagePopTextBackend) definition.Backend;
			if (!backend.isPlayQueued)
			{
				var count = (int) ((Time.ElapsedTime - backend.startTime) * 15);
				foreach (var label in definition.damageLabels)
				{
					label.maxVisibleCharacters = count;
					label.ForceMeshUpdate(true);

					var textInfo = label.textInfo;
					for (var t = 0; t != textInfo.characterCount; t++)
					{
						var charInfo = textInfo.characterInfo[t];
						var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];

						var sourceTopLeft     = charInfo.topLeft;
						var sourceTopRight    = charInfo.topRight;
						var sourceBottomLeft  = charInfo.bottomLeft;
						var sourceBottomRight = charInfo.bottomRight;

						var offset = (sourceTopLeft + sourceBottomRight) * 0.5f;

						var wave   = (float) (math.max((t - (Time.ElapsedTime - backend.startTime) * 15) * 1.33f, 0) + 1);
						var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, wave * Vector3.one);

						var destinationTopLeft     = matrix.MultiplyPoint3x4(sourceTopLeft - offset) + offset;
						var destinationTopRight    = matrix.MultiplyPoint3x4(sourceTopRight - offset) + offset;
						var destinationBottomLeft  = matrix.MultiplyPoint3x4(sourceBottomLeft - offset) + offset;
						var destinationBottomRight = matrix.MultiplyPoint3x4(sourceBottomRight - offset) + offset;

						meshInfo.vertices[charInfo.vertexIndex + 1] = destinationTopLeft;     // TL
						meshInfo.vertices[charInfo.vertexIndex + 2] = destinationTopRight;    // TR
						meshInfo.vertices[charInfo.vertexIndex + 0] = destinationBottomLeft;  // BL
						meshInfo.vertices[charInfo.vertexIndex + 3] = destinationBottomRight; // BR

						label.mesh.vertices = meshInfo.vertices;
					}
				}

				return;
			}

			foreach (var label in definition.damageLabels)
			{
				label.text                 = math.abs(backend.eventData.Damage).ToString();
				label.maxVisibleCharacters = 0;
			}

			definition.animator.SetTrigger("OnHit");

			Translation translationTarget;
			if (!EntityManager.TryGetComponentData(backend.DstEntity, out translationTarget))
				if (!EntityManager.TryGetComponentData(backend.eventData.Destination, out translationTarget))
					translationTarget = default;

			backend.isPlayQueued = false;
			backend.startTime    = Time.ElapsedTime;
			backend.transform.position = new Vector3
			{
				x = translationTarget.Value.x,
				y = translationTarget.Value.y,
				z = -10
			};

			foreach (var label in definition.damageLabels)
			{
				label.color = backend.eventData.Damage > 0 ? definition.healColor : definition.damageColor;
			}
		}

		protected override void ClearValues()
		{

		}
	}
}