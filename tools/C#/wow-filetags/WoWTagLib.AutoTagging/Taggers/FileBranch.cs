using WoWTagLib.AutoTagging.Services;

namespace WoWTagLib.AutoTagging.Taggers
{
    public static class FileBranch
    {
        public static void Tag()
        {
            if (AutoTagger.DataSource == null)
                throw new Exception("AutoTagger DataSource is not initialized.");

            // TODO: Get current list of products from a new TACTSharp function that returns (a filtered) summary?
            var wowProducts = new string[] { 
                // Mainline
                "wow", 
                "wowt",
                "wowxptr", 
                "wow_beta", 
                
                // Classic Prog
                "wow_classic", 
                "wow_classic_ptr", 
                "wow_classic_beta", 
                
                // Classic Era
                "wow_classic_era", 
                "wow_classic_era_ptr", 
                //"wow_classic_era_beta" 
            };

            var productToTagOption = new Dictionary<string, string>();
            foreach(var wowProduct in wowProducts)
            {
                var tagOption = AutoTagger.DataSource.GetTagOptionByAlias("FileBranchCurrent", wowProduct);
                if(tagOption == null)
                {
                    Console.WriteLine("No TagOption with alias " + wowProduct + ", skipping..");
                    continue;
                }

                productToTagOption[wowProduct] = tagOption;
            }

            // TODO: Better basedir/setting management
            var basedir = CASCManager.GetCurrentBaseDir();

            var availableIDsPerProduct = new Dictionary<string, uint[]>();
            foreach (var wowProduct in wowProducts)
            {
                try
                {
                    var buildInstance = CASCManager.NewBuildInstance(wowProduct, basedir);
                    if (buildInstance.Root == null)
                        throw new Exception("Root is null");

                    availableIDsPerProduct.Add(wowProduct, buildInstance.Root.GetAvailableFDIDs());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to initialize TACTSharp instance for product " + wowProduct + ": " + e.Message);
                }
            }

            // Clear all FileBranchCurrent tag mappings
            var currentFDIDs = AutoTagger.DataSource.GetFileDataIDsByTag("FileBranchCurrent").Select(x => x.FileDataID).ToList();
            foreach(var fdid in currentFDIDs)
                AutoTagger.DataSource.RemoveTagFromFDID(fdid, "FileBranchCurrent");

            foreach (var wowProduct in wowProducts)
            {
                var tagOption = productToTagOption[wowProduct];
                foreach (var fdid in availableIDsPerProduct[wowProduct])
                {
                    AutoTagger.DataSource.AddTagToFDID((int)fdid, "FileBranchCurrent", "Auto", tagOption);
                    AutoTagger.DataSource.AddTagToFDID((int)fdid, "FileBranchHistorical", "Auto", tagOption);
                }
            }
        }
    }
}
