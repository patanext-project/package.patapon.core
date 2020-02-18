using System;
using System.Collections.Generic;
using System.Linq;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using UnityEngine;

namespace DataScripts.Models.GameMode.Structures
{
	public class ChangeSpriteOnHealthUpdate : MonoBehaviour, IBackendReceiver
	{
		[Serializable]
		public struct Obj : IComparable<Obj>
		{
			public float Key;
			public Sprite Value;

			public int CompareTo(Obj other)
			{
				return Key.CompareTo(other.Key);
			}

			public static bool operator <(Obj left, Obj right)
			{
				return left.CompareTo(right) < 0;
			}

			public static bool operator >(Obj left, Obj right)
			{
				return left.CompareTo(right) > 0;
			}

			public static bool operator <=(Obj left, Obj right)
			{
				return left.CompareTo(right) <= 0;
			}

			public static bool operator >=(Obj left, Obj right)
			{
				return left.CompareTo(right) >= 0;
			}
		}

		public Obj[] objs;
		public SpriteRenderer spriteRenderer;
		
		private void OnEnable()
		{
			Array.Sort(objs);
			if (spriteRenderer == null)
				spriteRenderer = GetComponent<SpriteRenderer>();
		}

		public RuntimeAssetBackendBase Backend { get; set; }
		public void OnBackendSet()
		{
		}

		public void OnPresentationSystemUpdate()
		{
			if (!Backend.DstEntityManager.TryGetComponentData(Backend.DstEntity, out LivableHealth health) 
			    // kinda ugly for now but it's needed to show the sprite when there is no team set...
			|| Backend.DstEntityManager.TryGetComponentData(Backend.DstEntity, out Relative<TeamDescription> relativeTeam) && relativeTeam.Target == default)
				spriteRenderer.sprite = objs.LastOrDefault().Value;
			else
			{
				var sprite = objs.LastOrDefault().Value;
				var fraction = 0f;
				if (health.Value > 0 && health.Max > 0)
					fraction = (float) health.Value / health.Max;
				for (var i = 0; i != objs.Length; i++)
				{
					if (fraction <= objs[i].Key)
					{
						sprite = objs[i].Value;
						break;
					}
				}

				spriteRenderer.sprite = sprite;
			}
		}
	}
}