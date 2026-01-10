using System.Text.RegularExpressions;
using TACTSharp;

namespace WoWTagLib.AutoTagging.Services
{
    public static partial class CASCManager
    {
        public static List<uint> AvailableFDIDs = new();
        public static string BuildName = "";
        private static BuildInstance? buildInstance;
        private static Jenkins96 hasher = new Jenkins96();
        private static Dictionary<uint, string> Listfile;

        [GeneratedRegex(@"(?<=e:\{)([0-9a-fA-F]{16})(?=,)", RegexOptions.Compiled)]
        private static partial Regex eKeyRegex();

        public static BuildInstance NewBuildInstance(string product, string basedir = "", string buildConfigOverride = "", string cdnConfigOverride = "")
        {
            Console.WriteLine("Initializing TACTSharp instance for " + product + "...");
            var buildInstance = new BuildInstance();

            buildInstance.Settings.Product = product;

            if(!string.IsNullOrEmpty(buildConfigOverride))
                buildInstance.Settings.BuildConfig = buildConfigOverride;

            if(!string.IsNullOrEmpty(cdnConfigOverride))
                buildInstance.Settings.CDNConfig = cdnConfigOverride;

            if (string.IsNullOrEmpty(buildInstance.Settings.BuildConfig) || string.IsNullOrEmpty(buildInstance.Settings.CDNConfig))
            {
                var versions = buildInstance.cdn.GetPatchServiceFile(product).Result;
                foreach (var line in versions.Split('\n'))
                {
                    if (!line.StartsWith(buildInstance.Settings.Region + "|"))
                        continue;

                    var splitLine = line.Split('|');

                    if (buildInstance.Settings.BuildConfig == null)
                        buildInstance.Settings.BuildConfig = splitLine[1];

                    if (buildInstance.Settings.CDNConfig == null)
                        buildInstance.Settings.CDNConfig = splitLine[2];

                    if (splitLine.Length >= 7 && !string.IsNullOrEmpty(splitLine[6]))
                        buildInstance.Settings.ProductConfig = splitLine[6];
                }
            }

            buildInstance.Settings.Locale = RootInstance.LocaleFlags.enUS;
            buildInstance.Settings.Region = "eu";
            buildInstance.Settings.RootMode = RootInstance.LoadMode.Normal;

            buildInstance.Settings.AdditionalCDNs.AddRange(["casc.wago.tools", "cdn.arctium.tools"]);

            if (!string.IsNullOrEmpty(basedir))
            {
                buildInstance.Settings.BaseDir = basedir;
                buildInstance.cdn.OpenLocal();
            }

            if(buildInstance.Settings.BuildConfig == null || buildInstance.Settings.CDNConfig == null)
                throw new Exception("BuildConfig or CDNConfig is null");

            buildInstance.LoadConfigs(buildInstance.Settings.BuildConfig, buildInstance.Settings.CDNConfig);

            if (buildInstance.BuildConfig == null || buildInstance.CDNConfig == null)
                throw new Exception("Failed to load build configs");

            buildInstance.Load();

            if (buildInstance.Encoding == null || buildInstance.Root == null || buildInstance.Install == null || buildInstance.GroupIndex == null)
                throw new Exception("Failed to load build components");

            return buildInstance;
        }

        public static void InitializeTACT(ref BuildInstance build)
        {
            buildInstance = build;

            if (buildInstance == null || buildInstance.BuildConfig == null || buildInstance.Root == null)
                throw new Exception("TACTSharp buildInstance not initialized.");

            BuildName = buildInstance.BuildConfig.Values["build-name"][0];
            AvailableFDIDs = buildInstance.Root.GetAvailableFDIDs().ToList();

            if (File.Exists("listfile.csv"))
            {
                Listfile = File.ReadAllLines("listfile.csv")
                    .Select(line => line.Split(';', 2))
                    .Where(parts => parts.Length == 2)
                    .ToDictionary(parts => uint.Parse(parts[0]), parts => parts[1]);
            }
            else
            {
                Listfile = [];
            }
        }

        public static Dictionary<uint, List<ulong>> GetEncryptionKeyList()
        {
            if (buildInstance == null || buildInstance.Encoding == null || buildInstance.Root == null)
                throw new Exception("TACTSharp buildInstance not initialized.");

            var statusList = new Dictionary<uint, List<ulong>>();
            var statusListLock = new Lock();

            Parallel.ForEach(buildInstance.Root.GetAvailableFDIDs(), fdid =>
            {
                var entries = buildInstance.Root.GetEntriesByFDID(fdid);
                if (entries.Count == 0)
                    return;

                lock (statusListLock)
                {
                    if (statusList.ContainsKey(fdid))
                        return;

                    if ((entries[0].contentFlags & RootInstance.ContentFlags.Encrypted) != 0)
                        statusList.TryAdd(fdid, new List<ulong>());
                }
                var cKey = entries[0].md5.AsSpan();
                var eKeys = buildInstance.Encoding.FindContentKey(cKey);
                if (eKeys != false)
                {
                    var eSpec = buildInstance.Encoding.GetESpec(eKeys[0]);
                    var matches = eKeyRegex().Matches(eSpec.eSpec);

                    if (matches.Count > 0)
                    {
                        var keys = matches.Cast<Match>().Select(m => BitConverter.ToUInt64(Convert.FromHexString(m.Value), 0)).ToList();
                        if (keys.Count > 0)
                        {
                            lock (statusListLock)
                            {
                                if (statusList.TryGetValue(fdid, out List<ulong>? encryptedIDs))
                                    encryptedIDs.AddRange(keys);
                                else
                                    statusList[fdid] = [.. keys];
                            }
                        }
                    }
                }
            });

            return statusList;
        }

        public static string GetCurrentBaseDir()
        {
            if (buildInstance == null)
                throw new Exception("TACTSharp BuildInstance is not initialized.");

            return buildInstance.Settings.BaseDir == null ? "" : buildInstance.Settings.BaseDir;
        }

        public static async Task<Stream> GetFileByID(uint filedataid)
        {
            if (buildInstance == null)
                throw new Exception("TACTSharp BuildInstance is not initialized.");

            return new MemoryStream(buildInstance.OpenFileByFDID(filedataid));
        }

        public static string GetFilenameByID(uint filedataid)
        {
            if (Listfile.TryGetValue(filedataid, out var filename))
                return filename;

            return "";
        }

        public static bool FileExists(uint fileDataID)
        {
            return AvailableFDIDs.Contains(fileDataID);
        }

        public static async Task<Stream> GetFileByName(string name)
        {
            if (buildInstance == null || buildInstance.Root == null)
                throw new Exception("TACTSharp BuildInstance is not initialized.");

            return new MemoryStream(buildInstance.OpenFileByFDID(buildInstance.Root.GetEntriesByLookup(hasher.ComputeHash(name))[0].fileDataID));
        }

        public static async Task<int> GetFileDataIDByName(string name)
        {
            if (buildInstance == null || buildInstance.Root == null)
                throw new Exception("TACTSharp BuildInstance is not initialized.");

            var entries = buildInstance.Root.GetEntriesByLookup(hasher.ComputeHash(name));
            if (entries.Count == 0)
                return 0;
            return (int)entries[0].fileDataID;
        }

        public static async Task<ulong> GetHashByFileDataID(int filedataid)
        {
            if (buildInstance == null || buildInstance.Root == null)
                throw new Exception("TACTSharp BuildInstance is not initialized.");

            var entries = buildInstance.Root.GetEntriesByFDID((uint)filedataid);
            if (entries.Count == 0)
                return 0;
            return entries[0].lookup;
        }
    }
}
