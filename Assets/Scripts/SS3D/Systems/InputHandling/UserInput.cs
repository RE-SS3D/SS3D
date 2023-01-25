namespace SS3D.Systems.InputHandling
{
    public static class UserInput
    {
        private static IInputService _inputService;

        private static void SetInputService(IInputService inputService)
        {
            _inputService = inputService;
        }

        public static bool GetButtonUp(string buttonName)
        {
            CreateServiceIfNeeded();
            return _inputService.GetButtonUp(buttonName);
        }

        public static bool GetButtonDown(string buttonName)
        {
            CreateServiceIfNeeded();
            return _inputService.GetButtonDown(buttonName);
        }

        public static bool GetButton(string buttonName)
        {
            CreateServiceIfNeeded();
            return _inputService.GetButton(buttonName);
        }

        public static float GetAxisRaw(string axisName)
        {
            CreateServiceIfNeeded();
            return _inputService.GetAxisRaw(axisName);
        }

        private static void CreateServiceIfNeeded()
        {
            if (_inputService == null)
            {
                _inputService = new PlayerInput();
            }
        }
    }
}