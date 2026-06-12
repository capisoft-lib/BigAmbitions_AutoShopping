using UnityEngine;

namespace AutoShopping
{
    /// <summary>
    /// Fixed store HUD placement. Toggle HUD uses vanilla reference width (370px);
    /// main shopping panel is wider. No runtime UI tree scans or adaptive resizing.
    /// </summary>
    internal static class BuildingHudLayout
    {
        internal const float PanelWidth = BaGameUiChrome.RefPanelWidth;
        internal const float MainPanelWidth = BaGameUiChrome.RefPanelWidth * 2f;
        internal const float MainPanelTopScreenMargin =
            80f + BaGameUiChrome.FooterStatusVerticalSavings + BaGameUiChrome.PanelTopClearanceSavings;
        internal const float ToggleHudTopOffset = 180f;
        internal const float RightScreenMargin = 16f;
        internal const float ToggleHudWidthScale = 0.7f;
        internal const float MainPanelLeftMargin = BaGameUiChrome.ScreenMarginX + 3f;
        internal const float ToggleHudRightMargin = RightScreenMargin - 15f;

        internal static float GetPanelWidth() => PanelWidth;

        internal static float GetToggleHudWidth() => PanelWidth * ToggleHudWidthScale;

        internal static float GetMainPanelWidth() => MainPanelWidth;

        internal static Vector2 GetToggleHudPosition(float panelWidth, float panelHeight)
        {
            var x = Screen.width - ToggleHudRightMargin - panelWidth;
            var y = Screen.height - ToggleHudTopOffset - panelHeight;
            y = Mathf.Clamp(
                y,
                BaGameUiChrome.ScreenMarginY,
                Screen.height - BaGameUiChrome.TopScreenMargin - panelHeight);

            return new Vector2(x, y);
        }

        internal static Vector2 GetMainPanelPosition(float panelHeight)
        {
            var bottom = BaGameUiChrome.ScreenMarginY
                         + BaGameUiChrome.FallbackCartHeight
                         + BaGameUiChrome.PanelGapAboveCart;
            return new Vector2(MainPanelLeftMargin, bottom);
        }
    }
}
