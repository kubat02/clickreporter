using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace clickreporter
{
    public partial class Form1 : Form
    {
        // ── Global keyboard hook ──────────────────────────────────────────────
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN     = 0x0100;
        private const int WM_SYSKEYDOWN  = 0x0104;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _hookCallback;
        private IntPtr _hookId = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
                                                       IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
                                                     IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // ── State ─────────────────────────────────────────────────────────────
        private long   _todayCount;          // in-memory counter for today
        private string _logDate;             // "yyyy-MM-dd" of the current session
        private readonly string _logFolder;
        private readonly string _logFile;
        private readonly Timer  _saveTimer;
        private bool   _yesterdayShown;      // show "yesterday" popup only once

        // ── Startup-run registry key ──────────────────────────────────────────
        private const string AppName    = "KeystrokeReporter";
        private const string RunKey     = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public Form1()
        {
            InitializeComponent();

            // Log dosya yolu: Belgeler\MyClickLog\keylog.txt
            _logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "MyClickLog");
            _logFile = Path.Combine(_logFolder, "keylog.txt");

            EnsureStartup();
            LoadTodayCount();
            ShowYesterdayPopupIfNeeded();

            // Global klavye kancası
            _hookCallback = HookCallback;
            _hookId = SetHook(_hookCallback);

            // 30 saniyede bir otomatik kayıt
            _saveTimer = new Timer { Interval = 30_000 };
            _saveTimer.Tick += (s, e) => SaveCount();
            _saveTimer.Start();
        }

        // ── Windows başlangıcına kayıt ────────────────────────────────────────
        private static bool IsStartupEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false))
                {
                    if (key == null) return false;
                    var existing = key.GetValue(AppName) as string;
                    return string.Equals(existing, Application.ExecutablePath,
                                         StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
        }

        private static void SetStartup(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true))
                {
                    if (key == null) return;
                    if (enable)
                        key.SetValue(AppName, Application.ExecutablePath);
                    else
                        key.DeleteValue(AppName, throwOnMissingValue: false);
                }
            }
            catch { }
        }

        private static void EnsureStartup()
        {
            if (!IsStartupEnabled())
                SetStartup(true);
        }

        // ── Log dosyasından bugünkü sayacı oku ───────────────────────────────
        private void LoadTodayCount()
        {
            _logDate    = DateTime.Today.ToString("yyyy-MM-dd");
            _todayCount = 0;

            if (!Directory.Exists(_logFolder))
                Directory.CreateDirectory(_logFolder);

            // Bugünün satırını dosyanın herhangi bir yerinde ara
            long saved = ReadCountForDate(_logDate);
            if (saved > 0)
                _todayCount = saved;   // kaldığı yerden devam et
        }

        // ── "Dünkü" bildirim popup ────────────────────────────────────────────
        private void ShowYesterdayPopupIfNeeded()
        {
            if (_yesterdayShown) return;

            string yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            long yCount = ReadCountForDate(yesterday);

            if (yCount <= 0) return;

            _yesterdayShown = true;
            string msg = $"{yesterday} tarihinde toplam tuş vuruşunuz:\n\n" +
                         $"{yCount:N0} tuş\n\nİyi çalışmalar!";
            MessageBox.Show(msg, "Dünkü Tuş Vuruşu",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private long ReadCountForDate(string date)
        {
            if (!File.Exists(_logFile)) return 0;
            try
            {
                using (var fs = new FileStream(_logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var parts = line.Trim().Split('\t');
                        if (parts.Length >= 2 && parts[0] == date
                            && long.TryParse(parts[1], out long c))
                            return c;
                    }
                }
            }
            catch { }
            return 0;
        }

        // ── Log dosyasına yaz ─────────────────────────────────────────────────
        private void SaveCount()
        {
            try
            {
                // Gece yarısı geçişi kontrolü
                string today = DateTime.Today.ToString("yyyy-MM-dd");
                if (today != _logDate)
                {
                    _logDate    = today;
                    long existing = ReadCountForDate(today);
                    _todayCount = existing > 0 ? existing : 0;
                }

                // Tüm satırları oku, bugünkü satırı güncelle / ekle
                var lines = new System.Collections.Generic.List<string>();
                bool found = false;

                if (File.Exists(_logFile))
                {
                    using (var fs = new FileStream(_logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                            if (!string.IsNullOrWhiteSpace(line))
                                lines.Add(line.Trim());
                    }
                }

                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith(_logDate + "\t"))
                    {
                        lines[i] = $"{_logDate}\t{_todayCount}";
                        found = true;
                        break;
                    }
                }
                if (!found)
                    lines.Add($"{_logDate}\t{_todayCount}");

                // Atomik yazma
                string tmp = _logFile + ".tmp";
                File.WriteAllLines(tmp, lines);
                if (File.Exists(_logFile))
                    File.Replace(tmp, _logFile, null);
                else
                    File.Move(tmp, _logFile);
            }
            catch { }
        }

        // ── Global hook kurulumu ──────────────────────────────────────────────
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var mod = System.Diagnostics.Process.GetCurrentProcess().MainModule)
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                                        GetModuleHandle(mod.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 &&
               (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                _todayCount++;
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // ── Tray icon: sağ tık → Exit ─────────────────────────────────────────
        private void contextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            menuTodayItem.Text    = $"Bugünkü Tuş: {_todayCount:N0}";
            menuStartup.Checked   = IsStartupEnabled();
        }

        private void menuStartup_Click(object sender, EventArgs e)
        {
            bool newState = !IsStartupEnabled();
            SetStartup(newState);
            menuStartup.Checked = newState;
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            SaveCount();
            Application.Exit();
        }

        // ── Tray icon: klavye simgesi (runtime üretim) ────────────────────────
        private static System.Drawing.Icon CreateTrayIcon()
        {
            var bmp = new System.Drawing.Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Arka plan: koyu mavi, yuvarlak köşeli
                using (var bg = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 25, 55, 115)))
                    g.FillRectangle(bg, 2, 2, 28, 28);
                // Tuş rengi: açık gri
                using (var kb = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230, 240, 248, 255)))
                {
                    // Üst sıra: 4 tuş
                    g.FillRectangle(kb, 4,  6, 4, 4);
                    g.FillRectangle(kb, 10, 6, 4, 4);
                    g.FillRectangle(kb, 16, 6, 4, 4);
                    g.FillRectangle(kb, 22, 6, 6, 4);
                    // Orta sıra: 4 tuş
                    g.FillRectangle(kb, 4,  13, 4, 4);
                    g.FillRectangle(kb, 10, 13, 4, 4);
                    g.FillRectangle(kb, 16, 13, 4, 4);
                    g.FillRectangle(kb, 22, 13, 6, 4);
                    // Space bar
                    g.FillRectangle(kb, 6,  20, 20, 4);
                }
            }
            return System.Drawing.Icon.FromHandle(bmp.GetHicon());
        }

        // ── Form kapatma: sisteme gömülü, görünmez ────────────────────────────
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Visible   = false;
            ShowInTaskbar = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;   // X butonuna tıklanırsa sadece gizle
                return;
            }
            SaveCount();
            if (_hookId != IntPtr.Zero)
                UnhookWindowsHookEx(_hookId);
            _saveTimer?.Stop();
            base.OnFormClosing(e);
        }
    }
}
