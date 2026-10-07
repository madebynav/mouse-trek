using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Pixel Trek")]
[assembly: AssemblyDescription("A lightweight, local desktop pixel odometer")]
[assembly: AssemblyCompany("Pixel Trek")]
[assembly: AssemblyProduct("Pixel Trek")]
[assembly: AssemblyVersion("0.3.2.0")]
[assembly: AssemblyFileVersion("0.3.2.0")]

namespace PixelTrek
{
    internal static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindow(string className, string title);
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length >= 2 && args[0] == "--self-test") return Tests.Run(args[1]);
            if (args.Length >= 2 && args[0] == "--preview") { Preview(args[1]); return 0; }
            if(args.Length>=2&&args[0]=="--benchmark")return Benchmark.Run(args[1]);
            bool smoke = args.Length >= 2 && args[0] == "--smoke";
            string dataDirectory = smoke ? Path.GetFullPath(args[1]) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PixelTrek");
            string mutexName = "Local\\PixelTrek-" + WindowsIdentity.GetCurrent().User.Value + (smoke ? "-smoke" : "");
            bool first;
            using (var mutex = new Mutex(true, mutexName, out first))
            {
                if (!first) { IntPtr existing = FindWindow(null, "Pixel Trek"); if (existing != IntPtr.Zero) Native.PostMessage(existing, 0x8010, IntPtr.Zero, IntPtr.Zero); return 0; }
                try
                {
                    var tracker = new Tracker(); var store = new Store(dataDirectory); SaveData saved = store.Load();
                    if (saved != null) tracker.Load(saved, DateTime.UtcNow);
                    if (!store.CanSave) tracker.Paused = true;
                    var prefs = saved != null && saved.Preferences != null ? saved.Preferences : new Preferences();
                    using (var engine = new InputEngine(tracker))
                    {
                        if (!engine.Start()) { MessageBox.Show(engine.Error ?? "Could not start input tracking.", "Pixel Trek", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1; }
                        using (var widget = new Widget(tracker, store, prefs, engine))
                        {
                            System.Windows.Forms.Timer duration = null;
                            System.Windows.Forms.Timer capture = null;
                            if (smoke && args.Length >= 3)
                            {
                                duration = new System.Windows.Forms.Timer { Interval = Math.Max(1000, Int32.Parse(args[2], CultureInfo.InvariantCulture) * 1000) };
                                duration.Tick += delegate { duration.Stop(); widget.Quit(); }; duration.Start();
                                capture = new System.Windows.Forms.Timer { Interval = 3000 };
                                capture.Tick += delegate
                                {
                                    capture.Stop(); Directory.CreateDirectory(dataDirectory);
                                    using (var image = new Bitmap(widget.Width, widget.Height)) { widget.DrawToBitmap(image, new Rectangle(0, 0, widget.Width, widget.Height)); image.Save(Path.Combine(dataDirectory, "Native-Widget.png"), ImageFormat.Png); }
                                };
                                capture.Start();
                            }
                            Application.Run(widget);
                            if (duration != null) duration.Dispose();
                            if (capture != null) capture.Dispose();
                            if (smoke)
                            {
                                using (var process = Process.GetCurrentProcess()) File.WriteAllText(Path.Combine(dataDirectory, "smoke-result.txt"), "Input startup: " + (engine.Error ?? "OK") + "\r\nWorking set bytes: " + process.WorkingSet64 + "\r\nPrivate bytes: " + process.PrivateMemorySize64 + "\r\nCPU time ms: " + process.TotalProcessorTime.TotalMilliseconds + "\r\nSaved successfully: " + (store.Error ?? "OK") + "\r\n");
                            }
                        }
                    }
                    return 0;
                }
                catch (Exception ex) { MessageBox.Show("Pixel Trek could not start.\n\n" + ex.Message, "Pixel Trek", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1; }
                finally { mutex.ReleaseMutex(); }
            }
        }
        static void Preview(string directory)
        {
            Directory.CreateDirectory(directory);
            var v=new View {Today=new Counts {Pixels=1248562,Keys=18429,Clicks=2841,PeakSpeedPixelsPerSecond=9000,PeakAccelerationPixelsPerSecondSquared=35000},Hour=new Counts {Pixels=128432,Keys=2214,Clicks=382},Total=new Counts {Pixels=12485732,Keys=382142,Clicks=98214},BestDay=new Counts(),Days=new System.Collections.Generic.List<DayBucket>(),StartedUtc=DateTime.UtcNow.ToString("o")};
            LiveRates rates=new LiveRates {Speed=5000,Acceleration=25000};
            foreach(var city in Adventure.Cities)
            {
                using(var bitmap=new Bitmap(Dashboard.Width*2,Dashboard.Height*2))using(var g=Graphics.FromImage(bitmap))
                {g.ScaleTransform(2,2);Dashboard.Render(g,v,false,0,false,"LIVE",96,rates,0,city.Id,1);bitmap.Save(Path.Combine(directory,"PixelTrek-"+city.Id+".png"),ImageFormat.Png);}
                NativePreview(directory,city.Id,v);
            }
            PreviewViews(directory,rates);
            using(var bitmap=new Bitmap(Dashboard.Width*2,Dashboard.CompactHeight*2))using(var g=Graphics.FromImage(bitmap))
            {g.ScaleTransform(2,2);Dashboard.Render(g,v,true,0,false,"LIVE",96,rates);bitmap.Save(Path.Combine(directory,"PixelTrek-Compact.png"),ImageFormat.Png);}
            var early=new View {Today=new Counts {Pixels=10000,Keys=100,Clicks=20},Hour=new Counts(),Total=new Counts {Pixels=.01*96/.0254*1000,Keys=200,Clicks=50},BestDay=new Counts(),Days=new System.Collections.Generic.List<DayBucket>()};
            string streetExample=Path.Combine(directory,"street-example");Directory.CreateDirectory(streetExample);NativePreview(streetExample,"sydney",early);
            File.Copy(Path.Combine(streetExample,"Native-sydney.png"),Path.Combine(directory,"PixelTrek-First-20m.png"),true);
            using(var bitmap=Art.Recap(v,96,"new-york"))bitmap.Save(Path.Combine(directory,"PixelTrek-Recap-Example.png"),ImageFormat.Png);
            using(var form=new ScaleForm(96))RenderForm(form,Path.Combine(directory,"PixelTrek-Scale.png"));
            v.Days.Add(new DayBucket {Date=DateTime.Today.ToString("yyyy-MM-dd"),Counts=v.Today.Copy()});
            using(var form=new DetailForm(v,96))RenderForm(form,Path.Combine(directory,"PixelTrek-History.png"));
        }
        static void RenderForm(Form form,string path)
        {
            EnsureHandles(form);
            using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));bitmap.Save(path,ImageFormat.Png);}
        }
        static void EnsureHandles(Control control){IntPtr handle=control.Handle;foreach(Control child in control.Controls)EnsureHandles(child);}
        static void NativePreview(string directory,string cityId,View sample)
        {
            DateTime now=DateTime.UtcNow;var tracker=new Tracker();tracker.Add(sample.Today.Pixels,sample.Today.Keys,sample.Today.Clicks,now);tracker.Add(sample.Total.Pixels-sample.Today.Pixels,sample.Total.Keys-sample.Today.Keys,sample.Total.Clicks-sample.Today.Clicks,now.AddDays(-1));
            var prefs=new Preferences {CityId=cityId};string scratch=Path.Combine(directory,"native-preview-state-"+cityId);
            using(var engine=new InputEngine(tracker))using(var widget=new Widget(tracker,new Store(scratch),prefs,engine))
            {widget.Location=new Point(-30000,-30000);widget.Navigate(1);widget.Show();Application.DoEvents();RenderForm(widget,Path.Combine(directory,"Native-"+cityId+".png"));widget.Hide();widget.Quit();}
        }
        static void PreviewViews(string directory,LiveRates rates)
        {
            var tracker=new Tracker();DateTime local=DateTime.Today.AddHours(12);
            for(int i=0;i<30;i++)if(i%6!=0)tracker.Add((120000+i*28000)*(1+Math.Sin(i*.8)),1500+(i*751)%9000,100+(i*313)%1900,local.AddDays(i-29).ToUniversalTime());
            using(var engine=new InputEngine(tracker))using(var widget=new Widget(tracker,new Store(Path.Combine(directory,"preview-only-state")),new Preferences(),engine))
            {
                widget.Location=new Point(-30000,-30000);widget.Show();Application.DoEvents();widget.PreviewRates(rates);
                RenderForm(widget,Path.Combine(directory,"PixelTrek-Dashboard.png"));
                File.Copy(Path.Combine(directory,"PixelTrek-Dashboard.png"),Path.Combine(directory,"PixelTrek-Widget.png"),true);
                widget.Navigate(2);RenderForm(widget,Path.Combine(directory,"PixelTrek-Graph.png"));
                foreach(Control c in widget.Controls)if(c is Button&&c.Text=="Cumulative")((Button)c).PerformClick();
                RenderForm(widget,Path.Combine(directory,"PixelTrek-Graph-Cumulative.png"));widget.Hide();widget.Quit();
            }
            var empty=new Tracker();using(var engine=new InputEngine(empty))using(var widget=new Widget(empty,new Store(Path.Combine(directory,"empty-only-state")),new Preferences(),engine))
            {widget.Location=new Point(-30000,-30000);widget.Show();Application.DoEvents();widget.Navigate(2);RenderForm(widget,Path.Combine(directory,"PixelTrek-Graph-Empty.png"));widget.Hide();widget.Quit();}
            using(var form=new DetailForm(empty.Read(DateTime.UtcNow,true),96))RenderForm(form,Path.Combine(directory,"PixelTrek-History-Empty.png"));
            using(var engine=new InputEngine(tracker))using(var widget=new Widget(tracker,new Store(Path.Combine(directory,"compact-only-state")),new Preferences{Compact=true},engine))
            {widget.Location=new Point(-30000,-30000);widget.Show();Application.DoEvents();RenderForm(widget,Path.Combine(directory,"Native-Compact.png"));widget.Hide();widget.Quit();}
        }
    }
}
