using WoWTagLib.Types;

namespace WoWTagLib.Interfaces
{
    public interface ITagDataSource
    {
        /// <summary>
        /// Get list of all tags.
        /// </summary>
        public List<Tag> GetTags();

        /// <summary>
        /// Get tags for a specific FileDataID.
        /// </summary>
        public List<(string Tag, MappingSource TagSource, string TagValue)> GetTagsByFileDataID(int fileDataID);

        /// <summary>
        /// Adds or updates a tag.
        /// </summary>
        public void AddOrUpdateTag(string name, string key, string description, string type, string category, bool allowMultiple);

        /// <summary>
        /// Deletes a tag.
        /// </summary>
        public void DeleteTag(string key);

        /// <summary>
        /// Adds or updates a tag option.
        /// </summary>
        public void AddOrUpdateTagOption(string tagKey, string name, string description, string aliases);

        /// <summary>
        /// Deletes a tag option.
        /// </summary>
        public void DeleteTagOption(string tagKey, string name);

        /// <summary>
        /// Maps a FileDataID to a tag & value.
        /// </summary>
        public void AddTagToFDID(int fileDataID, string tagKey, string tagSource, string tagValue);

        /// <summary>
        /// Remove tag from a FileDataID.
        /// </summary>
        public void RemoveTagFromFDID(int fileDataID, string tagKey, string tagValue);

        /// <summary>
        /// Gets whether this data source requires a separate saving step. e.g. disk-based sources will require this, in-memory or database-based ones might not.
        /// </summary>
        public bool RequiresSavingStep();

        /// <summary>
        /// Gets whether there are unsaved changes.
        /// </summary>
        public bool HasUnsavedChanges();

        /// <summary>
        /// Loads tags from the source.
        /// </summary>
        public void Load();

        /// <summary>
        /// Saves tags to the source.
        /// </summary>
        public void Save();
    }
}
