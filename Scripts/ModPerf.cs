using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Buildings;
using Helpers;
using UnityEngine;

namespace AutoShopping
{
    /// <summary>
    /// Lightweight aggregated profiler — writes summaries to Logs/auto_shopping_perf.log.
    /// Disabled by default; enable via PlayerPrefs key mod_&lt;modId&gt;_perf_log = 1.
    /// </summary>
    internal static class ModPerf
    {
        private const double SlowThresholdMs = 12.0;
        private const float FlushIntervalSeconds = 5f;

        private struct Bucket
        {
            internal double TotalMs;
            internal int Count;
            internal double MaxMs;
        }

        private static readonly Dictionary<string, Bucket> Buckets = new Dictionary<string, Bucket>();
        private static float _nextFlush;
        private static int _driverUpdateFrames;

        internal static bool Enabled => AutoShoppingConfig.LogPerf;

        internal static Scope Measure(string name) => new Scope(name);

        internal readonly struct Scope : System.IDisposable
        {
            private readonly string _name;
            private readonly long _startTicks;
            private readonly bool _enabled;

            internal Scope(string name)
            {
                _name = name;
                _enabled = Enabled;
                _startTicks = _enabled ? Stopwatch.GetTimestamp() : 0L;
            }

            public void Dispose()
            {
                if (!_enabled)
                    return;

                Record(_name, ElapsedMs(_startTicks));
            }
        }

        internal static void Record(string name, double milliseconds)
        {
            if (!Enabled || string.IsNullOrEmpty(name))
                return;

            if (!Buckets.TryGetValue(name, out var bucket))
                bucket = default;

            bucket.TotalMs += milliseconds;
            bucket.Count++;
            if (milliseconds > bucket.MaxMs)
                bucket.MaxMs = milliseconds;

            Buckets[name] = bucket;

            if (milliseconds >= SlowThresholdMs)
                ModLog.PerfSlow(name, milliseconds);
        }

        internal static void RecordSlow(string name, double milliseconds, string detail)
        {
            Record(name, milliseconds);
            if (milliseconds >= SlowThresholdMs && !string.IsNullOrEmpty(detail))
                ModLog.PerfSlow(name, milliseconds, detail);
        }

        internal static void NotifyDriverUpdate() => _driverUpdateFrames++;

        internal static void Tick()
        {
            if (!Enabled)
                return;

            var now = Time.unscaledTime;
            if (now < _nextFlush)
                return;

            _nextFlush = now + FlushIntervalSeconds;
            FlushSummary();
        }

        internal static void FlushSummary()
        {
            if (!Enabled || Buckets.Count == 0)
                return;

            var summary = BuildSummary();
            Buckets.Clear();
            _driverUpdateFrames = 0;
            ModLog.PerfSummary(summary);
        }

        private static string BuildSummary()
        {
            var sb = new StringBuilder(512);
            sb.Append("context inside=").Append(BuildingManager.IsInsideBuilding);
            sb.Append(" panel=").Append(AutoShoppingPanel.IsVisible);
            sb.Append(" queue=").Append(AutoShoppingDriver.Instance?.ActionQueue?.PendingCount ?? 0);
            sb.Append(" products=").Append(StoreSession.Current?.Products.Count ?? 0);

            try
            {
                var bm = InstanceBehavior<BuildingManager>.Instance;
                if (bm?.allItemControllers != null)
                    sb.Append(" controllers=").Append(bm.allItemControllers.Count);
            }
            catch
            {
                // ignore
            }

            sb.Append(" | updates=").Append(_driverUpdateFrames).Append('/').Append(FlushIntervalSeconds).Append('s');
            sb.AppendLine();

            foreach (var pair in Buckets)
            {
                var bucket = pair.Value;
                if (bucket.Count == 0)
                    continue;

                var avg = bucket.TotalMs / bucket.Count;
                sb.Append(pair.Key)
                    .Append(" count=").Append(bucket.Count)
                    .Append(" total=").Append(bucket.TotalMs.ToString("F2")).Append("ms")
                    .Append(" avg=").Append(avg.ToString("F2")).Append("ms")
                    .Append(" max=").Append(bucket.MaxMs.ToString("F2")).Append("ms")
                    .AppendLine();
            }

            return sb.ToString();
        }

        private static double ElapsedMs(long startTicks) =>
            (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;
    }
}
