using P4TLB.MasterServer;
using Patapon.Mixed.Units;
using Patapon.Mixed.Units.Statistics;
using Patapon4TLB.Core;
using Patapon4TLB.Default.Player;
using Unity.Collections;
using Unity.Entities;

namespace Patapon.Mixed.GameModes
{
	public static class KitTempUtility
	{
		public static void Set(NativeString64 kit, ref UnitStatistics statistics, DynamicBuffer<UnitDefinedAbilities> definedAbilities, ref UnitDisplayedEquipment display)
		{
			statistics.Health              = 1000;
			statistics.Attack              = 24;
			statistics.Defense             = 7;
			statistics.BaseWalkSpeed       = 2f;
			statistics.FeverWalkSpeed      = 2.2f;
			statistics.AttackSpeed         = 2.0f;
			statistics.MovementAttackSpeed = 3.1f;
			statistics.Weight              = 8.5f;
			statistics.AttackSeekRange     = 16f;

			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.BasicMarch), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.BasicBackward), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.BasicRetreat), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.BasicJump), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.BasicParty), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.BasicCharge), 0));

			display = default;
			switch (kit)
			{
				case NativeString64 _ when kit.Equals(UnitKnownTypes.Taterazay):
					SetAsTaterazay(ref statistics, definedAbilities, ref display);
					break;
				case NativeString64 _ when kit.Equals(UnitKnownTypes.Yarida):
					SetAsYarida(ref statistics, definedAbilities, ref display);
					break;
				case NativeString64 _ when kit.Equals(UnitKnownTypes.Yumiyacha):
					SetAsYumiyacha(ref statistics, definedAbilities, ref display);
					break;
			}
		}

		private static void SetAsTaterazay(ref UnitStatistics statistics, DynamicBuffer<UnitDefinedAbilities> definedAbilities, ref UnitDisplayedEquipment display)
		{
			statistics.Health           = 240;
			statistics.Attack           = 24;
			statistics.Defense          = 7;
			statistics.Weight           = 8.5f;
			statistics.AttackMeleeRange = 2.3f;

			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.TateBasicAttack), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.TateBasicDefend), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.TateBasicDefendFrontal), 0, AbilitySelection.Top));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.TateRushAttack), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.TateEnergyField), 0));

			display.Mask           = new NativeString64("Masks/n_taterazay");
			display.LeftEquipment  = new NativeString64("Shields/default_shield");
			display.RightEquipment = new NativeString64("Swords/default_sword");
		}
		
		private static void SetAsYarida(ref UnitStatistics statistics, DynamicBuffer<UnitDefinedAbilities> definedAbilities, ref UnitDisplayedEquipment display)
		{
			statistics.Health           = 190;
			statistics.Attack           = 28;
			statistics.Defense          = 0;
			statistics.Weight           = 6;
			statistics.AttackMeleeRange = 2.6f;

			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.YariBasicAttack), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.YariLeapSpear), 0, AbilitySelection.Top));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.YariBasicDefend), 0));
			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.YariFearSpear), 0));

			display.Mask           = new NativeString64("Masks/n_yarida");
			display.RightEquipment = new NativeString64("Spears/default_spear");
		}
		
		private static void SetAsYumiyacha(ref UnitStatistics statistics, DynamicBuffer<UnitDefinedAbilities> definedAbilities, ref UnitDisplayedEquipment display)
		{
			statistics.Health           = 175;
			statistics.Attack           = 12;
			statistics.Defense          = 0;
			statistics.Weight           = 6;
			statistics.AttackMeleeRange = 1f;

			definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.YumiBasicAttack), 0));
			
			display.Mask           = new NativeString64("Masks/n_yarida");
			display.RightEquipment = new NativeString64("Spears/default_spear");
		}
	}
}