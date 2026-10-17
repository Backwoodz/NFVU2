using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace NFVU2.Setup {
public sealed class InstallState {
    public int Schema = 1;
    public Dictionary<string, string> Mod = new Dictionary<string, string>();
    public Dictionary<string, string> Loader = new Dictionary<string, string>();
}
public sealed class Journal {
    public string Backup;
    public Dictionary<string, string> Before;
}
// No Unity/game assemblies are loaded by the installer.
public sealed class InstallerCore {
    public const string ModRoot = "BepInEx/plugins/Rachel350Z/";
    public const string StateFile = ".nfvu2/install.json";
    const string Pending = ".nfvu2/pending.json";
    const string Legacy = ModRoot + "nfvu2-installed-files.json";
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 };
    static readonly Regex Stamp = new Regex(@"^\d{8}-\d{6}-\d{6}$");
    public readonly string Root;
    readonly Action guard;
    readonly Action<string> report;
    // Injectable only for the isolated test executable, never exposed as a CLI bypass.
    public InstallerCore(string executable, Action guard = null, Action<string> report = null) {
        Root = Path.GetDirectoryName(Path.GetFullPath(executable));
        this.guard = guard ?? RequireClosed;
        this.report = report ?? (_ => {});
        if (!String.Equals(Path.GetFileName(executable), "valheim.exe", StringComparison.OrdinalIgnoreCase)
            || !File.Exists(Safe(Root, "valheim.exe")) || !Directory.Exists(Safe(Root, "valheim_Data")))
            throw new IOException("Select valheim.exe in your Valheim installation folder.");
    }
    public static void RequireClosed() {
        foreach (var process in Process.GetProcessesByName("valheim")) {
            using (process) { throw new IOException("Close Valheim before installing or uninstalling NFVU2."); }
        }
    }
    public static string Hash(string path) {
        using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }
    public static string Safe(string root, string name) {
        if (String.IsNullOrEmpty(name) || name.Contains('\\') || name.Contains(':') || name.StartsWith("/")
            || name.Split('/').Any(p => p == ".." || p == "." || p.Length == 0 || p.EndsWith(".") || p.EndsWith(" ")))
            throw new IOException("Unsafe path: " + name);
        string basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        string target = Path.GetFullPath(Path.Combine(basePath, name.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Path leaves the installation folder.");
        // Check every ancestor, including junctions above the selected game directory.
        for (string p = target; !String.IsNullOrEmpty(p); p = Path.GetDirectoryName(p)) {
            FileAttributes attributes;
            try { attributes = File.GetAttributes(p); }
            catch (FileNotFoundException) { continue; }
            catch (DirectoryNotFoundException) { continue; }
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked folders/files are not supported: " + p);
        }
        return target;
    }
    public static bool ModName(string name) {
        if (!name.StartsWith(ModRoot, StringComparison.Ordinal)) return false;
        string n = name.Substring(ModRoot.Length);
        if (n == "Rachel350Z.dll" || n == "body.json") return true;
        if (n == "transformer/transformer.json" || n == "transformer/transformer.bin") return true;
        return Regex.IsMatch(n, @"^(radio|menu|audio/turbo|audio/effects|audio/hummer|audio/transformer|hud|hummer|interior|turret|minigun)/[A-Za-z0-9_ .-]+\.(json|wav|ogg)$");
    }
    public static bool LoaderName(string n) {
        return n == "winhttp.dll" || n == "doorstop_config.ini" || n == ".doorstop_version"
            || n == "BepInEx/config/BepInEx.cfg"
            || Regex.IsMatch(n, @"^BepInEx/core/[A-Za-z0-9_.-]+\.(dll|xml)$");
    }
    static bool ManagedName(string n) { return ModName(n) || LoaderName(n) || n == StateFile || n == Legacy; }
    void ValidateMap(Dictionary<string, string> map, Func<string, bool> allowed, bool nullable = false) {
        if (map == null) throw new IOException("Invalid installation manifest.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in map) {
            Safe(Root, pair.Key);
            if (!allowed(pair.Key) || !seen.Add(pair.Key) || !(nullable && pair.Value == null)
                && (pair.Value == null || !Regex.IsMatch(pair.Value, "^[a-f0-9]{64}$")))
                throw new IOException("Invalid installation manifest entry: " + pair.Key);
        }
    }
    InstallState ReadState() {
        string path = Safe(Root, StateFile);
        if (File.Exists(path)) {
            var state = Json.Deserialize<InstallState>(File.ReadAllText(path));
            if (state == null || state.Schema != 1) throw new IOException("Unsupported installation manifest.");
            ValidateMap(state.Mod, ModName); ValidateMap(state.Loader, LoaderName);
            return state;
        }
        var legacy = new InstallState();
        path = Safe(Root, Legacy);
        if (File.Exists(path)) {
            var names = Json.Deserialize<string[]>(File.ReadAllText(path));
            if (names == null) throw new IOException("Invalid legacy manifest.");
            foreach (string n in names) {
                string full = ModRoot + n;
                if (!ModName(full)) throw new IOException("Invalid legacy manifest entry.");
                string installed = Safe(Root, full);
                if (File.Exists(installed)) legacy.Mod.Add(full, Hash(installed));
            }
        }
        return legacy;
    }
    public bool HasInstallation { get { return File.Exists(Safe(Root, StateFile)) || File.Exists(Safe(Root, Legacy)); } }
    static void CopyVerified(string source, string target, string expected) {
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        File.Copy(source, target, true);
        if (Hash(target) != expected) throw new IOException("Checksum verification failed: " + target);
    }
    bool LoaderPresent() {
        string dll = Safe(Root, "BepInEx/core/BepInEx.dll");
        bool complete = File.Exists(dll) && File.Exists(Safe(Root, "BepInEx/core/BepInEx.Preloader.dll"))
            && File.Exists(Safe(Root, "BepInEx/core/0Harmony.dll")) && File.Exists(Safe(Root, "winhttp.dll"))
            && File.Exists(Safe(Root, "doorstop_config.ini"));
        if (complete) {
            if (AssemblyName.GetAssemblyName(dll).Version.Major != 5)
                throw new IOException("This mod needs BepInEx 5 for Valheim. Your existing loader was left unchanged.");
            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(Safe(Root, "doorstop_config.ini"))) {
                string value = line.Trim(); int equals = value.IndexOf('=');
                if (equals <= 0 || value.StartsWith("#") || value.StartsWith(";")) continue;
                options[value.Substring(0, equals).Trim()] = value.Substring(equals + 1).Trim();
            }
            string enabled, target;
            bool hasTarget = options.TryGetValue("target_assembly", out target) || options.TryGetValue("targetAssembly", out target);
            if (!options.TryGetValue("enabled", out enabled) || !String.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase)
                || !hasTarget || !String.Equals(target.Replace('\\', '/'), "BepInEx/core/BepInEx.Preloader.dll", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Your existing Doorstop configuration does not enable the local BepInEx loader. Enable/repair it first; its settings were left unchanged.");
            return true;
        }
        if (FilesWithoutLinks(Safe(Root, "BepInEx/core")).Any() || File.Exists(Safe(Root, "winhttp.dll"))
            || File.Exists(Safe(Root, "doorstop_config.ini")) || File.Exists(Safe(Root, ".doorstop_version")))
            throw new IOException("An incomplete mod loader was found. Repair your BepInEx installation first; no files were replaced.");
        return false;
    }
    public void Install(Stream payload) {
        RunLocked(() => {
            var previous = ReadState();
            bool existingLoader = LoaderPresent();
            string stage = Path.Combine(Path.GetTempPath(), "NFVU2-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);
            try {
                var desired = new InstallState { Loader = previous.Loader };
                var writes = new Dictionary<string, string>();
                using (var archive = new ZipArchive(payload, ZipArchiveMode.Read, true)) {
                    var manifest = archive.GetEntry("payload.json");
                    if (manifest == null) throw new IOException("Installer payload manifest is missing.");
                    Dictionary<string, string> hashes;
                    using (var reader = new StreamReader(manifest.Open())) hashes = Json.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
                    ValidateMap(hashes, n => ModName(n) || LoaderName(n));
                    if (!hashes.ContainsKey(ModRoot + "Rachel350Z.dll") || !hashes.ContainsKey(ModRoot + "body.json")
                        || !hashes.ContainsKey("winhttp.dll") || !hashes.ContainsKey("BepInEx/core/BepInEx.dll"))
                        throw new IOException("Installer payload is incomplete.");
                    if (archive.Entries.Count != hashes.Count + 1 || archive.Entries.Select(e => e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != archive.Entries.Count)
                        throw new IOException("Unexpected or duplicate files in installer payload.");
                    foreach (var entry in hashes) {
                        bool mod = ModName(entry.Key);
                        if (!mod && existingLoader) continue;
                        if (entry.Key == "BepInEx/config/BepInEx.cfg" && File.Exists(Safe(Root, entry.Key))) continue;
                        guard(); report("Preparing " + Path.GetFileName(entry.Key));
                        var zipped = archive.GetEntry(entry.Key);
                        if (zipped == null) throw new IOException("Missing payload: " + entry.Key);
                        string target = Safe(stage, entry.Key);
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        using (var source = zipped.Open()) using (var output = File.Create(target)) source.CopyTo(output);
                        if (Hash(target) != entry.Value) throw new IOException("Damaged installer payload: " + entry.Key);
                        writes.Add(entry.Key, target);
                        (mod ? desired.Mod : desired.Loader)[entry.Key] = entry.Value;
                    }
                }
                string state = Safe(stage, StateFile);
                Directory.CreateDirectory(Path.GetDirectoryName(state));
                File.WriteAllText(state, Json.Serialize(desired));
                writes.Add(StateFile, state);
                var deletes = previous.Mod.Keys.Except(desired.Mod.Keys).ToList();
                if (File.Exists(Safe(Root, Legacy))) deletes.Add(Legacy);
                Transact(writes, deletes);
            } finally { DeleteTree(stage); }
        });
    }
    public void Uninstall(bool removeLoader) {
        RunLocked(() => {
            var state = ReadState();
            if (!HasInstallation) throw new IOException("No tracked NFVU2 installation found. Install once with this wizard before uninstalling a manually copied version.");
            var deletes = new List<string>(state.Mod.Keys);
            foreach (var item in state.Mod) {
                string path = Safe(Root, item.Key);
                if (File.Exists(path) && Hash(path) != item.Value)
                    throw new IOException("A mod file has changed since installation. Save your changes elsewhere, then repair with Install / Update first: " + item.Key);
            }
            if (removeLoader && state.Loader.Count > 0) {
                foreach (string directory in new[] { "BepInEx/plugins", "BepInEx/patchers" }) {
                    string path = Safe(Root, directory);
                    foreach (string file in FilesWithoutLinks(path)) {
                        string relative = file.Substring(Root.Length + 1).Replace('\\', '/');
                        if (!state.Mod.ContainsKey(relative) && relative != Legacy)
                            throw new IOException("Other files/mods use BepInEx. Uncheck 'Remove bundled BepInEx' to keep them working.");
                    }
                }
                foreach (var item in state.Loader) {
                    string path = Safe(Root, item.Key);
                    if (!File.Exists(path)) continue;
                    if (Hash(path) != item.Value) {
                        // BepInEx rewrites its config; retain that user's settings.
                        if (item.Key == "BepInEx/config/BepInEx.cfg") continue;
                        throw new IOException("BepInEx has changed since installation. Uncheck 'Remove bundled BepInEx' to preserve it.");
                    }
                    deletes.Add(item.Key);
                }
            }
            deletes.Add(StateFile); deletes.Add(Legacy);
            Transact(new Dictionary<string, string>(), deletes);
        });
    }
    void RunLocked(Action action) {
        guard();
        string mutex = "Local\\NFVU2-" + BitConverter.ToString(SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(Root.ToUpperInvariant()))).Replace("-", "");
        using (var gate = new System.Threading.Mutex(false, mutex)) {
            bool held;
            try { held = gate.WaitOne(0); } catch (System.Threading.AbandonedMutexException) { held = true; }
            if (!held) throw new IOException("Another NFVU2 installer is already working on this folder.");
            try { Recover(); action(); } finally { gate.ReleaseMutex(); }
        }
    }
    void Transact(Dictionary<string, string> writes, IEnumerable<string> deletes) {
        guard();
        var touched = writes.Keys.Concat(deletes).Distinct().ToList();
        var before = new Dictionary<string, string>();
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffffff");
        string backup = Safe(Root, ".nfvu2/backups/" + stamp);
        if (Directory.Exists(backup)) throw new IOException("Backup already exists; retry.");
        foreach (string n in touched) {
            if (!ManagedName(n)) throw new IOException("Unmanaged transaction path.");
            string path = Safe(Root, n);
            if (Directory.Exists(path)) throw new IOException("A folder occupies an installation file: " + n);
            before.Add(n, File.Exists(path) ? Hash(path) : null);
        }
        Directory.CreateDirectory(backup);
        foreach (var entry in before) if (entry.Value != null) {
            guard(); CopyVerified(Safe(Root, entry.Key), Safe(backup, entry.Key), entry.Value);
        }
        // Save recovery information before changing even the first installed file.
        var journal = new Journal { Backup = stamp, Before = before };
        string pending = Safe(Root, Pending);
        File.WriteAllText(Safe(backup, "snapshot.json"), Json.Serialize(journal));
        string temporary = Safe(Root, ".nfvu2/pending.tmp");
        File.WriteAllText(temporary, Json.Serialize(journal));
        File.Move(temporary, pending);
        try {
            foreach (var file in writes) {
                guard(); report("Installing " + Path.GetFileName(file.Key));
                CopyVerified(file.Value, Safe(Root, file.Key), Hash(file.Value));
            }
            foreach (string name in deletes) { guard(); string path = Safe(Root, name); if (File.Exists(path)) File.Delete(path); }
            guard();
            foreach (var file in writes)
                if (Hash(Safe(Root, file.Key)) != Hash(file.Value)) throw new IOException("Final checksum mismatch.");
            foreach (string name in deletes)
                if (File.Exists(Safe(Root, name))) throw new IOException("Could not remove " + name);
            File.Delete(pending);
        } catch (Exception error) {
            try { Recover(); }
            catch (Exception recovery) { throw new IOException("Installation interrupted. Close Valheim and run this wizard again to restore the backup. " + error.Message + " Recovery: " + recovery.Message, error); }
            throw new IOException("Operation failed; previous files were restored. " + error.Message, error);
        }
        // Cleanup failure must never turn a verified installation into a rollback.
        try { PruneBackups(); }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { report("Operation completed; backup cleanup skipped: " + e.Message); }
    }
    void Recover() {
        string pending = Safe(Root, Pending);
        if (!File.Exists(pending)) return;
        report("Restoring interrupted installation..."); guard();
        var journal = Json.Deserialize<Journal>(File.ReadAllText(pending));
        if (journal == null || journal.Backup == null || !Stamp.IsMatch(journal.Backup)) throw new IOException("Invalid recovery journal.");
        ValidateMap(journal.Before, ManagedName, true);
        string backup = Safe(Root, ".nfvu2/backups/" + journal.Backup);
        // Validate the entire backup before touching the live installation.
        foreach (var entry in journal.Before)
            if (entry.Value != null && (!File.Exists(Safe(backup, entry.Key)) || Hash(Safe(backup, entry.Key)) != entry.Value))
                throw new IOException("Recovery backup is damaged: " + entry.Key);
        foreach (var entry in journal.Before) {
            guard(); string target = Safe(Root, entry.Key);
            if (entry.Value == null) { if (File.Exists(target)) File.Delete(target); }
            else CopyVerified(Safe(backup, entry.Key), target, entry.Value);
        }
        File.Delete(pending);
    }
    public static IEnumerable<string> FilesWithoutLinks(string directory) {
        if (!Directory.Exists(directory)) yield break;
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException("Refusing linked directory.");
        foreach (string path in Directory.GetFileSystemEntries(directory)) {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Refusing linked contents.");
            if (Directory.Exists(path)) { foreach (string file in FilesWithoutLinks(path)) yield return file; }
            else yield return path;
        }
    }
    public static void DeleteTree(string directory) {
        // Fully inspect before recursive deletion; never traverse reparse points.
        if (!Directory.Exists(directory)) return;
        FilesWithoutLinks(directory).ToList();
        Directory.Delete(directory, true);
    }
    void PruneBackups() {
        string folder = Safe(Root, ".nfvu2/backups");
        var stale = Directory.GetDirectories(folder).Where(p => Stamp.IsMatch(Path.GetFileName(p)))
            .OrderByDescending(p => p, StringComparer.Ordinal).Skip(2).ToList();
        foreach (string p in stale) { Safe(folder, Path.GetFileName(p)); FilesWithoutLinks(p).ToList(); }
        foreach (string p in stale) DeleteTree(p);
    }
}
}
