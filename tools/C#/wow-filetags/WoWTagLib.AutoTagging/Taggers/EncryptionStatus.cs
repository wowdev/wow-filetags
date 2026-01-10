using TACTSharp;
using WoWTagLib.AutoTagging.Services;

namespace WoWTagLib.AutoTagging.Taggers
{
    public class EncryptionStatus
    {
        public static void Tag()
        {
            if (AutoTagger.DataSource == null)
                throw new Exception("AutoTagger DataSource is not initialized.");

            var encryptionKeyList = CASCManager.GetEncryptionKeyList();
            Console.WriteLine("[EncryptionStatus tagger] Found " + encryptionKeyList.Count + " encrypted files.");

            var tagLock = new Lock();

            foreach (var encryptedFile in encryptionKeyList)
            {
                string encryptionStatus = "";

                if (encryptedFile.Value.Count == 0)
                    encryptionStatus = "Not Encrypted (Deduped)";
                else if (encryptedFile.Value.All(value => KeyService.TryGetKey(value, out _)))
                    encryptionStatus = "Encrypted (Known)";
                else if (encryptedFile.Value.Any(value => KeyService.TryGetKey(value, out _)))
                    encryptionStatus = "Mixed";
                else
                    encryptionStatus = "Encrypted (Unknown)";

                lock (tagLock)
                    AutoTagger.DataSource.AddTagToFDID((int)encryptedFile.Key, "EncryptionStatus", encryptionStatus);

            }

        }
    }
}
