using package.stormiumteam.shared;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct TaterazayKitMarchAction : IComponentData
	{
		public struct Settings : IComponentData
		{
			public byte Flags;

			public bool AutoDefense
			{
				get => MainBit.GetBitAt(Flags, 0) != 0;
				set => MainBit.SetBitAt(ref Flags, 0, value);
			}

			public Settings(bool autoDefense)
			{
				Flags = 0;

				MainBit.SetBitAt(ref Flags, 0, autoDefense);
			}
		}
	}
}