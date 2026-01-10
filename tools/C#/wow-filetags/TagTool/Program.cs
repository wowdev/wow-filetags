using WoWTagLib.AutoTagging;
using WoWTagLib.AutoTagging.Services;

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
            else if (mode == "autotag")
            {
                var repo = new WoWTagLib.DataSources.Repository(args[1], verify: true, verbose: true);
                var tactProduct = args[2];

                var basedir = args.Length > 4 ? args[4] : "";
                var buildInstance = CASCManager.NewBuildInstance(tactProduct, basedir);

                var autoTagger = new AutoTagger(buildInstance, repo);
                if (args.Length > 3 && !string.IsNullOrEmpty(args[3]) && args[3] == "all")
                {
                    var availableTaggers = AutoTagger.ListTaggers();
                    foreach (var tagger in availableTaggers)
                    {
                        Console.WriteLine("Running autotagger: " + tagger);
                        autoTagger.RunTagger(tagger);
                    }
                }

                repo.Save();
            }
            else
            {
                Console.WriteLine("Unknown mode. Supported modes are: rewrite, tag");
            }
        }
    }
}
