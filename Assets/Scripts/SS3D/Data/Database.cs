using Coimbra;

namespace SS3D.Data
{
    public static class Database
    {                                                                   
        public static IconDatabase _icons;

        public static IconDatabase Icons => _icons != null ? _icons : GetIcons();

        public static IconDatabase GetIcons()
        {
            ScriptableSettings.TryGet(out IconDatabase icons);

            _icons = icons;

            return _icons;
        }
    }
}