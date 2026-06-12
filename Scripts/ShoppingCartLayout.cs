using UI;
using UnityEngine;

namespace AutoShopping
{
    internal readonly struct CartScreenRect
    {
        internal readonly float Left;
        internal readonly float Bottom;
        internal readonly float Width;
        internal readonly float Height;

        internal CartScreenRect(float left, float bottom, float width, float height)
        {
            Left = left;
            Bottom = bottom;
            Width = width;
            Height = height;
        }

        internal float Top => Bottom + Height;
    }

    internal static class ShoppingCartLayout
    {
        internal static bool TryGetCartRect(out CartScreenRect rect)
        {
            rect = default;
            try
            {
                if (!InstanceBehavior<UIs>.IsInitialized)
                    return false;

                var itemPanel = InstanceBehavior<UIs>.Instance?.playerHUD?.itemPanelUI;
                if (itemPanel == null)
                    return false;

                var panel = itemPanel.panel;
                if (panel == null)
                    return false;

                var panelRect = panel.GetComponent<RectTransform>();
                if (panelRect == null)
                    return false;

                return TryReadScreenRect(panelRect, out rect);
            }
            catch
            {
                return false;
            }
        }

        internal static CartScreenRect GetFallbackRect()
        {
            var width = Mathf.Max(BaGameUiChrome.MinFallbackPanelWidth, Screen.width - BaGameUiChrome.ScreenMarginX * 2f);
            var height = BaGameUiChrome.FallbackCartHeight;
            return new CartScreenRect(BaGameUiChrome.ScreenMarginX, BaGameUiChrome.ScreenMarginY, width, height);
        }

        internal static CartScreenRect ResolveCartRect()
        {
            if (TryGetCartRect(out var rect) && rect.Width > 40f && rect.Height > 20f)
                return rect;

            return GetFallbackRect();
        }

        internal static bool TryReadScreenRect(RectTransform rectTransform, out CartScreenRect rect)
        {
            rect = default;
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var left = corners[0].x;
            var bottom = corners[0].y;
            var width = corners[2].x - corners[0].x;
            var height = corners[1].y - corners[0].y;

            if (width <= 0f || height <= 0f)
                return false;

            rect = new CartScreenRect(left, bottom, width, height);
            return true;
        }
    }
}
