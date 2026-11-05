using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.IO;

internal static class CameraArtwork {
    private static GraphicsPath Rounded(float x, float y, float w, float h, float r) {
        GraphicsPath p = new GraphicsPath();
        p.AddArc(x, y, r * 2, r * 2, 180, 90);
        p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        p.CloseFigure(); return p;
    }
    internal static void Draw(Graphics graphics, Rectangle bounds, bool tile) {
        GraphicsState saved = graphics.Save();
        graphics.TranslateTransform(bounds.X, bounds.Y);
        graphics.ScaleTransform(bounds.Width / 64f, bounds.Height / 64f);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (tile) {
            using (GraphicsPath p = Rounded(1, 1, 62, 62, 15))
            using (LinearGradientBrush bg = new LinearGradientBrush(new Point(0, 0), new Point(64, 64), Color.FromArgb(41, 51, 66), Color.FromArgb(19, 25, 35)))
                graphics.FillPath(bg, p);
        }
        using (GraphicsPath hump = Rounded(22, 13, 22, 14, 4))
        using (Brush mint = new SolidBrush(Color.FromArgb(126, 245, 211))) graphics.FillPath(mint, hump);
        using (GraphicsPath body = Rounded(9, 21, 47, 31, 7))
        using (LinearGradientBrush mint = new LinearGradientBrush(new Point(10, 20), new Point(54, 52), Color.FromArgb(143, 255, 223), Color.FromArgb(43, 190, 157))) graphics.FillPath(mint, body);
        using (Brush shadow = new SolidBrush(Color.FromArgb(33, 107, 96))) graphics.FillEllipse(shadow, 22, 25, 25, 25);
        using (Brush lens = new SolidBrush(Color.FromArgb(23, 38, 52))) graphics.FillEllipse(lens, 25, 28, 19, 19);
        using (Pen rim = new Pen(Color.FromArgb(144, 244, 222), 1.6f)) graphics.DrawEllipse(rim, 28, 31, 13, 13);
        using (Brush glass = new SolidBrush(Color.FromArgb(80, 128, 156))) graphics.FillEllipse(glass, 30, 32, 5, 5);
        using (Brush light = new SolidBrush(Color.FromArgb(234, 255, 250))) {
            graphics.FillEllipse(light, 46, 26, 4, 4);
            graphics.FillEllipse(light, 30, 32, 2.5f, 2.5f);
        }
        graphics.Restore(saved);
    }
    internal static void Export(string directory) {
        Directory.CreateDirectory(directory);
        int[] sizes = { 16, 20, 24, 32, 48, 64, 128, 256 };
        byte[][] frames = new byte[sizes.Length][];
        for (int i = 0; i < sizes.Length; i++) {
            using (Bitmap image = new Bitmap(sizes[i], sizes[i])) {
                using (Graphics g = Graphics.FromImage(image)) Draw(g, new Rectangle(0, 0, sizes[i], sizes[i]), true);
                using (MemoryStream stream = new MemoryStream()) {
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Png); frames[i] = stream.ToArray();
                }
                if (sizes[i] == 256) image.Save(Path.Combine(directory, "camera.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(directory, "camera.ico")))) {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
            int offset = 6 + sizes.Length * 16;
            for (int i = 0; i < sizes.Length; i++) {
                writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(frames[i].Length); writer.Write(offset); offset += frames[i].Length;
            }
            foreach (byte[] frame in frames) writer.Write(frame);
        }
    }
}

internal static class Native {
    [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("dwmapi.dll")] internal static extern int DwmFlush();
}

internal static class ChatFocus {
    private static uint cachedProcessId;
    private static bool cachedResult;
    internal static bool IsChatProcess(string name) {
        return string.Equals(name, "ChatGPT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Codex", StringComparison.OrdinalIgnoreCase);
    }
    internal static bool IsChatWindow(IntPtr window) {
        if (window == IntPtr.Zero) return false;
        uint processId;
        Native.GetWindowThreadProcessId(window, out processId);
        if (processId == 0) return false;
        if (processId != cachedProcessId) {
            cachedProcessId = 0;
            cachedResult = false;
            try {
                using (Process process = Process.GetProcessById((int)processId))
                    cachedResult = IsChatProcess(process.ProcessName);
                cachedProcessId = processId;
            } catch (ArgumentException) { }
              catch (System.ComponentModel.Win32Exception) { }
              catch (InvalidOperationException) { }
        }
        return cachedResult;
    }
}

internal static class Program {
    [STAThread] private static int Main(string[] args) {
        if (args.Length == 2 && args[0] == "--export-icons") { CameraArtwork.Export(args[1]); return 0; }
        if (args.Length == 1 && args[0] == "--self-test") {
            Rectangle r = RegionPicker.Between(new Point(180, 90), new Point(30, 10));
            bool focusChecks = ChatFocus.IsChatProcess("ChatGPT") && ChatFocus.IsChatProcess("codex")
                && !ChatFocus.IsChatProcess("CodexScreenshot") && !ChatFocus.IsChatProcess("msedge")
                && !ChatFocus.IsChatProcess("codex-code-mode-host") && !ChatFocus.IsChatProcess(null)
                && !ChatFocus.IsChatWindow(IntPtr.Zero);
            return r == new Rectangle(30, 10, 150, 80) && focusChecks ? 0 : 1;
        }
        bool first;
        using (Mutex mutex = new Mutex(true, "Local\\CodexScreenshotButton", out first)) {
            if (!first) return 0;
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ScreenshotButton());
        }
        return 0;
    }
}

internal sealed class ScreenshotButton : Form {
    private readonly NotifyIcon tray;
    private readonly ToolTip tip;
    private readonly System.Windows.Forms.Timer pasteTimer;
    private readonly System.Windows.Forms.Timer focusTimer;
    private Point dragStart;
    private bool moving, busy, userHidden;
    private IntPtr destination;
    private int waitTicks;

    protected override bool ShowWithoutActivation { get { return true; } }
    protected override void SetVisibleCore(bool value) {
        base.SetVisibleCore(value && !userHidden && !busy && ChatFocus.IsChatWindow(Native.GetForegroundWindow()));
    }
    private void UpdateVisibility() {
        if (busy || moving || (ContextMenuStrip != null && ContextMenuStrip.Visible)) return;
        bool shouldShow = !userHidden && ChatFocus.IsChatWindow(Native.GetForegroundWindow());
        if (Visible != shouldShow) Visible = shouldShow;
    }
    private void FinishCapture() { busy = false; UpdateVisibility(); }
    protected override CreateParams CreateParams {
        get {
            CreateParams p = base.CreateParams;
            p.ExStyle |= 0x08000000 | 0x00000080; // NOACTIVATE, TOOLWINDOW
            return p;
        }
    }
    public ScreenshotButton() {
        Text = "Codex Screenshot";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(116, 42);
        BackColor = Color.FromArgb(29, 35, 46);
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        Rectangle work = Screen.PrimaryScreen.WorkingArea;
        Location = new Point(work.Right - Width - 26, work.Bottom - Height - 115);
        AccessibleName = "Recortar e anexar print";
        AccessibleDescription = "Clique na caixa de mensagem do chat, depois neste botao e arraste a area desejada.";
        tip = new ToolTip();
        tip.SetToolTip(this, "Clique na mensagem do chat, depois em Print. Arraste pelos pontos para mover. Esc cancela o recorte.");
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Mostrar quando o chat estiver em foco", null, delegate { userHidden = false; UpdateVisibility(); });
        menu.Items.Add("Ocultar botao", null, delegate { userHidden = true; Hide(); });
        menu.Items.Add("Sair", null, delegate { Close(); });
        ContextMenuStrip = menu;
        tray = new NotifyIcon { Icon = Icon, Text = "Codex Screenshot - Ctrl+Alt+S", Visible = true, ContextMenuStrip = menu };
        tray.DoubleClick += delegate { userHidden = false; UpdateVisibility(); };
        pasteTimer = new System.Windows.Forms.Timer { Interval = 100 };
        pasteTimer.Tick += PasteWhenReady;
        focusTimer = new System.Windows.Forms.Timer { Interval = 150 };
        focusTimer.Tick += delegate { UpdateVisibility(); };
        focusTimer.Start();
    }
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        Native.RegisterHotKey(Handle, 1, 0x4003, (uint)Keys.S); // optional Ctrl+Alt+S, no-repeat
    }
    protected override void WndProc(ref Message m) {
        if (m.Msg == 0x0021) { m.Result = new IntPtr(3); return; } // MA_NOACTIVATE
        if (m.Msg == 0x0312 && m.WParam.ToInt32() == 1) { BeginCapture(); return; }
        base.WndProc(ref m);
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        CameraArtwork.Draw(e.Graphics, new Rectangle(8, 4, 34, 34), false);
        using (Font font = new Font("Segoe UI", 11, FontStyle.Bold))
            e.Graphics.DrawString("Print", font, Brushes.White, 43, 10);
        using (Brush brush = new SolidBrush(Color.FromArgb(123, 133, 150)))
            for (int y = 14; y < 30; y += 5) e.Graphics.FillEllipse(brush, 103, y, 2, 2);
    }
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        dragStart = e.Location;
        moving = e.X >= 96;
        if (moving) Capture = true;
    }
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if (moving) Location = new Point(Left + e.X - dragStart.X, Top + e.Y - dragStart.Y);
    }
    protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        if (moving) { moving = false; Capture = false; return; }
        if (ClientRectangle.Contains(e.Location)) BeginCapture();
    }
    private void BeginCapture() {
        if (busy) return;
        destination = Native.GetForegroundWindow();
        if (!ChatFocus.IsChatWindow(destination)) {
            Notify("Clique primeiro na caixa de mensagem do chat."); return;
        }
        busy = true;
        Hide();
        try {
            Native.DwmFlush();
            Rectangle desktop = SystemInformation.VirtualScreen;
            if ((long)desktop.Width * desktop.Height > 80000000) throw new Exception("Area de tela muito grande.");
            using (Bitmap snapshot = new Bitmap(desktop.Width, desktop.Height)) {
                using (Graphics g = Graphics.FromImage(snapshot))
                    g.CopyFromScreen(desktop.Location, Point.Empty, desktop.Size);
                using (RegionPicker picker = new RegionPicker(snapshot, desktop)) {
                    if (picker.ShowDialog() != DialogResult.OK) { FinishCapture(); return; }
                    using (Bitmap image = snapshot.Clone(picker.Selection, snapshot.PixelFormat))
                        Clipboard.SetDataObject(image, true, 10, 80);
                }
            }
            if (!Native.IsWindow(destination) || !Native.SetForegroundWindow(destination)) {
                Notify("Recorte copiado. Clique na mensagem e pressione Ctrl+V.");
                FinishCapture(); return;
            }
            waitTicks = 0;
            pasteTimer.Start();
        } catch (Exception ex) {
            Notify("Nao foi possivel recortar: " + ex.Message);
            FinishCapture();
        }
    }
    private void PasteWhenReady(object sender, EventArgs e) {
        // Wait for mouse/hotkey release before sending a single Ctrl+V.
        if (++waitTicks < 3) return;
        if (Native.GetAsyncKeyState(0x01) < 0 || Native.GetAsyncKeyState(0x11) < 0 || Native.GetAsyncKeyState(0x12) < 0 || Native.GetAsyncKeyState(0x10) < 0) {
            if (waitTicks < 30) return;
        }
        pasteTimer.Stop();
        if (waitTicks >= 30 || Native.GetForegroundWindow() != destination) {
            Notify("Recorte copiado. Clique na mensagem e pressione Ctrl+V.");
        } else {
            try { SendKeys.SendWait("^v"); }
            catch { Notify("Recorte copiado. Cole com Ctrl+V."); }
        }
        FinishCapture();
    }
    private void Notify(string text) { tray.ShowBalloonTip(3500, "Codex Screenshot", text, ToolTipIcon.Info); }
    protected override void Dispose(bool disposing) {
        if (disposing) {
            Native.UnregisterHotKey(Handle, 1);
            focusTimer.Dispose(); pasteTimer.Dispose(); tip.Dispose(); tray.Visible = false; tray.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class RegionPicker : Form {
    private readonly Bitmap snapshot;
    private Point anchor;
    private bool selecting;
    public Rectangle Selection { get; private set; }
    internal static Rectangle Between(Point a, Point b) {
        return Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
    }
    public RegionPicker(Bitmap image, Rectangle bounds) {
        snapshot = image;
        Text = "Selecione a area - Esc cancela";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        KeyPreview = true;
    }
    protected override void OnShown(EventArgs e) { base.OnShown(e); Activate(); }
    protected override void OnPaint(PaintEventArgs e) {
        e.Graphics.DrawImageUnscaled(snapshot, 0, 0);
        using (Brush shade = new SolidBrush(Color.FromArgb(105, 0, 0, 0))) e.Graphics.FillRectangle(shade, ClientRectangle);
        if (Selection.Width > 0 && Selection.Height > 0) {
            e.Graphics.DrawImage(snapshot, Selection, Selection, GraphicsUnit.Pixel);
            using (Pen pen = new Pen(Color.FromArgb(71, 202, 169), 2)) e.Graphics.DrawRectangle(pen, Selection);
        }
        Point p = PointToClient(MousePosition);
        Rectangle label = new Rectangle(Math.Max(12, Math.Min(p.X + 18, Width - 350)), Math.Max(12, Math.Min(p.Y + 24, Height - 42)), 330, 30);
        using (Brush bg = new SolidBrush(Color.FromArgb(235, 29, 35, 46))) e.Graphics.FillRectangle(bg, label);
        using (Font font = new Font("Segoe UI", 10)) e.Graphics.DrawString("Arraste a area  |  Esc cancela", font, Brushes.White, label.X + 10, label.Y + 5);
    }
    protected override void OnMouseDown(MouseEventArgs e) {
        if (e.Button == MouseButtons.Right) { DialogResult = DialogResult.Cancel; return; }
        if (e.Button == MouseButtons.Left) { anchor = e.Location; selecting = true; Capture = true; }
    }
    protected override void OnMouseMove(MouseEventArgs e) {
        if (selecting) Selection = Rectangle.Intersect(ClientRectangle, Between(anchor, e.Location));
        Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e) {
        if (!selecting || e.Button != MouseButtons.Left) return;
        selecting = false; Capture = false;
        Selection = Rectangle.Intersect(ClientRectangle, Between(anchor, e.Location));
        if (Selection.Width >= 2 && Selection.Height >= 2) DialogResult = DialogResult.OK;
        else Invalidate();
    }
    protected override void OnKeyDown(KeyEventArgs e) {
        if (e.KeyCode == Keys.Escape) DialogResult = DialogResult.Cancel;
        base.OnKeyDown(e);
    }
}
