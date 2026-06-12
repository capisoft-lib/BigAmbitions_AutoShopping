using System.Reflection;
using UI;
using UI.CurrentBuilding;
using UnityEngine;

namespace AutoShopping
{
    internal static class BuildingHudLayout
    {
        private static readonly FieldInfo BuildingPanelField = typeof(CurrentBuildingUI).GetField(
            "panel",
            BindingFlags.Instance | BindingFlags.NonPublic);
        internal const float GapBelowBuildingPanel = 12f;
        internal const float RightScreenMargin = 16f;
        internal const float FallbackTopOffset = 180f;

        internal static bool TryGetBuildingPanelRect(out CartScreenRect rect)
        {
            rect = default;
            try
            {
                if (!InstanceBehavior<UIs>.IsInitialized)
                    return false;

                var buildingUi = InstanceBehavior<UIs>.Instance?.playerHUD?.currentBuildingUI;
                if (buildingUi == null)
                    return false;

                var panel = BuildingPanelField?.GetValue(buildingUi) as GameObject;
                if (panel == null || !panel.activeInHierarchy)
                    return false;

                var panelRect = panel.GetComponent<RectTransform>();
                if (panelRect == null)
                    return false;

                return ShoppingCartLayout.TryReadScreenRect(panelRect, out rect);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Match lateral HUD width to the in-game Bizphone / building panel when available.</summary>
        internal static float GetReferenceHudWidth()
        {
            if (TryGetBuildingPanelRect(out var building) && building.Width > 20f)
                return building.Width;

            return BaGameUiChrome.RefPanelWidth;
        }

        internal static Vector2 GetToggleHudPosition(float panelWidth, float panelHeight)
        {
            var x = Screen.width - RightScreenMargin - panelWidth;
            float y;

            if (TryGetBuildingPanelRect(out var building) && building.Width > 20f && building.Height > 20f)
            {
                y = building.Bottom - GapBelowBuildingPanel - panelHeight;
            }
            else
            {
                y = Screen.height - FallbackTopOffset - panelHeight;
            }

            y = Mathf.Clamp(
                y,
                BaGameUiChrome.ScreenMarginY,
                Screen.height - BaGameUiChrome.TopScreenMargin - panelHeight);

            return new Vector2(x, y);
        }
    }
}
