using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace PixelTrek
{
    internal static class Dashboard
    {
        public const int Width=575, Height=472, CompactHeight=82;
        static readonly Font Big=new Font("Segoe UI",22,FontStyle.Bold);
        static readonly Font BadgeNumber=new Font("Segoe UI",15,FontStyle.Bold);
        static readonly PointF[] Pins={new PointF(80,147),new PointF(287,147),new PointF(495,147),new PointF(495,180),new PointF(287,180),new PointF(80,180)};
        static readonly PointF[][] Trails=BuildTrails();
        static void T(Graphics g,string text,float x,float y,Color color,Font font=null){Art.Text(g,text,font??Art.Label,color,x,y);}
        static void Fit(Graphics g,string text,RectangleF rect,Color color,Font font=null,StringAlignment alignment=StringAlignment.Near)
        {
            using(var brush=new SolidBrush(color))using(var format=new StringFormat {Alignment=alignment,LineAlignment=StringAlignment.Near,Trimming=StringTrimming.EllipsisCharacter})g.DrawString(text,font??Art.Label,brush,rect,format);
        }
        static void Bar(Graphics g,RectangleF r,double fraction,Color color)
        {
            Art.Box(g,Color.FromArgb(48,60,78),r,2);
            float w=(float)(r.Width*Math.Max(0,Math.Min(1,fraction)));
            if(w>=1)using(var b=new SolidBrush(color))g.FillRectangle(b,r.X,r.Y,w,r.Height);
        }
        public static void Render(Graphics g,View view,bool compact,int frame,bool resting,string status,double ppi,LiveRates rates,int period=0,string cityId=null)
        {
            g.Clear(Art.Background);g.SmoothingMode=SmoothingMode.AntiAlias;
            if(compact){Compact(g,view,ppi,rates,frame,resting);return;}
            using(var pen=new Pen(Color.FromArgb(46,58,76)))g.DrawRectangle(pen,0,0,Width-1,Height-1);
            Art.Mouse(g,25,12,32,frame,resting);T(g,"PIXEL TREK",69,12,Art.White,Art.Title);
            T(g,status=="LIVE"?(resting?"Tiny paws. Big city.":"Your cursor is going places."):status,70,34,status=="LIVE"?Art.Muted:Art.Peach,Art.Small);
            CityTrek city=Adventure.City(cityId);double km=Display.Kilometres(view.Total.Pixels,ppi);
            DrawJourney(g,city,Adventure.Progress(city,km),km);
            DrawBadge(g,km,city);
            DrawMotion(g,rates,ppi);
            Counts c=period==1?view.Hour:period==2?view.Total:view.Today;
            Color[] colors={Art.Cyan,Art.Purple,Art.Peach};string[] headings={"CURSOR","KEY TAPS","CLICKS"};
            for(int i=0;i<3;i++)
            {
                float x=18+i*184;Art.Box(g,Art.Panel,new RectangleF(x,421,171,45),9);
                T(g,headings[i],x+11,426,Art.Muted,Art.Small);
                string number=i==0?Display.Kilometres(c.Pixels,ppi).ToString("0.000")+" km":Display.Short(i==1?c.Keys:c.Clicks);
                Fit(g,number,new RectangleF(x+10,440,153,25),colors[i],Art.Number);
                if(i==0)Fit(g,Display.Short(c.Pixels)+" px",new RectangleF(x+73,427,88,13),Art.Muted,Art.Small,StringAlignment.Far);
            }
        }
        static void Compact(Graphics g,View v,double ppi,LiveRates rates,int frame,bool resting)
        {
            Art.Mouse(g,20,14,28,frame,resting);T(g,"TODAY",65,9,Art.Muted,Art.Small);
            T(g,Display.Km(v.Today.Pixels,ppi),65,29,Art.Cyan,Art.Bold);
            T(g,Display.Short(v.Today.Keys)+" taps",260,29,Art.Purple,Art.Bold);T(g,Display.Short(v.Today.Clicks)+" clicks",409,29,Art.Peach,Art.Bold);
            T(g,Display.Short(v.Today.Pixels)+" px",65,56,Art.Muted,Art.Small);T(g,"Accel "+Display.Rate(rates.Acceleration,ppi)+" m/s\u00b2 eq",260,56,Art.Muted,Art.Small);
        }
        static void DrawJourney(Graphics g,CityTrek city,TrekProgress progress,double lifetimeKm)
        {
            Art.Box(g,Art.Panel,new RectangleF(18,58,539,202),13);
            T(g,city.Name.ToUpperInvariant(),34,71,Art.Cyan,Art.Bold);
            Fit(g,"Lap "+Display.Short(progress.Lap)+" / illustrated trail",new RectangleF(159,74,205,17),Art.Muted,Art.Small);
            Fit(g,lifetimeKm.ToString("0.000")+" km eq",new RectangleF(375,71,164,22),Art.White,Art.Bold,StringAlignment.Far);
            using(var track=new Pen(Color.FromArgb(62,74,96),3)){track.StartCap=track.EndCap=LineCap.Round;foreach(var trail in Trails)g.DrawLines(track,trail);}
            PointF current=Pins[0];
            for(int i=0;i<Trails.Length;i++)
            {
                double fraction=Math.Max(0,Math.Min(1,(progress.Km-city.Stops[i].Km)/(city.Stops[i+1].Km-city.Stops[i].Km)));
                if(fraction>0){var part=Portion(Trails[i],fraction);using(var pen=new Pen(Art.Cyan,3)){pen.StartCap=pen.EndCap=LineCap.Round;if(part.Length>1)g.DrawLines(pen,part);}current=part[part.Length-1];}
            }
            for(int i=0;i<Pins.Length;i++)
            {
                bool reached=progress.Km>=city.Stops[i].Km-1e-10;Color color=reached?Art.Cyan:Art.Muted;
                using(var b=new SolidBrush(Art.Panel))g.FillEllipse(b,Pins[i].X-6,Pins[i].Y-6,12,12);
                using(var pen=new Pen(color,1.7f))g.DrawEllipse(pen,Pins[i].X-5,Pins[i].Y-5,10,10);
                if(reached)using(var b=new SolidBrush(color))g.FillEllipse(b,Pins[i].X-2,Pins[i].Y-2,4,4);
                float y=i<3?99:192;
                Fit(g,city.Stops[i].Label,new RectangleF(Pins[i].X-73,y,146,28),reached?Art.White:Art.Muted,Art.Small,StringAlignment.Center);
                string distance=i==0?"START":(i==1?"":"~ ")+Adventure.Distance(city.Stops[i].Km)+" eq";
                Fit(g,distance,new RectangleF(Pins[i].X-72,i<3?128:221,144,15),i==1?Art.Purple:Art.Muted,Art.Small,StringAlignment.Center);
            }
            using(var glow=new SolidBrush(Color.FromArgb(65,Art.Purple)))g.FillEllipse(glow,current.X-10,current.Y-10,20,20);
            using(var dot=new SolidBrush(Art.Purple))g.FillEllipse(dot,current.X-4,current.Y-4,8,8);
            string next=progress.NextIndex>=city.Stops.Length?"City complete. One more tiny victory!":"NEXT  "+city.Stops[progress.NextIndex].Name+"  /  "+Adventure.Distance(city.Stops[progress.NextIndex].Km-progress.Km)+" eq away";
            Fit(g,next,new RectangleF(34,242,505,17),Art.White,Art.Small);
        }
        static void DrawBadge(Graphics g,double km,CityTrek city)
        {
            Art.Box(g,Color.FromArgb(34,31,49),new RectangleF(18,267,539,50),10);
            var goal=Adventure.Next(km);var previous=Adventure.Milestones.LastOrDefault(m=>m.Km<=km);double start=previous==null?0:previous.Km;
            T(g,goal==null?"TEN OF TEN":"NEXT BADGE",34,271,Art.Muted,Art.Small);
            T(g,goal==null?"100 km":Adventure.Distance(goal.Km),33,284,Art.Purple,BadgeNumber);
            Fit(g,goal==null?"Your mouse has stories.":city.Name+" explorer",new RectangleF(143,272,393,20),Art.White,Art.Bold);
            Fit(g,goal==null?"Every extra pixel still counts.":Adventure.CityComparison(city,goal),new RectangleF(143,292,393,14),Art.Muted,Art.Small);
            Bar(g,new RectangleF(34,310,505,3),goal==null?1:(km-start)/(goal.Km-start),Art.Purple);
        }
        static void DrawMotion(Graphics g,LiveRates rates,double ppi)
        {
            Art.Box(g,Art.Panel,new RectangleF(18,324,539,60),10);
            T(g,"CURSOR SPEED",34,333,Art.Muted,Art.Small);Fit(g,Display.Rate(rates.Speed,ppi)+" m/s eq",new RectangleF(34,353,180,24),Art.Cyan,Art.Bold);
            T(g,"ACCELERATION",236,331,Art.Muted,Art.Small);Fit(g,Display.Rate(rates.Acceleration,ppi),new RectangleF(230,344,119,36),Art.Peach,Big);
            T(g,"m/s\u00b2 eq",353,358,Art.Muted,Art.Small);
            double fraction=Math.Max(0,Math.Min(1,Display.Metres(rates.Acceleration,ppi)/20));
            for(int i=0;i<12;i++){double angle=2*Math.PI*i/12,rad=17+5*fraction;float x=505+(float)(Math.Cos(angle)*rad),y=354+(float)(Math.Sin(angle)*rad);using(var b=new SolidBrush(i<Math.Ceiling(fraction*12)?Art.Purple:Color.FromArgb(52,64,87)))g.FillEllipse(b,x-2.5f,y-2.5f,5,5);}
            using(var b=new SolidBrush(Art.Cyan))g.FillEllipse(b,497,346,16,16);
        }
        static PointF[][] BuildTrails()
        {
            var result=new PointF[5][];
            for(int i=0;i<5;i++)
            {
                PointF a=Pins[i],d=Pins[i+1],b,c;
                if(i==2){b=new PointF(546,a.Y);c=new PointF(546,d.Y);}
                else {float dx=(d.X-a.X)/3;b=new PointF(a.X+dx,a.Y+26);c=new PointF(d.X-dx,d.Y-26);}
                result[i]=new PointF[41];
                for(int j=0;j<=40;j++){float t=j/40f,u=1-t;result[i][j]=new PointF(u*u*u*a.X+3*u*u*t*b.X+3*u*t*t*c.X+t*t*t*d.X,u*u*u*a.Y+3*u*u*t*b.Y+3*u*t*t*c.Y+t*t*t*d.Y);}
            }
            return result;
        }
        internal static PointF[] Portion(PointF[] points,double fraction)
        {
            fraction=Math.Max(0,Math.Min(1,fraction));if(fraction==1)return points;
            double length=0;for(int i=1;i<points.Length;i++)length+=Distance(points[i-1],points[i]);
            double target=length*fraction,used=0;var result=new System.Collections.Generic.List<PointF>();result.Add(points[0]);
            for(int i=1;i<points.Length;i++)
            {
                double segment=Distance(points[i-1],points[i]);
                if(used+segment>=target){float f=segment<=0?0:(float)((target-used)/segment);result.Add(new PointF(points[i-1].X+(points[i].X-points[i-1].X)*f,points[i-1].Y+(points[i].Y-points[i-1].Y)*f));break;}
                result.Add(points[i]);used+=segment;
            }
            return result.ToArray();
        }
        static double Distance(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
    }
}
