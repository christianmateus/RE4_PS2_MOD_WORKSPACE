using System;
using System.IO;

namespace RE4_PS2_EFF_Textures_Tool
{
    public class EffMain
    {
        private string filepath { get; set; }
        public EffMain(string effFile, string command)
        {
            filepath = effFile;
            if (command == "extract")
            {
                Extract();
            }
            else
            {
                Repack();
            }
        }
        private void Extract()
        {
            string filename = Path.GetFileNameWithoutExtension(filepath);

            BinaryReader br = new BinaryReader(File.OpenRead(filepath));
            br.BaseStream.Position = 0x18;
            uint effTexturesStartOffset = br.ReadUInt32();
            uint effTexturesEndOffset = br.ReadUInt32();

            // If there is no texture chunk
            if (effTexturesStartOffset == 0)
            {
                Console.WriteLine($"No textures found on '{filename}'");
                return;
            }

            // Get textures chunk (contains header with count and offsets)
            br.BaseStream.Position = effTexturesStartOffset;
            byte[] textures = br.ReadBytes((int)(effTexturesEndOffset - br.BaseStream.Position));
            br.Close();

            // Create folder
            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);
            }

            // Heff stands for Header Effects (contains header info with index and offsets)
            BinaryWriter bw = new BinaryWriter(File.Create($"{filename}/{filename}.HEFF"));
            bw.Write(textures);
            bw.Close();

            // Reads the .heff and extracts every texture from it
            BinaryReader br2 = new BinaryReader(File.OpenRead($"{filename}/{filename}.HEFF"));
            uint fileCount = br2.ReadUInt32();

            for (int i = 0; i < fileCount; i++)
            {
                BinaryWriter bw2 = new BinaryWriter(File.Create($"{filename}/" + filename + "_" + i + ".tpl"));
                br2.BaseStream.Position = 0x04 + (0x04 * i);
                uint startOffset = br2.ReadUInt32();
                uint endOffset = br2.ReadUInt32();

                br2.BaseStream.Position = startOffset;
                if (i != fileCount - 1)
                {
                    byte[] tpl = br2.ReadBytes((int)(endOffset - startOffset));
                    bw2.Write(tpl);
                }
                else
                {
                    byte[] tpl = br2.ReadBytes((int)(br2.BaseStream.Length - startOffset));
                    bw2.Write(tpl);
                }
                bw2.Close();
            }
            br2.Close();
        }
        private void Repack()
        {
            string filename = Path.GetFileNameWithoutExtension(filepath);

            // Get original eff parts
            BinaryReader br = new BinaryReader(File.OpenRead(filepath));
            br.BaseStream.Position = 0x18;
            uint effTextureOffset = br.ReadUInt32();
            uint effTextureOffsetEnd = br.ReadUInt32();

            // Get the last 4 pointers from the header (they need to be updated if any .tpl changes size)
            uint[] pointerArray = new uint[4];
            for (int i = 0; i < 4; i++)
            {
                pointerArray[i] = br.ReadUInt32();
            }

            br.BaseStream.Position = 0;
            byte[] topPart = br.ReadBytes((int)effTextureOffset);
            br.BaseStream.Position = effTextureOffsetEnd;

            byte[] bottomPart = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
            br.Close();

            // -------------------------------------------------
            // WRITE [This section has a predefined value of 800 in the loops because I was not able to sort the array correctly]
            // Create Heff file with all tpl files in the folder
            string[] tplFiles = Directory.GetFiles(filename, "*.tpl");

            int acumulatorLenght = 0;
            int offsetTexturesStart;

            BinaryWriter heff = new BinaryWriter(File.Create($"{filename}/{filename}.HEFF"));
            heff.Write((uint)tplFiles.Length);

            // Write header
            for (int i = 0; i < 800; i++)
            {
                if (File.Exists($"{filename}/" + filename + "_" + i + ".tpl"))
                {
                    BinaryReader tpl = new BinaryReader(File.OpenRead($"{filename}/" + filename + "_" + i + ".tpl"));
                    heff.Write(acumulatorLenght);
                    acumulatorLenght += (int)tpl.BaseStream.Length;
                    tpl.Close();
                }
            }

            // Write padding
            for (int i = 0; i < 4; i++)
            {
                if (!heff.BaseStream.Position.ToString("X").EndsWith("0"))
                {
                    heff.Write((uint)0x00);
                }
                else break;
            }
            offsetTexturesStart = (int)heff.BaseStream.Position;

            // Write tpl
            for (int i = 0; i < 800; i++)
            {
                if (File.Exists($"{filename}/" + filename + "_" + i + ".tpl"))
                {
                    BinaryReader tpl = new BinaryReader(File.OpenRead($"{filename}/" + filename + "_" + i + ".tpl"));
                    byte[] tplBytes = tpl.ReadBytes((int)tpl.BaseStream.Length);
                    tpl.Close();
                    heff.Write(tplBytes);
                }
            }

            // Update all offsets (workaround)
            acumulatorLenght = 0;
            heff.BaseStream.Position = 0x04;
            for (int i = 0; i < 800; i++)
            {
                if (File.Exists($"{filename}/" + filename + "_" + i + ".tpl"))
                {
                    BinaryReader tpl = new BinaryReader(File.OpenRead($"{filename}/" + filename + "_" + i + ".tpl"));
                    heff.Write((uint)(acumulatorLenght + offsetTexturesStart));
                    acumulatorLenght += (int)tpl.BaseStream.Length;
                    tpl.Close();
                }
            }
            heff.Close();

            // Get Heff data
            BinaryReader brHeff = new BinaryReader(File.OpenRead($"{filename}/{filename}.HEFF"));
            byte[] brHeffBytes = brHeff.ReadBytes((int)brHeff.BaseStream.Length);
            brHeff.Close();

            // Insert back to .EFF
            BinaryWriter newEff = new BinaryWriter(File.Create(filepath));
            newEff.Write(topPart);
            newEff.Write(brHeffBytes);
            newEff.Write(bottomPart);

            // Updates .EFF header offsets
            newEff.BaseStream.Position = 0x1C;
            uint originalHeffSize = effTextureOffsetEnd - effTextureOffset;
            int sizeDifference = (int)(brHeffBytes.Length - originalHeffSize); // Size difference between original and modified heff

            if (brHeffBytes.Length != originalHeffSize)
            {
                newEff.Write(effTextureOffsetEnd + sizeDifference);
                for (int x = 0; x < 4; x++)
                {
                    if (pointerArray[x] > 0)
                    {
                        newEff.Write((uint)(sizeDifference + pointerArray[x]));
                    }
                }
            }
            newEff.Close();
        }
    }
}