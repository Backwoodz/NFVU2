using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NFVU2.Setup {
sealed partial class SetupWindow {
    readonly ReleaseUpdates updates=new ReleaseUpdates();
    readonly CancellationTokenSource lifetime=new CancellationTokenSource();
    readonly Label updateStatus=new Label { AutoSize=true,MaximumSize=new Size(620,0) };
    readonly Button checkUpdates=new Button { Text="Check for updates",AutoSize=true };
    readonly Button getUpdate=new Button { Text="Download update",AutoSize=true,Enabled=false };
    readonly Button changelog=new Button { Text="Changelog",AutoSize=true };
    readonly Button cancelUpdate=new Button { Text="Cancel download",AutoSize=true,Visible=false };
    CancellationTokenSource downloadCancellation;
    UpdateRelease available;
    bool checking,closeAfterDownload;
    Version CurrentVersion { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
    string CurrentDisplay { get { return "v"+CurrentVersion.ToString(3); } }
    bool NewerAvailable { get { return available!=null&&available.IsNewerThan(CurrentVersion); } }
    Control UpdateSection() {
        var panel=new TableLayoutPanel { Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Margin=new Padding(0,0,0,14) };
        panel.Controls.Add(new Label { Text="Included: "+CurrentDisplay+". Updates are checked when setup opens.",AutoSize=true,MaximumSize=new Size(620,0) });
        updateStatus.Text="Ready to check GitHub.";panel.Controls.Add(updateStatus);
        var buttons=new FlowLayoutPanel { Dock=DockStyle.Top,AutoSize=true,Margin=new Padding(0,5,0,0) };
        buttons.Controls.Add(checkUpdates);buttons.Controls.Add(getUpdate);buttons.Controls.Add(changelog);buttons.Controls.Add(cancelUpdate);
        panel.Controls.Add(buttons);
        checkUpdates.Click+=async (_,__)=>await CheckUpdates();
        getUpdate.Click+=async (_,__)=>await DownloadUpdate();
        cancelUpdate.Click+=(_,__)=>downloadCancellation?.Cancel();
        changelog.Click+=(_,__)=>ShowChangelog();
        return panel;
    }
    void UpdateButtons() {
        checkUpdates.Enabled=!busy&&!checking;
        getUpdate.Enabled=!busy&&NewerAvailable;
        if(!busy)install.Text=NewerAvailable?"Install latest / Update":"Install / Update";
    }
    async Task CheckUpdates() {
        if(checking||busy)return;
        checking=true;UpdateButtons();updateStatus.Text="Checking GitHub for updates…";
        try {
            var release=await updates.Latest(lifetime.Token);
            if(IsDisposed)return;
            ShowRelease(release);
        } catch(OperationCanceledException) {
            if(!IsDisposed)updateStatus.Text="Update check timed out. The included version is available offline.";
        } catch(Exception) {
            if(!IsDisposed)updateStatus.Text="GitHub could not be reached or its update is incomplete. The included version is available offline.";
        } finally { checking=false;if(!IsDisposed)UpdateButtons(); }
    }
    void ShowRelease(UpdateRelease release) {
        available=release;
        updateStatus.Text=NewerAvailable?"Update available: "+available.Display+". View the changelog or download it below.":"No newer public update found. You can install the included version.";
        getUpdate.Text=NewerAvailable?"Update to "+available.Version:"Download update";
        UpdateButtons();
    }
    void ShowChangelog() {
        string text,title;
        if(NewerAvailable) { title="Changelog — "+available.Display;text=String.IsNullOrWhiteSpace(available.Notes)?"This release has no notes.":available.Notes; }
        else {
            title="Included changelog — "+CurrentDisplay;
            using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("NFVU2.Changelog"))
            using(var reader=new StreamReader(resource))text=reader.ReadToEnd();
        }
        using(var window=new Form { Text=title,Size=new Size(740,570),MinimumSize=new Size(440,330),StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false }) {
            var layout=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=1,RowCount=2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new TextBox { Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,TabStop=false,
                Text=text.Replace("\r\n","\n").Replace("\n",Environment.NewLine) },0,0);
            var close=new Button { Text="Close",AutoSize=true,Anchor=AnchorStyles.Right,DialogResult=DialogResult.OK,Margin=new Padding(0,12,0,0) };
            layout.Controls.Add(close,0,1);window.Controls.Add(layout);window.AcceptButton=close;window.CancelButton=close;window.ShowDialog(this);
        }
    }
    async Task DownloadUpdate() {
        if(busy||!NewerAvailable)return;
        var release=available;string selected=path.Text;bool handoff=false;
        busy=true;browse.Enabled=install.Enabled=uninstall.Enabled=loader.Enabled=false;UpdateButtons();
        downloadCancellation=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        cancelUpdate.Visible=true;progress.Visible=true;progress.Style=ProgressBarStyle.Continuous;progress.Value=0;
        status.Text="Downloading "+release.Display+". The verified new installer will continue the update.";
        var messages=new Progress<UpdateProgress>(p=>{
            if(!IsDisposed&&downloadCancellation!=null){progress.Value=p.Percent;status.Text="Downloading update: "+p.Percent+"% ("+(p.Bytes/1048576)+" / "+(p.Total/1048576)+" MB)";}
        });
        try {
            string cache=Path.Combine(Path.GetTempPath(),"NFVU2-Setup",Guid.NewGuid().ToString("N"));
            string executable=await updates.Download(release,cache,messages,downloadCancellation.Token);
            downloadCancellation.Token.ThrowIfCancellationRequested();
            if(!String.IsNullOrEmpty(selected)){new InstallerCore(selected);InstallerCore.RequireClosed();}
            using(Process.Start(new ProcessStartInfo(executable,SetupArguments.ForUpdate(selected)) { UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(executable) })) {}
            handoff=true;
        } catch(OperationCanceledException) { status.Text="Update download cancelled. You can still install the included version."; }
        catch(Exception error) { status.Text=error.Message;MessageBox.Show(this,error.Message,"Update not installed",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        finally {
            downloadCancellation.Dispose();downloadCancellation=null;
            busy=false;cancelUpdate.Visible=false;progress.Visible=false;progress.Style=ProgressBarStyle.Marquee;
            browse.Enabled=loader.Enabled=true;install.Enabled=!String.IsNullOrEmpty(path.Text);
            try { uninstall.Enabled=!String.IsNullOrEmpty(path.Text)&&new InstallerCore(path.Text).HasInstallation; } catch { uninstall.Enabled=false; }
            UpdateButtons();if(handoff||closeAfterDownload)Close();
        }
    }
}
}
