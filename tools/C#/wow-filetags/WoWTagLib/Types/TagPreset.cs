namespace WoWTagLib.Types
{
    public record TagPreset
    {
        public required string Option { get; init; }
        public required string Description { get; init; }
        public required string Aliases { get; init; }
    }
}
