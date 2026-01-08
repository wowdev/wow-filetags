using WoWTagLib.Interfaces;
using WoWTagLib.Types;

namespace WoWTagLib.DataSources
{
    public class Repository : ITagDataSource
    {
        private readonly string Folder;
        private readonly bool Verify;
        private readonly bool Verbose;

        private bool UnsavedChanges;

        public List<Tag> Tags = [];
        public Dictionary<int, List<(string Tag, MappingSource TagSource, string TagValue)>> FileDataIDMap = [];

        public Repository(string folder, bool verify = false, bool verbose = false)
        {
            Folder = folder;
            Verify = verify;
            Verbose = verbose;

            Load();
        }

        public bool RequiresSavingStep() => true;

        public bool HasUnsavedChanges() => UnsavedChanges;

        public List<Tag> GetTags() => Tags;

        public Dictionary<int, List<(string Tag, MappingSource TagSource, string TagValue)>> GetFileDataIDMap() => FileDataIDMap;

        public List<(string Tag, MappingSource TagSource, string TagValue)> GetTagsByFileDataID(int fileDataID)
        {
            if (FileDataIDMap.TryGetValue(fileDataID, out var tags))
                return tags;
            else
                return [];
        }

        public List<(int FileDataID, MappingSource TagSource, string TagValue)> GetFileDataIDsByTag(string tagKey)
        {
            // TODO: This likely won't scale for obvious reasons. Might need a separate lookup, but need to beware of RAM in this economy. Revisit later.
            var results = new List<(int FileDataID, MappingSource TagSource, string TagValue)>();
            foreach (var entry in FileDataIDMap)
                foreach (var tag in entry.Value)
                    if (tag.Tag.Equals(tagKey, StringComparison.OrdinalIgnoreCase))
                        results.Add((entry.Key, tag.TagSource, tag.TagValue));

            return results;
        }

        public List<(int FileDataID, MappingSource TagSource)> GetFileDataIDsByTagAndValue(string tagKey, string tagValue)
        {
            // TODO: This likely won't scale for obvious reasons. Might need a separate lookup, but need to beware of RAM in this economy. Revisit later.
            var results = new List<(int FileDataID, MappingSource TagSource)>();
            foreach (var entry in FileDataIDMap)
                foreach (var tag in entry.Value)
                    if (tag.Tag.Equals(tagKey, StringComparison.OrdinalIgnoreCase) && tag.TagValue.Equals(tagValue, StringComparison.OrdinalIgnoreCase))
                        results.Add((entry.Key, tag.TagSource));

            return results;
        }

        public void AddOrUpdateTag(string name, string key, string description, string type, string category, bool allowMultiple)
        {
            var newTag = new Tag
            {
                Key = key,
                Name = name,
                Description = description,
                Type = type == "Custom" ? TagType.Custom : TagType.Preset,
                Category = category,
                AllowMultiple = allowMultiple,
                Presets = []
            };

            var tagIndex = Tags.FindIndex(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (tagIndex != -1)
            {
                newTag.Presets = Tags[tagIndex].Presets;
                Tags[tagIndex] = newTag;
            }
            else
            {
                Tags.Add(newTag);
            }

            UnsavedChanges = true;
        }

        public void DeleteTag(string key)
        {
            var tagIndex = Tags.FindIndex(t => t.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (tagIndex == -1)
                throw new Exception($"Tag with key '{key}' does not exist in the repository.");

            Tags.RemoveAt(tagIndex);

            // Also remove tag from all FileDataID mappings
            foreach (var fdid in FileDataIDMap.Keys.ToList())
            {
                FileDataIDMap[fdid] = [.. FileDataIDMap[fdid].Where(t => !t.Tag.Equals(key, StringComparison.OrdinalIgnoreCase))];
            }

            UnsavedChanges = true;
        }

        public void AddOrUpdateTagOption(string tagKey, string name, string description, string aliases)
        {
            if (string.IsNullOrEmpty(name))
                return;

            var targetTag = Tags.FirstOrDefault(t => t.Key == tagKey);
            if (targetTag == null)
                throw new Exception($"Tag with key '{tagKey}' does not exist in the repository.");

            var newPreset = new TagPreset()
            {
                Aliases = aliases,
                Description = description,
                Option = name
            };

            var presetIndex = targetTag.Presets.FindIndex(t => t.Option.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (presetIndex != -1)
                targetTag.Presets[presetIndex] = newPreset;
            else
                targetTag.Presets.Add(newPreset);

            UnsavedChanges = true;
        }

        public void DeleteTagOption(string tagKey, string name)
        {
            var targetTag = Tags.FirstOrDefault(t => t.Key == tagKey);
            if (targetTag == null)
                throw new Exception($"Tag with key '{tagKey}' does not exist in the repository.");

            var presetIndex = targetTag.Presets.FindIndex(t => t.Option.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (presetIndex == -1)
                return;

            targetTag.Presets.RemoveAt(presetIndex);
            UnsavedChanges = true;
        }

        public void AddTagToFDID(int fileDataID, string tagKey, string tagSource, string tagValue)
        {
            if (!Enum.TryParse<MappingSource>(tagSource, out var source))
                throw new Exception($"Invalid tag source '{tagSource}'.");

            var tag = Tags.FirstOrDefault(t => t.Key.Equals(tagKey, StringComparison.OrdinalIgnoreCase));
            if (tag == null)
                throw new Exception($"Tag '{tagKey}' does not exist in the repository.");

            if (!FileDataIDMap.TryGetValue(fileDataID, out var tagsForFDID))
                FileDataIDMap[fileDataID] = tagsForFDID = [];

            if(!tag.AllowMultiple)
                tagsForFDID.RemoveAll(t => t.Tag.Equals(tagKey, StringComparison.OrdinalIgnoreCase));

            if(tag.Type == TagType.Preset)
            {
                var preset = tag.Presets.FirstOrDefault(p => p.Option.Equals(tagValue, StringComparison.OrdinalIgnoreCase));
                if (preset == null)
                    throw new Exception($"Tag '{tagKey}' does not have a preset option '{tagValue}'.");
            }

            if (tagsForFDID.Any(t => t.Tag.Equals(tagKey, StringComparison.OrdinalIgnoreCase) && t.TagSource == source && t.TagValue.Equals(tagValue, StringComparison.OrdinalIgnoreCase)))
                return;

            tagsForFDID.Add((tag.Key, source, tagValue));

            UnsavedChanges = true;
        }

        public void RemoveTagFromFDID(int fileDataID, string tagKey, string tagValue)
        {
            if (!FileDataIDMap.TryGetValue(fileDataID, out var tagsForFDID))
                return;

            FileDataIDMap[fileDataID] = [.. tagsForFDID.Where(t => !(t.Tag.Equals(tagKey, StringComparison.OrdinalIgnoreCase) && t.TagValue.Equals(tagValue, StringComparison.OrdinalIgnoreCase)))];

            UnsavedChanges = true;
        }

        public void Load()
        {
            Tags = [];
            FileDataIDMap = [];
            UnsavedChanges = false;

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

                mappings = [.. mappings.OrderBy(x => x.FDID)];

                using (var writer = new StreamWriter(mappingFile))
                using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csv.WriteHeader<TagMapping>();
                    csv.NextRecord();
                    csv.WriteRecords(mappings);
                }
            }

            UnsavedChanges = false;

            // TODO: Delete any orphaned files from tags that have since been removed or renamed
        }
    }
}
