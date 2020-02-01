using System;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Mathematics;
using UnityEngine;

namespace Patapon.Mixed.GamePlay
{
	public static class AbilityUtility
	{
		public static int CompileStat(GameComboState combo, int original, double defaultMultiplier, double feverMultiplier, double perfectMultiplier)
		{
			var originalF = original * defaultMultiplier;
			if (combo.IsFever)
			{
				originalF *= feverMultiplier;
				if (combo.IsPerfect)
					originalF *= perfectMultiplier;
			}

			return original + ((int) Math.Round(originalF) - original);
		}

		public static float CompileStat(GameComboState combo, float original, double defaultMultiplier, double feverMultiplier, double perfectMultiplier)
		{
			var originalF = original * defaultMultiplier;
			if (combo.IsFever)
			{
				originalF *= feverMultiplier;
				if (combo.IsPerfect)
					originalF *= perfectMultiplier;
			}

			return (float) (original + (originalF - original));
		}

		public static UnitPlayState CompileStat(GameComboState combo, UnitPlayState playState, in StatisticModifier defaultModifier, in StatisticModifier feverModifier, in StatisticModifier perfectModifier)
		{
			defaultModifier.Multiply(ref playState);
			if (combo.IsFever)
			{
				feverModifier.Multiply(ref playState);
				if (combo.IsPerfect)
					perfectModifier.Multiply(ref playState);
			}

			return playState;
		}

		public static float GetTargetVelocityX(float3 targetPosition, float3 previousPosition, float3 previousVelocity, UnitPlayState playState, float acceleration, UTick tick, float deaccel_distance = -1, float deaccel_distance_max = -1)
		{
			var speed   = math.lerp(math.abs(previousVelocity.x), playState.MovementAttackSpeed, playState.GetAcceleration() * acceleration * tick.Delta);
			if (deaccel_distance >= 0)
			{
				var dist = math.distance(targetPosition.x, previousPosition.x);
				if (dist > deaccel_distance && dist < deaccel_distance_max)
				{
					speed *= math.unlerp(deaccel_distance, deaccel_distance_max, dist);
					speed =  math.max(speed, tick.Delta);
				}
			}
			
			Debug.Log(speed);
			var newPosX = Mathf.MoveTowards(previousPosition.x, targetPosition.x, speed * tick.Delta);

			return (newPosX - previousPosition.x) / tick.Delta;
		}
	}
}