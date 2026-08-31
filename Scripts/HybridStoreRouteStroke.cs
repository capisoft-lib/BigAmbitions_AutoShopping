using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace AutoShopping
{
  /// <summary>
  /// Uses LineRenderer on EA 0.11 and an explicit floor ribbon on player builds
  /// where the LineRenderer styling accessors are stripped (Big Ambitions 1.0 beta).
  /// </summary>
  internal sealed class HybridStoreRouteStroke
  {
    private static bool _backendLogged;

    private readonly GameObject _root;
    private readonly LineRenderer _line;
    private readonly MeshRenderer _meshRenderer;
    private readonly Mesh _mesh;
    private Vector3[] _points = Array.Empty<Vector3>();
    private float _width;
    private Color _color;

    private HybridStoreRouteStroke(
      GameObject root,
      LineRenderer line,
      MeshRenderer meshRenderer,
      Mesh mesh)
    {
      _root = root;
      _line = line;
      _meshRenderer = meshRenderer;
      _mesh = mesh;
    }

    internal bool IsReady =>
      _root != null && (_line != null || (_meshRenderer != null && _mesh != null));

    internal static HybridStoreRouteStroke Attach(GameObject root)
    {
      if (root == null)
        return null;

      HybridStoreRouteStroke stroke;
      if (StoreRouteLineRendererCompat.SupportsRouteRendering)
      {
        var meshRenderer = root.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
          meshRenderer.enabled = false;

        var line = root.GetComponent<LineRenderer>();
        if (line == null)
          line = root.AddComponent<LineRenderer>();
        line.enabled = true;
        stroke = new HybridStoreRouteStroke(root, line, null, null);
      }
      else
      {
        var legacyLine = root.GetComponent<LineRenderer>();
        if (legacyLine != null)
          legacyLine.enabled = false;

        var filter = root.GetComponent<MeshFilter>();
        if (filter == null)
          filter = root.AddComponent<MeshFilter>();

        var renderer = root.GetComponent<MeshRenderer>();
        if (renderer == null)
          renderer = root.AddComponent<MeshRenderer>();
        renderer.enabled = true;

        var mesh = filter.sharedMesh;
        if (mesh == null)
        {
          mesh = new Mesh { name = "AutoShopping_WayToRibbon" };
          filter.sharedMesh = mesh;
        }

        stroke = new HybridStoreRouteStroke(root, null, renderer, mesh);
      }

      if (!_backendLogged)
      {
        _backendLogged = true;
        ModLog.Boot(
          "Way to route backend=" +
          (StoreRouteLineRendererCompat.SupportsRouteRendering ? "line_renderer" : "ribbon_mesh"));
      }

      return stroke;
    }

    internal void ApplyStyle(float width, Color color)
    {
      var safeWidth = Mathf.Max(0.001f, width);
      var widthChanged = Mathf.Abs(_width - safeWidth) > 0.0001f;
      _width = safeWidth;
      _color = color;

      if (_line != null)
      {
        StoreRouteLineRendererCompat.ApplyStyle(_line, _width);
        _line.shadowCastingMode = ShadowCastingMode.Off;
        _line.receiveShadows = false;
        StoreRouteMaterial.ApplyLine(_line, _color);
        return;
      }

      if (_meshRenderer == null || _mesh == null)
        return;

      _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
      _meshRenderer.receiveShadows = false;
      if (widthChanged && _points.Length >= 2)
        StoreRouteRibbonMeshBuilder.Populate(_mesh, _points, _width);
      StoreRouteMaterial.ApplyMesh(_meshRenderer, _color);
    }

    internal void SetPositions(Vector3[] points)
    {
      _points = points ?? Array.Empty<Vector3>();
      if (_line != null)
      {
        _line.positionCount = _points.Length;
        if (_points.Length > 0)
          _line.SetPositions(_points);
      }
      else if (_mesh != null)
      {
        StoreRouteRibbonMeshBuilder.Populate(_mesh, _points, _width);
      }

      if (_line != null)
        StoreRouteMaterial.ApplyLine(_line, _color);
      else if (_meshRenderer != null)
        StoreRouteMaterial.ApplyMesh(_meshRenderer, _color);
    }
  }

  internal static class StoreRouteLineRendererCompat
  {
    private const BindingFlags InstancePublic = BindingFlags.Instance | BindingFlags.Public;

    internal static readonly bool SupportsRouteRendering =
      HasWritableProperty("useWorldSpace") &&
      HasWritableProperty("startWidth") &&
      HasWritableProperty("endWidth");

    internal static void ApplyStyle(LineRenderer line, float width)
    {
      TrySet(line, "useWorldSpace", true);
      TrySet(line, "alignment", LineAlignment.View);
      TrySet(line, "textureMode", LineTextureMode.Stretch);
      TrySet(line, "numCapVertices", 4);
      TrySet(line, "numCornerVertices", 4);
      TrySet(line, "loop", false);
      TrySet(line, "startWidth", width);
      TrySet(line, "endWidth", width);
    }

    private static bool TrySet<T>(LineRenderer line, string propertyName, T value)
    {
      if (line == null)
        return false;

      try
      {
        var property = typeof(LineRenderer).GetProperty(propertyName, InstancePublic);
        if (property == null || !property.CanWrite)
          return false;
        property.SetValue(line, value, null);
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static bool HasWritableProperty(string propertyName)
    {
      try
      {
        var property = typeof(LineRenderer).GetProperty(propertyName, InstancePublic);
        return property != null && property.CanWrite;
      }
      catch
      {
        return false;
      }
    }
  }

  internal static class StoreRouteMaterial
  {
    private static Shader _shader;
    private static int _baseColorId;
    private static int _colorId;

    internal static void ApplyLine(LineRenderer line, Color color)
    {
      if (line == null)
        return;

      line.startColor = color;
      line.endColor = color;
      ApplyRenderer(line, color);
    }

    internal static void ApplyMesh(MeshRenderer renderer, Color color) =>
      ApplyRenderer(renderer, color);

    private static void ApplyRenderer(Renderer renderer, Color color)
    {
      if (renderer == null)
        return;

      var shader = GetShader();
      if (shader == null)
        return;

      if (renderer.material == null || renderer.material.shader != shader)
        renderer.material = new Material(shader);

      renderer.material.color = color;
      if (renderer.material.HasProperty(_baseColorId))
        renderer.material.SetColor(_baseColorId, color);
      if (renderer.material.HasProperty(_colorId))
        renderer.material.SetColor(_colorId, color);
    }

    private static Shader GetShader()
    {
      if (_shader != null)
        return _shader;

      string[] names =
      {
        "Sprites/Default",
        "Unlit/Color",
        "Legacy Shaders/Particles/Alpha Blended"
      };
      foreach (var name in names)
      {
        var shader = Shader.Find(name);
        if (shader == null)
          continue;

        _shader = shader;
        _baseColorId = Shader.PropertyToID("_BaseColor");
        _colorId = Shader.PropertyToID("_Color");
        return _shader;
      }

      return null;
    }
  }

  internal static class StoreRouteRibbonMeshBuilder
  {
    private const float DirectionEpsilonSq = 0.000001f;
    private const float MinimumMiterDenominator = 0.5f;

    internal static void Populate(Mesh mesh, Vector3[] points, float width)
    {
      if (mesh == null)
        return;

      mesh.Clear();
      if (points == null || points.Length < 2)
        return;

      var vertices = new Vector3[points.Length * 2];
      var uvs = new Vector2[vertices.Length];
      var triangles = new int[(points.Length - 1) * 6];
      var halfWidth = Mathf.Max(0.0005f, width * 0.5f);
      var distance = 0f;

      for (var i = 0; i < points.Length; i++)
      {
        if (i > 0)
          distance += Vector3.Distance(points[i - 1], points[i]);

        var previous = FindDirection(points, i, -1);
        var next = FindDirection(points, i, 1);
        var offset = ResolveOffset(previous, next, halfWidth);
        var vertex = i * 2;
        vertices[vertex] = points[i] + offset;
        vertices[vertex + 1] = points[i] - offset;
        uvs[vertex] = new Vector2(distance, 0f);
        uvs[vertex + 1] = new Vector2(distance, 1f);

        if (i >= points.Length - 1)
          continue;

        var triangle = i * 6;
        triangles[triangle] = vertex;
        triangles[triangle + 1] = vertex + 2;
        triangles[triangle + 2] = vertex + 1;
        triangles[triangle + 3] = vertex + 1;
        triangles[triangle + 4] = vertex + 2;
        triangles[triangle + 5] = vertex + 3;
      }

      mesh.vertices = vertices;
      mesh.uv = uvs;
      mesh.triangles = triangles;
      mesh.RecalculateBounds();
    }

    private static Vector3 FindDirection(Vector3[] points, int index, int step)
    {
      var cursor = index + step;
      while (cursor >= 0 && cursor < points.Length)
      {
        var direction = step < 0
          ? points[index] - points[cursor]
          : points[cursor] - points[index];
        direction.y = 0f;
        var lengthSq = direction.sqrMagnitude;
        if (lengthSq > DirectionEpsilonSq)
          return direction / Mathf.Sqrt(lengthSq);
        cursor += step;
      }

      return Vector3.zero;
    }

    private static Vector3 ResolveOffset(Vector3 previous, Vector3 next, float halfWidth)
    {
      if (previous.sqrMagnitude <= DirectionEpsilonSq)
        previous = next;
      if (next.sqrMagnitude <= DirectionEpsilonSq)
        next = previous;
      if (next.sqrMagnitude <= DirectionEpsilonSq)
        return Vector3.right * halfWidth;

      var previousNormal = new Vector3(-previous.z, 0f, previous.x);
      var nextNormal = new Vector3(-next.z, 0f, next.x);
      var miter = previousNormal + nextNormal;
      if (miter.sqrMagnitude <= DirectionEpsilonSq)
        return nextNormal * halfWidth;

      miter /= Mathf.Sqrt(miter.sqrMagnitude);
      var denominator = Mathf.Abs(Vector3.Dot(miter, nextNormal));
      var extent = halfWidth / Mathf.Max(MinimumMiterDenominator, denominator);
      return miter * extent;
    }
  }
}
