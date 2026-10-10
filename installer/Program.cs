using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NFVU2.Setup {
static class Program {
    [STAThread] static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SetupWindow());
    }
}
sealed class SetupWindow : Form {
    readonly TextBox path = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
    readonly Button browse = new Button { Text = "Browse...", AutoSize = true };
    readonly Button install = new Button { Text = "Install / Update", AutoSize = true, Enabled = false };
    readonly Button uninstall = new Button { Text = "Uninstall", AutoSize = true, Enabled = false };
    readonly CheckBox loader = new CheckBox { Text = "Also remove bundled BepInEx when uninstalling", AutoSize = true };
    readonly Label status = new Label { Text = "Select valheim.exe to get started.", AutoSize = true, MaximumSize = new Size(620, 0) };
    readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill, Visible = false, Style = ProgressBarStyle.Marquee };
    bool busy;
    public SetupWindow() {
        Text = "NFVU2 Setup"; ClientSize = new Size(720, 610); MinimumSize = new Size(736, 649);
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24), ColumnCount = 1, RowCount = 8 };
        for (int i = 0; i < 8; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var logo = new PictureBox { Dock = DockStyle.Top, Height = 220, SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(14, 22, 30), Margin = new Padding(0, 0, 0, 16),
            AccessibleName = "Need for Valheim: Underground 2", TabStop = false };
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NFVU2.Logo"))
        using (var original = Image.FromStream(stream)) logo.Image = new Bitmap(original);
        FormClosed += (_, __) => logo.Image.Dispose();
        layout.Controls.Add(logo);
        layout.Controls.Add(new Label { Text = "Choose Valheim. Install. Drive.\nClose the game before making changes.", AutoSize = true, Margin = new Padding(0, 0, 0, 18) });
        var selection = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
        selection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); selection.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selection.Controls.Add(path, 0, 0); selection.Controls.Add(browse, 1, 0); layout.Controls.Add(selection);
        layout.Controls.Add(loader);
        layout.Controls.Add(new Label { Text = "BepInEx is included for fresh installs. Existing loaders are preserved.\nUninstall keeps your settings, saves and backups. Other mods are kept.", AutoSize = true, Margin = new Padding(0, 10, 0, 14) });
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0) };
        var notices = new Button { Text = "LICENSE", AutoSize = true };
        notices.Click += (_, __) => {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NFVU2.Notices"))
            using (var reader = new StreamReader(stream))
            using (var window = new Form { Text = "LICENSE", Size = new Size(720, 540), MinimumSize = new Size(420, 300), StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false }) {
                var content = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 2 };
                content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                content.Controls.Add(new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill, TabStop = false, Text = reader.ReadToEnd().Replace("\r\n", "\n").Replace("\n", Environment.NewLine) }, 0, 0);
                var close = new Button { Text = "Close", AutoSize = true, Anchor = AnchorStyles.Right,
                    DialogResult = DialogResult.OK, Margin = new Padding(0, 12, 0, 0) };
                content.Controls.Add(close, 0, 1);
                window.Controls.Add(content);
                window.AcceptButton = close; window.CancelButton = close;
                window.Shown += (_, __) => close.Focus();
                window.ShowDialog(this);
            }
        };
        actions.Controls.Add(install); actions.Controls.Add(uninstall); actions.Controls.Add(notices); layout.Controls.Add(actions);
        layout.Controls.Add(progress); layout.Controls.Add(status); Controls.Add(layout);
        browse.Click += (_, __) => Choose();
        install.Click += async (_, __) => await Execute(false);
        uninstall.Click += async (_, __) => await Execute(true);
        FormClosing += (_, e) => { if (busy) e.Cancel = true; };
    }
    void Choose() {
        using (var dialog = new OpenFileDialog { Title = "Select valheim.exe", Filter = "Valheim|valheim.exe", CheckFileExists = true }) {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try {
                var core = new InstallerCore(dialog.FileName);
                path.Text = dialog.FileName; install.Enabled = true; uninstall.Enabled = core.HasInstallation;
                status.Text = core.HasInstallation ? "NFVU2 found. Ready to update or uninstall." : "Ready to install NFVU2.";
            } catch (Exception e) { MessageBox.Show(this, e.Message, "Select Valheim", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
    async Task Execute(bool removing) {
        if (removing && MessageBox.Show(this, "Remove NFVU2 from this Valheim installation?\nYour saves and personal settings will be kept.", "Uninstall NFVU2", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        busy = true; browse.Enabled = install.Enabled = uninstall.Enabled = loader.Enabled = false;
        progress.Visible = true;
        string selected = path.Text; bool removeLoader = loader.Checked;
        var messages = new Progress<string>(s => status.Text = s);
        try {
            await Task.Run(() => {
                var core = new InstallerCore(selected, report: s => ((IProgress<string>)messages).Report(s));
                if (removing) core.Uninstall(removeLoader);
                else using (var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("NFVU2.Payload")) {
                    if (payload == null) throw new IOException("This installer is missing its payload.");
                    core.Install(payload);
                }
            });
            status.Text = removing ? "NFVU2 removed. Settings and backups were kept." : "Installed! Launch Valheim normally, open a single-player world, then press F9 (controller: L3) and choose Garage.";
        } catch (UnauthorizedAccessException) {
            status.Text = "Windows denied access. Close this wizard, right-click the EXE and choose Run as administrator, then select Valheim again.";
        } catch (Exception e) { status.Text = e.Message; MessageBox.Show(this, e.Message, "NFVU2 Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally {
            busy = false; progress.Visible = false; browse.Enabled = install.Enabled = loader.Enabled = true;
            try { uninstall.Enabled = new InstallerCore(selected).HasInstallation; } catch { uninstall.Enabled = false; }
        }
    }
}
}
