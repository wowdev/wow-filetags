using CsvHelper.Configuration.Attributes;

namespace WoWTagLib.Types
{
    public record Tag
    {
        public required string Key { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required TagType Type { get; init; }
        public required TagSource Source { get; init; }
        public required string Category { get; init; }
        public required bool AllowMultiple { get; init; }
        public required TagStatus Status { get; init; }
        [Ignore]
        public List<TagPreset> Presets { get; set; } = [];
    }

    public enum TagType
    {
        Preset,
        Custom,
        PresetSplit
    }

    public enum TagSource
    {
        Auto,
        Manual
    }

    public enum TagStatus
    {
        Supported,
        WIP,
        Planned,
        Deprecated
    }
}
