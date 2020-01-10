using System;
using Unity.Collections;
using Unity.Entities;

namespace Patapon.Mixed.Units.Statistics
{
	// Everyone will call the kit as classes, but here this is just for differentiating the Class 'object' type and Class 'unit type' word.
	public struct UnitCurrentKit : IComponentData
	{
		public NativeString64 Value;
	}

	public static class UnitKnownTypes
	{
		public static readonly NativeString64 Taterazay = new NativeString64("taterazay");
		public static readonly NativeString64 Yarida    = new NativeString64("yarida");
	}
}