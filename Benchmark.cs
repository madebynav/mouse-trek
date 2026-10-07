using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PixelTrek
{
    // Opt-in isolated native verification; synthetic aggregates never touch the user's state.
    internal static class Benchmark
    {
        [DllImport("user32.dll")]static extern int GetGuiResources(IntPtr process,int kind);
        internal static int Run(string directory)
        {
            Directory.CreateDirectory(directory);var report=new List<string>();var tracker=new Tracker();
            var prefs=new Preferences{OnTop=false};var clock=Stopwatch.StartNew();
            string[] names={"idle visible","active synthetic motion","hidden synthetic motion","paused visible"};
            int phase=0;double phaseStart=0,baseCpu=0;long paints=0,fast=0,reads=0;int maxGdi=0;long maxPrivate=0;int sample=0;
            using(var process=Process.GetCurrentProcess())using(var input=new InputEngine(tracker))using(var widget=new Widget(tracker,new Store(Path.Combine(directory,"isolated-state")),prefs,input))using(var timer=new Timer{Interval=100})using(var paintTimer=new Timer{Interval=30})using(var bitmap=new Bitmap(widget.Width,widget.Height))using(var graphics=Graphics.FromImage(bitmap))
            {
                widget.Location=new Point(-30000,-30000);widget.Show();Application.DoEvents();
                paints=widget.PaintCount;reads=widget.SnapshotCount;baseCpu=process.TotalProcessorTime.TotalMilliseconds;
                input.Live.Reset(Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency);
                double lastPaint=0;
                paintTimer.Tick+=delegate{double now=clock.Elapsed.TotalSeconds;if(widget.Visible&&(widget.RenderInterval==30||now-lastPaint>=1)){widget.BenchmarkPaint(graphics);lastPaint=now;}};
                paintTimer.Start();
                timer.Tick+=delegate
                {
                    double now=clock.Elapsed.TotalSeconds;
                    if(phase==1||phase==2)
                    {
                        double dx=480*Math.Sin(sample*.7),dy=400*Math.Cos(sample*.7),distance=Math.Sqrt(dx*dx+dy*dy);sample++;
                        input.Live.Add(dx,dy,distance);tracker.Add(distance,2,1,DateTime.UtcNow);input.SampleRates();
                    }
                    process.Refresh();maxGdi=Math.Max(maxGdi,GetGuiResources(process.Handle,0));maxPrivate=Math.Max(maxPrivate,process.PrivateMemorySize64);
                    if(now-phaseStart>=8)
                    {
                        double duration=now-phaseStart;
                        report.Add(names[phase]+": "+duration.ToString("0.00",CultureInfo.InvariantCulture)+" s; CPU "+(process.TotalProcessorTime.TotalMilliseconds-baseCpu).ToString("0.0",CultureInfo.InvariantCulture)+" ms; working set "+process.WorkingSet64+" bytes; peak sampled private "+maxPrivate+" bytes; max sampled GDI "+maxGdi+"; paints "+(widget.PaintCount-paints)+"; fast ticks "+(widget.FastTickCount-fast)+"; aggregate reads "+(widget.SnapshotCount-reads)+"; ending timer "+widget.RenderInterval+" ms");
                        phase++;if(phase==4){timer.Stop();paintTimer.Stop();widget.Quit();return;}
                        phaseStart=now;baseCpu=process.TotalProcessorTime.TotalMilliseconds;paints=widget.PaintCount;fast=widget.FastTickCount;reads=widget.SnapshotCount;maxGdi=0;maxPrivate=0;
                        if(phase==2)widget.Hide();
                        if(phase==3){tracker.Paused=true;input.Live.Reset(Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency);widget.Show();}
                    }
                };
                timer.Start();Application.Run(widget);
            }
            // Measure the unchanged engine independently of the renderer.
            var kinetics=new Kinematics();kinetics.Reset(0);double firstSpeed=0;
            for(int i=1;i<=20;i++){kinetics.Add(500,0,500);var r=kinetics.Sample(i*.1);if(i==1)firstSpeed=r.Speed;}
            report.Add("Measurement sample: 100 ms; 120 ms exponential filter. A 5000 px/s step produces "+firstSpeed.ToString("0.00",CultureInfo.InvariantCulture)+" px/s at the first sample.");
            report.Add("Display interpolation: "+GaugeMotion.Step(0,5000,.033).ToString("0.00",CultureInfo.InvariantCulture)+" px/s after 33 ms toward a 5000 px/s target. Needle and numeral share the same value.");
            report.Add("Synthetic fixture: actual Widget.OnPaint/GDI+ rendered into a reused offscreen bitmap at native timer cadence. No global hooks, cursor movement, typed text or production-state writes. Resource figures are short-run observations; the fixture adds its own paint timer/bitmap.");
            File.WriteAllLines(Path.Combine(directory,"benchmark.txt"),report);return 0;
        }
    }
}
