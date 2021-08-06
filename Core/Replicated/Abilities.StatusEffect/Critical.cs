using PataNext.Client.Graphics.Animation.Units.Base;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace PataNext.Game.Abilities.Effects
{
	public struct CriticalState : IStatusEffectState
	{
		public float Resistance     { get; set; }
		public float RegenPerSecond { get; set; }
		public float Power          { get; set; }
		public float Immunity       { get; set; }
		public float ReceivePower   { get; set; }
		
		public class Register : RegisterStatusEffectSystemState<CriticalState>
		{
		}
	}
	
	public struct CriticalSettings : IStatusEffectSettings
	{
		public float Resistance     { get; set; }
		public float RegenPerSecond { get; set; }
		public float Power          { get; set; }
		public float Immunity       { get; set; }
		
		public class Register : RegisterStatusEffectSystemSettings<CriticalSettings>
		{
		}
	}
}