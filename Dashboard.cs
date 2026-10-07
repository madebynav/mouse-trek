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
        public static void Render(Graphics g,View view,bool compact,int frame,bool resting,string status,double ppi,LiveRates rates,int period=0,string cityId=null,int page=0,string notice=null,GraphWindow graph=null,GraphSeries series=null,int selected=29,bool cumulative=false,bool[] shown=null)
        {
            g.Clear(Art.Background);g.SmoothingMode=SmoothingMode.AntiAlias;
            if(compact){Compact(g,view,ppi,rates,frame,resting);return;}
            using(var pen=new Pen(Color.FromArgb(46,58,76)))g.DrawRectangle(pen,0,0,Width-1,Height-1);
            Art.Mouse(g,25,12,32,frame,resting);T(g,"PIXEL TREK",69,12,Art.White,Art.Title);
            T(g,status=="LIVE"?(resting?"Tiny paws. Big city.":"Your cursor is going places."):status,70,34,status=="LIVE"?Art.Muted:Art.Peach,Art.Small);
            if(page==1){Treks(g,view,ppi,cityId);return;}
            if(page==2){Graph(g,graph,series,selected,cumulative,shown);return;}
            DrawMotion(g,rates,ppi);
            Art.Box(g,Color.FromArgb(29,39,49),new RectangleF(18,314,539,33),8);
            T(g,String.IsNullOrEmpty(notice)?"Every little move adds up.":"MILESTONE  /  "+notice,31,323,String.IsNullOrEmpty(notice)?Art.Muted:Art.Green,Art.Small);
            T(g,"Screen equivalents at "+DistanceScale.ValidPpi(ppi).ToString("0.#")+" PPI. Pixels are the original distance.",20,352,Art.Muted,Art.Small);
            Counts c=period==1?view.Hour:period==2?view.Total:view.Today;
            Color[] colors={Art.Cyan,Art.Purple,Art.Peach};string[] headings={"CURSOR","KEY TAPS","CLICKS"};
            for(int i=0;i<3;i++)
            {
                float x=18+i*184;Art.Box(g,Art.Panel,new RectangleF(x,403,171,57),9);
                T(g,headings[i],x+11,409,Art.Muted,Art.Small);
                string number=i==0?Display.Kilometres(c.Pixels,ppi).ToString("0.000")+" km eq":Display.Short(i==1?c.Keys:c.Clicks);
                Fit(g,number,new RectangleF(x+10,430,153,25),colors[i],Art.Number);
                if(i==0)Fit(g,Display.Short(c.Pixels)+" px",new RectangleF(x+73,410,88,13),Art.Muted,Art.Small,StringAlignment.Far);
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
            Fit(g,"Lap "+Display.Short(progress.Lap)+" / "+(progress.Km/city.Length*100).ToString("0")+"% / schematic",new RectangleF(159,74,205,17),Art.Muted,Art.Small);
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
            Dial(g,18,"CURSOR SPEED",rates.Speed,ppi,3,"m/s equivalent",Art.Cyan);
            Dial(g,294,"ACCELERATION",rates.Acceleration,ppi,20,"m/s\u00b2 equivalent",Art.Peach);
        }
        static readonly PointF[] DialArc=Arc(78);
        static readonly PointF[] TickOuter=Ticks(69),TickInner=Ticks(64),TickMajor=Ticks(59);
        static PointF[] Arc(float radius){return Enumerable.Range(0,91).Select(i=>Polar(radius,i/90.0)).ToArray();}
        static PointF[] Ticks(float radius){return Enumerable.Range(0,31).Select(i=>Polar(radius,i/30.0)).ToArray();}
        static PointF Polar(float radius,double fraction){double a=(135+270*fraction)*Math.PI/180;return new PointF((float)(radius*Math.Cos(a)),(float)(radius*Math.Sin(a)));}
        static void Dial(Graphics g,float x,string title,double raw,double ppi,double max,string units,Color color)
        {
            Art.Box(g,Art.Panel,new RectangleF(x,64,263,238),13);
            Fit(g,title,new RectangleF(x+18,80,227,24),Art.Muted,Art.Bold);
            double value=Display.Metres(GaugeMotion.Finite(raw),ppi),fraction=Math.Min(1,value/max);
            var save=g.Save();g.TranslateTransform(x+131.5f,187);
            using(var track=new Pen(Color.FromArgb(51,65,82),4)){track.StartCap=track.EndCap=LineCap.Round;g.DrawLines(track,DialArc);}
            if(fraction>1e-10)
            {
                int count=Math.Max(2,(int)Math.Ceiling(fraction*90)+1);var points=new PointF[count];
                for(int i=0;i<count;i++)points[i]=Polar(78,fraction*i/(count-1));
                using(var glow=new Pen(Color.FromArgb(24,color),10))g.DrawLines(glow,points);
                using(var pen=new Pen(color,3.5f)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawLines(pen,points);}
            }
            using(var pen=new Pen(Color.FromArgb(92,108,129),1))for(int i=0;i<31;i++)g.DrawLine(pen,TickOuter[i],i%5==0?TickMajor[i]:TickInner[i]);
            for(int i=0;i<5;i++)
            {
                var p=Polar(47,i/4.0);Fit(g,(max*i/4).ToString("0.#"),new RectangleF(p.X-18,p.Y-7,36,16),Art.Muted,Art.Small,StringAlignment.Center);
            }
            var tip=Polar(58,fraction);var tail=Polar(-10,fraction);
            using(var glow=new Pen(Color.FromArgb(30,color),7)){glow.EndCap=LineCap.Round;g.DrawLine(glow,tail,tip);}
            using(var pen=new Pen(color,2)){pen.EndCap=LineCap.Round;g.DrawLine(pen,tail,tip);}
            using(var b=new SolidBrush(Art.White))g.FillEllipse(b,-3,-3,6,6);
            g.Restore(save);
            Fit(g,Display.Rate(GaugeMotion.Finite(raw),ppi),new RectangleF(x+17,231,229,35),color,Big,StringAlignment.Center);
            Fit(g,units,new RectangleF(x+17,270,229,17),Art.Muted,Art.Small,StringAlignment.Center);
            if(value>max)Fit(g,"above dial range",new RectangleF(x+150,81,94,15),color,Art.Small,StringAlignment.Far);
        }
        static void Treks(Graphics g,View v,double ppi,string cityId)
        {
            T(g,"TREKS",98,66,Art.White,Art.Title);
            CityTrek city=Adventure.City(cityId);double km=Display.Kilometres(v.Total.Pixels,ppi);
            var save=g.Save();g.TranslateTransform(0,44);DrawJourney(g,city,Adventure.Progress(city,km),km);DrawBadge(g,km,city);g.Restore(save);
            for(int i=0;i<2;i++)
            {
                bool keys=i==0;long count=keys?v.Total.Keys:v.Total.Clicks;long goal=Adventure.NextInput(count,keys);Color color=keys?Art.Purple:Art.Peach;float x=18+i*276;
                Art.Box(g,Art.Panel,new RectangleF(x,374,263,70),9);
                T(g,keys?"KEY TAP ACHIEVEMENTS":"CLICK ACHIEVEMENTS",x+13,383,Art.Muted,Art.Small);
                Fit(g,Adventure.InputTitle(count,keys),new RectangleF(x+13,401,239,20),color,Art.Bold);
                Fit(g,Display.Short(count)+" total  /  "+(goal>0?"next "+Display.Short(goal):"all tiers reached"),new RectangleF(x+13,425,239,16),Art.Muted,Art.Small);
            }
            T(g,"Schematic trails, approximate legs. Open All badges for the complete achievement ladders.",20,452,Art.Muted,Art.Small);
        }
        internal static readonly RectangleF Plot=new RectangleF(61,181,464,163);
        static void Graph(Graphics g,GraphWindow window,GraphSeries data,int selected,bool cumulative,bool[] shown)
        {
            T(g,"30-DAY GRAPH",98,66,Art.White,Art.Title);
            if(window==null||data==null)return;
            shown=shown??new[]{true,true,true};selected=Math.Max(0,Math.Min(29,selected));
            Art.Box(g,Art.Panel,new RectangleF(18,138,539,246),12);
            T(g,"RELATIVE SCALE  /  each line uses its own 30-day maximum",32,149,Art.Muted,Art.Small);
            using(var pen=new Pen(Color.FromArgb(48,61,79),1))for(int i=0;i<5;i++)
            {
                float y=Plot.Bottom-Plot.Height*i/4;g.DrawLine(pen,Plot.Left,y,Plot.Right,y);
                Fit(g,(i*25)+"%",new RectangleF(22,y-7,32,15),Art.Muted,Art.Small,StringAlignment.Far);
            }
            float selectedX=Plot.X+Plot.Width*selected/29;
            using(var line=new Pen(Color.FromArgb(93,109,132),1)){line.DashStyle=DashStyle.Dash;g.DrawLine(line,selectedX,Plot.Top,selectedX,Plot.Bottom);}
            Color[] colors={Art.Cyan,Art.Purple,Art.Peach};DashStyle[] styles={DashStyle.Solid,DashStyle.Dot,DashStyle.Dash};
            for(int s=0;s<3;s++)if(shown[s])
            {
                var points=new PointF[30];for(int i=0;i<30;i++)points[i]=new PointF(Plot.X+Plot.Width*i/29,Plot.Bottom-(float)data.Normal(s,i)*Plot.Height);
                using(var pen=new Pen(colors[s],2)){pen.LineJoin=LineJoin.Round;pen.StartCap=pen.EndCap=LineCap.Round;pen.DashStyle=styles[s];g.DrawLines(pen,points);}
                using(var b=new SolidBrush(colors[s]))for(int i=0;i<30;i++)
                {
                    float r=i==selected?4:2;PointF p=points[i];
                    if(s==0)g.FillEllipse(b,p.X-r,p.Y-r,r*2,r*2);
                    else if(s==1)g.FillRectangle(b,p.X-r,p.Y-r,r*2,r*2);
                    else g.FillPolygon(b,new[]{new PointF(p.X,p.Y-r),new PointF(p.X+r,p.Y+r),new PointF(p.X-r,p.Y+r)});
                }
            }
            if(data.Max.All(n=>n==0))Fit(g,"No activity recorded yet",new RectangleF(95,240,395,24),Art.Muted,Art.Bold,StringAlignment.Center);
            foreach(int i in new[]{0,7,14,21,29})Fit(g,window.Dates[i].ToString("d MMM"),new RectangleF(Plot.X+Plot.Width*i/29-27,354,54,16),Art.Muted,Art.Small,StringAlignment.Center);
            Art.Box(g,Color.FromArgb(29,39,49),new RectangleF(18,390,539,61),9);
            Fit(g,window.Dates[selected].ToString("ddd, d MMM")+(selected==29?"  /  TODAY, PARTIAL":"")+(cumulative?"  /  RUNNING WINDOW TOTAL":"  /  DAILY TOTAL"),new RectangleF(31,395,515,16),Art.White,Art.Small);
            string[] labels={"km eq","key taps","clicks"};
            for(int s=0;s<3;s++)Fit(g,data.Values[s][selected].ToString(s==0?"0.000000":"N0")+" "+labels[s],new RectangleF(31+s*176,416,169,20),colors[s],Art.Bold);
            double pixels=0;for(int i=cumulative?0:selected;i<=selected;i++)pixels+=window.Days[i].Pixels;
            T(g,pixels.ToString("0.###")+" px",31,436,Art.Muted,Art.Small);
            T(g,cumulative?"Baseline: 0 before "+window.Dates[0].ToString("d MMM")+". Window totals; lifetime is separate.":"Select a date: click the chart or use Left / Right, Home / End.",20,454,Art.Muted,Art.Small);
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
