using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PixelTrek
{
    internal static class Tests
    {
        static int count;
        static readonly List<string> lines = new List<string>();
        static void Assert(bool test, string message) { if (!test) throw new Exception(message); count++; lines.Add("PASS " + message); }
        public static int Run(string directory)
        {
            Directory.CreateDirectory(directory);
            try
            {
                var motion = new MotionCounter();
                Assert(motion.Move(0, 0, false) == 0, "First point creates baseline");
                Assert(motion.Move(3, 4, false) == 5, "Diagonal travel uses Euclidean distance");
                Assert(motion.Move(-3, -4, false) == 10, "Negative multi-monitor coordinates work");
                Assert(motion.Move(1000, 1000, true) == 0, "Injected cursor movement is excluded");
                Assert(motion.Move(1003, 1004, false) == 5, "Ignored cursor jump updates the baseline");
                motion.Reset(); Assert(motion.Move(10000, 20000, false) == 0, "Resume resets cursor baseline");
                var keys = new KeyCounter(); IntPtr device = new IntPtr(12);
                Assert(keys.Change(device, 30, 0, false), "New key press counts");
                Assert(!keys.Change(device, 30, 0, false), "Held-key repeat does not count");
                Assert(!keys.Change(device, 30, 0, true) && keys.Change(device, 30, 0, false), "Release permits a new press");
                Assert(keys.Change(new IntPtr(13), 30, 0, false), "Two keyboards have independent held states");
                Assert(keys.Change(device, 30, 2, false), "Extended key codes are distinct");
                keys.Reset(); Assert(keys.Change(device, 30, 0, false), "Session reset clears held keys");
                DateTime now = DateTime.UtcNow.Date.AddHours(12); var t = new Tracker();
                t.Add(99, 2, 3, now.AddSeconds(-3600)); t.Add(10, 4, 5, now.AddSeconds(-3599)); t.Add(20, 6, 7, now);
                View v = t.Read(now, true);
                Assert(v.Total.Pixels == 129 && v.Total.Keys == 12 && v.Total.Clicks == 15, "All-time totals sum every event");
                Assert(v.Hour.Pixels == 30 && v.Hour.Keys == 10 && v.Hour.Clicks == 12, "Rolling hour excludes exact 3600-second boundary");
                Assert(t.Read(now.AddSeconds(1), false).Hour.Pixels == 20, "Rolling hour expires without new input");
                Assert(t.Read(now.AddHours(2), false).Hour.Pixels == 0, "Hour is empty after a long shutdown");
                t.Paused = true; t.Add(100, 100, 100, now); Assert(t.Read(now, false).Total.Pixels == 129, "Pause excludes activity"); t.Paused = false;
                var midnight = DateTime.Today.AddDays(2).ToUniversalTime(); var dayTracker = new Tracker();
                dayTracker.Add(10, 1, 2, midnight.AddSeconds(-1)); dayTracker.Add(20, 3, 4, midnight);
                Assert(dayTracker.Read(midnight, true).Today.Pixels == 20 && dayTracker.Read(midnight, false).Total.Pixels == 30, "Local midnight resets today and preserves lifetime");
                Assert(dayTracker.Read(midnight.AddSeconds(1), false).Hour.Pixels == 30, "Rolling hour crosses local midnight");
                var store = new Store(Path.Combine(directory, "storage")); var prefs = new Preferences { X = -500, Y = 20, HasPosition = true, Compact = true };
                Assert(store.Save(t.Snapshot(prefs, now)), "First atomic save succeeds");
                SaveData data = store.Load(); var resumed = new Tracker(); resumed.Load(data, now);
                Assert(resumed.Read(now, false).Total.Keys == 12 && resumed.Read(now, false).Hour.Pixels == 30 && data.Preferences.Compact, "Restart restores totals, hour and preferences");
                resumed.Load(data, now.AddHours(2)); Assert(resumed.Read(now.AddHours(2), false).Hour.Pixels == 0, "Restart discards expired hour buckets");
                t.Add(5, 1, 1, now); Assert(store.Save(t.Snapshot(prefs, now)) && File.Exists(store.FilePath + ".bak"), "Replacement save maintains backup");
                File.WriteAllText(store.FilePath, "broken-json"); var recovered = new Store(store.DirectoryPath); data = recovered.Load();
                Assert(data != null && data.Total.Pixels == 129 && recovered.Warning != null, "Corrupt primary recovers previous valid backup");
                File.WriteAllText(store.FilePath + ".bak", "also-broken"); var broken = new Store(store.DirectoryPath);
                Assert(broken.Load() == null && !broken.CanSave && !broken.Save(t.Snapshot(prefs, now)), "Unreadable saves are preserved instead of overwritten");
                var history = new Tracker();
                for (int i = 0; i < 400; i++) history.Add(1, 1, 1, now.AddDays(i));
                Assert(history.Read(now.AddDays(399), true).Days.Count == 366 && history.Read(now.AddDays(399), false).Total.Pixels == 400, "Bounded daily history preserves all-time totals");
                var h2 = new Tracker(); h2.Load(history.Snapshot(new Preferences(), now.AddDays(399)), now.AddDays(399));
                Assert(h2.Read(now.AddDays(399), false).BestDay.Keys == 1, "Best-day records survive restart");
                Assert(Adventure.Next(.1).Km == .5 && Adventure.InputTitle(1000000,true) == "Million-key club", "Real-distance and key-tap milestones replace pixel ranks");
                Assert(Display.Short(1248562) == "1.25M", "Large values format compactly");
                var pipeline = new Tracker(); using (var engine = new InputEngine(pipeline))
                {
                    engine.ProcessMouse(0x200, 0, 0, false, now); engine.ProcessMouse(0x200, 3, 4, false, now);
                    engine.ProcessMouse(0x201, 3, 4, false, now); engine.ProcessMouse(0x202, 3, 4, false, now); engine.ProcessMouse(0x204, 3, 4, false, now);
                    engine.ProcessKey(device, 30, 0, 65, now); engine.ProcessKey(device, 30, 0, 65, now); engine.ProcessKey(device, 30, 1, 65, now); engine.ProcessKey(device, 30, 0, 65, now);
                    View p = pipeline.Read(now, false);
                    Assert(p.Total.Pixels == 5 && p.Total.Clicks == 2 && p.Total.Keys == 2 && p.Today.Keys == 2 && p.Hour.Pixels == 5, "Native event pipeline updates all three periods and rejects repeat/up events");
                    engine.ProcessMouse(0x201, 3, 4, true, now); engine.ProcessMouse(0x200, 1000, 1000, true, now);
                    Assert(pipeline.Read(now, false).Total.Pixels == 5 && pipeline.Read(now, false).Total.Clicks == 2, "Native event pipeline rejects injected mouse events");
                    pipeline.Paused = true; engine.ProcessKey(device, 31, 0, 66, now); engine.ProcessMouse(0x201, 1000, 1000, false, now);
                    Assert(pipeline.Read(now, false).Total.Keys == 2 && pipeline.Read(now, false).Total.Clicks == 2, "Native event pipeline respects pause");
                    SaveData paused = pipeline.Snapshot(prefs, now); var restartPaused = new Tracker(); restartPaused.Load(paused, now);
                    Assert(restartPaused.Paused, "Explicit pause survives restart");
                }
                Assert(Math.Abs(Display.Kilometres(1000000, 96) - 0.2645833333333333) < 0.0000001, "96-PPI reference conversion: 1M px is 0.264583 km equivalent");
                Assert(Math.Abs(Display.Kilometres(1000000, 192) * 2 - Display.Kilometres(1000000, 96)) < 0.0000001, "Doubling PPI halves distance equivalents without changing pixels");
                Assert(Display.Kilometres(96, 0) == Display.Kilometres(96, 96), "Invalid or absent saved scale falls back to 96 PPI");
                Assert(Math.Abs(DistanceScale.FromMonitor(1920, 1080, 24) - 91.78779875) < 0.0001, "Monitor dimensions calculate pixel density");
                bool rejected = false; try { DistanceScale.FromMonitor(1920, 1080, 0); } catch (ArgumentException) { rejected = true; }
                Assert(rejected, "Invalid calibration dimensions are rejected");
                var kinetics = new Kinematics(); kinetics.Reset(0);
                Assert(kinetics.Sample(0.1).Speed == 0 && kinetics.Read().Acceleration == 0, "Stationary cursor has zero speed and acceleration");
                kinetics.Add(10, 0, 10); LiveRates rising = kinetics.Sample(0.2);
                Assert(rising.Speed > 0 && rising.Acceleration > 0 && !Double.IsNaN(rising.Acceleration), "Movement produces finite smoothed velocity and acceleration");
                for (int i = 3; i <= 80; i++) { kinetics.Add(10, 0, 10); kinetics.Sample(i * 0.1); }
                LiveRates constant = kinetics.Read();
                Assert(Math.Abs(constant.Speed - 100) < 0.001 && constant.Acceleration < 0.001, "Constant straight velocity converges to near-zero acceleration");
                kinetics.Add(-10, 0, 10); LiveRates turning = kinetics.Sample(8.1);
                Assert(Math.Abs(turning.Speed - constant.Speed) < 0.01 && turning.Acceleration > 100, "Direction reversal accelerates even at unchanged path speed");
                LiveRates stopping = kinetics.Sample(8.2); Assert(stopping.Acceleration > 0 && stopping.Speed < turning.Speed, "Braking contributes to acceleration magnitude");
                for (int i = 83; i <= 110; i++) kinetics.Sample(i * 0.1);
                Assert(kinetics.Read().Speed == 0 && kinetics.Read().Acceleration == 0, "Live rates settle completely to zero after stopping");
                kinetics.Add(10000, 10000, 14142); kinetics.Sample(100);
                Assert(kinetics.Read().Speed == 0 && kinetics.Read().Acceleration == 0, "Long sampling gaps discard stale motion instead of spiking");
                kinetics.Reset(101); kinetics.Add(10, 0, 10); kinetics.Sample(101.1); kinetics.Reset(102);
                Assert(kinetics.Read().Speed == 0 && kinetics.Read().Acceleration == 0, "Pause/session reset clears live rates immediately");
                var rateTracker = new Tracker(); rateTracker.Add(50, 1, 1, now);
                rateTracker.ObserveRates(100, 1000, now.AddSeconds(-3600)); rateTracker.ObserveRates(50, 500, now);
                View recorded = rateTracker.Read(now, true);
                Assert(recorded.Total.PeakSpeedPixelsPerSecond == 100 && recorded.Hour.PeakSpeedPixelsPerSecond == 50 && recorded.Today.PeakAccelerationPixelsPerSecondSquared == 1000, "Peak rates use maxima for each time period, not sums");
                Assert(recorded.Total.Pixels == 50 && recorded.Total.Keys == 1 && recorded.Total.Clicks == 1, "Rate observations never inflate distance or input counts");
                rateTracker.Paused = true; rateTracker.ObserveRates(10000, 10000, now);
                Assert(rateTracker.Read(now, false).Total.PeakSpeedPixelsPerSecond == 100, "Paused samples do not update peaks"); rateTracker.Paused = false;
                var newerStore = new Store(Path.Combine(directory, "new-format"));
                Assert(newerStore.Save(rateTracker.Snapshot(new Preferences { PixelsPerInch = 144 }, now)), "New metrics persist with calibrated scale");
                var rateReload = new Tracker(); SaveData rateSaved = newerStore.Load(); rateReload.Load(rateSaved, now);
                Assert(rateSaved.Preferences.PixelsPerInch == 144 && rateReload.Read(now, false).Total.PeakAccelerationPixelsPerSecondSquared == 1000, "Reload preserves peaks and distance scale");
                string csv = Exports.Csv(recorded, 96);
                Assert(csv.StartsWith("date,pixels,km_equivalent,") && csv.Contains("peak_acceleration_m_s2_equivalent") && csv.Contains(",1,1,") && csv.EndsWith("\r\n"), "CSV includes auditable counts, units, scale and peak fields");
                var oldDirectory = Path.Combine(directory, "legacy"); Directory.CreateDirectory(oldDirectory);
                File.WriteAllText(Path.Combine(oldDirectory, "counters.json"), "{\"Version\":1,\"StartedUtc\":\"2026-10-07T00:00:00Z\",\"Total\":{\"Pixels\":12345,\"Keys\":90,\"Clicks\":12},\"BestDay\":{\"Pixels\":12345,\"Keys\":90,\"Clicks\":12},\"Days\":[],\"Seconds\":[],\"Preferences\":{\"Compact\":true},\"Paused\":false}");
                SaveData legacy = new Store(oldDirectory).Load(); var oldTracker = new Tracker(); oldTracker.Load(legacy, now);
                Assert(oldTracker.Read(now, false).Total.Pixels == 12345 && legacy.Preferences.PixelsPerInch == 96 && oldTracker.Read(now, false).Total.PeakSpeedPixelsPerSecond == 0, "v0.1 counters migrate without losing totals; new peaks start at zero");
                var stress = new Tracker();
                for (int i = 0; i < 3600; i++) { stress.Add(1, 1, 1, now.AddSeconds(-i)); stress.ObserveRates(1000, 10000, now.AddSeconds(-i)); }
                var stressStore = new Store(Path.Combine(directory, "full-hour"));
                Assert(stressStore.Save(stress.Snapshot(new Preferences(), now)) && stressStore.Load().Seconds.Count == 3600, "A full populated rolling hour saves and reloads within storage limits");
                GaugeRenderingRegression(); TrekTests.Run(Assert,directory);DesignTests.Run(Assert,directory);
                lines.Add("\r\n" + count + " checks passed."); File.WriteAllLines(Path.Combine(directory, "test-results.txt"), lines); return 0;
            }
            catch (Exception ex) { lines.Add("FAIL " + ex); File.WriteAllLines(Path.Combine(directory, "test-results.txt"), lines); return 1; }
        }
        static void GaugeRenderingRegression()
        {
            var view = new View { Today = new Counts(), Hour = new Counts(), Total = new Counts(), BestDay = new Counts() };
            var sweeps = new List<double> { 0, 1e-12, 1e-8, 0.00001, 0.001, 0.01, 0.05, 1.13, 30, 90, 179.99, 180, 360 };
            for (int i = 1; i <= 40; i++) sweeps.Add(i / 10.0);
            foreach (float dpiScale in new [] { 1f, 1.25f, 1.5f, 2f, 3f })
            {
                using (var bitmap = new Bitmap((int)(Dashboard.Width * dpiScale), (int)(Dashboard.Height * dpiScale)))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.ScaleTransform(dpiScale, dpiScale);
                    foreach (double sweep in sweeps)
                    {
                        double pixelsAcceleration = sweep / 180 * 20 * 96 / 0.0254;
                        try { Art.RenderWidget(graphics, view, false, 0, true, "LIVE", 96, new LiveRates { Acceleration = pixelsAcceleration }); }
                        catch (Exception ex) { throw new Exception("Gauge rendering failed at sweep " + sweep + " degrees, DPI scale " + dpiScale, ex); }
                    }
                }
                Assert(true, "Gauge renders zero, tiny, normal and over-range acceleration at DPI scale " + dpiScale);
            }
        }
    }
}
