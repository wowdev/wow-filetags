using TACTSharp;
using WoWTagLib.AutoTagging;

namespace TagTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: TagTool <mode> <arguments>");
                Console.WriteLine("Supported modes:");
                Console.WriteLine("  rewrite <repository_path>");
                Console.WriteLine("  tag <repository_path> <fdid> <tagKey> <tagValue> <tagSource>");
                Console.WriteLine("  autotag <repository_path> <tact_product> (taggers)");
                return;
            }

            var mode = args[0];

            if (mode == "rewrite")
            {
                var repo = new WoWTagLib.DataSources.Repository(args[1], verify: true, verbose: true);
                repo.Save();
            }
            else if (mode == "tag")
            {
                var repo = new WoWTagLib.DataSources.Repository(args[1], verify: true, verbose: true);
                int fdid = int.Parse(args[2]);
                string tagKey = args[3];
                string tagValue = args[4];
                string tagSource = args[5];
                repo.AddTagToFDID(fdid, tagKey, tagSource, tagValue);
                repo.Save();
            }
            else if(mode == "autotag")
            {
                var repo = new WoWTagLib.DataSources.Repository(args[1], verify: true, verbose: true);
                var tactProduct = args[2];

                Console.WriteLine("Initializing TACTSharp instance for " + tactProduct + "...");
                var buildInstance = new BuildInstance();

                buildInstance.Settings.Product = tactProduct;

                var versions = buildInstance.cdn.GetPatchServiceFile(tactProduct).Result;
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

                buildInstance.Settings.Locale = RootInstance.LocaleFlags.enUS;
                buildInstance.Settings.Region = "eu";
                buildInstance.Settings.RootMode = RootInstance.LoadMode.Normal;
                //buildInstance.Settings.CDNDir = SettingsManager.CDNFolder;

                buildInstance.Settings.AdditionalCDNs.AddRange(["casc.wago.tools", "cdn.arctium.tools"]);

                if (!string.IsNullOrEmpty(args[4]))
                {
                    buildInstance.Settings.BaseDir = args[4];
                    buildInstance.cdn.OpenLocal();
                }

                buildInstance.LoadConfigs(buildInstance.Settings.BuildConfig, buildInstance.Settings.CDNConfig);

                if (buildInstance.BuildConfig == null || buildInstance.CDNConfig == null)
                    throw new Exception("Failed to load build configs");

                buildInstance.Load();

                if (buildInstance.Encoding == null || buildInstance.Root == null || buildInstance.Install == null || buildInstance.GroupIndex == null)
                    throw new Exception("Failed to load build components");

                var autoTagger = new AutoTagger(buildInstance, repo);
                autoTagger.RunTagger("FileType");
                repo.Save();
            }
            else
            {
                Console.WriteLine("Unknown mode. Supported modes are: rewrite, tag");
            }
        }
    }
}
