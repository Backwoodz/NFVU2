using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NFVU2.Setup {
static class Program {
    [STAThread] static void Main(string[] args) {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try { Application.Run(new SetupWindow(SetupArguments.Parse(args))); }
        catch(Exception error) { MessageBox.Show(error.Message,"NFVU2 Setup",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
    }
}
sealed partial class SetupWindow : Form {
    readonly TextBox path = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
    readonly Button browse = new Button { Text = "Browse...", AutoSize = true };
    readonly Button install = new Button { Text = "Install / Update", AutoSize = true, Enabled = false };
    readonly Button uninstall = new Button { Text = "Uninstall", AutoSize = true, Enabled = false };
    readonly CheckBox loader = new CheckBox { Text = "Also remove bundled BepInEx when uninstalling", AutoSize = true };
    readonly Label status = new Label { Text = "Select valheim.exe to get started.", AutoSize = true, MaximumSize = new Size(620, 0) };
    readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill, Visible = false, Style = ProgressBarStyle.Marquee };
    bool busy;
    public SetupWindow() : this(new SetupArguments()) {}
    public SetupWindow(SetupArguments options) {
        Text = "NFVU2 Setup — "+CurrentDisplay; ClientSize = new Size(720, 790); MinimumSize = new Size(736, 740);
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24), ColumnCount = 1, RowCount = 9 };
        for (int i = 0; i < 9; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var logo = new PictureBox { Dock = DockStyle.Top, Height = 220, SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(14, 22, 30), Margin = new Padding(0, 0, 0, 16),
            AccessibleName = "Need for Valheim: Underground 2", TabStop = false };
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NFVU2.Logo"))
        using (var original = Image.FromStream(stream)) logo.Image = new Bitmap(original);
        FormClosed += (_, __) => logo.Image.Dispose();
        layout.Controls.Add(logo);
        layout.Controls.Add(new Label { Text = "Choose Valheim. Install. Drive.\nClose the game before making changes.", AutoSize = true, Margin = new Padding(0, 0, 0, 18) });
        layout.Controls.Add(UpdateSection());
        var selection = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
        selection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); selection.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selection.Controls.Add(path, 0, 0); selection.Controls.Add(browse, 1, 0); layout.Controls.Add(selection);
        layout.Controls.Add(loader);
        layout.Controls.Add(new Label { Text = "BepInEx lets Valheim load mods. NFVU2 needs it to run.\nSetup installs it automatically if needed; an existing installation is kept.\nLeave it installed if you use other mods.\nUninstall keeps your settings, saves, backups and other mods.", AutoSize = true, MaximumSize = new Size(620, 0), Margin = new Padding(0, 10, 0, 14) });
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
        install.Click += async (_, __) => { if(NewerAvailable)await DownloadUpdate();else await Execute(false); };
        uninstall.Click += async (_, __) => await Execute(true);
        FormClosing += (_, e) => {
            if(busy){e.Cancel=true;if(downloadCancellation!=null){closeAfterDownload=true;downloadCancellation.Cancel();}}
        };
        FormClosed += (_, __) => { lifetime.Cancel();updates.Dispose();lifetime.Dispose(); };
        Shown += async (_, __) => {
            if(options.Executable!=null) {
                try { SelectPath(options.Executable);if(options.Install)await Execute(false); }
                catch(Exception error) { status.Text=error.Message; }
            }
            if(!IsDisposed)await CheckUpdates();
        };
    }
    void Choose() {
        using (var dialog = new OpenFileDialog { Title = "Select valheim.exe", Filter = "Valheim|valheim.exe", CheckFileExists = true }) {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try {
                SelectPath(dialog.FileName);
            } catch (Exception e) { MessageBox.Show(this, e.Message, "Select Valheim", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
    void SelectPath(string selected) {
        var core=new InstallerCore(selected);
        path.Text=selected;install.Enabled=true;uninstall.Enabled=core.HasInstallation;
        status.Text=core.HasInstallation?"NFVU2 found. Ready to update or uninstall.":"Ready to install NFVU2.";
    }
    async Task Execute(bool removing) {
        if(busy)return;
        if (removing && MessageBox.Show(this, "Remove NFVU2 from this Valheim installation?\nYour saves and personal settings will be kept.", "Uninstall NFVU2", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        busy = true; browse.Enabled = install.Enabled = uninstall.Enabled = loader.Enabled = false;
        UpdateButtons();
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
            UpdateButtons();
        }
    }
}
}
