using System.Text;
using WoWTagLib.AutoTagging.Services;

namespace WoWTagLib.AutoTagging.Taggers
{
    public class FileType
    {
        public static void Tag()
        {
            if (AutoTagger.DataSource == null)
                throw new Exception("AutoTagger DataSource is not initialized.");

            var tagLock = new Lock();

            var availableFiles = CASCManager.AvailableFDIDs;
            var currentFiles = AutoTagger.DataSource.GetFileDataIDsByTag("FileType");
            var currentFDIDs = currentFiles.Select(f => (uint)f.FileDataID).ToList();

            var filesTODO = availableFiles.Except(currentFDIDs).ToList();

            Console.WriteLine("[FileType Tagger] Tagging " + filesTODO.Count + " files...");

            // TODO: Crawl basic DB2s...
            var numFilesTotal = filesTODO.Count;
            var numFilesDone = 0;
            var numFilesSkipped = 0;
            Parallel.ForEach(filesTODO, unknownFile =>
            {
                try
                {
                    //if (CASC.EncryptionStatuses.TryGetValue(unknownFile, out CASC.EncryptionStatus value) && value == CASC.EncryptionStatus.EncryptedUnknownKey)
                    //{
                    //    numFilesSkipped++;
                    //    numFilesDone++;
                    //    return;
                    //}

                    var file = CASCManager.GetFileByID((uint)unknownFile).Result;
                    if (file == null)
                    {
                        numFilesSkipped++;
                        numFilesDone++;
                        return;
                    }

                    using (var bin = new BinaryReader(file))
                    {
                        if (bin.BaseStream.Length < 4)
                            return;

                        var magic = bin.ReadBytes(4);
                        var type = "unk";
                        if (magic[0] == 0 || magic[0] == 4)
                        {
                            if (bin.BaseStream.Length >= 8)
                            {
                                var wwfMagic = bin.ReadUInt32();
                                switch (wwfMagic)
                                {
                                    case 0x932C64B4: // WWFParticulateGroup
                                        type = "wwf";
                                        break;
                                }
                            }

                            bin.BaseStream.Position = 4;
                        }

                        var magicString = Encoding.ASCII.GetString(magic);
                        switch (magicString)
                        {
                            case "MD21":
                            case "MD20":
                                type = "m2";
                                break;
                            case "SKIN":
                                type = "skin";
                                break;
                            case "OggS":
                                type = "ogg";
                                break;
                            case "BLP2":
                                type = "blp";
                                break;
                            case "REVM":
                                bin.ReadBytes(8); // length + ver
                                var secondChunk = bin.ReadBytes(4);
                                var subChunk = Encoding.ASCII.GetString(secondChunk);
                                switch (subChunk)
                                {
                                    case "RDHM": // ADT root
                                        var newLength = bin.ReadInt32();
                                        bin.ReadBytes(newLength);
                                        var thirdChunk = bin.ReadBytes(4);
                                        var subSubChunk = Encoding.ASCII.GetString(thirdChunk);
                                        if (subSubChunk == "EFMM")
                                        {
                                            type = "wdt";
                                        }
                                        else
                                        {
                                            type = "adt";
                                        }
                                        break;
                                    case "FDDM": // ADT OBJ
                                    case "DDLM": // ADT OBJ
                                    case "DFLM": // ADT OBJ
                                    case "XDMM": // ADT OBJ (old)
                                        type = "objadt";
                                        break;
                                    case "DHLM": // ADT LOD
                                        type = "lodadt";
                                        break;
                                    case "PMAM": // ADT TEX
                                        type = "texadt";
                                        break;
                                    case "DHOM": // WMO root
                                        type = "wmo";
                                        break;
                                    case "PGOM": // WMO GROUP
                                        type = "gwmo";
                                        break;
                                    case "DHPM": // WDT root
                                    case "IOAM": // WDT OCC/LGT
                                    case "3LPM":
                                    case "IMVP": // WDT MPV
                                    case "GOFV": // WDT FOGS
                                        type = "wdt";
                                        break;
                                    default:
                                        Console.WriteLine("Unknown sub chunk " + subChunk + " for file " + (uint)unknownFile);
                                        type = "chUNK";
                                        break;
                                }
                                break;
                            case "RVXT":
                                type = "tex";
                                break;
                            case "AFM2":
                            case "AFSA":
                            case "AFSB":
                                type = "anim";
                                break;
                            case "WDC5":
                            case "WDC4":
                            case "WDC3":
                                type = "db2";
                                break;
                            case "RIFF":
                                type = "avi";
                                break;
                            case "HSXG":
                                type = "bls";
                                break;
                            case "SKL1":
                                type = "skel";
                                break;
                            case "SYHP":
                                type = "phys";
                                break;
                            case "TAFG":
                                type = "gfat";
                                break;
                            case "M3DT":
                            case "M3SI":
                            case "M3ST":
                            case "MES3":
                            case "M3CL":
                            case "M3SV":
                            case "M3XF":
                                type = "m3";
                                break;
                            case "m3SL":
                            case "m3SH":
                            case "m3SP":
                            case "m3ST":
                            case "m3SS":
                            case "m3MD":
                            case "m3S2":
                            case "m3M2":
                            case "m3DB":
                            case "m3ML":
                                type = "mtl3lib";
                                break;
                            case "*QIL":
                                type = "liq";
                                break;
                            case "<?xm":
                                type = "xml";
                                break;
                            case "???1":
                                type = "srt";
                                break;
                            case "<Ui ":
                                type = "xml";
                                break;
                            case "## T":
                                type = "toc";
                                break;
                            case "#pra":
                                type = "hlsl";
                                break;
                            default:
                                break;
                        }

                        if (magicString.StartsWith("ID3") || (magic[0] == 0xFF && magic[1] == 0xFB))
                            type = "mp3";

                        if (type == "unk")
                        {
                            Console.WriteLine((uint)unknownFile + " - Unknown magic " + magicString + " (" + Convert.ToHexString(magic) + ")");
                        }
                        else
                        {
                            var tagValue = "";
                            switch (type)
                            {
                                case "mp3":
                                    tagValue = "MP3";
                                    break;
                                case "ogg":
                                    tagValue = "OGG";
                                    break;
                                case "blp":
                                    tagValue = "BLP";
                                    break;
                                case "m2":
                                    tagValue = "M2 Model";
                                    break;
                                case "m3":
                                    tagValue = "M3 Model";
                                    break;
                                case "skin":
                                    tagValue = "Skin";
                                    break;
                                case "anim":
                                    tagValue = "Anim";
                                    break;
                                case "phys":
                                    tagValue = "Phys";
                                    break;
                                case "skel":
                                    tagValue = "Skel";
                                    break;
                                case "wmo":
                                    tagValue = "Root WMO";
                                    break;
                                case "gwmo":
                                    tagValue = "Group WMO";
                                    break;
                                case "adt":
                                    tagValue = "Root ADT";
                                    break;
                                case "lodadt":
                                    tagValue = "LOD ADT";
                                    break;
                                case "texadt":
                                    tagValue = "Tex0 ADT";
                                    break;
                                case "objadt":
                                    tagValue = "Obj0 ADT";
                                    break;
                                case "wdt":
                                    tagValue = "Root WDT";
                                    break;
                                case "avi":
                                    tagValue = "AVI";
                                    break;
                                case "db2":
                                    tagValue = "DB2";
                                    break;
                                case "bls":
                                    tagValue = "BLS";
                                    break;
                                case "xml":
                                    tagValue = "XML";
                                    break;
                                case "toc":
                                    tagValue = "TOC";
                                    break;
                                case "srt":
                                    tagValue = "SRT";
                                    break;
                                case "gfat":
                                    tagValue = "GFAT";
                                    break;
                                case "tex":
                                    tagValue = "TEX";
                                    break;
                                case "hlsl":
                                    tagValue = "HLSL";
                                    break;
                                default:
                                    Console.WriteLine("[FileType Tagger] No FileType tag mapping for " + type + " for file " + (uint)unknownFile);
                                    break;
                            }

                            if (!string.IsNullOrEmpty(tagValue))
                                lock (tagLock)
                                    AutoTagger.DataSource.AddTagToFDID((int)unknownFile, "FileType", "Auto", tagValue);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!e.Message.Contains("nknown keyname"))
                    {
                        Console.WriteLine("[FileType Tagger] Failed to guess type for file " + unknownFile + ": " + e.Message + "\n" + e.StackTrace);
                    }
                    numFilesSkipped++;
                    numFilesDone++;
                }

                if (numFilesDone % 1000 == 0)
                    Console.WriteLine("[FileType Tagger] Analyzed " + numFilesDone + "/" + numFilesTotal + " files (skipped " + numFilesSkipped + " unreadable files)");

                numFilesDone++;
            });
        }
    }
}
