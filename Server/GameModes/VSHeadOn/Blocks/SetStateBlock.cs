using GmMachine;
using Patapon.Mixed.GameModes.VSHeadOn;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class SetStateBlock : Block
	{
		private MpVersusHeadOnGameMode.ModeContext m_HeadOnCtx;

		public readonly MpVersusHeadOn.State State;

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