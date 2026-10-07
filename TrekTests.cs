using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace PixelTrek
{
    internal static class TrekTests
    {
        public static void Run(Action<bool,string> check,string directory)
        {
            check(Adventure.Cities.Length==5&&String.Join(",",Adventure.Cities.Select(c=>c.Name).ToArray())=="New York,Sydney,London,Moscow,Paris","Exactly the five requested cities, with New York first");
            check(new Preferences().CityId=="new-york"&&Adventure.City(null).Name=="New York"&&Adventure.City("old-wa").Name=="New York","New and obsolete city preferences use the New York default");
            check(Adventure.Cities.Select(c=>c.Id).Distinct().Count()==5,"City IDs are unique and stable for saved preferences");
            foreach(var city in Adventure.Cities)
            {
                check(city.Stops.Length==6&&city.Stops.All(p=>!String.IsNullOrWhiteSpace(p.Name)&&!String.IsNullOrWhiteSpace(p.Label)),city.Name+": every node has a place name");
                check(city.Stops[0].Km==0&&city.Stops[1].Km==.02&&city.Stops[1].Name==city.Street,city.Name+": first street checkpoint is exactly 20 m equivalent");
                check(city.Stops.Skip(1).Select((p,i)=>p.Km>city.Stops[i].Km).All(v=>v)&&city.Length>1&&city.Length<20,city.Name+": route distances are finite, increasing and city-scale");
                var early=Adventure.Progress(city,.01);var street=Adventure.Progress(city,.02);
                check(early.NextIndex==1&&Math.Abs(early.LegFraction-.5)<1e-10&&street.NextIndex==2,city.Name+": 10 m is halfway to the 20 m street, which unlocks at its threshold");
                var complete=Adventure.Progress(city,city.Length);var repeat=Adventure.Progress(city,city.Length+1e-6);
                check(complete.NextIndex==6&&complete.Lap==1&&repeat.Lap==2&&repeat.NextIndex==1,city.Name+": exact completion is shown before the next lap starts");
                check(Adventure.Milestones.All(m=>!String.IsNullOrEmpty(Adventure.CityComparison(city,m))),city.Name+": small and long-distance badges have city context");
            }
            check(Adventure.Milestones.Length==10&&Adventure.KeyGoals.Length==11&&Adventure.ClickGoals.Length==11,"Ten km badges and expanded input achievements retained");
            check(Adventure.CityComparison(Adventure.City("paris"),Adventure.Milestones[0]).Contains("Rue de Rivoli")&&Adventure.CityComparison(Adventure.City("sydney"),Adventure.Milestones[0]).Contains("Alfred Street"),"Small-distance badge context changes with the city street");
            check(Adventure.Distance(.0005)=="<1 m"&&Adventure.Distance(.02)=="20 m","Submetre distances do not round to a misleading zero");
            var source=new [] {new PointF(0,0),new PointF(10,0),new PointF(10,30)};
            var half=Dashboard.Portion(source,.5);check(half.Last().X==10&&Math.Abs(half.Last().Y-10)<1e-6,"Traveller progress follows sampled curve length, not sample indices");
            DateTime now=DateTime.UtcNow;var tracker=new Tracker();tracker.Add(5000000,100000,50000,now);
            string scratch=Path.Combine(directory,"native-city-selector");var store=new Store(scratch);Directory.CreateDirectory(scratch);
            string oldInsights=Path.Combine(scratch,"insights.json");File.WriteAllText(oldInsights,"legacy insights must remain untouched");
            var prefs=new Preferences();
            using(var engine=new InputEngine(tracker))using(var widget=new Widget(tracker,store,prefs,engine))
            {
                check(widget.ClientSize.Width==Dashboard.Width&&widget.ClientSize.Height==Dashboard.Height&&widget.CitySelector.Items.Count==5,"Native home is 575 x 472 with one five-city dropdown");
                check(widget.SaveNow(),"Default city and counters save before the first selection change");
                foreach(var city in Adventure.Cities)
                {
                    widget.CitySelector.SelectedItem=city;
                    var data=store.Load();var v=tracker.Read(now,false);
                    check(prefs.CityId==city.Id&&data.Preferences.CityId==city.Id&&v.Total.Pixels==5000000&&v.Total.Keys==100000&&v.Total.Clicks==50000,city.Name+": native selector persists the choice without resetting totals");
                    using(var bitmap=new Bitmap(widget.Width,widget.Height)){IntPtr handle=widget.Handle;foreach(System.Windows.Forms.Control control in widget.Controls){IntPtr childHandle=control.Handle;}widget.DrawToBitmap(bitmap,new Rectangle(0,0,widget.Width,widget.Height));}
                }
                check(File.ReadAllText(oldInsights)=="legacy insights must remain untouched","Trek edition never loads or rewrites the old insight file");
                widget.Quit();
            }
            var reload=store.Load();check(reload.Preferences.CityId=="paris"&&reload.Total.Pixels==5000000,"Restart retains the selected city and all odometer counts");
            var json=new JavaScriptSerializer();
            string legacy="{\"Version\":1,\"StartedUtc\":\"2026-10-07T00:00:00Z\",\"Total\":{\"Pixels\":100,\"Keys\":2,\"Clicks\":3},\"BestDay\":{},\"Days\":[],\"Seconds\":[],\"Preferences\":{\"InsightsEnabled\":true,\"ContextEnabled\":true,\"CalendarEnabled\":true,\"SelectedTab\":5,\"RouteIndex\":1,\"SelectedPeriod\":2},\"Paused\":false}";
            string oldDirectory=Path.Combine(directory,"v030-migration");Directory.CreateDirectory(oldDirectory);File.WriteAllText(Path.Combine(oldDirectory,"counters.json"),legacy);
            var imported=new Store(oldDirectory).Load();check(imported!=null&&imported.Preferences.CityId=="new-york"&&imported.Preferences.SelectedPeriod==2&&imported.Total.Keys==2,"v0.3.0 preferences migrate to Trek-only while preserving period and counts");
            string saved=json.Serialize(reload);check(!saved.Contains("InsightsEnabled")&&!saved.Contains("ContextEnabled")&&!saved.Contains("CalendarEnabled")&&!saved.Contains("SelectedTab"),"New saves contain no analytics opt-ins or removed navigation state");
            check(typeof(InputEngine).Assembly.GetTypes().All(t=>!t.Name.Contains("Insight")&&!t.Name.Contains("Calendar")&&!t.Name.Contains("Pointing")),"Removed analytics and calendar engines are absent from the built executable");
            foreach(var city in Adventure.Cities)foreach(int period in new [] {0,1,2})
            {
                var v=new View {Today=new Counts {Pixels=1,Keys=2,Clicks=3},Hour=new Counts(),Total=new Counts {Pixels=city.Length*10*96/.0254*1000},BestDay=new Counts()};
                using(var bitmap=new Bitmap(Dashboard.Width,Dashboard.Height))using(var graphics=Graphics.FromImage(bitmap))Dashboard.Render(graphics,v,false,0,true,"PAUSED",96,new LiveRates(),period,city.Id);
            }
            check(true,"All five city homes render in all periods, including repeated laps and paused state");
        }
    }
}
