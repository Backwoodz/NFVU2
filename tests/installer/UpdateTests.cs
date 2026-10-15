using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using NFVU2.Setup;

static class UpdateTests {
    static int checks;
    static readonly byte[] Payload=Encoding.ASCII.GetBytes("MZ-test-payload");
    static string Hash(byte[] bytes) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
    static string Name(string version="0.29.1",int revision=0) { return "NFVU2-Setup-"+version+(revision==0?"":"-r"+revision)+".exe"; }
    static byte[] Checksum(string name=null) { return Encoding.UTF8.GetBytes(Hash(Payload)+"  "+(name??Name())+"\n"); }
    static object Asset(string name,string version,long size,string digest=null) {
        return new { name=name,browser_download_url=ReleaseUpdates.Repository+"/releases/download/v"+version+"/"+name,size=size,digest=digest };
    }
    static string Json(string version="0.29.1",int revision=0,bool draft=false,bool prerelease=false,object[] extra=null) {
        var assets=new List<object> {
            Asset(Name(version,revision),version,Payload.Length,"sha256:"+Hash(Payload)),
            Asset(Path.ChangeExtension(Name(version,revision),".sha256"),version,Checksum(Name(version,revision)).Length,"sha256:"+Hash(Checksum(Name(version,revision))))
        };
        if(extra!=null)assets.AddRange(extra);
        return new JavaScriptSerializer().Serialize(new { tag_name="v"+version,body="Release notes\n- Test update",draft=draft,prerelease=prerelease,assets=assets });
    }
    static void Check(bool condition,string message) { if(!condition)throw new Exception(message);checks++;Console.WriteLine("PASS "+message); }
    static void Fails(Action action,string message) { try { action(); } catch(Exception) { Check(true,message);return; }throw new Exception("Expected rejection: "+message); }
    sealed class DownloadHandler : HttpMessageHandler {
        public string Metadata=Json(),Redirect;
        public byte[] Data=Payload,Sidecar=Checksum();
        public HttpStatusCode ApiStatus=HttpStatusCode.OK;
        public Action OnRead;
        public bool Stall,StallMetadata;
        public readonly List<string> Requests=new List<string>();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) {
            token.ThrowIfCancellationRequested();string url=request.RequestUri.AbsoluteUri;Requests.Add(url);
            if(url==ReleaseUpdates.Endpoint)return Task.FromResult(new HttpResponseMessage(ApiStatus){Content=StallMetadata?(HttpContent)new StreamContent(new StalledStream()):new StringContent(Metadata,Encoding.UTF8,"application/json")});
            if(url.EndsWith(".sha256"))return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(Sidecar)});
            if(Redirect!=null) { var result=new HttpResponseMessage(HttpStatusCode.Redirect);result.Headers.Location=new Uri(Redirect);return Task.FromResult(result); }
            var response=new HttpResponseMessage(HttpStatusCode.OK){Content=Stall?(HttpContent)new StreamContent(new StalledStream()):OnRead==null?(HttpContent)new ByteArrayContent(Data):new StreamContent(new ReadStream(Data,OnRead))};
            // Simulate a truncated or tampered server body with a nominal header.
            response.Content.Headers.ContentLength=Payload.Length;
            return Task.FromResult(response);
        }
    }
    sealed class StalledStream : Stream {
        readonly TaskCompletionSource<int> pending=new TaskCompletionSource<int>();
        public override bool CanRead {get{return true;}} public override bool CanSeek {get{return false;}} public override bool CanWrite {get{return false;}}
        public override long Length {get{throw new NotSupportedException();}} public override long Position {get{throw new NotSupportedException();}set{throw new NotSupportedException();}}
        public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken token) { return pending.Task; }
        protected override void Dispose(bool disposing) { pending.TrySetException(new ObjectDisposedException("stalled network stream"));base.Dispose(disposing); }
        public override int Read(byte[] buffer,int offset,int count){throw new NotSupportedException();}
        public override void Flush(){} public override long Seek(long offset,SeekOrigin origin){throw new NotSupportedException();}
        public override void SetLength(long value){throw new NotSupportedException();} public override void Write(byte[] buffer,int offset,int count){throw new NotSupportedException();}
    }
    sealed class ReadStream : MemoryStream {
        readonly Action callback;
        public ReadStream(byte[] data,Action callback):base(data){this.callback=callback;}
        public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken token) {
            token.ThrowIfCancellationRequested();int n=Read(buffer,offset,count);callback();return Task.FromResult(n);
        }
    }
    static void Main(string[] args) { Run(args).GetAwaiter().GetResult(); }
    static async Task Run(string[] args) {
        if(args.Length==1&&args[0]=="--live") {
            using(var client=new ReleaseUpdates()) { var live=await client.Latest(CancellationToken.None);Console.WriteLine(live==null?"No public release":live.Display+" | "+live.Setup.Name); }return;
        }
        var current=new Version(0,29,0,0);var release=ReleaseUpdates.Parse(Json());
        Check(release.IsNewerThan(current),"patch update without revision is offered");
        Check(!ReleaseUpdates.Parse(Json(version:"0.28.0",revision:6)).IsNewerThan(current),"legacy public setup is recognized without downgrade");
        Check(!ReleaseUpdates.Parse(Json(version:"0.29.0")).IsNewerThan(current),"same semantic version is current");
        Check(ReleaseUpdates.Parse(Json(version:"0.30.0")).IsNewerThan(current),"minor update is offered");
        Check(ReleaseUpdates.Parse(Json(version:"1.0.0")).IsNewerThan(current),"major update is offered");
        Check(ReleaseUpdates.Parse(Json(version:"0.29.10")).IsNewerThan(new Version(0,29,9)),"patches compare numerically, not alphabetically");
        Check(!ReleaseUpdates.Parse(Json(version:"0.29.0",revision:99)).IsNewerThan(current),"legacy revision never counts as a semantic update");
        Check(release.Display=="0.29.1"&&!release.Setup.Name.Contains("-r"),"display and new filename use only Major.Minor.Patch");
        var highest=ReleaseUpdates.Parse(Json(version:"0.28.0",revision:6,extra:new[]{Asset(Name("0.28.0",12),"0.28.0",Payload.Length),Asset(Path.ChangeExtension(Name("0.28.0",12),".sha256"),"0.28.0",93)}));
        Check(highest.Setup.Name==Name("0.28.0",12),"legacy release selects highest complete old asset for compatibility");
        var incomplete=ReleaseUpdates.Parse(Json(extra:new[]{Asset(Name(revision:99),"0.29.1",Payload.Length)}));
        Check(incomplete.Setup.Name==Name(),"incomplete old-style asset cannot replace new verified pair");
        var mixed=ReleaseUpdates.Parse(Json(revision:12,extra:new[]{Asset(Name(),"0.29.1",Payload.Length),Asset(Path.ChangeExtension(Name(),".sha256"),"0.29.1",93)}));
        Check(mixed.Setup.Name==Name(),"canonical filename takes priority over legacy revisions");
        Fails(()=>ReleaseUpdates.Parse(Json().Replace("v0.29.1\"","v0.30.0\"")),"release tag and installer version must match");
        Fails(()=>ReleaseUpdates.Parse(Json().Replace("NFVU2-Setup-0.29.1.sha256","missing.sha256")),"missing checksum pair rejected");
        Check(release.Notes.Contains("Test update"),"release changelog is retained for display");
        Fails(()=>ReleaseUpdates.Parse(Json(draft:true)),"draft releases rejected");
        Fails(()=>ReleaseUpdates.Parse(Json(prerelease:true)),"prereleases rejected");
        Fails(()=>ReleaseUpdates.Parse(Json().Replace("v0.29.1\"","v0.29.1-beta\"")),"unsupported release tags rejected");
        Fails(()=>ReleaseUpdates.Parse(Json().Replace("github.com/Backwoodz","github.com/OtherOwner")),"downloads outside official repository rejected");
        Fails(()=>ReleaseUpdates.Parse(Json().Replace("https://github.com","http://github.com")),"insecure asset URLs rejected");
        Fails(()=>ReleaseUpdates.Parse(Json(extra:new[]{Asset(Name(),"0.29.1",Payload.Length)})),"ambiguous duplicate setup rejected");
        Fails(()=>ReleaseUpdates.Parse(Json().Replace("\"size\":15","\"size\":2147483649")),"oversized release download rejected");
        Check(ReleaseUpdates.Parse(Json().Replace("sha256:"+Hash(Payload),"sha256:"+Hash(Payload).ToUpperInvariant())).Setup!=null,"SHA digest comparison accepts hexadecimal case");
        Check(ReleaseUpdates.ParseChecksum(Encoding.UTF8.GetString(Checksum()),Name())==Hash(Payload),"published SHA sidecar format accepted");
        Fails(()=>ReleaseUpdates.ParseChecksum(Hash(Payload)+"  different.exe",Name()),"sidecar must identify exact setup file");
        Fails(()=>ReleaseUpdates.ParseChecksum(Hash(Payload),Name()),"unlabelled checksum rejected");
        Fails(()=>ReleaseUpdates.ParseChecksum(Encoding.UTF8.GetString(Checksum())+"extra",Name()),"ambiguous multi-line checksum rejected");
        Check(SetupArguments.Parse(new string[0]).Executable==null,"ordinary offline startup needs no arguments");
        var forwarded=SetupArguments.Parse(new[]{"--valheim",Path.Combine(Path.GetTempPath(),"Valheim with spaces","valheim.exe"),"--install"});
        Check(forwarded.Install&&SetupArguments.ForUpdate(forwarded.Executable).Contains("\" --install"),"selected game path is quoted and forwarded for update continuation");
        Fails(()=>SetupArguments.Parse(new[]{"--install"}),"update continuation cannot bypass selecting Valheim");
        Fails(()=>SetupArguments.ForUpdate("bad\"argument"),"embedded quotes cannot inject setup arguments");
        string root=Path.Combine(Path.GetTempPath(),"nfvu2-update-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try {
            var handler=new DownloadHandler();
            using(var client=new ReleaseUpdates(handler)) {
                var parsed=await client.Latest(CancellationToken.None);Check(parsed.Version==new Version(0,29,1),"public release fetched through asynchronous client");
                string path=await client.Download(parsed,Path.Combine(root,"good"),null,CancellationToken.None);
                Check(File.Exists(path)&&InstallerCore.Hash(path)==Hash(Payload),"download accepted only after exact size and SHA-256 match");
                Check(!File.Exists(path+".download"),"verified download promoted from partial file");
            }
            foreach(string failure in new[]{"corrupt","truncated","checksum-name","digest","redirect"}) {
                handler=new DownloadHandler();
                if(failure=="corrupt"){handler.Data=(byte[])Payload.Clone();handler.Data[0]^=1;}
                if(failure=="truncated")handler.Data=new byte[]{1,2};
                if(failure=="checksum-name")handler.Sidecar=Checksum("other.exe");
                if(failure=="digest")handler.Metadata=Json().Replace("sha256:"+Hash(Payload),"sha256:"+new string('0',64));
                if(failure=="redirect")handler.Redirect="http://example.invalid/setup.exe";
                string folder=Path.Combine(root,failure);
                using(var client=new ReleaseUpdates(handler)) {
                    var parsed=await client.Latest(CancellationToken.None);
                    Fails(()=>client.Download(parsed,folder,null,CancellationToken.None).GetAwaiter().GetResult(),"bad update cannot be launched: "+failure);
                    Check(!File.Exists(Path.Combine(folder,Name()))&&!File.Exists(Path.Combine(folder,Name()+".download")),"failed download leaves no executable or partial: "+failure);
                }
            }
            handler=new DownloadHandler();using(var cancel=new CancellationTokenSource())using(var client=new ReleaseUpdates(handler)) {
                handler.OnRead=()=>cancel.Cancel();string folder=Path.Combine(root,"cancel");
                Fails(()=>client.Download(release,folder,null,cancel.Token).GetAwaiter().GetResult(),"in-flight update download can be cancelled");
                Check(!File.Exists(Path.Combine(folder,Name()+".download")),"cancelled partial update cleaned up");
            }
            using(var cancel=new CancellationTokenSource())using(var client=new ReleaseUpdates(new DownloadHandler{Stall=true})) {
                cancel.CancelAfter(40);
                try { await client.Download(release,Path.Combine(root,"stalled"),null,cancel.Token);throw new Exception("stalled download did not cancel"); }
                catch(OperationCanceledException) { Check(true,"cancellation interrupts an already pending Framework network read"); }
                Check(!File.Exists(Path.Combine(root,"stalled",Name()+".download")),"stalled partial update cleaned up");
            }
            using(var cancel=new CancellationTokenSource())using(var client=new ReleaseUpdates(new DownloadHandler{StallMetadata=true})) {
                cancel.CancelAfter(40);
                try { await client.Latest(cancel.Token);throw new Exception("stalled metadata did not cancel"); }
                catch(OperationCanceledException) { Check(true,"stalled metadata body does not defeat startup cancellation"); }
            }
            using(var client=new ReleaseUpdates(new DownloadHandler{ApiStatus=HttpStatusCode.NotFound}))
                Check(await client.Latest(CancellationToken.None)==null,"repository without published release retains offline setup");
            using(var client=new ReleaseUpdates(new DownloadHandler{ApiStatus=HttpStatusCode.Forbidden}))
                Fails(()=>client.Latest(CancellationToken.None).GetAwaiter().GetResult(),"rate-limited API reports failure without touching installation");
        } finally { InstallerCore.DeleteTree(root); }
        Console.WriteLine(checks+" update checks passed; no real game or downloaded executable was run.");
    }
}
