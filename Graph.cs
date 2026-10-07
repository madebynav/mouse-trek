using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PixelTrek
{
    // Only 30 aggregate dates are held by the chart, never individual events.
    internal sealed class GraphWindow
    {
        internal readonly DateTime[] Dates = new DateTime[30];
        internal readonly Counts[] Days = new Counts[30];
        internal GraphWindow(IEnumerable<DayBucket> history, DateTime today)
        {
            var lookup = (history ?? Enumerable.Empty<DayBucket>()).ToDictionary(b => b.Date, b => b.Counts, StringComparer.Ordinal);
            for (int i = 0; i < 30; i++)
            {
                Dates[i] = today.Date.AddDays(i - 29); Counts c;
                Days[i] = lookup.TryGetValue(Dates[i].ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), out c) ? c.Copy() : new Counts();
            }
        }
        internal void UpdateToday(DateTime today, Counts counts)
        {
            today = today.Date;
            if (Dates[29] != today)
            {
                // Shift from the bounded snapshot on a date change. No history scan per frame.
                var old = Dates.Select((d, i) => new DayBucket { Date = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Counts = Days[i] }).ToArray();
                var next = new GraphWindow(old, today);
                Array.Copy(next.Dates, Dates, 30); Array.Copy(next.Days, Days, 30);
            }
            Days[29] = counts.Copy();
        }
        internal GraphSeries Series(bool cumulative, double ppi)
        {
            var result = new GraphSeries();
            for (int i = 0; i < 30; i++)
            {
                double[] values = { Display.Kilometres(Days[i].Pixels, ppi), Days[i].Keys, Days[i].Clicks };
                for (int s = 0; s < 3; s++)
                {
                    result.Values[s][i] = values[s] + (cumulative && i > 0 ? result.Values[s][i - 1] : 0);
                    result.Max[s] = Math.Max(result.Max[s], result.Values[s][i]);
                }
            }
            return result;
        }
    }
    internal sealed class GraphSeries
    {
        internal readonly double[][] Values = { new double[30], new double[30], new double[30] };
        internal readonly double[] Max = new double[3];
        internal double Normal(int series, int index) { return Max[series] <= 0 ? 0 : Values[series][index] / Max[series]; }
    }
    // Fast display interpolation is separate from the unchanged measurement filter.
    internal sealed class GaugeMotion
    {
        internal LiveRates Value;
        internal static double Finite(double v) { return Double.IsNaN(v) || Double.IsInfinity(v) || v < 0 ? 0 : v; }
        internal static double Step(double value, double target, double elapsed)
        {
            target = Finite(target); value = Finite(value);
            double tau = target >= value ? .035 : .075;
            double next = value + (target - value) * (1 - Math.Exp(-Math.Max(0, Math.Min(.25, elapsed)) / tau));
            return Math.Abs(next - target) < Math.Max(.05, target * .0005) ? target : next;
        }
        internal bool Advance(LiveRates target, double elapsed)
        {
            Value = new LiveRates { Speed = Step(Value.Speed, target.Speed, elapsed), Acceleration = Step(Value.Acceleration, target.Acceleration, elapsed) };
            return Value.Speed != Finite(target.Speed) || Value.Acceleration != Finite(target.Acceleration);
        }
        internal void Reset() { Value = new LiveRates(); }
    }
    internal sealed class MilestoneNotice
    {
        Counts previous;
        internal MilestoneNotice(Counts baseline) { previous = baseline.Copy(); }
        internal string Observe(Counts total, double ppi)
        {
            var messages = new List<string>();
            // Both sides use the same current PPI. A scale change alone crosses nothing.
            double before = Display.Kilometres(previous.Pixels, ppi), after = Display.Kilometres(total.Pixels, ppi);
            var distance = Adventure.Milestones.LastOrDefault(m => before < m.Km && after >= m.Km);
            if (distance != null) messages.Add(Adventure.Distance(distance.Km) + " travelled");
            long key = Adventure.KeyGoals.LastOrDefault(n => previous.Keys < n && total.Keys >= n);
            long click = Adventure.ClickGoals.LastOrDefault(n => previous.Clicks < n && total.Clicks >= n);
            if (key > 0) messages.Add(Display.Short(key) + " key taps");
            if (click > 0) messages.Add(Display.Short(click) + " clicks");
            previous = total.Copy(); return String.Join("  /  ", messages.ToArray());
        }
    }
}
