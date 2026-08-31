using BAModAPI;
using BigAmbitions.Mods;
using Capisoft.Lib.BaUnifiedUI.Options;
using UnityEngine;

namespace AutoShopping
{
    internal static class AutoShoppingConfig
    {
        private const string AutoOpenOnEnterKey = "auto_open_on_enter";
        private const string AutoPickCartKey = "auto_pick_cart";
        private const string RouteLineColorKey = "route_line_color";
        private const string LogEnabledKey = "log_enabled";
        private const string LogVerboseKey = "log_verbose";
        private const string PerfLogKey = "perf_log";

        internal const int MaxItemSlots = 8;
        internal const float VisibilityPollInterval = 0.2f;
        internal const string Version = "1.0.1";

        private const bool DefaultAutoOpenOnEnter = true;
        private const bool DefaultAutoPickCartEnabled = true;
        internal static readonly Color DefaultRouteLineColor = new Color(0.2f, 0.85f, 1f, 0.9f);
        private const string OptionsDefaultsRevisionKey = "options_defaults_rev";
        private const int OptionsDefaultsRevision = 1;

        internal static bool AutoOpenOnEnter { get; private set; } = DefaultAutoOpenOnEnter;
        internal static bool AutoPickCartEnabled { get; private set; } = DefaultAutoPickCartEnabled;
        internal static Color RouteLineColor { get; private set; } = DefaultRouteLineColor;
        internal static bool LogEnabled { get; private set; }
        internal static bool LogVerbose { get; private set; }
        internal static bool LogPerf { get; private set; }

        private static ModContext _context;
        private static BaColorPickerHandle _routeLineColorHandle;

        internal static void Initialize(ModContext context)
        {
            _context = context;
            MigrateOptionsDefaultsIfNeeded();
            AutoOpenOnEnter = LoadModOptionBool(AutoOpenOnEnterKey, DefaultAutoOpenOnEnter);
            AutoPickCartEnabled = LoadModOptionBool(AutoPickCartKey, DefaultAutoPickCartEnabled);
            LogEnabled = LoadLegacyBool(LogEnabledKey, defaultValue: false);
            LogVerbose = LoadLegacyBool(LogVerboseKey, defaultValue: false);
            LogPerf = LoadLegacyBool(PerfLogKey, defaultValue: false);
            ModLog.Initialize(context);
            RegisterOptions();
        }

        internal static void Shutdown()
        {
            AutoShoppingShortcuts.Shutdown();
            _routeLineColorHandle = null;
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
                .AddToggle(AutoOpenOnEnterKey, "autoshopping_option_auto_open_on_enter", DefaultAutoOpenOnEnter,
                    value => AutoOpenOnEnter = value)
                .AddToggle(AutoPickCartKey, "autoshopping_option_auto_cart_on_enter", DefaultAutoPickCartEnabled,
                    value => AutoPickCartEnabled = value)
                .AddColorPicker(
                    RouteLineColorKey,
                    "autoshopping_option_route_line_color",
                    DefaultRouteLineColor,
                    out _routeLineColorHandle,
                    OnRouteLineColorChanged,
                    ModUiText.CreateColorPickerUiText());

            options = AutoShoppingShortcuts.AddOptions(options);

            try
            {
                OptionsService.Register(_context.ModId, options);
                RouteLineColor = _routeLineColorHandle.Color;
                StoreItemRouteService.SetLineColor(RouteLineColor);
            }
            catch (System.Exception ex)
            {
                AutoShoppingShortcuts.Shutdown();
                _routeLineColorHandle = null;
                ModLog.Warn("Failed to register mod options: " + ex.Message);
            }
        }

        private static void OnRouteLineColorChanged(Color color)
        {
            RouteLineColor = color;
            StoreItemRouteService.SetLineColor(color);
        }

        private static void MigrateOptionsDefaultsIfNeeded()
        {
            if (_context == null)
                return;

            var revisionKey = BuildModOptionKey(OptionsDefaultsRevisionKey);
            if (UnityEngine.PlayerPrefs.GetInt(revisionKey, 0) >= OptionsDefaultsRevision)
                return;

            SaveModOptionBool(AutoOpenOnEnterKey, DefaultAutoOpenOnEnter);
            SaveModOptionBool(AutoPickCartKey, DefaultAutoPickCartEnabled);
            UnityEngine.PlayerPrefs.SetInt(revisionKey, OptionsDefaultsRevision);
        }

        private static void SaveModOptionBool(string optionId, bool value)
        {
            if (_context == null || string.IsNullOrEmpty(optionId))
                return;

            UnityEngine.PlayerPrefs.SetInt(BuildModOptionKey(optionId), value ? 1 : 0);
        }

        private static bool LoadModOptionBool(string optionId, bool defaultValue)
        {
            if (_context == null || string.IsNullOrEmpty(optionId))
                return defaultValue;

            var key = BuildModOptionKey(optionId);
            if (UnityEngine.PlayerPrefs.HasKey(key))
                return UnityEngine.PlayerPrefs.GetInt(key) != 0;

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
