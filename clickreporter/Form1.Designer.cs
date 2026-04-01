namespace clickreporter
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components       = new System.ComponentModel.Container();
            this.notifyIcon       = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenu      = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuTodayItem    = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSep          = new System.Windows.Forms.ToolStripSeparator();
            this.menuStartup      = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSep2         = new System.Windows.Forms.ToolStripSeparator();
            this.menuExit         = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu.SuspendLayout();

            // notifyIcon
            this.notifyIcon.ContextMenuStrip = this.contextMenu;
            this.notifyIcon.Icon             = CreateTrayIcon();
            this.notifyIcon.Text             = "Key Strike Counter";
            this.notifyIcon.Visible          = true;

            // menuTodayItem (bilgi ogesi, tiklanamaz)
            this.menuTodayItem.Text    = "Bugunku Tus: 0";
            this.menuTodayItem.Enabled = false;

            // menuStartup
            this.menuStartup.Text          = "Windows ile Basla";
            this.menuStartup.CheckOnClick  = false;
            this.menuStartup.Click        += new System.EventHandler(this.menuStartup_Click);

            // menuExit
            this.menuExit.Text   = "Cikis (Exit)";
            this.menuExit.Click += new System.EventHandler(this.menuExit_Click);

            // contextMenu
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.menuTodayItem,
                this.menuSep,
                this.menuStartup,
                this.menuSep2,
                this.menuExit
            });
            this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenu_Opening);
            this.contextMenu.ResumeLayout(false);

            // Form1
            this.AutoScaleMode   = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize      = new System.Drawing.Size(1, 1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.ShowInTaskbar   = false;
            this.Text            = "Key Strike Counter";
            this.WindowState     = System.Windows.Forms.FormWindowState.Minimized;
        }

        private System.Windows.Forms.NotifyIcon        notifyIcon;
        private System.Windows.Forms.ContextMenuStrip  contextMenu;
        private System.Windows.Forms.ToolStripMenuItem menuTodayItem;
        private System.Windows.Forms.ToolStripSeparator menuSep;
        private System.Windows.Forms.ToolStripMenuItem menuStartup;
        private System.Windows.Forms.ToolStripSeparator menuSep2;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
    }
}
