using TACTSharp;
using WoWTagLib.AutoTagging.Services;
using WoWTagLib.Interfaces;

namespace WoWTagLib.AutoTagging
{
    public class AutoTagger
    {
        public static ITagDataSource? DataSource { get; set; } = null;

        public AutoTagger(BuildInstance buildInstance, ITagDataSource dataSource)
        {
            CASCManager.InitializeTACT(ref buildInstance);
            DataSource = dataSource;
        }

        public void RunTagger(string tagger)
        {
            // Get class type from name
            var taggerType = Type.GetType("WoWTagLib.AutoTagging.Taggers." + tagger);
            if (taggerType == null)
                throw new Exception("Tagger type not found: " + tagger);

            // Invoke static Tag method
            var tagMethod = taggerType.GetMethod("Tag");
            if (tagMethod == null)
                throw new Exception("Tag method not found in tagger: " + tagger);

            tagMethod.Invoke(null, null);
        }

        public static List<string> ListTaggers()
        {
            var taggerNamespace = "WoWTagLib.AutoTagging.Taggers";
            var taggerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && type.Namespace == taggerNamespace && type.GetMethod("Tag") != null);

            return [.. taggerTypes.Select(type => type.Name)];
        }
    }
}
