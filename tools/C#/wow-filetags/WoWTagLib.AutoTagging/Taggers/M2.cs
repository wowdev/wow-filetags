namespace WoWTagLib.AutoTagging.Taggers
{
    public class M2
    {
        public static void Tag()
        {
            if (AutoTagger.DataSource == null)
                throw new Exception("AutoTagger DataSource is not initialized.");

            var tagLock = new Lock();

            var currentFiles = AutoTagger.DataSource.GetFileDataIDsByTagAndValue("FileType", "M2 Model");
            var currentFDIDs = currentFiles.Select(f => (uint)f.FileDataID).ToList();

            var numFilesTotal = currentFDIDs.Count;
            Console.WriteLine("[M2 Tagger] Processing " + numFilesTotal + " M2 files...");
            var numFilesDone = 0;
            var numFilesSkipped = 0;
            Parallel.ForEach(currentFDIDs, m2FDID =>
            {
                try
                {

                }
                catch (Exception e)
                {
                    Console.WriteLine($"[M2 Tagger] Error processing file {m2FDID}: {e.Message}");
                }
            });
        }
    }
}
