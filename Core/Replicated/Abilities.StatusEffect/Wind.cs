using PataNext.Client.Graphics.Animation.Units.Base;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace PataNext.Game.Abilities.Effects
{
	public struct WindState : IStatusEffectState
	{
		public float Resistance     { get; set; }
		public float RegenPerSecond { get; set; }
		public float Power          { get; set; }
		public float Immunity       { get; set; }
		public float ReceivePower   { get; set; }
		
		public class Register : RegisterStatusEffectSystemState<WindState>
		{
		}
	}
	
	public struct WindSettings : IStatusEffectSettings
	{
		public float Resistance     { get; set; }
		public float RegenPerSecond { get; set; }
		public float Power          { get; set; }
		public float Immunity       { get; set; }
		
		public class Register : RegisterStatusEffectSystemSettings<WindSettings>
		{
		}
	}
}