using WoWTagLib.AutoTagging.Services;

namespace WoWTagLib.AutoTagging.Taggers
{
    public class M2
    {
        public static void Tag()
        {
            if (AutoTagger.DataSource == null)
                throw new Exception("AutoTagger DataSource is not initialized.");

            var tagLock = new Lock();

            var currentFDIDs = AutoTagger.DataSource.GetFileDataIDsByTagAndValue("FileType", "M2 Model");

            Console.WriteLine("[M2 Tagger] Processing " + currentFDIDs.Count + " M2 files...");

            Parallel.ForEach(currentFDIDs, async fdid =>
            {
                try
                {
                    var m2File = await CASCManager.GetFileByID((uint)fdid);
                    using (var bin = new BinaryReader(m2File))
                    {
                        if (bin.ReadUInt32() == 0)
                        {
                            Console.WriteLine("[M2 Tagger] " + fdid + " is encrypted, skipping..");
                            return;
                        }

                        bin.BaseStream.Position -= 4;

                        while (bin.BaseStream.Position < bin.BaseStream.Length)
                        {
                            var chunkType = new string(bin.ReadChars(4));
                            var chunkSize = bin.ReadInt32();

                            switch (chunkType)
                            {
                                case "MD21":
                                    var prevPos = bin.BaseStream.Position;

                                    bin.BaseStream.Position = 36;
                                    var nAnimations = bin.ReadUInt32();
                                    var ofsAnimations = bin.ReadUInt32();

                                    bin.BaseStream.Position = 52;
                                    var nBones = bin.ReadUInt32();
                                    var ofsBones = bin.ReadUInt32();

                                    bin.BaseStream.Position = 168;
                                    var boundingBoxMinX = bin.ReadSingle();
                                    var boundingBoxMinY = bin.ReadSingle();
                                    var boundingBoxMinZ = bin.ReadSingle();
                                    var boundingBoxMaxX = bin.ReadSingle();
                                    var boundingBoxMaxY = bin.ReadSingle();
                                    var boundingBoxMaxZ = bin.ReadSingle();
                                    var boundingSphereRadius = bin.ReadSingle();

                                    // TODO: Collision box may be a better indicator but isn't always present. 
                                    //var collisionBoxMinX = bin.ReadSingle();
                                    //var collisionBoxMinY = bin.ReadSingle();
                                    //var collisionBoxMinZ = bin.ReadSingle();
                                    //var collisionBoxMaxX = bin.ReadSingle();
                                    //var collisionBoxMaxY = bin.ReadSingle();
                                    //var collisionBoxMaxZ = bin.ReadSingle();
                                    //var collisionSphereRadius = bin.ReadSingle();

                                    // TODO: ModelFeature HasCollision?

                                    // Determine size category based on collisionSphereRadius, if 0 then use boundingSphereRadius. Can also fall back to ModelFileData for encrypted models maybe?
                                    // Notable sample models:

                                    // Tiny >0 - 0.71
                                    // 7525410-7525487  - Murloc Plushies   - 0,41801652	0,43824014

                                    // Small 0.71 - 3
                                    // 7338177          - Crate             - 0,8889543	    0,8955718

                                    // Medium 3 - 12

                                    // Large 12 - 40
                                    // 6799104          - Apple Tree        - 15.531525     14.510344
                                    // 7277638          - FelBatMount       - 21,393536     1,103744
                                    // 7469299          - CherryBlossom     - 37,11647      7,132223

                                    // Very Large 40 - 100
                                    // 7345762          - Animated Birdge   - 60,032154	    60,472664

                                    // Huge 100+
                                    // 6405957          - Ethereal Aperture - 478,90884     0


                                    var sizeTagValue = "";
                                    if (boundingSphereRadius > 0 && boundingSphereRadius < 0.71)
                                        sizeTagValue = "Tiny";
                                    else if (boundingSphereRadius >= 0.71 && boundingSphereRadius < 3)
                                        sizeTagValue = "Small";
                                    else if (boundingSphereRadius >= 3 && boundingSphereRadius < 12)
                                        sizeTagValue = "Medium";
                                    else if (boundingSphereRadius >= 12 && boundingSphereRadius < 40)
                                        sizeTagValue = "Large";
                                    else if (boundingSphereRadius >= 40 && boundingSphereRadius < 100)
                                        sizeTagValue = "Very Large";
                                    else if (boundingSphereRadius >= 100)
                                        sizeTagValue = "Huge";

                                    if (boundingSphereRadius > 0)
                                        lock (tagLock)
                                            AutoTagger.DataSource.AddTagToFDID((int)fdid, "M2Size", sizeTagValue);

                                    bin.BaseStream.Position = 264;
                                    var nEvents = bin.ReadUInt32();
                                    var ofsEvents = bin.ReadUInt32();

                                    bin.BaseStream.Position = 272;
                                    var nLights = bin.ReadInt32();
                                    var ofsLights = bin.ReadInt32();
                                    if (nLights > 0)
                                        lock (tagLock)
                                            AutoTagger.DataSource.AddTagToFDID((int)fdid, "ModelFeature", "Lights");


                                    bin.BaseStream.Position = 304;
                                    var nParticles = bin.ReadUInt32();
                                    var ofsParticles = bin.ReadUInt32();
                                    if (nParticles > 0)
                                        lock (tagLock)
                                            AutoTagger.DataSource.AddTagToFDID((int)fdid, "ModelFeature", "Particles");

                                    bin.BaseStream.Position = ofsAnimations + 8;
                                    if (nAnimations > 1) // not super accurate
                                        lock (tagLock)
                                            AutoTagger.DataSource.AddTagToFDID((int)fdid, "ModelFeature", "Animations");

                                    bin.BaseStream.Position = ofsBones + 8;
                                    for (var i = 0; i < nBones; i++)
                                    {
                                        bin.ReadUInt32();
                                        var boneFlags = bin.ReadUInt32();
                                        if ((boneFlags & 0x400) != 0)
                                            lock (tagLock)
                                                AutoTagger.DataSource.AddTagToFDID((int)fdid, "ModelFeature", "Physics");

                                        bin.ReadBytes(80);
                                    }

                                    bin.BaseStream.Position = ofsEvents + 8;
                                    for (var i = 0; i < nEvents; i++)
                                    {
                                        var identifier = System.Text.Encoding.UTF8.GetString(bin.ReadBytes(4));
                                        if (identifier == "$DSL" || identifier == "$DSO")
                                            lock (tagLock)
                                                AutoTagger.DataSource.AddTagToFDID((int)fdid, "ModelFeature", "Sounds");
                                        var data = bin.ReadUInt32();
                                        bin.ReadBytes(24);
                                    }

                                    bin.BaseStream.Position = prevPos + chunkSize;
                                    break;
                                default:
                                    bin.BaseStream.Position += chunkSize;
                                    break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[M2 Tagger] Error processing file {fdid}: {e.Message}");
                    Console.WriteLine(e.StackTrace);
                }
            });

            Console.WriteLine("[M2 Tagger] Completed processing M2 files.");
        }
    }
}
