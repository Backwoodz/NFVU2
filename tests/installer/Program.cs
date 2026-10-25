using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using NFVU2.Setup;

[assembly: System.Reflection.AssemblyVersion("5.0.0.0")]
static class Tests {
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
    static int checks;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
    static void Fails(Action action, string name) { try { action(); } catch (IOException) { checks++; return; } throw new Exception("Expected failure: " + name); }
    static void Put(string root, string name, string data) { string p = Path.Combine(root, name); Directory.CreateDirectory(Path.GetDirectoryName(p)); File.WriteAllText(p, data); }
    static string Read(string root, string name) { return File.ReadAllText(Path.Combine(root, name)); }
    static MemoryStream Payload(string dll = "new", bool stale = false, bool corrupt = false, bool ogg = false, bool robot = false,
        bool ownPhysics = false, byte[] managedPlugin = null, bool driftAssets = false) {
        var stream = new MemoryStream();
        var files = new Dictionary<string, string> {
            {InstallerCore.ModRoot+"Rachel350Z.dll", dll}, {InstallerCore.ModRoot+"body.json", "model"},
            {InstallerCore.ModRoot+(ogg?"radio/song.ogg":"radio/song.wav"), "music"}, {"winhttp.dll", "loader"},
            {"BepInEx/core/BepInEx.dll", "test-loader"}, {"doorstop_config.ini", "config"}
        };
        if (ownPhysics) files.Add(InstallerCore.ModRoot+"NFVU2.DriftPhysics.dll", "own physics");
        if (driftAssets) {
            files.Add(InstallerCore.ModRoot+"audio/tyres/skid.ogg", "tyre audio");
            files.Add(InstallerCore.ModRoot+"audio/tyres/manifest.json", "{}");
            files.Add(InstallerCore.ModRoot+"venue/sponsor.png", "sponsor artwork");
            files.Add(InstallerCore.ModRoot+"venue/manifest.json", "{}");
            files.Add(InstallerCore.ModRoot+"loading/background.png", "loading artwork");
            files.Add(InstallerCore.ModRoot+"loading/manifest.json", "{}");
        }
        if (stale) files.Add(InstallerCore.ModRoot+"menu/old.json", "old");
        if (robot) {
            files.Add(InstallerCore.ModRoot+"transformer/transformer.json", "robot manifest");
            files.Add(InstallerCore.ModRoot+"transformer/transformer.bin", "robot animation");
            files.Add(InstallerCore.ModRoot+"audio/transformer/step_01.wav", "robot impact");
            files.Add(InstallerCore.ModRoot+"audio/transformer/manifest.json", "{}");
            files.Add(InstallerCore.ModRoot+"minigun/minigun.json", "minigun model");
            files.Add(InstallerCore.ModRoot+"minigun/rig.json", "minigun rig");
            files.Add(InstallerCore.ModRoot+"minigun/motor_loop.wav", "minigun motor");
        }
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
            var hashes = new Dictionary<string, string>();
            foreach (var file in files) {
                byte[] bytes = managedPlugin != null && file.Key == InstallerCore.ModRoot+"Rachel350Z.dll"
                    ? managedPlugin : Encoding.UTF8.GetBytes(file.Value);
                using (var sha = System.Security.Cryptography.SHA256.Create()) hashes[file.Key] = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                using (var output = archive.CreateEntry(file.Key).Open()) output.Write(bytes, 0, bytes.Length);
            }
            if (corrupt) hashes[InstallerCore.ModRoot+"Rachel350Z.dll"] = new string('0', 64);
            using (var writer = new StreamWriter(archive.CreateEntry("payload.json").Open())) writer.Write(Json.Serialize(hashes));
        }
        stream.Position = 0; return stream;
    }
    static string Fixture(string parent, string name) {
        string root = Path.Combine(parent, name); Directory.CreateDirectory(root);
        Put(root, "valheim.exe", "fake game"); Directory.CreateDirectory(Path.Combine(root, "valheim_Data")); return root;
    }
    static InstallerCore Core(string root, Action guard = null) { return new InstallerCore(Path.Combine(root, "valheim.exe"), guard ?? (() => {})); }
    static byte[] PluginWithPhysicsDependency(string folder) {
        var dependency = AppDomain.CurrentDomain.DefineDynamicAssembly(new System.Reflection.AssemblyName("NFVU2.DriftPhysics"),
            System.Reflection.Emit.AssemblyBuilderAccess.Save, folder);
        var module = dependency.DefineDynamicModule("physics", "NFVU2.DriftPhysics.dll");
        var physicsType = module.DefineType("OwnPhysics", System.Reflection.TypeAttributes.Public).CreateType();
        dependency.Save("NFVU2.DriftPhysics.dll");
        var plugin = AppDomain.CurrentDomain.DefineDynamicAssembly(new System.Reflection.AssemblyName("DriftDependencyFixture"),
            System.Reflection.Emit.AssemblyBuilderAccess.Save, folder);
        var pluginModule = plugin.DefineDynamicModule("plugin", "DriftDependencyFixture.dll");
        var pluginType = pluginModule.DefineType("Plugin", System.Reflection.TypeAttributes.Public);
        pluginType.DefineField("Physics", physicsType, System.Reflection.FieldAttributes.Public);
        pluginType.CreateType(); plugin.Save("DriftDependencyFixture.dll");
        return File.ReadAllBytes(Path.Combine(folder, "DriftDependencyFixture.dll"));
    }
    // A real BepInEx assembly is unnecessary: update tests exercise a legacy
    // manifest with fresh loader setup, and existing-loader preservation is
    // covered separately with a tiny version-5 managed test assembly.
    static void CompleteLoader(string root) {
        string source = System.Reflection.Assembly.GetExecutingAssembly().Location;
        File.Copy(source, Path.Combine(root, "BepInEx/core/BepInEx.dll"), true);
        Put(root, "BepInEx/core/BepInEx.Preloader.dll", "preloader"); Put(root, "BepInEx/core/0Harmony.dll", "harmony");
        Put(root, "doorstop_config.ini", "[General]\nenabled=true\ntarget_assembly=BepInEx\\core\\BepInEx.Preloader.dll\n");
        var state = Json.Deserialize<InstallState>(Read(root, InstallerCore.StateFile));
        state.Loader["BepInEx/core/BepInEx.dll"] = InstallerCore.Hash(Path.Combine(root, "BepInEx/core/BepInEx.dll"));
        state.Loader["BepInEx/core/BepInEx.Preloader.dll"] = InstallerCore.Hash(Path.Combine(root, "BepInEx/core/BepInEx.Preloader.dll"));
        state.Loader["BepInEx/core/0Harmony.dll"] = InstallerCore.Hash(Path.Combine(root, "BepInEx/core/0Harmony.dll"));
        state.Loader["doorstop_config.ini"] = InstallerCore.Hash(Path.Combine(root, "doorstop_config.ini"));
        Put(root, InstallerCore.StateFile, Json.Serialize(state));
    }
    static void Main(string[] args) {
        // Separate process entry points let the Python developer installer
        // run between two operations of the actual Windows setup engine.
        if (args.Length == 2 && args[0] == "handoff-install") {
            using (var zip = Payload(ownPhysics: true)) Core(args[1]).Install(zip);
            CompleteLoader(args[1]); return;
        }
        if (args.Length == 2 && args[0] == "handoff-uninstall-reinstall") {
            Core(args[1]).Uninstall(false);
            Check(!File.Exists(Path.Combine(args[1], InstallerCore.ModRoot+"Rachel350Z.dll")), "locally updated mod uninstalls through the wizard");
            Check(Read(args[1], "BepInEx/config/personal.cfg") == "keep settings", "handoff uninstall preserves settings");
            Check(Read(args[1], "BepInEx/plugins/personal.dll") == "keep other mod", "handoff uninstall preserves other mods");
            using (var zip = Payload(ownPhysics: true)) Core(args[1]).Install(zip);
            Check(Read(args[1], InstallerCore.ModRoot+"Rachel350Z.dll") == "new", "wizard reinstall succeeds after local handoff");
            return;
        }
        string parent = Path.Combine(Path.GetTempPath(), "NFVU2-tests-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(parent);
        try {
            string root = Fixture(parent, "fresh spaces ü");
            using (var zip = Payload(stale: true)) Core(root).Install(zip);
            Check(Read(root, InstallerCore.ModRoot+"Rachel350Z.dll") == "new", "fresh install");
            Check(Read(root, "winhttp.dll") == "loader", "loader install");
            Check(Read(root, InstallerCore.ModRoot+"radio/song.wav") == "music", "radio installed automatically");
            CompleteLoader(root);
            Put(root, "BepInEx/plugins/other/keep.dll", "other");
            Put(root, "BepInEx/config/local.rachel350z.prototype.cfg", "settings");
            Put(root, ".nfvu2/backups/Flatgrass/keep.txt", "named backup");
            for (int i = 0; i < 3; i++) using (var zip = Payload("update")) Core(root).Install(zip);
            Check(Read(root, InstallerCore.ModRoot+"Rachel350Z.dll") == "update", "update");
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"menu/old.json")), "stale removed");
            Check(Read(root, InstallerCore.ModRoot+"radio/song.wav") == "music", "radio retained by update");
            Put(root, InstallerCore.ModRoot+"radio/personal.wav", "custom song");
            using (var zip = Payload("update", ogg: true)) Core(root).Install(zip);
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"radio/song.wav")), "Ogg update removes owned WAV");
            Check(Read(root, InstallerCore.ModRoot+"radio/song.ogg") == "music", "Ogg update installs replacement music");
            Check(Read(root, InstallerCore.ModRoot+"radio/personal.wav") == "custom song", "Ogg update preserves personal music");
            Check(Directory.GetDirectories(Path.Combine(root, ".nfvu2/backups")).Length == 3, "two backups plus named");
            Fails(() => Core(root).Uninstall(true), "other mods block loader removal");
            Check(Core(root).HasInstallation, "blocked uninstall untouched");
            Core(root).Uninstall(false);
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"Rachel350Z.dll")), "uninstalled");
            Check(Read(root, "BepInEx/plugins/other/keep.dll") == "other", "other mod preserved");
            Check(Read(root, "BepInEx/config/local.rachel350z.prototype.cfg") == "settings", "settings preserved");
            Check(File.Exists(Path.Combine(root, "winhttp.dll")), "shared loader retained");

            root = Fixture(parent, "remove-all");
            using (var zip = Payload()) Core(root).Install(zip);
            Core(root).Uninstall(true);
            Check(!File.Exists(Path.Combine(root, "winhttp.dll")), "owned loader removed");

            root = Fixture(parent, "own-physics");
            byte[] dependencyPlugin = PluginWithPhysicsDependency(parent);
            using (var zip = Payload(managedPlugin: dependencyPlugin))
                Fails(() => Core(root).Install(zip), "new plugin without own physics is rejected");
            Check(!Core(root).HasInstallation, "missing dependency rejected before installation writes");
            using (var zip = Payload(ownPhysics: true, managedPlugin: dependencyPlugin)) Core(root).Install(zip);
            Check(Read(root, InstallerCore.ModRoot+"NFVU2.DriftPhysics.dll") == "own physics", "own physics installed with dependent plugin");
            CompleteLoader(root);
            using (var zip = Payload(managedPlugin: File.ReadAllBytes(System.Reflection.Assembly.GetExecutingAssembly().Location))) Core(root).Install(zip);
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"NFVU2.DriftPhysics.dll")), "legacy assembly remains supported and removes stale owned physics");
            using (var zip = Payload(ownPhysics: true, managedPlugin: dependencyPlugin)) Core(root).Install(zip);
            Core(root).Uninstall(true);
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"NFVU2.DriftPhysics.dll")), "own physics removed during uninstall");

            Check(InstallerCore.ModName(InstallerCore.ModRoot+"audio/transformer/step_01.wav"), "robot impact path allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"audio/exhaust/overrun_01.wav"), "prepared exhaust audio allowed");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"audio/exhaust/source.bank"), "original exhaust bank excluded");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"audio/exhaust/runtime.dll"), "foreign sound runtime excluded");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"audio/transformer/manifest.json"), "robot provenance manifest allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"minigun/minigun.json"), "shoulder minigun model allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"minigun/motor_loop.wav"), "shoulder minigun prepared audio allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"transformer/transformer.json"), "robot manifest allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"transformer/transformer.bin"), "robot animation binary allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"NFVU2.DriftPhysics.dll"), "own physics assembly allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"audio/tyres/skid.ogg"), "prepared tyre audio allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"audio/tyres/manifest.json"), "tyre audio manifest allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"loading/background.png"), "native loading artwork allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"venue/sponsor.png"), "first Parkade sponsor artwork allowed");
            Check(InstallerCore.ModName(InstallerCore.ModRoot+"venue/manifest.json"), "venue artwork manifest allowed");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"venue/source.bun"), "original venue archive excluded");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"loading/private.dll"), "loading artwork cannot import a plugin");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"audio/tyres/source.bank"), "original tyre audio bank excluded");
            root = Fixture(parent, "first-parkade-assets");
            using (var zip = Payload(driftAssets: true)) Core(root).Install(zip);
            Check(Read(root, InstallerCore.ModRoot+"audio/tyres/skid.ogg") == "tyre audio", "tyre audio installs through the actual engine");
            Check(Read(root, InstallerCore.ModRoot+"venue/sponsor.png") == "sponsor artwork", "first Parkade artwork installs through the actual engine");
            Check(Read(root, InstallerCore.ModRoot+"loading/background.png") == "loading artwork", "loading artwork installs through the actual engine");
            Core(root).Uninstall(true);
            Check(!Directory.Exists(Path.Combine(root, InstallerCore.ModRoot+"venue")) || !Directory.EnumerateFiles(Path.Combine(root, InstallerCore.ModRoot+"venue")).Any(), "uninstall removes managed venue artwork");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"NFVU2.OtherPhysics.dll"), "unrelated physics assembly rejected");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"transformer/private.bin"), "unrelated robot binary excluded");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"minigun/private.bin"), "binary extension remains excluded elsewhere");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"transformer/../private.dll"), "robot traversal rejected");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"minigun/../private.dll"), "minigun traversal rejected");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"minigun/source.mp3"), "minigun original source excluded");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"audio/transformer/../step.wav"), "robot path traversal rejected");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"audio/transformer/nested/step.wav"), "nested robot source folder rejected");
            Check(!InstallerCore.ModName(InstallerCore.ModRoot+"audio/transformer/source.mp3"), "original robot recording excluded");
            root = Fixture(parent, "robot-audio");
            using (var zip = Payload(robot: true)) Core(root).Install(zip);
            Check(Read(root, InstallerCore.ModRoot+"transformer/transformer.json") == "robot manifest", "robot manifest installs through normal engine");
            Check(Read(root, InstallerCore.ModRoot+"transformer/transformer.bin") == "robot animation", "robot animation installs through normal engine");
            Check(Read(root, InstallerCore.ModRoot+"audio/transformer/step_01.wav") == "robot impact", "robot impacts install through normal engine");
            Check(Read(root, InstallerCore.ModRoot+"audio/transformer/manifest.json") == "{}", "robot provenance manifest installed");
            Check(Read(root, InstallerCore.ModRoot+"minigun/minigun.json") == "minigun model", "original minigun model installs through normal engine");
            Check(Read(root, InstallerCore.ModRoot+"minigun/rig.json") == "minigun rig", "minigun moving rig installs through normal engine");
            Check(Read(root, InstallerCore.ModRoot+"minigun/motor_loop.wav") == "minigun motor", "minigun prepared audio installs through normal engine");
            Core(root).Uninstall(true);
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"audio/transformer/step_01.wav")), "owned robot impacts uninstall");
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"transformer/transformer.bin")), "owned robot animation uninstalls");
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"transformer/transformer.json")), "owned robot manifest uninstalls");
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"minigun/minigun.json")), "owned shoulder minigun assets uninstall");

            root = Fixture(parent, "corrupt");
            using (var zip = Payload(corrupt: true)) Fails(() => Core(root).Install(zip), "checksum");
            Check(!Directory.Exists(Path.Combine(root, "BepInEx")), "corruption leaves game untouched");
            using (var zip = Payload()) Fails(() => Core(root, () => { throw new IOException("running"); }).Install(zip), "running game");
            Check(!Directory.Exists(Path.Combine(root, ".nfvu2")), "guard before mutation");

            root = Fixture(parent, "rollback");
            bool triggered = false;
            Action failOnce = () => { if (!triggered && File.Exists(Path.Combine(root, InstallerCore.ModRoot+"Rachel350Z.dll"))) { triggered = true; throw new IOException("simulated write failure"); } };
            using (var zip = Payload()) Fails(() => Core(root, failOnce).Install(zip), "mid-copy failure");
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"Rachel350Z.dll")), "partial write rolled back");
            Check(!File.Exists(Path.Combine(root, ".nfvu2/pending.json")), "recovered journal cleared");

            root = Fixture(parent, "interrupted");
            bool stopped = false;
            Action stop = () => { if (stopped || File.Exists(Path.Combine(root, InstallerCore.ModRoot+"Rachel350Z.dll"))) { stopped = true; throw new IOException("game started"); } };
            using (var zip = Payload()) Fails(() => Core(root, stop).Install(zip), "interrupted rollback");
            Check(File.Exists(Path.Combine(root, ".nfvu2/pending.json")), "journal retained");
            using (var zip = Payload()) Core(root).Install(zip);
            Check(!File.Exists(Path.Combine(root, ".nfvu2/pending.json")), "next run recovers");
            Put(root, InstallerCore.ModRoot+"Rachel350Z.dll", "personal changes");
            Fails(() => Core(root).Uninstall(false), "modified file preserved");
            Check(Read(root, InstallerCore.ModRoot+"Rachel350Z.dll") == "personal changes", "changes intact");

            root = Fixture(parent, "unsafe");
            Fails(() => InstallerCore.Safe(root, "../outside"), "traversal");
            Fails(() => InstallerCore.Safe(root, "file:stream"), "ADS");
            Fails(() => InstallerCore.Safe(root, "folder./file"), "Windows alias");
            Put(root, "BepInEx/plugins/Rachel350Z/nfvu2-installed-files.json", "[\"../../outside.dll\"]");
            Fails(() => Core(root).Uninstall(false), "legacy escape");
            root = Fixture(parent, "partial-loader"); Put(root, "winhttp.dll", "foreign");
            using (var zip = Payload()) Fails(() => Core(root).Install(zip), "partial loader");
            Check(Read(root, "winhttp.dll") == "foreign", "foreign proxy preserved");

            root = Fixture(parent, "legacy");
            Put(root, InstallerCore.ModRoot+"Rachel350Z.dll", "legacy dll");
            Put(root, InstallerCore.ModRoot+"menu/old.json", "old asset");
            Put(root, InstallerCore.ModRoot+"nfvu2-installed-files.json", "[\"Rachel350Z.dll\",\"menu/old.json\"]");
            Put(root, InstallerCore.ModRoot+"personal.txt", "keep");
            using (var zip = Payload()) Core(root).Install(zip);
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"menu/old.json")), "legacy stale removal");
            Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"nfvu2-installed-files.json")), "legacy migrated");
            Check(Read(root, InstallerCore.ModRoot+"personal.txt") == "keep", "untracked file preserved");
            string snapshot = Directory.GetDirectories(Path.Combine(root, ".nfvu2/backups")).Single();
            Check(Read(snapshot, InstallerCore.ModRoot+"Rachel350Z.dll") == "legacy dll", "previous version backed up");
            CompleteLoader(root);
            bool failedUpdate = false;
            Action updateGuard = () => { if (!failedUpdate && Read(root, InstallerCore.ModRoot+"Rachel350Z.dll") == "failed update") { failedUpdate = true; throw new IOException("update failed"); } };
            using (var zip = Payload("failed update")) Fails(() => Core(root, updateGuard).Install(zip), "update failure");
            Check(Read(root, InstallerCore.ModRoot+"Rachel350Z.dll") == "new", "previous version restored");
            Put(root, "doorstop_config.ini", "enabled=false\ntarget_assembly=BepInEx\\core\\BepInEx.Preloader.dll");
            using (var zip = Payload()) Fails(() => Core(root).Install(zip), "disabled loader rejected");
            Check(Read(root, "doorstop_config.ini").StartsWith("enabled=false"), "existing loader config preserved");

            string outside = Path.Combine(parent, "outside"); Directory.CreateDirectory(outside); Put(outside, "keep.txt", "keep");
            root = Fixture(parent, "junction");
            string junction = Path.Combine(root, "BepInEx");
            using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \""+junction+"\" \""+outside+"\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true })) {
                process.StandardOutput.ReadToEnd(); process.WaitForExit();
                if (process.ExitCode != 0) throw new Exception("Could not create junction test fixture.");
            }
            try {
                using (var zip = Payload()) Fails(() => Core(root).Install(zip), "junction install rejected");
                Fails(() => InstallerCore.DeleteTree(root), "junction cleanup rejected");
                Check(Read(outside, "keep.txt") == "keep", "junction destination intact");
            } finally { Directory.Delete(junction); }

            if (args.Length == 1) {
                root = Fixture(parent, "real-payload");
                using (var zip = File.OpenRead(args[0])) Core(root).Install(zip);
                Check(File.Exists(Path.Combine(root, InstallerCore.ModRoot+"Rachel350Z.dll")), "real payload installs");
                Check(File.Exists(Path.Combine(root, InstallerCore.ModRoot+"radio/tracks.json")), "real payload music installed");
                using (var zip = File.OpenRead(args[0])) Core(root).Install(zip);
                Check(Core(root).HasInstallation, "real payload updates");
                Core(root).Uninstall(true);
                Check(!File.Exists(Path.Combine(root, InstallerCore.ModRoot+"Rachel350Z.dll")), "real payload uninstalls");
                Check(!File.Exists(Path.Combine(root, "winhttp.dll")), "real loader uninstalls");
            }
            Console.WriteLine("Windows installer: " + checks + " checks passed.");
        } finally { InstallerCore.DeleteTree(parent); }
    }
}
