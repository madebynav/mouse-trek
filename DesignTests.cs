using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PixelTrek
{
    internal static class DesignTests
    {
        internal static void Run(Action<bool,string> check,string directory)
        {
            DateTime today=new DateTime(2026,10,8);
            var graph=new GraphWindow(new[]{new DayBucket {Date="2026-09-09",Counts=new Counts {Pixels=100,Keys=3,Clicks=4}},new DayBucket {Date="2026-09-11",Counts=new Counts {Pixels=200,Keys=5,Clicks=6}},new DayBucket {Date="2026-09-08",Counts=new Counts {Keys=9999}}},today);
            check(graph.Dates.Length==30&&graph.Dates[0]==today.AddDays(-29)&&graph.Dates[29]==today,"Graph uses 30 consecutive local dates, including today");
            check(graph.Days[1].Keys==0&&graph.Days[0].Keys==3,"Missing dates are zero-filled; outside-window totals are excluded");
            graph.UpdateToday(today,new Counts {Pixels=300,Keys=7,Clicks=8});
            var daily=graph.Series(false,96);var sum=graph.Series(true,96);
            check(daily.Values[1][29]==7&&sum.Values[1][29]==15&&sum.Values[2][29]==18,"Daily values and cumulative window totals are distinct and exact");
            check(Math.Abs(sum.Values[0][29]-Display.Kilometres(600,96))<1e-12,"Cumulative distance uses the displayed window and screen-equivalent units");
            check(daily.Normal(0,29)==1&&daily.Normal(1,29)==1&&daily.Normal(2,29)==1,"Each unlike series has its own independent 30-day maximum");
            var empty=new GraphWindow(null,today);var zero=empty.Series(true,96);
            check(zero.Normal(0,29)==0&&zero.Normal(1,0)==0&&zero.Max.All(n=>n==0),"All-zero graph normalization is finite");
            graph.UpdateToday(today.AddDays(1),new Counts {Keys=2});
            check(graph.Dates[29]==today.AddDays(1)&&graph.Days[28].Keys==7&&graph.Series(true,96).Values[1][29]==14,"Midnight shifts one date, retains yesterday and drops the oldest date");
            var restart=new GraphWindow(graph.Dates.Select((d,i)=>new DayBucket{Date=d.ToString("yyyy-MM-dd"),Counts=graph.Days[i]}),today.AddDays(1));
            check(restart.Series(true,96).Values[1][29]==14,"Chart restart snapshot reproduces running window totals");
            graph.UpdateToday(today.AddDays(70),new Counts());check(graph.Days.All(c=>c.Keys==0),"Long gaps empty all old chart points without unbounded history");
            var milestone=new MilestoneNotice(new Counts{Pixels=10000000,Keys=100000,Clicks=10000});
            check(milestone.Observe(new Counts{Pixels=10000000,Keys=100000,Clicks=10000},96)=="","Historical milestones do not replay on launch");
            check(milestone.Observe(new Counts{Pixels=10000000,Keys=100000,Clicks=10000},20)=="","Changing PPI without input does not celebrate historical thresholds");
            var crossing=new MilestoneNotice(new Counts{Pixels=.099*96/.0254*1000,Keys=999,Clicks=99});
            var reached=new Counts{Pixels=.101*96/.0254*1000,Keys=1000,Clicks=100};
            string notice=crossing.Observe(reached,96);
            check(notice.Contains("100 m")&&notice.Contains(Display.Short(1000)+" key taps")&&notice.Contains("100 clicks")&&crossing.Observe(reached,96)=="","Distance/key/click crossings celebrate once, without replay");
            var motion=new GaugeMotion();bool unsettled=motion.Advance(new LiveRates{Speed=1000,Acceleration=10000},.033);
            check(unsettled&&motion.Value.Speed>600&&motion.Value.Acceleration>6000,"Gauge attack reaches over 60 percent within one 33 ms frame");
            for(int i=0;i<40;i++)motion.Advance(new LiveRates(),.033);
            check(motion.Value.Speed==0&&motion.Value.Acceleration==0,"Gauge decay settles to exact zero and permits idle refresh");
            motion.Advance(new LiveRates{Speed=Double.NaN,Acceleration=Double.PositiveInfinity},.033);
            check(motion.Value.Speed==0&&motion.Value.Acceleration==0,"Nonfinite gauge targets are sanitized before drawing");
            var tracker=new Tracker();var prefs=new Preferences();
            using(var engine=new InputEngine(tracker))using(var widget=new Widget(tracker,new Store(Path.Combine(directory,"design-native")),prefs,engine))
            {
                check(widget.CurrentPage==0&&!widget.CitySelector.Visible,"Dashboard starts with the city selector hidden");
                widget.Navigate(1);widget.Show();Application.DoEvents();check(widget.CitySelector.Visible&&widget.CurrentPage==1,"Treks reveals the native city selector in the same window");
                widget.Navigate(2);check(widget.CurrentPage==2&&!widget.CitySelector.Visible,"Graph navigation hides the Trek selector");
                widget.Hide();Application.DoEvents();check(widget.RenderInterval>=250,"Hidden widget uses bounded low-frequency refresh");widget.Quit();
            }
            var v=new View{Today=new Counts(),Hour=new Counts(),Total=new Counts(),BestDay=new Counts()};
            foreach(float scale in new[]{1f,1.25f,1.5f,2f,3f})using(var bitmap=new Bitmap((int)(575*scale),(int)(472*scale)))using(var g=Graphics.FromImage(bitmap))
            {
                g.ScaleTransform(scale,scale);
                foreach(double rate in new[]{0d,1e-12,1d,50000d,1e12,Double.NaN,Double.PositiveInfinity})Dashboard.Render(g,v,false,0,true,"LIVE",96,new LiveRates{Speed=rate,Acceleration=rate});
                Dashboard.Render(g,v,false,0,true,"LIVE",96,new LiveRates(),0,"sydney",1);
                Dashboard.Render(g,v,false,0,true,"LIVE",96,new LiveRates(),0,null,2,null,empty,zero,29,true,new[]{true,true,true});
                Dashboard.Render(g,v,false,0,true,"LIVE",96,new LiveRates(),0,null,2,null,restart,restart.Series(false,96),0,false,new[]{false,false,false});
                check(true,"New dashboard, Treks and empty/nonempty Graph render at scale "+scale);
            }
        }
    }
}
