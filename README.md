# wow-filetags
**CONCEPT, STILL WORK IN PROGRESS**
## Description
An alternative way of classifying World of Warcraft files. Instead of filenames (see [wow-listfile](https://github.com/wowdev/wow-listfile)) this is a tag-based approach instead. Targeting the latest mainline/Retail WoW version, although some tags may have information about other branches too (e.g. FileBranch* tags).

## Repository storage format
The data is stored in various split-up CSV files inside of this repo. Releases in various other formats for actual consumption by tools are available (see below), but this text-based approach for the repository itself was chosen for easy of editing/tracking purposes.

Keep in mind that CSV file structure can change in the future. There are also a few tags are experimental/exist for testing limits and such while we work on the format.

### meta/tags.csv
Available tags are specified in this file.
#### Fields
- Key: Name for tag with no spaces/special chars (used as filename elsewhere)
- Name: Name, can be same as key but with spaces/special characters
- Description: Longer description of tag
- Type: 
    - `Preset` for tags limited to preset options (see presets below)
    - `PresetSplit` for preset options with split mappings
    - `Custom` for tags with no preset options
- Source:
    - `Auto` for tags that are automatically updated. Existing values can/will be overwritten by automated tooling.
    - `Manual` for tags that have manual values. Existing values can not be overwritten by automated tooling.
- Category: Tag category, e.g. "Technical", "Historical", "Classification" or "Location"
- AllowMultiple: Boolean, whether multiple tag values for the same file are allowed

### presets/(tag key).csv
Preset options for tags with the "Preset" type. 
#### Fields
- Option: Name of option, should be somewhat short
- Description: Longer description of option
- Aliases: Comma-separated list of aliases for the option

### mappings/(tag key).csv
Mappings between FileDataIDs and tag values. 

> [!NOTE]  
> These CSVs should remain under 100MB in size. If files are starting to get to that size for tags with preset options, consider switching to `PresetSplit` instead (see below). For custom options, probably have shorter values for now, custom tags with split files may be supported in the future.

#### Fields
- FDID: FileDataID of file
- Value: Value of tag, if tag is a preset tag this should match `Option` exactly

### mappings/(tag key)/(preset option).csv
Mappings between FileDataIDs and tag values split up by preset option. This is for `PresetSplit` tags with particularly many mappings. Value is implied by filename.

#### Fields
- FDID: FileDataID of file

## Release formats
### SQLite database
A pre-compiled SQLite database. Any IDs outside of FileDataIDs are *not* considered stable and can/will change in future releases.

#### Tables
TODO
