namespace WoWTagLib.Types
{
    public record TagMapping
    {
        public required int FDID { get; init; }
        public required string Value { get; init; }
    }

    public record TagMappingSplit
    {
        public required int FDID { get; init; }
    }
}
