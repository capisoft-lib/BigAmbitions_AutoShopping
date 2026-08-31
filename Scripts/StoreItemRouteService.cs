using Controllers;
using Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace AutoShopping
{
  internal static class StoreItemRouteService
  {
    private const string RootName = "AutoShopping_ItemRoute";
    private const float LineWidth = 0.1f;
    private const float FloorOffset = 0.035f;
    private const float RecalcIntervalSeconds = 0.15f;
    private const float OriginMoveResampleSq = 0.75f;
    private static Color _lineColor = AutoShoppingConfig.DefaultRouteLineColor;

    private static readonly NavMeshPath NavPath = new NavMeshPath();
    private static GameObject _root;
    private static HybridStoreRouteStroke _line;
    private static string _activeItemName = string.Empty;
    private static Vector3 _targetPosition;
    private static float _lastRecalcTime = -999f;
    private static Vector3 _lastOrigin;
    private static bool _pathVisible;

    internal static bool HasActiveRoute => !string.IsNullOrEmpty(_activeItemName);

    internal static string ActiveItemName => _activeItemName;

    internal static void SetLineColor(Color color)
    {
      _lineColor = color;
      ApplyLineStyle();
    }

    internal static bool IsActiveItem(string itemName) =>
      !string.IsNullOrEmpty(itemName) &&
      string.Equals(_activeItemName, itemName, System.StringComparison.Ordinal);

    internal static bool TrySetRouteToItem(string itemName, out string errorMessage)
    {
      errorMessage = null;
      if (string.IsNullOrEmpty(itemName) || !GameState.IsInsideSupportedInterior())
      {
        errorMessage = ModUiText.ErrorUnreachable;
        return false;
      }

      var shelf = ShoppingCargoHelper.FindNearestPurchasableShelf(itemName);
      if (shelf == null)
      {
        errorMessage = ModUiText.ErrorUnreachable;
        return false;
      }

      _activeItemName = itemName;
      _targetPosition = shelf.transform.position;
      _lastRecalcTime = -999f;

      if (!ForceRecalc())
      {
        _activeItemName = string.Empty;
        errorMessage = ModUiText.ErrorUnreachable;
        return false;
      }

      return true;
    }

    internal static void Clear()
    {
      _activeItemName = string.Empty;
      HideLine();
    }

    internal static void Tick()
    {
      if (!HasActiveRoute)
        return;

      if (!GameState.IsInsideSupportedInterior())
      {
        Clear();
        return;
      }

      var origin = PlayerHelper.GetPosition();
      var now = Time.unscaledTime;
      if (now - _lastRecalcTime < RecalcIntervalSeconds &&
          (origin - _lastOrigin).sqrMagnitude < OriginMoveResampleSq &&
          _pathVisible)
        return;

      var shelf = ShoppingCargoHelper.FindNearestPurchasableShelf(_activeItemName);
      if (shelf != null)
        _targetPosition = shelf.transform.position;

      ForceRecalc();
    }

    private static bool ForceRecalc()
    {
      var origin = PlayerHelper.GetPosition();
      _lastOrigin = origin;
      _lastRecalcTime = Time.unscaledTime;

      if (!TryCalculatePath(origin, _targetPosition, out var points))
      {
        HideLine();
        return false;
      }

      ShowLine(points);
      return true;
    }

    private static bool TryCalculatePath(Vector3 origin, Vector3 target, out Vector3[] points)
    {
      points = null;
      var filter = PlayerController.navMeshQueryFilter;
      var sampleOrigin = origin;

      try
      {
        var character = PlayerHelper.PlayerController?.Character;
        if (character != null)
          sampleOrigin = character.transform.position;
      }
      catch
      {
        // ignore
      }

      if (!TrySampleOnNavMesh(sampleOrigin, 12f, filter, out var navOrigin) &&
          !TrySampleOnNavMesh(origin, 12f, filter, out navOrigin))
        return false;

      if (!TrySampleOnNavMesh(target, 64f, filter, out var navTarget) &&
          !TrySampleOnNavMeshAnyArea(target, 64f, out navTarget))
        return false;

      if (!TryCalcPath(navOrigin, navTarget, filter, NavPath) &&
          !(NavMesh.CalculatePath(navOrigin, navTarget, NavMesh.AllAreas, NavPath) &&
            NavPath.status != NavMeshPathStatus.PathInvalid))
        return false;

      var corners = NavPath.corners;
      if (corners == null || corners.Length < 2)
        return false;

      points = corners;
      return true;
    }

    private static bool TrySampleOnNavMesh(
      Vector3 position,
      float maxDistance,
      NavMeshQueryFilter filter,
      out Vector3 sampled)
    {
      sampled = position;
      if (NavMesh.SamplePosition(position, out var hit, maxDistance, filter))
      {
        sampled = hit.position;
        return true;
      }

      return NavMesh.SamplePosition(position, out hit, maxDistance * 2f, NavMesh.AllAreas) &&
             (sampled = hit.position).sqrMagnitude > 0.01f;
    }

    private static bool TrySampleOnNavMeshAnyArea(Vector3 position, float maxDistance, out Vector3 sampled)
    {
      sampled = position;
      return NavMesh.SamplePosition(position, out var hit, maxDistance, NavMesh.AllAreas) &&
             (sampled = hit.position).sqrMagnitude > 0.01f;
    }

    private static bool TryCalcPath(
      Vector3 from,
      Vector3 to,
      NavMeshQueryFilter filter,
      NavMeshPath path) =>
      NavMesh.CalculatePath(from, to, filter, path) &&
      path.status != NavMeshPathStatus.PathInvalid;

    private static void EnsureLine()
    {
      if (_line != null && _line.IsReady && _root != null)
        return;

      _root = GameObject.Find(RootName);
      if (_root == null)
      {
        _root = new GameObject(RootName);
        Object.DontDestroyOnLoad(_root);
      }

      _line = HybridStoreRouteStroke.Attach(_root);

      ApplyLineStyle();
    }

    private static void ApplyLineStyle()
    {
      if (_line == null)
        return;

      _line.ApplyStyle(LineWidth, _lineColor);
    }

    private static void ShowLine(Vector3[] points)
    {
      EnsureLine();
      if (_root == null || _line == null || points == null || points.Length < 2)
      {
        HideLine();
        return;
      }

      _pathVisible = true;
      _root.SetActive(true);
      var elevated = new Vector3[points.Length];
      for (var i = 0; i < points.Length; i++)
        elevated[i] = points[i] + Vector3.up * FloorOffset;
      _line.SetPositions(elevated);
    }

    private static void HideLine()
    {
      _pathVisible = false;
      if (_root != null)
        _root.SetActive(false);
    }
  }
}
