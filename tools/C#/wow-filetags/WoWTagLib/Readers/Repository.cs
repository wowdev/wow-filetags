using WoWTagLib.Types;

namespace WoWTagLib.Readers
{
    public class Repository
    {
        private readonly string Folder;
        private readonly bool Verify;
        private readonly bool Verbose;

        public List<Tag> Tags = [];
        public Dictionary<int, List<(string Tag, MappingSource TagSource, string TagValue)>> FileDataIDMap = [];

        public Repository(string folder, bool verify = false, bool verbose = false)
        {
            Folder = folder;
            Verify = verify;
            Verbose = verbose;

            Load();
        }

        public void Load()
        {
            Tags = [];
            FileDataIDMap = [];

            var tagsFile = Path.Combine(Folder, "meta", "tags.csv");
            if (!File.Exists(tagsFile))
                throw new FileNotFoundException("tags.csv file not found!");

            using (var reader = new StreamReader(tagsFile))
            using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
            {
                Tags = [.. csv.GetRecords<Tag>()];
            }

            foreach (var tag in Tags)
            {
                if (tag.Type == TagType.Preset)
                {
                    var presetsFile = Path.Combine(Folder, "presets", $"{tag.Key}.csv");
                    if (!File.Exists(presetsFile))
                    {
                        if (Verbose)
                            Console.WriteLine("!!! Missing preset file for tag: " + tag.Key);

                        if (Verify)
                            throw new FileNotFoundException("Missing preset file for tag: " + tag.Key);

                        continue;
                    }

                    var tagPresets = new List<TagPreset>();

                    using (var reader = new StreamReader(presetsFile))
                    using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        tagPresets = [.. csv.GetRecords<TagPreset>()];
                    }

                    Tags[Tags.FindIndex(t => t.Key == tag.Key)].Presets = tagPresets;
                }

                var mappingFile = Path.Combine(Folder, "mappings", $"{tag.Key}.csv");
                if (!File.Exists(mappingFile))
                {
                    if (Verbose)
                        Console.WriteLine("!!! Missing mapping file for tag: " + tag.Key);

                    if (Verify)
                        throw new FileNotFoundException("Missing mapping file for tag: " + tag.Key);

                    continue;
                }

                using (var reader = new StreamReader(mappingFile))
                using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
                {
                    var mappings = csv.GetRecords<TagMapping>();
                    foreach (var mapping in mappings)
                    {
                        if (!FileDataIDMap.ContainsKey(mapping.FDID))
                            FileDataIDMap[mapping.FDID] = [];

                        FileDataIDMap[mapping.FDID].Add((tag.Key, mapping.Source, mapping.Value));
                    }
                }
            }
        }

        public void Save()
        {
            // Tags
            var tagsFile = Path.Combine(Folder, "meta", "tags.csv");
            using (var writer = new StreamWriter(tagsFile))
            using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<Tag>();
                csv.NextRecord();
                csv.WriteRecords(Tags);
            }

            // Presets
            foreach (var tag in Tags)
            {
                if (tag.Type == TagType.Preset)
                {
                    var presetsFile = Path.Combine(Folder, "presets", $"{tag.Key}.csv");
                    using (var writer = new StreamWriter(presetsFile))
                    using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        csv.WriteHeader<TagPreset>();
                        csv.NextRecord();
                        csv.WriteRecords(tag.Presets);
                    }
                }
            }

            // Mappings
            foreach (var tag in Tags)
            {
                var mappingFile = Path.Combine(Folder, "mappings", $"{tag.Key}.csv");
                var mappings = new List<TagMapping>();
                foreach (var mapEntry in FileDataIDMap)
                {
                    var fdid = mapEntry.Key;
                    var entries = mapEntry.Value;

                    foreach (var entry in entries)
                    {
                        if (entry.Tag == tag.Key)
                        {
                            mappings.Add(new TagMapping
                            {
                                FDID = fdid,
                                Source = entry.TagSource,
                                Value = entry.TagValue
                            });
                        }
                    }
                }

                using (var writer = new StreamWriter(mappingFile))
                using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csv.WriteHeader<TagMapping>();
                    csv.NextRecord();
                    csv.WriteRecords(mappings);
                }
            }

            // TODO: Delete any orphaned files from tags that have since been removed or renamed
        }
    }
}
