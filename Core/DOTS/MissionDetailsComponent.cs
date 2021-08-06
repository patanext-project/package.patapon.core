using System;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;

namespace PataNext.UnityCore.DOTS
{
	public struct MissionDetailsComponent : ISharedComponentData, IEquatable<MissionDetailsComponent>
	{
		public ResPath Path;
		public ResPath Scenar;

		public string Name;

		public bool Equals(MissionDetailsComponent other)
		{
			return Path.Equals(other.Path) && Scenar.Equals(other.Scenar) && Name == other.Name;
		}

		public override bool Equals(object obj)
		{
			return obj is MissionDetailsComponent other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Path.GetHashCode();
				hashCode = (hashCode * 397) ^ Scenar.GetHashCode();
				hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}