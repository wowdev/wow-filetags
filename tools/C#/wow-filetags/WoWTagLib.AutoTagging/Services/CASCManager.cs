using TACTSharp;

namespace WoWTagLib.AutoTagging.Services
{
    public static class CASCManager
    {
        public static List<uint> AvailableFDIDs = new();
        public static string BuildName = "";
        private static BuildInstance? buildInstance;
        private static Jenkins96 hasher = new Jenkins96();

        public static void InitializeTACT(ref BuildInstance build)
        {
            buildInstance = build;

            if (buildInstance == null || buildInstance.BuildConfig == null || buildInstance.Root == null)
                throw new Exception("TACTSharp buildInstance not initialized.");

            BuildName = buildInstance.BuildConfig.Values["build-name"][0];
            AvailableFDIDs = buildInstance.Root.GetAvailableFDIDs().ToList();
        }

        public static async Task<Stream> GetFileByID(uint filedataid)
        {
            if (buildInstance == null || buildInstance.Root == null)
                throw new Exception("TACTSharp BuildInstance is not initialized.");

            return new MemoryStream(buildInstance.OpenFileByFDID(filedataid));
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
