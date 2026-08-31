using BAModAPI;
using BigAmbitions.Mods;
using Capisoft.Lib.BaUnifiedUI.Shortcuts;
using UnityEngine.InputSystem;

namespace AutoShopping
{
    /// <summary>Owns configurable shortcuts that mirror the visible AutoShopping buttons.</summary>
    internal static class AutoShoppingShortcuts
    {
        private const string ToggleShortcutOptionId = "toggle_shortcut";
        private const string ClearShortcutOptionId = "clear_list_shortcut";
        private const string PayShortcutOptionId = "pay_shortcut";
        private const string CancelActionsShortcutOptionId = "cancel_actions_shortcut";

        private static readonly BaKeybind DefaultToggleShortcut = new BaKeybind(Key.F8);
        private static BaKeybindHandle _toggleShortcut;
        private static BaKeybindHandle _clearShortcut;
        private static BaKeybindHandle _payShortcut;
        private static BaKeybindHandle _cancelActionsShortcut;

        internal static ModOptions AddOptions(ModOptions options)
        {
            DisposeHandle();

            return options
                .AddSplitter()
                .AddKeybind(
                    ToggleShortcutOptionId,
                    "autoshopping_option_toggle_shortcut",
                    DefaultToggleShortcut,
                    out _toggleShortcut,
                    OnBindingChanged,
                    uiText: ModUiText.CreateShortcutUiText())
                .AddKeybind(
                    ClearShortcutOptionId,
                    "autoshopping_option_clear_list_shortcut",
                    BaKeybind.Unbound,
                    out _clearShortcut,
                    OnBindingChanged,
                    uiText: ModUiText.CreateShortcutUiText())
                .AddKeybind(
                    PayShortcutOptionId,
                    "autoshopping_option_pay_shortcut",
                    BaKeybind.Unbound,
                    out _payShortcut,
                    OnBindingChanged,
                    uiText: ModUiText.CreateShortcutUiText())
                .AddKeybind(
                    CancelActionsShortcutOptionId,
                    "autoshopping_option_cancel_actions_shortcut",
                    BaKeybind.Unbound,
                    out _cancelActionsShortcut,
                    OnBindingChanged,
                    uiText: ModUiText.CreateShortcutUiText());
        }

        internal static void Tick()
        {
            if (_toggleShortcut != null && _toggleShortcut.WasPressedThisFrame())
            {
                AutoShoppingToggleHud.TryInvokeToggleShortcut();
                return;
            }

            if (_clearShortcut != null && _clearShortcut.WasPressedThisFrame())
            {
                AutoShoppingPanel.TryInvokeClearShortcut();
                return;
            }

            if (_payShortcut != null && _payShortcut.WasPressedThisFrame())
            {
                AutoShoppingPanel.TryInvokePayShortcut();
                return;
            }

            if (_cancelActionsShortcut != null && _cancelActionsShortcut.WasPressedThisFrame())
                AutoShoppingPanel.TryInvokeCancelActionsShortcut();
        }

        internal static string AddToggleButtonHint(string label)
        {
            return AddButtonHint(label, _toggleShortcut);
        }

        internal static string AddClearButtonHint(string label) => AddButtonHint(label, _clearShortcut);

        internal static string AddPayButtonHint(string label) => AddButtonHint(label, _payShortcut);

        internal static string AddCancelActionsButtonHint(string label) =>
            AddButtonHint(label, _cancelActionsShortcut);

        internal static void Shutdown() => DisposeHandle();

        private static void OnBindingChanged(BaKeybind _)
        {
            AutoShoppingToggleHud.RefreshVisual();
            AutoShoppingPanel.RefreshShortcutHints();
        }

        private static string AddButtonHint(string label, BaKeybindHandle handle)
        {
            if (handle == null || !handle.IsBound)
                return label;

            var binding = handle.Binding.ToDisplayString().Replace(" + ", "+");
            return label + "\n<size=70%>[" + binding + "]</size>";
        }

        private static void DisposeHandle()
        {
            _toggleShortcut?.Dispose();
            _clearShortcut?.Dispose();
            _payShortcut?.Dispose();
            _cancelActionsShortcut?.Dispose();
            _toggleShortcut = null;
            _clearShortcut = null;
            _payShortcut = null;
            _cancelActionsShortcut = null;
        }
    }
}
