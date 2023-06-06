using Coimbra.Services.Events;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Screens.Events;

namespace SS3D.Systems.Screens
{
	public sealed class LobbyScreen : GameScreen
	{
		protected override void OnStart()
		{
			base.OnStart();

			AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
		}

		private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
		{
			GameScreens.SwitchTo(ScreenType);
		}
	}
}