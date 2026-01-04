# wow-filetags
**CONCEPT, STILL WORK IN PROGRES**
## Description
An alternative way of classifying World of Warcraft files. Instead of filenames (see [wow-listfile](https://github.com/wowdev/wow-listfile)) this is a tag-based approach instead.
## Repository storage format
The data is stored in various split-up CSV files inside of this repo. Releases in various other formats for actual consumption by tools are available (see below), but this text-based approach for the repository itself was chosen for easy of editing/tracking purposes.

Keep in mind that CSV file structure can change in the future. There are also a few tags are experimental/exist for testing limits and such while we work on the format.

### meta/tags.csv
Available tags are specified in this file.
#### Fields
- Key: Name for tag with no spaces/special chars (used as filename elsewhere)
- Name: Name, can be same as key but with spaces/special characters
- Description: Longer description of tag
- Type: `Preset` for tags limited to preset options (see presets below) or `Custom`
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
> Mapping CSVs should remain under 100MB in size. They could realistically only be larger if there was a mapping for each file (or several per file) ***AND*** the option being very long. For a sense of scale, the [community listfile CSV](https://github.com/wowdev/wow-listfile/releases) is currently ~140MB *(which is why it is no longer in the repo, but in releases)*. If we ever approach this, tooling should be adjusted to support split up mappings. They would still be combined for releases similar to the listfile.

#### Fields
- FDID: FileDataID of file
- Source: `Auto` for automated mappings by tools or `Manual`.`Auto` mappings can be remapped by automated tools, manual mappings can not
- Value: Value of tag, if tag is a preset tag this should match `Option` exactly

## Release formats
### SQLite database
A pre-compiled SQLite database. Any IDs outside of FileDataIDs are *not* considered stable and can/will change in future releases.

#### Tables
TODO
