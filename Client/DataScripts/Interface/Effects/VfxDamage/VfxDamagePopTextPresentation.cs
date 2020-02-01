using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon.Client.OrderSystems.Vfx;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
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
		public int lastDamage;

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
		[UpdateAfter(typeof(ByOtherOrder))]
		public class BySelfOrder : InGameVfxOrderingSystem {}
		public class ByOtherOrder : InGameVfxOrderingSystem {}
		
		
		public Entity LocalPlayer;
		private int m_SelfOrder;
		private int m_OtherOrder;

		protected override void PrepareValues()
		{
			LocalPlayer = this.GetFirstSelfGamePlayer();
			m_SelfOrder = World.GetExistingSystem<BySelfOrder>().Order;
			m_SelfOrder = World.GetExistingSystem<ByOtherOrder>().Order;
		}

		protected override void Render(VfxDamagePopTextPresentation definition)
		{
			var backend = (VfxDamagePopTextBackend) definition.Backend;
			var sortingGroup = backend.GetComponent<SortingGroup>();
			
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

			var dmg = backend.eventData.Damage;
			foreach (var label in definition.damageLabels)
			{
				if (backend.lastDamage != dmg)
				{
					label.text = (dmg > 0 ? "+" : string.Empty) + math.abs(dmg);
				}
				label.maxVisibleCharacters = 0;
			}

			backend.lastDamage = dmg;

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

			var selfRelated = EntityManager.TryGetComponentData(backend.eventData.Destination, out Relative<PlayerDescription> destPlayer) && destPlayer.Target == LocalPlayer
			                   || EntityManager.TryGetComponentData(backend.eventData.Origin, out Relative<PlayerDescription> originPlayer) && originPlayer.Target == LocalPlayer;
			
			foreach (var label in definition.damageLabels)
			{
				var color = backend.eventData.Damage > 0 ? definition.healColor : definition.damageColor;
				if (!selfRelated)
					color = Color.Lerp(color, Color.black, 0.175f);

				label.color = Color.white;
				label.faceColor = color;
			}

			if (selfRelated)
				sortingGroup.sortingOrder = m_SelfOrder;
			else
				sortingGroup.sortingOrder = m_OtherOrder;
		}

		protected override void ClearValues()
		{

		}
	}
}