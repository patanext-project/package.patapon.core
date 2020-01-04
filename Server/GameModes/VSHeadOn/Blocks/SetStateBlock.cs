using GmMachine;
using Patapon.Mixed.GameModes.VSHeadOn;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class SetStateBlock : Block
	{
		public readonly MpVersusHeadOn.State               State;
		private         MpVersusHeadOnGameMode.ModeContext m_HeadOnCtx;

		public SetStateBlock(string name, MpVersusHeadOn.State state) : base(name)
		{
			State = state;
		}

		protected override bool OnRun()
		{
			m_HeadOnCtx.Data.PlayState = State;
			return true;
		}

		protected override void OnReset()
		{
			base.OnReset();

			m_HeadOnCtx = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
		}
	}
}