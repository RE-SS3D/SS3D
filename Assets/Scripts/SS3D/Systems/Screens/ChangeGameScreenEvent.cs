using Coimbra.Services.Events;

namespace SS3D.Systems.Screens
{
    public partial struct ChangeGameScreenEvent : IEvent
    {
        public readonly ScreenType Screen;

        public ChangeGameScreenEvent(ScreenType screen)
        {
            Screen = screen;
        }
    }
}