using BAModAPI;
using BigAmbitions.Mods;

namespace AutoShopping
{
    internal static class AutoShoppingConfig
    {
        private const string AutoOpenOnEnterKey = "auto_open_on_enter";
        private const string AutoPickCartKey = "auto_pick_cart";
        private const string LogEnabledKey = "log_enabled";
        private const string LogVerboseKey = "log_verbose";
        private const string PerfLogKey = "perf_log";

        internal const int MaxItemSlots = 8;
        internal const float VisibilityPollInterval = 0.2f;
        internal const string Version = "0.12.0";

        internal static bool AutoOpenOnEnter { get; private set; } = true;
        internal static bool AutoPickCartEnabled { get; private set; } = true;
        internal static bool LogEnabled { get; private set; }
        internal static bool LogVerbose { get; private set; }
        internal static bool LogPerf { get; private set; }

        private static ModContext _context;

        internal static void Initialize(ModContext context)
        {
            _context = context;
            AutoOpenOnEnter = LoadModOptionBool(AutoOpenOnEnterKey, defaultValue: true);
            AutoPickCartEnabled = LoadModOptionBool(AutoPickCartKey, defaultValue: true);
            LogEnabled = LoadLegacyBool(LogEnabledKey, defaultValue: false);
            LogVerbose = LoadLegacyBool(LogVerboseKey, defaultValue: false);
            LogPerf = LoadLegacyBool(PerfLogKey, defaultValue: false);
            ModLog.Initialize(context);
            RegisterOptions();
        }

        internal static void Shutdown()
        {
            if (_context != null)
                OptionsService.RemoveModOptions(_context.ModId);

            _context = null;
            ModLog.Shutdown();
        }

        private static void RegisterOptions()
        {
            if (_context == null)
                return;

            var options = new ModOptions()
                .AddHeader("autoshopping_options_header")
                .AddToggle(AutoOpenOnEnterKey, "autoshopping_option_auto_open_on_enter", AutoOpenOnEnter,
                    value => AutoOpenOnEnter = value)
                .AddToggle(AutoPickCartKey, "autoshopping_option_auto_cart_on_enter", AutoPickCartEnabled,
                    value => AutoPickCartEnabled = value);

            try
            {
                OptionsService.Register(_context.ModId, options);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("Failed to register mod options: " + ex.Message);
            }
        }

        private static bool LoadModOptionBool(string optionId, bool defaultValue)
        {
            if (_context == null || string.IsNullOrEmpty(optionId))
                return defaultValue;

            var key = BuildModOptionKey(optionId);
            if (UnityEngine.PlayerPrefs.HasKey(key))
                return UnityEngine.PlayerPrefs.GetInt(key) != 0;

            var legacyKey = BuildLegacyPrefsKey(optionId);
            if (UnityEngine.PlayerPrefs.HasKey(legacyKey))
            {
                var value = UnityEngine.PlayerPrefs.GetInt(legacyKey) != 0;
                UnityEngine.PlayerPrefs.SetInt(key, value ? 1 : 0);
                return value;
            }

            return defaultValue;
        }

        private static bool LoadLegacyBool(string optionId, bool defaultValue)
        {
            if (_context == null || string.IsNullOrEmpty(optionId))
                return defaultValue;

            var key = BuildLegacyPrefsKey(optionId);
            if (!UnityEngine.PlayerPrefs.HasKey(key))
                return defaultValue;

            return UnityEngine.PlayerPrefs.GetInt(key) != 0;
        }

        private static string BuildModOptionKey(string optionId) => "m:" + _context.ModId + ":" + optionId;

        private static string BuildLegacyPrefsKey(string optionId) => "mod_" + _context.ModId + "_" + optionId;
    }
}
