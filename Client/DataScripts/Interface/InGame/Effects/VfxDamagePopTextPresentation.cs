using package.stormiumteam.shared.ecs;
using PataNext.Client.OrderSystems.Vfx;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems.Ext;
using StormiumTeam.GameBase.GamePlay.Events;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace PataNext.Client.DataScripts.Interface.InGame
{
	public class VfxDamagePopTextPresentation : RuntimeAssetPresentation
	{
		public TextMeshPro[] damageLabels;
		public Animator      animator;

		public GameObject defaultHitVfx, criticalHitVfx, healVfx;

		public Color damageColor;
		public Color healColor;
	}

	public class VfxDamagePopTextBackend : RuntimeAssetBackend<VfxDamagePopTextPresentation>
	{
		public bool isPlayQueued;
		public int  lastDamage;

		public TargetDamageEvent eventData;

		public double startTime;
		public double setToPoolAt;

		public override void OnReset()
		{
			isPlayQueued = false;
			lastDamage   = int.MaxValue;
		}

		public void Play(TargetDamageEvent damageEvent)
		{
			isPlayQueued = true;
			eventData    = damageEvent;
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class VfxDamagePopTextRenderSystem : BaseRenderSystem<VfxDamagePopTextPresentation>
	{
		[UpdateAfter(typeof(ByOtherOrder))]
		public class BySelfOrder : InGameVfxOrderingSystem
		{
		}

		public class ByOtherOrder : InGameVfxOrderingSystem
		{
		}


		public  Entity LocalPlayer;
		private int    m_SelfOrder;
		private int    m_OtherOrder;

		protected override void PrepareValues()
		{
			LocalPlayer = this.GetFirstSelfGamePlayer();
			m_SelfOrder = World.GetExistingSystem<BySelfOrder>().Order;
			m_SelfOrder = World.GetExistingSystem<ByOtherOrder>().Order;
		}

		protected override void Render(VfxDamagePopTextPresentation definition)
		{
			var backend      = (VfxDamagePopTextBackend) definition.Backend;
			var sortingGroup = backend.GetComponent<SortingGroup>();

			if (!backend.isPlayQueued)
			{
				var count = (int) ((Time.ElapsedTime - backend.startTime) * 15);
				foreach (var label in definition.damageLabels)
				{
					label.maxVisibleCharacters = count;
					if (Time.ElapsedTime + 10 > backend.startTime) 
						continue;
					
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
				label.text                 = (dmg > 0 ? "+" : string.Empty) + math.abs((int) math.round(dmg));
				label.maxVisibleCharacters = 0;
			}
			
			backend.lastDamage = (int) dmg;

			definition.animator.SetTrigger("OnHit");

			Translation translationTarget;
			if (!EntityManager.TryGetComponentData(backend.DstEntity, out translationTarget))
			{
				if (!EntityManager.TryGetComponentData(backend.eventData.Victim, out translationTarget))
					translationTarget = default;
				else
					translationTarget.Value.y += 0.25f;
			}

			backend.isPlayQueued = false;
			backend.startTime    = Time.ElapsedTime;
			backend.transform.position = new Vector3
			{
				x = translationTarget.Value.x,
				y = translationTarget.Value.y,
				z = -10
			};

			dmg                          = math.abs(dmg);

			var selfRelated = EntityManager.TryGetComponentData(backend.eventData.Victim, out Relative<PlayerDescription> destPlayer) && destPlayer.Target == LocalPlayer
			                  || EntityManager.TryGetComponentData(backend.eventData.Instigator, out Relative<PlayerDescription> originPlayer) && originPlayer.Target == LocalPlayer;

			var scale = 0.45f;
			if (selfRelated)
			{
				if (dmg >= 10)
					scale += 0.09f;
				if (dmg >= 50)
					scale += 0.07f;
				if (dmg >= 75)
					scale += 0.05f;
				if (dmg >= 100)
					scale += 0.03f;
			}
			else
			{
				scale = 0.475f;
			}

			backend.transform.localScale = Vector3.one * scale;
			
			var hitType = HitType.None;
			if (backend.eventData.Damage <= 0)
				hitType = HitType.Default;
			else if (backend.eventData.Damage > 0)
				hitType = HitType.Heal;

			foreach (var label in definition.damageLabels)
			{
				var color = hitType == HitType.Heal ? definition.healColor : definition.damageColor;
				if (!selfRelated)
					color = Color.Lerp(color, Color.black, 0.225f);

				label.color = color;
			}

			definition.defaultHitVfx.SetActive(hitType == HitType.Default);
			definition.defaultHitVfx.GetComponentInChildren<Animator>()
			          .SetTrigger("OnHit");

			definition.defaultHitVfx.transform.Rotate(0, 0, 25 * Random.value);
			
			definition.healVfx.SetActive(hitType == HitType.Heal);
			definition.healVfx.GetComponentInChildren<Animator>()
			          .SetTrigger("OnHit");
			
			definition.criticalHitVfx.SetActive(hitType == HitType.Critical);
			definition.criticalHitVfx.GetComponentInChildren<Animator>()
			          .SetTrigger("OnHit");

			if (selfRelated)
				sortingGroup.sortingOrder = m_SelfOrder;
			else
				sortingGroup.sortingOrder = m_OtherOrder;
		}

		protected override void ClearValues()
		{

		}

		enum HitType
		{
			None,
			Default,
			Critical,
			Heal
		}
	}
}