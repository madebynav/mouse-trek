using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelTrek
{
    public sealed class TrekMilestone
    {
        public double Km; public string Name, Comparison;
        public TrekMilestone(double km, string name, string comparison) { Km = km; Name = name; Comparison = comparison; }
    }
    public sealed class Place
    {
        public string Name, Label; public double Latitude, Longitude, Km;
        public Place(string name, double lat, double lon, string label = null) { Name = name; Label = label ?? name; Latitude = lat; Longitude = lon; }
    }
    public sealed class CityTrek
    {
        public string Id, Name, Street; public Place[] Stops;
        public double Length { get { return Stops[Stops.Length-1].Km; } }
        public CityTrek(string id, string name, string street, Place[] stops)
        {
            Id=id; Name=name; Street=street; Stops=stops;
            // The street marker is a playful 20 m checkpoint near the origin.
            Stops[1].Km=.02;
            for(int i=2;i<Stops.Length;i++) Stops[i].Km=Stops[i-1].Km+Adventure.GreatCircle(Stops[i-1],Stops[i]);
        }
        public override string ToString() { return Name; }
    }
    public sealed class TrekProgress
    {
        public double Lap, Km; public int NextIndex; public double LegFraction;
    }
    public static class Adventure
    {
        public static readonly TrekMilestone[] Milestones = {
            new TrekMilestone(.1, "Sprint straight", "An Olympic 100 m straight"),
            new TrekMilestone(.5, "Harbour span", "Almost Sydney Harbour Bridge's 503 m span"),
            new TrekMilestone(1, "Double harbour", "About two Sydney Harbour Bridge spans"),
            new TrekMilestone(2, "Golden approach", "73% of Golden Gate Bridge's 2.737 km length"),
            new TrekMilestone(5, "Park half-lap", "About half Central Park's 6.1-mile loop"),
            new TrekMilestone(8, "Golden triple", "About three Golden Gate Bridge lengths"),
            new TrekMilestone(10, "Central Park lap", "Just over Central Park's ~9.8 km loop"),
            new TrekMilestone(15, "City to surf", "Beyond Sydney's 14 km City2Surf course"),
            new TrekMilestone(50, "Marathon and more", "Beyond a 42.195 km London Marathon"),
            new TrekMilestone(100, "Kokoda equivalent", "Beyond the 96 km Kokoda Track")
        };
        // Approximate anchors, not road/trail routing. See docs/TREKS.md.
        public static readonly CityTrek[] Cities = {
            new CityTrek("new-york","New York","Broadway",new [] {
                new Place("Times Square",40.7580,-73.9855),new Place("Broadway",40.7580,-73.9855),
                new Place("Bryant Park",40.7536,-73.9832),new Place("Grand Central Terminal",40.7527,-73.9772,"Grand Central\nTerminal"),
                new Place("Empire State Building",40.7484,-73.9857,"Empire State\nBuilding"),new Place("Central Park",40.7644,-73.9738)}),
            new CityTrek("sydney","Sydney","Alfred Street",new [] {
                new Place("Circular Quay",-33.8615,151.2107),new Place("Alfred Street",-33.8615,151.2107),
                new Place("Sydney Opera House",-33.8568,151.2153,"Sydney Opera\nHouse"),new Place("The Rocks",-33.8591,151.2080),
                new Place("Sydney Harbour Bridge",-33.8523,151.2108,"Sydney Harbour\nBridge"),new Place("Sydney Town Hall",-33.8732,151.2069,"Sydney Town\nHall")}),
            new CityTrek("london","London","Whitehall",new [] {
                new Place("Trafalgar Square",51.5080,-.1281,"Trafalgar\nSquare"),new Place("Whitehall",51.5080,-.1281),
                new Place("Big Ben",51.5007,-.1246),new Place("London Eye",51.5033,-.1195),
                new Place("St James's Park",51.5025,-.1348,"St James's\nPark"),new Place("Buckingham Palace",51.5014,-.1419,"Buckingham\nPalace")}),
            new CityTrek("moscow","Moscow","Nikolskaya Street",new [] {
                new Place("Red Square",55.7558,37.6199),new Place("Nikolskaya Street",55.7558,37.6199,"Nikolskaya\nStreet"),
                new Place("St Basil's Cathedral",55.7525,37.6231,"St Basil's\nCathedral"),new Place("Bolshoi Theatre",55.7601,37.6186,"Bolshoi\nTheatre"),
                new Place("Gorky Park",55.7319,37.6038),new Place("Sparrow Hills",55.7102,37.5424)}),
            new CityTrek("paris","Paris","Rue de Rivoli",new [] {
                new Place("Louvre",48.8606,2.3376),new Place("Rue de Rivoli",48.8606,2.3376),
                new Place("Tuileries Garden",48.8635,2.3275,"Tuileries\nGarden"),new Place("Place de la Concorde",48.8656,2.3212,"Place de la\nConcorde"),
                new Place("Arc de Triomphe",48.8738,2.2950,"Arc de\nTriomphe"),new Place("Eiffel Tower",48.8584,2.2945)})
        };
        public static CityTrek City(string id) { return Cities.FirstOrDefault(c=>c.Id==id) ?? Cities[0]; }
        public static TrekProgress Progress(CityTrek city,double lifetimeKm)
        {
            if(Double.IsNaN(lifetimeKm)||Double.IsInfinity(lifetimeKm)||lifetimeKm<0)lifetimeKm=0;
            double laps=Math.Floor(lifetimeKm/city.Length), km=lifetimeKm%city.Length;
            if(lifetimeKm>0&&km<1e-9){km=city.Length;laps=Math.Max(0,laps-1);}
            int next=Array.FindIndex(city.Stops,p=>p.Km>km+1e-10);
            if(next<0)next=city.Stops.Length;
            double fraction=next>=city.Stops.Length?1:Math.Max(0,Math.Min(1,(km-city.Stops[next-1].Km)/(city.Stops[next].Km-city.Stops[next-1].Km)));
            return new TrekProgress {Lap=laps+1,Km=km,NextIndex=next,LegFraction=fraction};
        }
        public static double GreatCircle(Place a, Place b)
        {
            double rad = Math.PI/180, lat = (b.Latitude-a.Latitude)*rad, lon=(b.Longitude-a.Longitude)*rad;
            double q=Math.Sin(lat/2)*Math.Sin(lat/2)+Math.Cos(a.Latitude*rad)*Math.Cos(b.Latitude*rad)*Math.Sin(lon/2)*Math.Sin(lon/2);
            return 6371.0088 * 2 * Math.Asin(Math.Sqrt(Math.Min(1,q)));
        }
        public static TrekMilestone Next(double km) { return Milestones.FirstOrDefault(m=>m.Km>km); }
        public static string Title(double km) { var m=Milestones.LastOrDefault(t=>t.Km<=km); return m==null?"First little steps":m.Name; }
        public static string Distance(double km) { return km>0&&km<.001?"<1 m":km<1?(km*1000).ToString("0")+" m":km.ToString("0.#")+" km"; }
        public static string CityComparison(CityTrek city,TrekMilestone goal)
        {
            if(goal.Km<=.5)return (goal.Km/.02).ToString("0")+" tiny 20 m strolls on "+city.Street;
            if(goal.Km>=city.Length)return "About "+(goal.Km/city.Length).ToString("0.#")+" completions of this illustrated city trail";
            var progress=Progress(city,goal.Km);
            return "On the illustrated trail toward "+city.Stops[Math.Min(city.Stops.Length-1,progress.NextIndex)].Name;
        }
        public static readonly long[] KeyGoals = {1000,5000,10000,50000,100000,250000,500000,1000000,2500000,5000000,10000000};
        public static readonly long[] ClickGoals = {100,500,1000,5000,10000,25000,50000,100000,250000,500000,1000000};
        public static readonly string[] KeyNames = {"First sparks","Tap rhythm","Tap scout","Key voyager","Tap captain","Quarter-million taps","Half-million taps","Million-key club","Tap constellation","Five-million taps","Keyboard universe"};
        public static readonly string[] ClickNames = {"First footprints","Click rhythm","Click scout","Click voyager","Click captain","Click trailblazer","Click comet","Click constellation","Quarter-million clicks","Half-million clicks","Million-click club"};
        public static long NextInput(long count, bool keys) { return (keys?KeyGoals:ClickGoals).FirstOrDefault(n=>n>count); }
        public static string InputTitle(long count, bool keys) { var goals=keys?KeyGoals:ClickGoals; var names=keys?KeyNames:ClickNames; int i=Array.FindLastIndex(goals,n=>n<=count); return i<0?"Getting started":names[i]; }
    }
}
