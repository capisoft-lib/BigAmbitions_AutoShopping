using BAModAPI;

namespace AutoShopping
{
    internal static class AutoShoppingConfig
    {
        private const string AutoPickCartKey = "auto_pick_cart";

        internal const int MaxItemSlots = 8;
        internal const float ToggleKeyPollInterval = 0.05f;

        internal static bool AutoOpenOnEnter { get; private set; } = true;
        internal static bool AutoPickCartEnabled { get; private set; }
        internal static bool LogVerbose { get; private set; }

        private static ModContext _context;

        internal static void Initialize(ModContext context)
        {
            _context = context;
            ModLog.Initialize(context);
            AutoOpenOnEnter = true;
            LogVerbose = false;
            AutoPickCartEnabled = LoadBool(AutoPickCartKey, defaultValue: false);
            ModLog.Info("Auto pick cart = " + AutoPickCartEnabled);
        }

        internal static void Shutdown()
        {
            _context = null;
            ModLog.Shutdown();
        }

        internal static void SetAutoPickCartEnabled(bool value)
        {
            if (AutoPickCartEnabled == value)
                return;

            AutoPickCartEnabled = value;
            SaveBool(AutoPickCartKey, value);
            ModLog.Info("Auto pick cart = " + value);
            AutoShoppingToggleHud.RefreshVisual();
        }

        private static bool LoadBool(string optionId, bool defaultValue)
        {
            if (_context == null || string.IsNullOrEmpty(optionId))
                return defaultValue;

            var key = BuildPrefsKey(optionId);
            if (!UnityEngine.PlayerPrefs.HasKey(key))
                return defaultValue;

            return UnityEngine.PlayerPrefs.GetInt(key) != 0;
        }

        private static void SaveBool(string optionId, bool value)
        {
            if (_context == null || string.IsNullOrEmpty(optionId))
                return;

            UnityEngine.PlayerPrefs.SetInt(BuildPrefsKey(optionId), value ? 1 : 0);
        }

        private static string BuildPrefsKey(string optionId) => "mod_" + _context.ModId + "_" + optionId;
    }
}
