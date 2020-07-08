using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Patapon.Mixed.GamePlay;
using Unity.Collections;
using Unity.Serialization;
using Unity.Serialization.Json;
using UnityEngine;

namespace DefaultNamespace
{
	public static class StatisticModifierJson
	{
		public static string Convert(StatisticModifier modifier)
		{
			return string.Empty;
		}

		private static Dictionary<string, StatisticModifier> s_HashMap = new Dictionary<string, StatisticModifier>();

		/// <summary>
		/// don't mute the dictionary!
		/// </summary>
		/// <returns></returns>
		public static Dictionary<string, StatisticModifier> FromMap(string json)
		{
			s_HashMap.Clear();
			Debug.Log($"MAPAMAP\n{json}\n{json.ToLower()}");
			using (var stringReader = new MemoryStream(Encoding.UTF8.GetBytes(json)))
			using (var reader = new SerializedObjectReader(stringReader))
			{
				var view = reader.ReadObject();
				if (!view.TryGetMember("modifiers", out var modifierMember))
					return s_HashMap;

				var modifiers = modifierMember.Value().AsArrayView();
				foreach (var modifier in modifiers)
				{
					var modifierObj = modifier.AsObjectView();
					var id          = modifierObj["id"].AsStringView();

					var mod = StatisticModifier.Default;
					Deserialize(ref mod, modifierObj);
					s_HashMap[id.ToString()] = mod;
				}
			}

			return s_HashMap;
		}

		private static void Deserialize(ref StatisticModifier modifier, SerializedObjectView view)
		{
			void update(ref float original, string member)
			{
				if (view.TryGetValue(member, out var value))
					original = value.AsFloat();
			}

			update(ref modifier.Attack, "attack");
			update(ref modifier.Defense, "defense");

			update(ref modifier.ReceiveDamage, "receive_damage");

			update(ref modifier.MovementSpeed, "movement_speed");
			update(ref modifier.MovementAttackSpeed, "movement_attack_speed");
			update(ref modifier.MovementReturnSpeed, "movement_return_speed");
			update(ref modifier.AttackSpeed, "attack_speed");

			update(ref modifier.AttackSeekRange, "attack_seek_range");

			update(ref modifier.Weight, "weight");
		}

		public static StatisticModifier From(string json)
		{
			var modifier = StatisticModifier.Default;
			using (var reader = new SerializedObjectReader(json.ToLower()))
			{
				var entity = reader.ReadObject();
				Deserialize(ref modifier, entity);
			}

			return modifier;
		}
	}
}