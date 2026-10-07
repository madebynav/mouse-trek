using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace PixelTrek
{
    public sealed class Counts
    {
        public double Pixels { get; set; }
        public long Keys { get; set; }
        public long Clicks { get; set; }
        public double PeakSpeedPixelsPerSecond { get; set; }
        public double PeakAccelerationPixelsPerSecondSquared { get; set; }
        public Counts Copy() { return new Counts { Pixels = Pixels, Keys = Keys, Clicks = Clicks, PeakSpeedPixelsPerSecond = PeakSpeedPixelsPerSecond, PeakAccelerationPixelsPerSecondSquared = PeakAccelerationPixelsPerSecondSquared }; }
        public void Add(double pixels, long keys, long clicks) { Pixels += pixels; Keys += keys; Clicks += clicks; }
        public void Peaks(double speed, double acceleration) { PeakSpeedPixelsPerSecond = Math.Max(PeakSpeedPixelsPerSecond, speed); PeakAccelerationPixelsPerSecondSquared = Math.Max(PeakAccelerationPixelsPerSecondSquared, acceleration); }
        public bool Valid() { return ValidNumber(Pixels) && ValidNumber(PeakSpeedPixelsPerSecond) && ValidNumber(PeakAccelerationPixelsPerSecondSquared) && Keys >= 0 && Clicks >= 0; }
        static bool ValidNumber(double n) { return !Double.IsNaN(n) && !Double.IsInfinity(n) && n >= 0; }
    }
    public sealed class SecondBucket
    {
        public long Second { get; set; }
        public Counts Counts { get; set; }
    }
    public sealed class DayBucket
    {
        public string Date { get; set; }
        public Counts Counts { get; set; }
    }
    public sealed class Preferences
    {
        public bool Compact { get; set; }
        public bool OnTop { get; set; }
        public bool Animations { get; set; }
        public bool HasPosition { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public double PixelsPerInch { get; set; }
        public int SelectedPeriod { get; set; }
        public string CityId { get; set; }
        public Preferences() { OnTop = true; Animations = true; PixelsPerInch = 96; CityId = "new-york"; }
        public Preferences Copy() { return (Preferences)MemberwiseClone(); }
    }
    public sealed class SaveData
    {
        public int Version { get; set; }
        public string StartedUtc { get; set; }
        public Counts Total { get; set; }
        public Counts BestDay { get; set; }
        public List<DayBucket> Days { get; set; }
        public List<SecondBucket> Seconds { get; set; }
        public Preferences Preferences { get; set; }
        public bool Paused { get; set; }
    }
    public sealed class View
    {
        public Counts Total, Today, Hour, BestDay;
        public List<DayBucket> Days;
        public string StartedUtc;
        public bool Paused;
        public long Revision;
    }
    public sealed class Tracker
    {
        readonly object sync = new object();
        readonly SecondBucket[] ring = new SecondBucket[3601];
        readonly SortedDictionary<string, Counts> days = new SortedDictionary<string, Counts>(StringComparer.Ordinal);
        Counts total = new Counts(), best = new Counts();
        string started = DateTime.UtcNow.ToString("o");
        bool paused;
        long revision;
        public static long UnixSecond(DateTime utc) { return (utc.Ticks - 621355968000000000L) / TimeSpan.TicksPerSecond; }
        public static string Day(DateTime utc) { return utc.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
        public bool Paused { get { lock (sync) return paused; } set { lock (sync) { paused = value; revision++; } } }
        Counts EnsureSecond(DateTime utc)
        {
            long second = UnixSecond(utc);
            int index = (int)((second % ring.Length + ring.Length) % ring.Length);
            if (ring[index] == null || ring[index].Second != second) ring[index] = new SecondBucket { Second = second, Counts = new Counts() };
            return ring[index].Counts;
        }
        Counts EnsureDay(DateTime utc)
        {
            string date = Day(utc); Counts day;
            if (!days.TryGetValue(date, out day)) { day = new Counts(); days.Add(date, day); }
            while (days.Count > 366) days.Remove(days.Keys.First());
            return day;
        }
        public void ObserveRates(double speed, double acceleration, DateTime utc)
        {
            if (speed < 0 || acceleration < 0 || Double.IsNaN(speed) || Double.IsNaN(acceleration) || Double.IsInfinity(speed) || Double.IsInfinity(acceleration) || (speed == 0 && acceleration == 0)) return;
            lock (sync)
            {
                if (paused) return;
                total.Peaks(speed, acceleration); EnsureSecond(utc).Peaks(speed, acceleration); EnsureDay(utc).Peaks(speed, acceleration); best.Peaks(speed, acceleration);
                revision++;
            }
        }
        public void Add(double pixels, long keys, long clicks, DateTime utc)
        {
            if (pixels < 0 || Double.IsInfinity(pixels) || Double.IsNaN(pixels) || keys < 0 || clicks < 0) return;
            lock (sync)
            {
                if (paused) return;
                EnsureSecond(utc).Add(pixels, keys, clicks);
                total.Add(pixels, keys, clicks);
                Counts day = EnsureDay(utc);
                day.Add(pixels, keys, clicks);
                best.Pixels = Math.Max(best.Pixels, day.Pixels); best.Keys = Math.Max(best.Keys, day.Keys); best.Clicks = Math.Max(best.Clicks, day.Clicks);
                revision++;
            }
        }
        public View Read(DateTime utc, bool includeHistory)
        {
            lock (sync)
            {
                long now = UnixSecond(utc); Counts hour = new Counts(), today;
                foreach (SecondBucket b in ring)
                    if (b != null && b.Second > now - 3600 && b.Second <= now) { hour.Add(b.Counts.Pixels, b.Counts.Keys, b.Counts.Clicks); hour.Peaks(b.Counts.PeakSpeedPixelsPerSecond, b.Counts.PeakAccelerationPixelsPerSecondSquared); }
                if (!days.TryGetValue(Day(utc), out today)) today = new Counts();
                return new View { Total = total.Copy(), Today = today.Copy(), Hour = hour, BestDay = best.Copy(), StartedUtc = started, Paused = paused, Revision = revision,
                    Days = includeHistory ? days.Select(d => new DayBucket { Date = d.Key, Counts = d.Value.Copy() }).Reverse().ToList() : null };
            }
        }
        public SaveData Snapshot(Preferences preferences, DateTime utc)
        {
            lock (sync)
            {
                long now = UnixSecond(utc);
                return new SaveData { Version = 1, StartedUtc = started, Total = total.Copy(), BestDay = best.Copy(), Preferences = preferences.Copy(), Paused = paused,
                    Days = days.Select(d => new DayBucket { Date = d.Key, Counts = d.Value.Copy() }).ToList(),
                    Seconds = ring.Where(b => b != null && b.Second > now - 3600 && b.Second <= now).Select(b => new SecondBucket { Second = b.Second, Counts = b.Counts.Copy() }).ToList() };
            }
        }
        public void Load(SaveData data, DateTime utc)
        {
            Validate(data);
            lock (sync)
            {
                total = data.Total.Copy(); best = data.BestDay.Copy(); started = data.StartedUtc; paused = data.Paused;
                days.Clear(); Array.Clear(ring, 0, ring.Length);
                foreach (DayBucket d in data.Days.OrderBy(d => d.Date).TakeLastCompat(366)) days[d.Date] = d.Counts.Copy();
                long now = UnixSecond(utc);
                foreach (SecondBucket b in data.Seconds)
                    if (b.Second > now - 3600 && b.Second <= now)
                    {
                        int index = (int)((b.Second % ring.Length + ring.Length) % ring.Length);
                        ring[index] = new SecondBucket { Second = b.Second, Counts = b.Counts.Copy() };
                    }
                revision++;
            }
        }
        public static void Validate(SaveData d)
        {
            DateTime start;
            if (d == null || d.Version != 1 || d.Total == null || !d.Total.Valid() || d.BestDay == null || !d.BestDay.Valid() ||
                d.Days == null || d.Seconds == null || d.Days.Count > 366 || d.Seconds.Count > 3601 ||
                !DateTime.TryParse(d.StartedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out start)) throw new InvalidDataException("Invalid counters file.");
            var dates = new HashSet<string>(); var seconds = new HashSet<long>();
            foreach (DayBucket b in d.Days)
            {
                DateTime date;
                if (b == null || b.Counts == null || !b.Counts.Valid() || !DateTime.TryParseExact(b.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) || !dates.Add(b.Date)) throw new InvalidDataException("Invalid daily summary.");
            }
            foreach (SecondBucket b in d.Seconds) if (b == null || b.Counts == null || !b.Counts.Valid() || !seconds.Add(b.Second)) throw new InvalidDataException("Invalid hour summary.");
        }
    }
    public static class Compat
    {
        public static IEnumerable<T> TakeLastCompat<T>(this IEnumerable<T> list, int n) { var items = list.ToList(); return items.Skip(Math.Max(0, items.Count - n)); }
    }
    public sealed class Store
    {
        readonly object gate = new object();
        readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = 2000000 };
        public readonly string DirectoryPath;
        public string Warning { get; private set; }
        public string Error { get; private set; }
        bool blockSave;
        public Store(string directory) { DirectoryPath = directory; }
        public string FilePath { get { return Path.Combine(DirectoryPath, "counters.json"); } }
        public SaveData Load()
        {
            lock (gate)
            {
                bool any = File.Exists(FilePath) || File.Exists(FilePath + ".bak");
                foreach (string path in new [] { FilePath, FilePath + ".bak" })
                {
                    if (!File.Exists(path)) continue;
                    try
                    {
                        if (new FileInfo(path).Length > 2000000) throw new InvalidDataException();
                        SaveData d = json.Deserialize<SaveData>(File.ReadAllText(path)); Tracker.Validate(d);
                        if (path.EndsWith(".bak")) Warning = "Recovered counters from the previous save.";
                        return d;
                    }
                    catch (Exception ex) { if (!(ex is IOException || ex is ArgumentException || ex is InvalidOperationException)) throw; }
                }
                if (any) { blockSave = true; Warning = "Saved data could not be read. Original files are preserved; tracking is paused."; }
                return null;
            }
        }
        public bool CanSave { get { return !blockSave; } }
        public bool Save(SaveData data)
        {
            lock (gate)
            {
                if (blockSave) return false;
                try
                {
                    Directory.CreateDirectory(DirectoryPath);
                    string temp = FilePath + ".tmp";
                    byte[] bytes = Encoding.UTF8.GetBytes(json.Serialize(data));
                    using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                    if (File.Exists(FilePath)) File.Replace(temp, FilePath, FilePath + ".bak", true); else File.Move(temp, FilePath);
                    Error = null; return true;
                }
                catch (Exception ex) { Error = "Could not save counters: " + ex.Message; return false; }
            }
        }
    }
    public sealed class MotionCounter
    {
        bool have; int x, y;
        public double DeltaX { get; private set; }
        public double DeltaY { get; private set; }
        public void Reset() { have = false; DeltaX = DeltaY = 0; }
        public double Move(int nextX, int nextY, bool ignored)
        {
            DeltaX = !ignored && have ? (double)nextX - x : 0; DeltaY = !ignored && have ? (double)nextY - y : 0;
            double distance = Math.Sqrt(DeltaX * DeltaX + DeltaY * DeltaY);
            x = nextX; y = nextY; have = true; return distance;
        }
    }
    public sealed class KeyCounter
    {
        struct KeyId : IEquatable<KeyId>
        {
            public long Device; public int Code;
            public bool Equals(KeyId other) { return Device == other.Device && Code == other.Code; }
            public override bool Equals(object other) { return other is KeyId && Equals((KeyId)other); }
            public override int GetHashCode() { return Device.GetHashCode() ^ Code; }
        }
        readonly HashSet<KeyId> down = new HashSet<KeyId>();
        public bool Change(IntPtr device, int scanCode, int extendedFlags, bool release)
        {
            KeyId key = new KeyId { Device = device.ToInt64(), Code = scanCode | (extendedFlags << 16) };
            if (release) { down.Remove(key); return false; }
            return down.Add(key);
        }
        public void Reset() { down.Clear(); }
    }
    public static class Display
    {
        public static double Metres(double pixels, double ppi) { return pixels * 0.0254 / DistanceScale.ValidPpi(ppi); }
        public static double Kilometres(double pixels, double ppi) { return Metres(pixels, ppi) / 1000; }
        public static string Km(double pixels, double ppi) { return Kilometres(pixels, ppi).ToString("0.000", CultureInfo.InvariantCulture) + " km eq"; }
        public static string Rate(double pixels, double ppi) { double n = Metres(pixels, ppi); return n >= 1000 ? Short(n) : n.ToString("0.00", CultureInfo.InvariantCulture); }
        public static string Short(double n)
        {
            if (n >= 1000000000000d) return (n / 1000000000000d).ToString("0.##", CultureInfo.InvariantCulture) + "T";
            if (n >= 1000000000d) return (n / 1000000000d).ToString("0.##", CultureInfo.InvariantCulture) + "B";
            if (n >= 1000000d) return (n / 1000000d).ToString("0.##", CultureInfo.InvariantCulture) + "M";
            if (n >= 10000) return (n / 1000d).ToString("0.#", CultureInfo.InvariantCulture) + "K";
            return Math.Floor(n).ToString("N0", CultureInfo.CurrentCulture);
        }
    }
    public static class DistanceScale
    {
        public static double ValidPpi(double ppi) { return ppi >= 20 && ppi <= 1000 && !Double.IsNaN(ppi) ? ppi : 96; }
        public static double FromMonitor(double widthPixels, double heightPixels, double diagonalInches)
        {
            if (widthPixels <= 0 || heightPixels <= 0 || diagonalInches <= 0 || Double.IsNaN(widthPixels + heightPixels + diagonalInches) || Double.IsInfinity(widthPixels + heightPixels + diagonalInches)) throw new ArgumentException("Enter positive resolution and diagonal values.");
            double ppi = Math.Sqrt(widthPixels * widthPixels + heightPixels * heightPixels) / diagonalInches;
            if (ppi < 20 || ppi > 1000) throw new ArgumentException("Calculated pixel density is outside the supported range (20-1000 PPI).");
            return ppi;
        }
    }
    public struct LiveRates
    {
        public double Speed, Acceleration;
    }
    public sealed class Kinematics
    {
        readonly object sync = new object();
        double time = Double.NaN, dx, dy, distance, vx, vy, speed, acceleration;
        public void Add(double x, double y, double pathDistance) { lock (sync) { dx += x; dy += y; distance += pathDistance; } }
        public void Reset(double seconds) { lock (sync) { time = seconds; dx = dy = distance = vx = vy = speed = acceleration = 0; } }
        public LiveRates Read() { lock (sync) return new LiveRates { Speed = speed, Acceleration = acceleration }; }
        public LiveRates Sample(double seconds)
        {
            lock (sync)
            {
                double dt = seconds - time;
                if (Double.IsNaN(time) || dt > 1 || dt < 0) { time = seconds; dx = dy = distance = vx = vy = speed = acceleration = 0; return new LiveRates(); }
                if (dt < 0.02) return new LiveRates { Speed = speed, Acceleration = acceleration };
                double alpha = 1 - Math.Exp(-dt / 0.12);
                double nextX = vx + alpha * (dx / dt - vx), nextY = vy + alpha * (dy / dt - vy);
                double ax = (nextX - vx) / dt, ay = (nextY - vy) / dt;
                acceleration += alpha * (Math.Sqrt(ax * ax + ay * ay) - acceleration);
                speed += alpha * (distance / dt - speed);
                vx = nextX; vy = nextY; time = seconds; dx = dy = distance = 0;
                if (speed < 0.05 && acceleration < 0.05) speed = acceleration = vx = vy = 0;
                return new LiveRates { Speed = speed, Acceleration = acceleration };
            }
        }
    }
    public static class Exports
    {
        public static string Csv(View view, double ppi)
        {
            var text = new StringBuilder("date,pixels,km_equivalent,key_presses,clicks,peak_speed_m_s_equivalent,peak_acceleration_m_s2_equivalent,pixels_per_inch\r\n");
            foreach (DayBucket day in view.Days)
            {
                Counts c = day.Counts;
                text.Append(day.Date).Append(',').Append(c.Pixels.ToString("0.######", CultureInfo.InvariantCulture)).Append(',').Append(Display.Kilometres(c.Pixels, ppi).ToString("0.#########", CultureInfo.InvariantCulture)).Append(',')
                    .Append(c.Keys.ToString(CultureInfo.InvariantCulture)).Append(',').Append(c.Clicks.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(Display.Metres(c.PeakSpeedPixelsPerSecond, ppi).ToString("0.######", CultureInfo.InvariantCulture)).Append(',').Append(Display.Metres(c.PeakAccelerationPixelsPerSecondSquared, ppi).ToString("0.######", CultureInfo.InvariantCulture)).Append(',').Append(DistanceScale.ValidPpi(ppi).ToString("0.###", CultureInfo.InvariantCulture)).Append("\r\n");
            }
            return text.ToString();
        }
    }
}
