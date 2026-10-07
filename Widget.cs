using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PixelTrek
{
    internal static class Art
    {
        public static readonly Color Background = Color.FromArgb(17, 22, 32), Panel = Color.FromArgb(27, 34, 47), Muted = Color.FromArgb(146, 157, 177), White = Color.FromArgb(235, 241, 250);
        public static readonly Color Cyan = Color.FromArgb(100, 226, 219), Purple = Color.FromArgb(181, 154, 255), Peach = Color.FromArgb(255, 186, 137), Green = Color.FromArgb(131, 226, 160);
        public static readonly Font Small = new Font("Segoe UI", 8f), Label = new Font("Segoe UI", 9f), Bold = new Font("Segoe UI", 10f, FontStyle.Bold), Number = new Font("Segoe UI", 13.5f, FontStyle.Bold), Title = new Font("Segoe UI", 12f, FontStyle.Bold);
        public static void Text(Graphics g, string text, Font font, Color color, float x, float y) { using (var b = new SolidBrush(color)) g.DrawString(text, font, b, x, y, StringFormat.GenericTypographic); }
        public static GraphicsPath Rounded(RectangleF rect, float radius)
        {
            var p = new GraphicsPath(); float d = radius * 2;
            p.AddArc(rect.X, rect.Y, d, d, 180, 90); p.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); p.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
        }
        public static void Box(Graphics g, Color color, RectangleF rect, float radius) { using (var p = Rounded(rect, radius)) using (var b = new SolidBrush(color)) g.FillPath(b, p); }
        public static void Mouse(Graphics g, float x, float y, float size, int frame, bool resting)
        {
            GraphicsState save = g.Save(); g.TranslateTransform(x, y + (resting ? 0 : (frame % 2) * 1.5f)); g.ScaleTransform(size / 32f, size / 32f);
            using (var tail = new Pen(Purple, 2)) g.DrawBezier(tail, 8, 21, -3, 18, 0, 31, -6, 26);
            using (var body = new SolidBrush(Purple)) { g.FillEllipse(body, 5, 11, 24, 17); g.FillEllipse(body, 15, 1, 12, 12); g.FillEllipse(body, 7, 4, 11, 11); }
            using (var inner = new SolidBrush(Color.FromArgb(224, 190, 245))) { g.FillEllipse(inner, 18, 4, 6, 6); g.FillEllipse(inner, 10, 7, 5, 5); }
            using (var eye = new SolidBrush(Background)) { if (resting) g.FillRectangle(eye, 22, 16, 5, 2); else g.FillEllipse(eye, 23, 15, 3, 4); }
            using (var nose = new SolidBrush(Peach)) g.FillEllipse(nose, 28, 21, 4, 4);
            using (var feet = new Pen(Cyan, 2)) { g.DrawLine(feet, 12, 28, 12 + (frame % 2) * 2, 30); g.DrawLine(feet, 23, 28, 23 - (frame % 2) * 2, 30); }
            g.Restore(save);
        }
        public static void RenderWidget(Graphics g, View view, bool compact, int frame, bool resting, string status, double ppi, LiveRates rates)
        {
            Dashboard.Render(g, view, compact, frame, resting, status, ppi, rates);
        }
        public static Bitmap Recap(View v, double ppi, string cityId = null)
        {
            Bitmap image = new Bitmap(1080, 750);
            using (Graphics g = Graphics.FromImage(image))
            using (Font title = new Font("Segoe UI", 36, FontStyle.Bold))
            using (Font subtitle = new Font("Segoe UI", 15))
            using (Font metric = new Font("Segoe UI", 37, FontStyle.Bold))
            using (Font label = new Font("Segoe UI", 14, FontStyle.Bold))
            {
                g.Clear(Background); g.SmoothingMode = SmoothingMode.AntiAlias;
                Text(g, "MY DESKTOP ADVENTURE", label, Cyan, 55, 45);
                Text(g, "Every little move adds up.", title, White, 50, 83);
                Text(g, DateTime.Now.ToString("dddd, d MMMM yyyy"), subtitle, Muted, 55, 157);
                Mouse(g, 897, 86, 98, 0, false);
                Counts c = v.Today; string[] nums = { Display.Short(c.Pixels), Display.Short(c.Keys), Display.Short(c.Clicks) };
                string[] labels = { "PIXELS TRAVELLED", "KEY PRESSES", "MOUSE CLICKS" }; Color[] colors = { Cyan, Purple, Peach };
                for (int i = 0; i < 3; i++)
                {
                    int x = 55 + i * 330;
                    Box(g, Panel, new RectangleF(x, 236, 310, 184), 18);
                    Text(g, nums[i], metric, colors[i], x + 23, 260); Text(g, labels[i], label, Muted, x + 23, 348);
                    if (i == 0) Text(g, Display.Km(c.Pixels, ppi), subtitle, Cyan, x + 23, 383);
                }
                Text(g, Adventure.City(cityId).Name + " / " + Adventure.Title(Display.Kilometres(v.Total.Pixels, ppi)), label, Purple, 57, 450);
                Text(g, "Lifetime: " + Math.Floor(v.Total.Pixels).ToString("N0") + " pixels / " + Display.Km(v.Total.Pixels, ppi), subtitle, White, 55, 489);
                Text(g, "Today's peaks: " + Display.Rate(c.PeakSpeedPixelsPerSecond, ppi) + " m/s eq  /  " + Display.Rate(c.PeakAccelerationPixelsPerSecondSquared, ppi) + " m/s\u00b2 eq", subtitle, Peach, 55, 528);
                double next = Adventure.Next(Display.Kilometres(v.Total.Pixels, ppi)) == null ? 100 : Adventure.Next(Display.Kilometres(v.Total.Pixels, ppi)).Km;
                Box(g, Panel, new RectangleF(55, 581, 970, 8), 4);
                using (var b = new SolidBrush(Cyan)) g.FillRectangle(b, 55, 581, (float)(970 * Math.Min(1, Display.Kilometres(v.Total.Pixels, ppi) / next)), 8);
                Text(g, "Next checkpoint: " + Adventure.Distance(next) + " equivalent", subtitle, Muted, 55, 608);
                Text(g, "PIXEL TREK  /  Native. Local. Little.", label, Muted, 55, 669);
                Text(g, "Screen equivalents at " + DistanceScale.ValidPpi(ppi).ToString("0.#") + " PPI. Counts only; no typed text stored.", subtitle, Muted, 55, 706);
            }
            return image;
        }
    }
    public sealed class Widget : Form
    {
        readonly Tracker tracker;
        readonly Store store;
        readonly InputEngine input;
        readonly Preferences prefs;
        readonly NotifyIcon tray;
        readonly ContextMenuStrip menu;
        readonly System.Windows.Forms.Timer redraw = new System.Windows.Forms.Timer { Interval = 250 };
        readonly object preferenceGate = new object();
        readonly object saveGate = new object();
        readonly System.Threading.Timer saver;
        readonly ToolTip tips = new ToolTip();
        readonly Button shrink, hide, options;
        readonly Button[] periods=new Button[3];
        readonly ComboBox cities;
        Font cityFont;
        internal ComboBox CitySelector { get { return cities; } }
        View view;
        float scale = 1;
        int frame;
        long lastRevision = -1;
        long lastSecond = -1;
        DateTime lastActive = DateTime.MinValue;
        bool dragging, quitting, locked, sleeping;
        Point dragOrigin, windowOrigin;
        public Widget(Tracker tracker, Store store, Preferences preferences, InputEngine input)
        {
            this.tracker = tracker; this.store = store; prefs = preferences; this.input = input;
            prefs.CityId=Adventure.City(prefs.CityId).Id;
            prefs.SelectedPeriod=Math.Max(0,Math.Min(2,prefs.SelectedPeriod));
            Text = "Pixel Trek"; BackColor = Art.Background; ForeColor = Art.White; Font = Art.Label;
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; AutoScaleMode = AutoScaleMode.None;
            StartPosition = FormStartPosition.Manual; TopMost = prefs.OnTop;
            DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true);
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) scale = g.DpiX / 96f;
            var resource = typeof(Widget).Assembly.GetManifestResourceStream("PixelTrek.ico");
            if (resource != null) { using (resource) Icon = new Icon(resource); } else Icon = SystemIcons.Application;
            shrink = HeaderButton("\u2212", "Collapse / expand", delegate { lock (preferenceGate) prefs.Compact = !prefs.Compact; LayoutWidget(); ClampPosition(); SaveNow(); });
            hide = HeaderButton("\u2198", "Hide to tray (keeps tracking)", delegate { Hide(); });
            options = HeaderButton("\u22ef", "Menu", delegate { menu.Show(options, new Point(0, options.Height)); });
            menu = new ContextMenuStrip { BackColor = Art.Panel, ForeColor = Art.White, Font = Art.Label };
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkTable());
            menu.Opening += delegate { BuildMenu(); };
            ContextMenuStrip = menu;
            tray = new NotifyIcon { Icon = Icon, Text = "Pixel Trek - tracking locally", Visible = true, ContextMenuStrip = menu };
            tray.DoubleClick += delegate { RestoreWidget(); };
            string[] periodNames={"Today","Last hour","Lifetime"};
            for(int i=0;i<3;i++){int chosen=i;periods[i]=ActionButton(periodNames[i],delegate {lock(preferenceGate)prefs.SelectedPeriod=chosen;LayoutWidget();SaveNow();});}
            cities=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed,FlatStyle=FlatStyle.Flat,BackColor=Art.Panel,ForeColor=Art.White,Font=Art.Bold,AccessibleName="Choose your city",ItemHeight=24,MaxDropDownItems=5};
            foreach(var city in Adventure.Cities)cities.Items.Add(city);
            cities.SelectedItem=Adventure.City(prefs.CityId);
            cities.DrawItem+=delegate(object sender,DrawItemEventArgs e){if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;using(var brush=new SolidBrush(selected?Color.FromArgb(50,62,83):Art.Panel))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,cities.Items[e.Index].ToString(),cities.Font,new Rectangle(e.Bounds.X+8,e.Bounds.Y,e.Bounds.Width-10,e.Bounds.Height),selected?Art.Cyan:Art.White,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);e.DrawFocusRectangle();};
            cities.SelectedIndexChanged+=delegate {var city=cities.SelectedItem as CityTrek;if(city==null)return;lock(preferenceGate)prefs.CityId=city.Id;Invalidate();SaveNow();};
            Controls.Add(cities);
            tips.SetToolTip(cities,"Choose a city. Your lifetime totals stay the same; each city has its own illustrated route.");
            tips.SetToolTip(this,"Route: approximate straight-line landmark legs, with a playful 20 m street checkpoint. Screen equivalents, not actual walking directions. Right-click for history, scale and controls.");
            LayoutWidget();
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = prefs.HasPosition ? new Point(prefs.X, prefs.Y) : new Point(area.Right - Width - 18, area.Bottom - Height - 18);
            ClampPosition();
            view = tracker.Read(DateTime.UtcNow, false);
            redraw.Tick += Tick; redraw.Start();
            saver = new System.Threading.Timer(delegate { SaveNow(); }, null, 10000, 10000);
            SystemEvents.PowerModeChanged += PowerChanged;
            SystemEvents.DisplaySettingsChanged += DisplayChanged;
            Shown += delegate { Native.WTSRegisterSessionNotification(Handle, 0); string warning=store.Warning;if (!String.IsNullOrEmpty(warning)) MessageBox.Show(this, warning, "Pixel Trek saved data", MessageBoxButtons.OK, MessageBoxIcon.Information); };
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= 0x80; return cp; } }
        Button HeaderButton(string text, string hint, Action action)
        {
            var b = new Button { Text = text, FlatStyle = FlatStyle.Flat, ForeColor = Art.Muted, BackColor = Art.Background, TabStop = true, AccessibleName = hint, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Art.Panel; b.Click += delegate { action(); }; tips.SetToolTip(b, hint); Controls.Add(b); return b;
        }
        void LayoutWidget()
        {
            ClientSize=new Size((int)Math.Round(Dashboard.Width*scale),(int)Math.Round((prefs.Compact?Dashboard.CompactHeight:Dashboard.Height)*scale));
            var buttons=new [] {shrink,hide,options};for(int i=0;i<buttons.Length;i++)buttons[i].Bounds=BoundsAt(481+i*30,7,28,26);
            cities.Visible=!prefs.Compact;cities.Bounds=BoundsAt(338,14,137,29);cities.ItemHeight=Math.Max(20,(int)(24*scale));
            if(cityFont==null||Math.Abs(cityFont.Size-10*scale)>.01f){var previous=cityFont;cityFont=new Font("Segoe UI",10*scale,FontStyle.Bold);cities.Font=cityFont;if(previous!=null)previous.Dispose();}
            for(int i=0;i<3;i++){periods[i].Visible=!prefs.Compact;periods[i].Bounds=BoundsAt(18+i*184,391,171,24);periods[i].ForeColor=prefs.SelectedPeriod==i?Art.Cyan:Art.Muted;periods[i].BackColor=prefs.SelectedPeriod==i?Color.FromArgb(36,50,63):Art.Panel;}
            Invalidate();
        }
        Rectangle BoundsAt(int x,int y,int width,int height){return new Rectangle((int)(x*scale),(int)(y*scale),(int)(width*scale),(int)(height*scale));}
        Button ActionButton(string text,Action action){var b=new Button {Text=text,FlatStyle=FlatStyle.Flat,BackColor=Art.Panel,ForeColor=Art.White,Font=Art.Small,AccessibleName=text};b.FlatAppearance.BorderColor=Color.FromArgb(57,70,92);b.Click+=delegate{action();};Controls.Add(b);return b;}

        void Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow; long second = Tracker.UnixSecond(now);
            View fresh = tracker.Read(now, false);
            bool active = fresh.Revision != lastRevision;
            if (active) lastActive = now;
            bool animate = prefs.Animations && !fresh.Paused && now - lastActive < TimeSpan.FromSeconds(2);
            if (Visible && (active || animate || second != lastSecond))
            {
                view = fresh; if (animate) frame++;
                AccessibleDescription = "Today: " + Math.Floor(view.Today.Pixels).ToString("N0") + " pixels, " + view.Today.Keys + " key presses, " + view.Today.Clicks + " clicks. " + (view.Paused ? "Paused." : "Tracking locally.");
                Invalidate();
            }
            lastRevision = fresh.Revision; lastSecond = second;
            if (tray.Text != (fresh.Paused ? "Pixel Trek - paused" : "Pixel Trek - tracking locally")) tray.Text = fresh.Paused ? "Pixel Trek - paused" : "Pixel Trek - tracking locally";
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.ScaleTransform(scale, scale);
            string status = !String.IsNullOrEmpty(store.Error) ? "SAVE!" : input != null && input.Error != null ? "INPUT!" : tracker.Paused ? "PAUSED" : locked || sleeping ? "IDLE" : "LIVE";
            Dashboard.Render(e.Graphics, view ?? tracker.Read(DateTime.UtcNow, false), prefs.Compact, frame, !prefs.Animations || tracker.Paused || DateTime.UtcNow - lastActive > TimeSpan.FromSeconds(2), status, prefs.PixelsPerInch, input != null ? input.Live.Read() : new LiveRates(),prefs.SelectedPeriod,prefs.CityId);
        }
        void BuildMenu()
        {
            menu.Items.Clear();
            AddMenu("Show widget", false, RestoreWidget);
            AddMenu(tracker.Paused ? "Resume counting" : "Pause counting", tracker.Paused, delegate { tracker.Paused = !tracker.Paused; if (input != null) input.Reset(); Invalidate(); SaveNow(); });
            menu.Items.Add(new ToolStripSeparator());
            AddMenu("Compact strip", prefs.Compact, delegate { lock (preferenceGate) prefs.Compact = !prefs.Compact; LayoutWidget(); ClampPosition(); SaveNow(); });
            AddMenu("Always on top", prefs.OnTop, delegate { lock (preferenceGate) prefs.OnTop = !prefs.OnTop; TopMost = prefs.OnTop; SaveNow(); });
            AddMenu("Animate explorer", prefs.Animations, delegate { lock (preferenceGate) prefs.Animations = !prefs.Animations; SaveNow(); });
            AddMenu("Start with Windows", Startup.Enabled, delegate { try { Startup.Set(!Startup.Enabled); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Startup setting", MessageBoxButtons.OK, MessageBoxIcon.Warning); } });
            menu.Items.Add(new ToolStripSeparator());
            AddMenu("Screen scale / km conversion...", false, ScaleSettings);
            AddMenu("History, peaks & achievements...", false, delegate { using (var detail = new DetailForm(tracker.Read(DateTime.UtcNow, true), prefs.PixelsPerInch)) { detail.Icon = Icon; detail.ShowDialog(this); } });
            AddMenu("Save today's recap image...", false, ExportRecap);
            AddMenu("Export daily history CSV...", false, ExportCsv);
            AddMenu("Export counters backup...", false, ExportBackup);
            AddMenu("About Pixel Trek...", false, About);
            if (store.Error != null) AddMenu("Save problem...", false, delegate { MessageBox.Show(this, store.Error, "Pixel Trek"); });
            menu.Items.Add(new ToolStripSeparator());
            AddMenu("Quit & save", false, Quit);
        }
        void AddMenu(string title, bool check, Action action) { var item = new ToolStripMenuItem(title) { Checked = check, ForeColor = Art.White }; item.Click += delegate { action(); }; menu.Items.Add(item); }
        public void RestoreWidget() { Show(); ClampPosition(); Invalidate(); }
        void ExportRecap()
        {
            using (var dialog = new SaveFileDialog { Filter = "PNG image|*.png", FileName = "PixelTrek-" + DateTime.Now.ToString("yyyy-MM-dd") + ".png" })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    try { using (var image = Art.Recap(tracker.Read(DateTime.UtcNow, false), prefs.PixelsPerInch, prefs.CityId)) image.Save(dialog.FileName, ImageFormat.Png); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not save image"); }
        }
        void ScaleSettings()
        {
            using (var dialog = new ScaleForm(prefs.PixelsPerInch))
                if (dialog.ShowDialog(this) == DialogResult.OK) { lock (preferenceGate) prefs.PixelsPerInch = dialog.PixelsPerInch; SaveNow(); Invalidate(); }
        }
        void ExportCsv()
        {
            using (var dialog = new SaveFileDialog { Filter = "CSV spreadsheet|*.csv", FileName = "PixelTrek-history-" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv" })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    try { File.WriteAllText(dialog.FileName, Exports.Csv(tracker.Read(DateTime.UtcNow, true), prefs.PixelsPerInch), new System.Text.UTF8Encoding(true)); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not export history"); }
        }
        void ExportBackup()
        {
            SaveNow();
            using (var dialog = new SaveFileDialog { Filter = "JSON backup|*.json", FileName = "PixelTrek-backup-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json" })
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    try { if (!File.Exists(store.FilePath)) throw new IOException("No saved counters are available yet."); File.Copy(store.FilePath, dialog.FileName, true); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not export backup"); }
        }
        void About()
        {
            MessageBox.Show(this,"Pixel Trek / 0.3.1 - Trek edition\nA tiny desktop odometer. Five cities, one curious mouse.\n\nCounts cursor pixels, physical key taps and mouse clicks while running. No typed text, app names, calendar data, screenshots or cursor trails are stored. No network requests.\n\nKm, m/s and m/s\u00b2 are screen equivalents at "+DistanceScale.ValidPpi(prefs.PixelsPerInch).ToString("0.#")+" PPI. The city trail is a schematic: a 20 m street checkpoint followed by approximate straight-line landmark legs, not road routing. Switching cities maps your existing lifetime total onto the new route. Completed routes begin another lap.\n\nLast hour uses one-second summaries. Held-key repeats are ignored.\n\nData: "+store.DirectoryPath+"\n\nQuit before replacing app files to update. Programmatic jumps and cursor-lock games can affect observed distance. Secure desktops are outside tracking scope.","About Pixel Trek",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        public bool SaveNow()
        {
            lock (saveGate)
            {
                Preferences p; lock (preferenceGate) p = prefs.Copy();
                return store.Save(tracker.Snapshot(p, DateTime.UtcNow));
            }
        }
        public void Quit()
        {
            if (quitting) return;
            if (input != null) input.Suspend(true);
            if (!SaveNow() && store.CanSave)
            {
                if (MessageBox.Show(this, (store.Error ?? "Could not save counters.") + "\n\nQuit anyway? Unsaved activity could be lost.", "Save problem", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { if (input != null) input.Suspend(locked || sleeping); return; }
            }
            quitting = true; Close();
        }
        void ClampPosition()
        {
            Rectangle area = Screen.FromRectangle(Bounds).WorkingArea;
            Location = new Point(Math.Max(area.Left, Math.Min(Left, area.Right - Width)), Math.Max(area.Top, Math.Min(Top, area.Bottom - Height)));
            lock (preferenceGate) { prefs.X = Left; prefs.Y = Top; prefs.HasPosition = true; }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && e.Y < 55 * scale) { dragging = true; dragOrigin = Cursor.Position; windowOrigin = Location; Capture = true; }
        }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging) Location = new Point(windowOrigin.X + Cursor.Position.X - dragOrigin.X, windowOrigin.Y + Cursor.Position.Y - dragOrigin.Y); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if (dragging) { dragging = false; Capture = false; ClampPosition(); SaveNow(); } }
        void PowerChanged(object sender, PowerModeChangedEventArgs e)
        {
            sleeping = e.Mode == PowerModes.Suspend ? true : e.Mode == PowerModes.Resume ? false : sleeping;
            if (input != null) input.Suspend(locked || sleeping);
            SaveNow();
        }
        void DisplayChanged(object sender, EventArgs e) { if (!IsDisposed && IsHandleCreated) BeginInvoke((Action)delegate { ClampPosition(); if (input != null) input.Reset(); }); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x21 && (cities == null || !cities.Visible || !cities.Bounds.Contains(PointToClient(Cursor.Position)))) { m.Result = new IntPtr(3); return; } // Keep app focus except for keyboard-accessible city selection.
            if (m.Msg == 0x2E0)
            {
                scale = (m.WParam.ToInt64() & 0xFFFF) / 96f;
                var r = (NativeRect)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, typeof(NativeRect));
                Location = new Point(r.Left, r.Top); LayoutWidget(); ClampPosition(); return;
            }
            if (m.Msg == 0x2B1)
            {
                if (m.WParam.ToInt32() == 7) locked = true;
                if (m.WParam.ToInt32() == 8) locked = false;
                if (input != null) input.Suspend(locked || sleeping); SaveNow();
            }
            if (m.Msg == 0x11) { if (input != null) input.Suspend(true); SaveNow(); m.Result = new IntPtr(1); return; }
            if (m.Msg == 0x16) { if (m.WParam != IntPtr.Zero) { quitting = true; SaveNow(); } else if (input != null) input.Suspend(locked || sleeping); }
            if (m.Msg == 0x8010) { RestoreWidget(); return; }
            base.WndProc(ref m);
        }
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] struct NativeRect { public int Left, Top, Right, Bottom; }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!quitting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
            redraw.Stop(); saver.Dispose(); if (input != null) input.Dispose(); SaveNow();
            SystemEvents.PowerModeChanged -= PowerChanged; SystemEvents.DisplaySettingsChanged -= DisplayChanged;
            Native.WTSUnRegisterSessionNotification(Handle); tray.Visible = false; tray.Dispose();
            tips.Dispose(); menu.Dispose(); if(cityFont!=null)cityFont.Dispose(); base.OnFormClosing(e);
        }
    }
    internal sealed class DarkTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected { get { return Color.FromArgb(44, 55, 74); } }
        public override Color MenuItemBorder { get { return Art.Purple; } }
        public override Color ToolStripDropDownBackground { get { return Art.Panel; } }
        public override Color ImageMarginGradientBegin { get { return Art.Panel; } }
        public override Color ImageMarginGradientMiddle { get { return Art.Panel; } }
        public override Color ImageMarginGradientEnd { get { return Art.Panel; } }
    }
    internal static class Startup
    {
        const string KeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        public static bool Enabled { get { using (var key = Registry.CurrentUser.OpenSubKey(KeyPath)) return key != null && key.GetValue("PixelTrek") != null; } }
        public static void Set(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(KeyPath)) { if (enabled) key.SetValue("PixelTrek", "\"" + Application.ExecutablePath + "\""); else key.DeleteValue("PixelTrek", false); }
        }
    }
    internal sealed class DetailForm : Form
    {
        public DetailForm(View view, double ppi)
        {
            Text = "Pixel Trek - history & achievements"; BackColor = Art.Background; ForeColor = Art.White; Font = Art.Label;
            ClientSize = new Size(570, 490); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; TopMost = true;
            var intro = new Label { Text = "YOUR DESKTOP ADVENTURE", ForeColor = Art.Cyan, Font = Art.Title, Location = new Point(18, 16), AutoSize = true };
            var best = new Label { Text = "Best days  /  " + Display.Short(view.BestDay.Pixels) + " px   /   " + Display.Short(view.BestDay.Keys) + " keys   /   " + Display.Short(view.BestDay.Clicks) + " clicks", Location = new Point(18, 47), Size = new Size(535, 24), ForeColor = Art.Muted };
            var tabs = new TabControl { Location = new Point(18, 82), Size = new Size(534, 350) };
            var history = new TabPage("Daily history") { BackColor = Art.Panel }; var achievements = new TabPage("Achievements") { BackColor = Art.Panel }; var peaks = new TabPage("Speed & acceleration peaks") { BackColor = Art.Panel };
            tabs.TabPages.Add(history); tabs.TabPages.Add(peaks); tabs.TabPages.Add(achievements);
            var list = new ListView { Dock = DockStyle.Fill, View = System.Windows.Forms.View.Details, FullRowSelect = true, BackColor = Art.Panel, ForeColor = Art.White, BorderStyle = BorderStyle.None };
            list.Columns.Add("Date", 100); list.Columns.Add("Pixels", 130); list.Columns.Add("Km eq", 95); list.Columns.Add("Keys", 85); list.Columns.Add("Clicks", 85);
            foreach (DayBucket day in view.Days) list.Items.Add(new ListViewItem(new [] { day.Date, Math.Floor(day.Counts.Pixels).ToString("N0"), Display.Kilometres(day.Counts.Pixels, ppi).ToString("0.000"), day.Counts.Keys.ToString("N0"), day.Counts.Clicks.ToString("N0") }));
            history.Controls.Add(list);
            var rateList = new ListView { Dock = DockStyle.Fill, View = System.Windows.Forms.View.Details, FullRowSelect = true, BackColor = Art.Panel, ForeColor = Art.White, BorderStyle = BorderStyle.None };
            rateList.Columns.Add("Period", 135); rateList.Columns.Add("Peak speed / m/s eq", 174); rateList.Columns.Add("Peak accel / m/s\u00b2 eq", 193);
            Counts[] rateCounts = { view.Today, view.Hour, view.Total }; string[] periods = { "Today", "Last 60 minutes", "All time" };
            for (int i = 0; i < 3; i++) rateList.Items.Add(new ListViewItem(new [] { periods[i], Display.Rate(rateCounts[i].PeakSpeedPixelsPerSecond, ppi), Display.Rate(rateCounts[i].PeakAccelerationPixelsPerSecondSquared, ppi) }) { ForeColor = i == 2 ? Art.Cyan : Art.White });
            peaks.Controls.Add(rateList);
            var badges = new ListView { Dock = DockStyle.Fill, View = System.Windows.Forms.View.Details, FullRowSelect = true, BackColor = Art.Panel, ForeColor = Art.White, BorderStyle = BorderStyle.None };
            badges.Columns.Add("Achievement", 200); badges.Columns.Add("Milestone", 185); badges.Columns.Add("Status", 110);
            badges.Columns.Add("Real-world comparison",350);
            double lifetimeKm=Display.Kilometres(view.Total.Pixels,ppi);
            foreach(var m in Adventure.Milestones)badges.Items.Add(new ListViewItem(new[]{m.Name,Adventure.Distance(m.Km)+" eq",lifetimeKm>=m.Km?"Reached":"Ahead",m.Comparison}) {ForeColor=lifetimeKm>=m.Km?Art.Cyan:Art.Muted});
            for(int kind=0;kind<2;kind++)
            {
                bool keys=kind==0;long count=keys?view.Total.Keys:view.Total.Clicks;long[] goals=keys?Adventure.KeyGoals:Adventure.ClickGoals;string[] names=keys?Adventure.KeyNames:Adventure.ClickNames;
                for(int i=0;i<goals.Length;i++)badges.Items.Add(new ListViewItem(new[]{names[i],goals[i].ToString("N0")+(keys?" key taps":" clicks"),count>=goals[i]?"Unlocked":"Ahead"}) {ForeColor=count>=goals[i]?Art.Cyan:Art.Muted});
            }
            achievements.Controls.Add(badges);
            var note = new Label { Text = "Screen equivalents at " + DistanceScale.ValidPpi(ppi).ToString("0.#") + " PPI; acceleration includes direction changes.\n366 active dates kept. Lifetime totals and peaks are retained separately.", Location = new Point(18, 441), Size = new Size(534, 40), ForeColor = Art.Muted };
            Controls.AddRange(new Control[] { intro, best, tabs, note });
        }
    }
    internal sealed class ScaleForm : Form
    {
        readonly NumericUpDown ppi, width, height, diagonal;
        public double PixelsPerInch { get { return (double)ppi.Value; } }
        public ScaleForm(double value)
        {
            Text = "Pixel Trek - screen equivalents"; BackColor = Art.Background; ForeColor = Art.White; Font = Art.Label;
            ClientSize = new Size(470, 353); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; TopMost = true;
            LabelAt("SCREEN DISTANCE SCALE", 18, 17, 430, Art.Cyan, Art.Title);
            LabelAt("Kilometres, speed and acceleration use this screen scale.\nDefault: 96 PPI reference. One scale applies to all monitors\nand saved history; pixel counts always stay unchanged.", 18, 51, 433, Art.Muted, Art.Label, 60);
            LabelAt("Pixels per inch (PPI)", 18, 120, 180, Art.White, Art.Label);
            ppi = NumberAt(214, 116, 120, 20, 1000, (decimal)DistanceScale.ValidPpi(value), 1);
            var reference = ActionButton("Use 96", 345, 115, 105, delegate { ppi.Value = 96; });
            LabelAt("Optional: calculate from a monitor's resolution and diagonal", 18, 163, 432, Art.Muted, Art.Small);
            LabelAt("Width / px", 18, 190, 100, Art.White, Art.Small); LabelAt("Height / px", 152, 190, 100, Art.White, Art.Small); LabelAt("Diagonal / inches", 287, 190, 150, Art.White, Art.Small);
            width = NumberAt(18, 214, 115, 320, 32768, 1920, 0); height = NumberAt(152, 214, 115, 240, 32768, 1080, 0); diagonal = NumberAt(287, 214, 140, 5, 200, 24, 1);
            ActionButton("Calculate PPI", 18, 259, 145, delegate
            {
                try { ppi.Value = Math.Round((decimal)DistanceScale.FromMonitor((double)width.Value, (double)height.Value, (double)diagonal.Value), 1); }
                catch (ArgumentException ex) { MessageBox.Show(this, ex.Message, "Check monitor values"); }
            });
            var save = ActionButton("Save scale", 314, 307, 135, delegate { }); save.DialogResult = DialogResult.OK;
            var cancel = ActionButton("Cancel", 191, 307, 110, delegate { }); cancel.DialogResult = DialogResult.Cancel; AcceptButton = save; CancelButton = cancel;
        }
        void LabelAt(string text, int x, int y, int width, Color color, Font font, int height = 25) { Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(width, height), ForeColor = color, Font = font }); }
        NumericUpDown NumberAt(int x, int y, int w, decimal min, decimal max, decimal initial, int decimals)
        {
            var box = new NumericUpDown { Location = new Point(x, y), Size = new Size(w, 28), Minimum = min, Maximum = max, Value = initial, DecimalPlaces = decimals, Increment = decimals == 0 ? 1 : 0.1m, BackColor = Art.Panel, ForeColor = Art.White };
            Controls.Add(box); return box;
        }
        Button ActionButton(string text, int x, int y, int w, Action action)
        {
            var button = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 30), FlatStyle = FlatStyle.Flat, BackColor = Art.Panel, ForeColor = Art.White };
            button.FlatAppearance.BorderColor = Art.Purple; button.Click += delegate { action(); }; Controls.Add(button); return button;
        }
    }
}
