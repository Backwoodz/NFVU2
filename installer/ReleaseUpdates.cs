using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace NFVU2.Setup {
public sealed class ReleaseAsset {
    public readonly string Name, Digest;
    public readonly Uri Url;
    public readonly long Size;
    internal ReleaseAsset(string name, Uri url, long size, string digest) { Name=name; Url=url; Size=size; Digest=digest; }
}
public sealed class UpdateRelease {
    public readonly Version Version;
    public readonly string Notes, Page;
    public readonly ReleaseAsset Setup, Checksum;
    internal UpdateRelease(Version version,string notes,string page,ReleaseAsset setup,ReleaseAsset checksum) {
        Version=version; Notes=notes; Page=page; Setup=setup; Checksum=checksum;
    }
    public string Display { get { return Version.ToString(); } }
    public bool IsZip { get { return Setup.Name.EndsWith(".zip",StringComparison.Ordinal); } }
    public string ExecutableName { get { return IsZip?Path.ChangeExtension(Setup.Name,".exe"):Setup.Name; } }
    public bool IsNewerThan(Version current) {
        return Version.CompareTo(new Version(current.Major,current.Minor,Math.Max(0,current.Build)))>0;
    }
}
public sealed class UpdateProgress {
    public readonly long Bytes,Total;
    public UpdateProgress(long bytes,long total) { Bytes=bytes; Total=total; }
    public int Percent { get { return (int)(Bytes*100/Total); } }
}
// Public GitHub metadata only. No game files, paths or credentials are sent.
public sealed class ReleaseUpdates : IDisposable {
    public const string Repository="https://github.com/Backwoodz/NFVU2";
    public const string Endpoint="https://api.github.com/repos/Backwoodz/NFVU2/releases/latest";
    const long MaxInstaller=2L*1024*1024*1024;
    readonly HttpClient http;
    public ReleaseUpdates(HttpMessageHandler handler=null) {
        // The standalone Framework executable has no application TLS config.
        // GitHub requires TLS 1.2; never inherit Framework's legacy TLS default.
        ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
        http=new HttpClient(handler??new HttpClientHandler { AllowAutoRedirect=false });
        http.Timeout=Timeout.InfiniteTimeSpan;
        http.DefaultRequestHeaders.UserAgent.ParseAdd("NFVU2-Setup");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version","2022-11-28");
    }
    sealed class ApiAsset { public string name {get;set;} public string browser_download_url {get;set;} public string digest {get;set;} public long size {get;set;} }
    sealed class ApiRelease { public string tag_name {get;set;} public string body {get;set;} public bool draft {get;set;} public bool prerelease {get;set;} public ApiAsset[] assets {get;set;} }
    // Recognize old published assets for compatibility; new builds have no revision suffix.
    static readonly Regex SetupName=new Regex(@"^NFVU2-Setup-(\d+\.\d+\.\d+)(?:-r([1-9]\d*))?\.(exe|zip)$");
    public static UpdateRelease Parse(string json) {
        var release=new JavaScriptSerializer { MaxJsonLength=2*1024*1024 }.Deserialize<ApiRelease>(json);
        if(release==null || release.draft || release.prerelease || !Regex.IsMatch(release.tag_name??"",@"^v\d+\.\d+\.\d+$"))
            throw new IOException("No supported stable release was found.");
        Version version;
        if(!Version.TryParse(release.tag_name.Substring(1),out version))throw new IOException("Invalid release version.");
        UpdateRelease selected=null;
        long selectedRank=-1;
        var assets=release.assets??new ApiAsset[0];
        if(assets.Any(a=>a==null) || assets.GroupBy(a=>a.name,StringComparer.OrdinalIgnoreCase).Any(g=>g.Count()>1))
            throw new IOException("Duplicate or invalid release assets.");
        foreach(var asset in assets) {
            var match=SetupName.Match(asset.name??""); int revision=0; Version assetVersion;
            if(!match.Success || !Version.TryParse(match.Groups[1].Value,out assetVersion) || assetVersion!=version
                || match.Groups[2].Success && !Int32.TryParse(match.Groups[2].Value,out revision))continue;
            bool zip=match.Groups[3].Value=="zip";
            if(zip && match.Groups[2].Success)continue;
            string checksumName=zip?asset.name+".sha256":Path.ChangeExtension(asset.name,".sha256");
            var checksum=assets.SingleOrDefault(a=>a.name==checksumName);
            if(checksum==null)continue;
            var setup=Asset(asset,release.tag_name,MaxInstaller);
            var hash=Asset(checksum,release.tag_name,8192);
            long rank=zip?Int64.MaxValue:match.Groups[2].Success?revision:Int64.MaxValue-1;
            if(selected==null || rank>selectedRank) {
                selectedRank=rank;
                selected=new UpdateRelease(version,(release.body??"").Substring(0,Math.Min(131072,(release.body??"").Length)),
                    Repository+"/releases/tag/"+release.tag_name,setup,hash);
            }
        }
        if(selected==null)throw new IOException("The latest release has no complete Windows installer ZIP/EXE and checksum pair.");
        return selected;
    }
    static ReleaseAsset Asset(ApiAsset asset,string tag,long maximum) {
        string expected=Repository+"/releases/download/"+tag+"/"+asset.name;
        Uri url;
        if(asset.size<=0 || asset.size>maximum || !Uri.TryCreate(asset.browser_download_url,UriKind.Absolute,out url)
            || url.AbsoluteUri!=expected)throw new IOException("Invalid release download.");
        if(!String.IsNullOrEmpty(asset.digest) && !Regex.IsMatch(asset.digest,@"^sha256:[a-fA-F0-9]{64}$"))
            throw new IOException("Invalid release digest.");
        return new ReleaseAsset(asset.name,url,asset.size,asset.digest);
    }
    static bool Allowed(Uri uri) {
        return uri.Scheme=="https" && uri.IsDefaultPort && String.IsNullOrEmpty(uri.UserInfo)
            && (uri.Host=="api.github.com" || uri.Host=="github.com" || uri.Host=="release-assets.githubusercontent.com" || uri.Host=="objects.githubusercontent.com");
    }
    async Task<HttpResponseMessage> Get(Uri url,CancellationToken token) {
        for(int i=0;i<6;i++) {
            if(!Allowed(url))throw new IOException("Unexpected update download host.");
            var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,token).ConfigureAwait(false);
            int code=(int)response.StatusCode;
            if(code==301 || code==302 || code==303 || code==307 || code==308) {
                var location=response.Headers.Location;
                response.Dispose();
                if(location==null)throw new IOException("Missing update redirect.");
                url=location.IsAbsoluteUri?location:new Uri(url,location); continue;
            }
            return response;
        }
        throw new IOException("Too many update redirects.");
    }
    static async Task<byte[]> Small(HttpResponseMessage response,int maximum,CancellationToken token) {
        response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength>maximum)throw new IOException("Update response is too large.");
        using(var input=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
        using(var abort=token.Register(()=>input.Dispose()))
        using(var output=new MemoryStream()) {
            var buffer=new byte[16384];int count;
            while((count=await input.ReadAsync(buffer,0,buffer.Length,token).ConfigureAwait(false))!=0) {
                if(output.Length+count>maximum)throw new IOException("Update response is too large.");
                output.Write(buffer,0,count);
            }
            return output.ToArray();
        }
    }
    public async Task<UpdateRelease> Latest(CancellationToken token) {
        using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token)) {
            timeout.CancelAfter(TimeSpan.FromSeconds(12));
            try {
                using(var response=await Get(new Uri(Endpoint),timeout.Token).ConfigureAwait(false)) {
                    if(response.StatusCode==HttpStatusCode.NotFound)return null;
                    var bytes=await Small(response,2*1024*1024,timeout.Token).ConfigureAwait(false);
                    return Parse(Encoding.UTF8.GetString(bytes));
                }
            } catch { timeout.Token.ThrowIfCancellationRequested();throw; }
        }
    }
    static void Digest(byte[] bytes,ReleaseAsset asset) {
        if(String.IsNullOrEmpty(asset.Digest))return;
        using(var sha=SHA256.Create())if(Hex(sha.ComputeHash(bytes))!=asset.Digest.Substring(7).ToLowerInvariant())
            throw new IOException("Release checksum file did not match GitHub's digest.");
    }
    static string Hex(byte[] bytes) { return BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant(); }
    public static string ParseChecksum(string text,string filename) {
        var lines=text.TrimStart('\ufeff').Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
        if(lines.Length!=1)throw new IOException("Invalid checksum file.");
        var match=Regex.Match(lines[0],@"^([a-fA-F0-9]{64})[ \t]+\*?(.+)$");
        if(!match.Success || match.Groups[2].Value!=filename)throw new IOException("Checksum filename does not match the installer.");
        return match.Groups[1].Value.ToLowerInvariant();
    }
    public async Task<string> Download(UpdateRelease release,string directory,IProgress<UpdateProgress> progress,CancellationToken token) {
        InstallerCore.Safe(directory,release.ExecutableName);
        Directory.CreateDirectory(directory);
        string target=InstallerCore.Safe(directory,release.ExecutableName),partial=InstallerCore.Safe(directory,release.Setup.Name+".download");
        string extracted=InstallerCore.Safe(directory,release.ExecutableName+".download");
        if(File.Exists(target) || File.Exists(partial) || File.Exists(extracted))throw new IOException("Update destination is already in use.");
        bool ownPartial=false,ownExtracted=false;
        using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token)) {
            timeout.CancelAfter(TimeSpan.FromMinutes(20));
            try {
                string expected;
                using(var response=await Get(release.Checksum.Url,timeout.Token).ConfigureAwait(false)) {
                    var bytes=await Small(response,8192,timeout.Token).ConfigureAwait(false); Digest(bytes,release.Checksum);
                    expected=ParseChecksum(Encoding.UTF8.GetString(bytes),release.Setup.Name);
                }
                if(!String.IsNullOrEmpty(release.Setup.Digest) && release.Setup.Digest.Substring(7).ToLowerInvariant()!=expected)
                    throw new IOException("Release checksum and GitHub digest disagree.");
                using(var response=await Get(release.Setup.Url,timeout.Token).ConfigureAwait(false)) {
                    response.EnsureSuccessStatusCode();
                    if(response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value!=release.Setup.Size)
                        throw new IOException("Installer download size does not match the release.");
                    using(var input=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using(var abort=timeout.Token.Register(()=>input.Dispose()))
                    using(var output=new FileStream(partial,FileMode.CreateNew,FileAccess.Write,FileShare.None,131072,true))
                    using(var sha=SHA256.Create()) {
                        ownPartial=true;
                        var buffer=new byte[131072];long total=0;int count;var clock=Stopwatch.StartNew();
                        while((count=await input.ReadAsync(buffer,0,buffer.Length,timeout.Token).ConfigureAwait(false))!=0) {
                            total+=count;
                            if(total>release.Setup.Size)throw new IOException("Installer download exceeds expected size.");
                            await output.WriteAsync(buffer,0,count,timeout.Token).ConfigureAwait(false);
                            sha.TransformBlock(buffer,0,count,buffer,0);
                            if(progress!=null && clock.ElapsedMilliseconds>=100){progress.Report(new UpdateProgress(total,release.Setup.Size));clock.Restart();}
                        }
                        sha.TransformFinalBlock(new byte[0],0,0);
                        if(total!=release.Setup.Size || Hex(sha.Hash)!=expected)throw new IOException("Installer checksum verification failed. Nothing was started.");
                        if(progress!=null)progress.Report(new UpdateProgress(total,release.Setup.Size));
                    }
                }
                timeout.Token.ThrowIfCancellationRequested();
                if(InstallerCore.Hash(partial)!=expected)throw new IOException("Saved installer checksum verification failed.");
                timeout.Token.ThrowIfCancellationRequested();
                if(release.IsZip) {
                    string extractedHash;
                    using(var output=new FileStream(extracted,FileMode.CreateNew,FileAccess.Write,FileShare.None,131072,true)) {
                        ownExtracted=true;
                        extractedHash=await ExtractSetup(partial,release.ExecutableName,output,timeout.Token).ConfigureAwait(false);
                    }
                    if(InstallerCore.Hash(extracted)!=extractedHash)throw new IOException("Saved extracted installer checksum verification failed.");
                    timeout.Token.ThrowIfCancellationRequested();
                    File.Delete(partial);ownPartial=false;
                    File.Move(extracted,target);ownExtracted=false;
                } else { File.Move(partial,target);ownPartial=false; }
                return target;
            } catch {
                if(ownPartial && File.Exists(partial))File.Delete(partial);
                if(ownExtracted && File.Exists(extracted))File.Delete(extracted);
                timeout.Token.ThrowIfCancellationRequested();throw;
            }
        }
    }
    static async Task<string> ExtractSetup(string path,string executable,Stream output,CancellationToken token) {
        token.ThrowIfCancellationRequested();
        using(var input=File.OpenRead(path))
        using(var archive=new ZipArchive(input,ZipArchiveMode.Read)) {
            string checksum=Path.ChangeExtension(executable,".sha256");
            var names=new[]{executable,checksum,"INSTALL.txt"};
            if(archive.Entries.Count!=names.Length || names.Any(name=>archive.Entries.Count(e=>e.FullName==name)!=1)
                || archive.Entries.Any(e=>((e.ExternalAttributes>>16)&0xf000)==0xa000 || (e.ExternalAttributes&0x400)!=0))
                throw new IOException("Unexpected files or linked entries in the installer ZIP.");
            var setup=archive.GetEntry(executable);var hash=archive.GetEntry(checksum);var instructions=archive.GetEntry("INSTALL.txt");
            if(setup.Length<=0 || setup.Length>MaxInstaller || hash.Length<=0 || hash.Length>8192 || instructions.Length>65536)
                throw new IOException("Invalid installer ZIP entry size.");
            string expected;
            using(var source=hash.Open())using(var bytes=new MemoryStream()) {
                var buffer=new byte[8192];int count;
                while((count=await source.ReadAsync(buffer,0,buffer.Length,token).ConfigureAwait(false))!=0) {
                    if(bytes.Length+count>8192)throw new IOException("Installer ZIP checksum is too large.");
                    bytes.Write(buffer,0,count);
                }
                if(bytes.Length!=hash.Length)throw new IOException("Truncated installer ZIP checksum.");
                expected=ParseChecksum(Encoding.UTF8.GetString(bytes.ToArray()),executable);
            }
            using(var source=setup.Open())using(var sha=SHA256.Create()) {
                var buffer=new byte[131072];long total=0;int count;
                while((count=await source.ReadAsync(buffer,0,buffer.Length,token).ConfigureAwait(false))!=0) {
                    token.ThrowIfCancellationRequested();total+=count;
                    if(total>setup.Length || total>MaxInstaller)throw new IOException("Extracted installer exceeds its expected size.");
                    await output.WriteAsync(buffer,0,count,token).ConfigureAwait(false);
                    sha.TransformBlock(buffer,0,count,buffer,0);
                }
                sha.TransformFinalBlock(new byte[0],0,0);
                if(total!=setup.Length || Hex(sha.Hash)!=expected)throw new IOException("Extracted installer checksum verification failed. Nothing was started.");
            }
            return expected;
        }
    }
    public void Dispose() { http.Dispose(); }
}
public sealed class SetupArguments {
    public string Executable {get;private set;}
    public bool Install {get;private set;}
    public static SetupArguments Parse(string[] args) {
        if(args.Length==0)return new SetupArguments();
        if((args.Length!=2 && args.Length!=3) || args[0]!="--valheim" || String.IsNullOrWhiteSpace(args[1])
            || args[1].Contains('"') || args.Length==3 && args[2]!="--install")throw new IOException("Unsupported setup arguments.");
        return new SetupArguments { Executable=Path.GetFullPath(args[1]),Install=args.Length==3 };
    }
    public static string ForUpdate(string executable) {
        if(String.IsNullOrEmpty(executable))return "";
        var options=Parse(new[]{"--valheim",executable,"--install"});
        return "--valheim \""+options.Executable+"\" --install";
    }
}
}
