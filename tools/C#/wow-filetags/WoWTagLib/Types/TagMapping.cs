namespace WoWTagLib.Types
{
    public record TagMapping
    {
        public required int FDID { get; init; }
        public required MappingSource Source { get; init; }
        public required string Value { get; init; }
    }

    public enum MappingSource
    {
        Auto,
        Manual
    }
}
