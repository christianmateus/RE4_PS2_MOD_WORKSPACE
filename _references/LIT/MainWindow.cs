using RE4_PS2_LIT_Editor.Utils;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RE4_PS2_LIT_Editor
{
    public partial class mainWindow : Form
    {
        OpenFileDialog dialog = new OpenFileDialog();
        LIT.LitMainLightStruct LITMain = new LIT.LitMainLightStruct();
        TabCreator TabCreator = new TabCreator();
        string filepath = "";

        public mainWindow()
        {
            InitializeComponent();
            //Console.WriteLine(LIT);
        }

        private void openLITFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dialog.Filter = "RE4 LIT Files (*.LIT)|*.LIT";
            dialog.ShowDialog();
            filepath = dialog.FileName;

            if (filepath != "")
            {
                this.Text = "RE4 PS2 LIT Editor - " + dialog.FileName;
                LoadMainLight(0);
            }
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private uint[] GetGroupPointers()
        {
            BinaryReader br = new BinaryReader(File.Open(filepath, FileMode.Open));
            ushort groupCount = br.ReadUInt16();
            ushort unknown = br.ReadUInt16();
            uint[] groupPointers = new uint[groupCount];

            // Get all pointers
            for (int i = 0; i < groupCount; i++)
            {
                groupPointers[i] = br.ReadUInt32();
            }
            br.Close();
            spinBoxLightGroupIndex.Maximum = groupCount - 1;

            return groupPointers;
        }
        private void LoadMainLight(int groupIndex)
        {
            uint[] groupOffsets = GetGroupPointers();
            BinaryReader br = new BinaryReader(File.Open(filepath, FileMode.Open));

            if (groupOffsets[groupIndex] != 0)
            {
                br.BaseStream.Position = groupOffsets[groupIndex];

                LITMain.smd_R = br.ReadByte();
                LITMain.smd_G = br.ReadByte();
                LITMain.smd_B = br.ReadByte();
                LITMain.smd_A = br.ReadByte();
                LITMain.lightCount = br.ReadUInt32();
                // Fog
                LITMain.fogType = br.ReadUInt32();
                LITMain.fogStart = br.ReadSingle();
                LITMain.fogEnd = br.ReadSingle();
                LITMain.fog_R = br.ReadByte();
                LITMain.fog_G = br.ReadByte();
                LITMain.fog_B = br.ReadByte();
                LITMain.fog_A = br.ReadByte();
                // Sky
                LITMain.mirrorFogType = br.ReadUInt32();
                LITMain.mirrorFogStart = br.ReadSingle();
                LITMain.mirrorFogEnd = br.ReadSingle();
                LITMain.mirrorFog_R = br.ReadByte();
                LITMain.mirrorFog_G = br.ReadByte();
                LITMain.mirrorFog_B = br.ReadByte();
                LITMain.mirrorFog_A = br.ReadByte();
                // General
                LITMain.focalDistance = br.ReadInt32();
                LITMain.focusLevel = br.ReadByte();
                LITMain.unk1 = br.ReadByte();
                LITMain.focusMode = br.ReadByte();
                LITMain.blurRate = br.ReadByte();
                LITMain.unk2 = br.ReadInt32();
                // Tuning colors
                LITMain.tuningSMD_R = br.ReadByte();
                LITMain.tuningSMD_G = br.ReadByte();
                LITMain.tuningSMD_B = br.ReadByte();
                LITMain.tuningSMD_A = br.ReadByte();
                LITMain.tuningEnemy_R = br.ReadByte();
                LITMain.tuningEnemy_G = br.ReadByte();
                LITMain.tuningEnemy_B = br.ReadByte();
                LITMain.tuningEnemy_A = br.ReadByte();
                LITMain.tuningPlayer_R = br.ReadByte();
                LITMain.tuningPlayer_G = br.ReadByte();
                LITMain.tuningPlayer_B = br.ReadByte();
                LITMain.tuningPlayer_A = br.ReadByte();
                // Multiplers and other
                LITMain.smdMultiplier = br.ReadByte();
                LITMain.playerMultiplier = br.ReadByte();
                LITMain.unk3 = br.ReadByte();
                LITMain.unk4 = br.ReadByte();
                LITMain.distanceCulling = br.ReadSingle();
                LITMain.hokan = br.ReadSingle();
                // Wind
                LITMain.windDirection = br.ReadByte();
                LITMain.windPower = br.ReadByte();
                LITMain.windFrequency = br.ReadByte();
                LITMain.windUnk = br.ReadByte();
                // Blur
                LITMain.blurType = br.ReadByte();
                LITMain.blurPower = br.ReadByte();
                LITMain.mipMapMinLevel = br.ReadByte();
                LITMain.mipMapMaxLevel = br.ReadByte();
                // Contrast
                LITMain.filteringFormat = br.ReadByte();
                LITMain.contrastLevel = br.ReadByte();
                LITMain.contrastPower = br.ReadByte();
                LITMain.contrastBias = br.ReadByte();
                LITMain.levelOfDetailBias = br.ReadSingle();
                // Extra tuning
                LITMain.extraTuningEnemy_R = br.ReadByte();
                LITMain.extraTuningEnemy_G = br.ReadByte();
                LITMain.extraTuningEnemy_B = br.ReadByte();
                LITMain.extraTuningEnemy_A = br.ReadByte();
                LITMain.extraTuningPlayer_R = br.ReadByte();
                LITMain.extraTuningPlayer_G = br.ReadByte();
                LITMain.extraTuningPlayer_B = br.ReadByte();
                LITMain.extraTuningPlayer_A = br.ReadByte();
                // --------------------------------
                // Set values to fields
                spinRoomColorR.Value = LITMain.smd_R;
                spinRoomColorG.Value = LITMain.smd_G;
                spinRoomColorB.Value = LITMain.smd_B;
                spinRoomColorA.Value = LITMain.smd_A;
                spinLightCount.Value = LITMain.lightCount;
                // Fog
                cbFogType.SelectedIndex = (int)LITMain.fogType;
                spinFogStart.Value = (decimal)LITMain.fogStart;
                spinFogEnd.Value = (decimal)LITMain.fogEnd;
                spinFog_R.Value = LITMain.fog_R;
                spinFog_G.Value = LITMain.fog_G;
                spinFog_B.Value = LITMain.fog_B;
                spinFog_A.Value = LITMain.fog_A;
                // Mirror Fog (Sky)
                cbMirrorFogType.SelectedIndex = (int)LITMain.mirrorFogType;
                spinMirrorFogStart.Value = (decimal)LITMain.mirrorFogStart;
                spinMirrorFogEnd.Value = (decimal)LITMain.mirrorFogEnd;
                spinMirrorFog_R.Value = LITMain.mirrorFog_R;
                spinMirrorFog_G.Value = LITMain.mirrorFog_G;
                spinMirrorFog_B.Value = LITMain.mirrorFog_B;
                spinMirrorFog_A.Value = LITMain.mirrorFog_A;
                // General
                spinFocalDistance.Value = LITMain.focalDistance;
                spinFocusLevel.Value = LITMain.focusLevel;
                spinFocusMode.Value = LITMain.focusMode;
                spinBlurRate.Value = LITMain.blurRate;
                // Tuning
                spinTuningSMD_R.Value = LITMain.tuningSMD_R;
                spinTuningSMD_G.Value = LITMain.tuningSMD_G;
                spinTuningSMD_B.Value = LITMain.tuningSMD_B;
                spinTuningSMD_A.Value = LITMain.tuningSMD_A;
                spinTuningEnemy_R.Value = LITMain.tuningEnemy_R;
                spinTuningEnemy_G.Value = LITMain.tuningEnemy_G;
                spinTuningEnemy_B.Value = LITMain.tuningEnemy_B;
                spinTuningEnemy_A.Value = LITMain.tuningEnemy_A;
                spinTuningPlayer_R.Value = LITMain.tuningPlayer_R;
                spinTuningPlayer_G.Value = LITMain.tuningPlayer_G;
                spinTuningPlayer_B.Value = LITMain.tuningPlayer_B;
                spinTuningPlayer_A.Value = LITMain.tuningPlayer_A;
                // Multiplers and other
                spinMultiplierSMD.Value = LITMain.smdMultiplier;
                spinMultiplierPlayer.Value = LITMain.playerMultiplier;
                // Wind
                spinWindDirection.Value = LITMain.windDirection;
                spinWindFrequency.Value = LITMain.windFrequency;
                spinWindPower.Value = LITMain.windPower;
                spinWindUnknown.Value = LITMain.windUnk;
                // Blur
                spinBlurType.Value = LITMain.blurType;
                spinBlurPower.Value = LITMain.blurPower;
                spinMipMapMin.Value = LITMain.mipMapMinLevel;
                spinMipMapMax.Value = LITMain.mipMapMaxLevel;
                // Contrast
                spinFilteringFormat.Value = LITMain.filteringFormat;
                spinContrastLevel.Value = LITMain.contrastLevel;
                spinContrastPower.Value = LITMain.contrastPower;
                spinContrastBias.Value = LITMain.contrastBias;
                // Extra tuning colors
                spinExtraEnemy_R.Value = LITMain.extraTuningEnemy_R;
                spinExtraEnemy_G.Value = LITMain.extraTuningEnemy_G;
                spinExtraEnemy_B.Value = LITMain.extraTuningEnemy_B;
                spinExtraEnemy_A.Value = LITMain.extraTuningEnemy_A;
                spinExtraPlayer_R.Value = LITMain.extraTuningPlayer_R;
                spinExtraPlayer_G.Value = LITMain.extraTuningPlayer_G;
                spinExtraPlayer_B.Value = LITMain.extraTuningPlayer_B;
                spinExtraPlayer_A.Value = LITMain.extraTuningPlayer_A;
            }
            br.Close();
            LoadIndividualLights();

        }
        private void LoadIndividualLights()
        {
            Console.WriteLine("Total tabs: " + tabContainer.TabPages.Count.ToString());
            int tempTabCount = tabContainer.TabPages.Count;

            // Clear tabs
            for (int x = 1; x != tempTabCount; x++)
            {
                Console.WriteLine("Removing tab: " + x);
                tabContainer.TabPages.RemoveByKey($"tab{x}");
            }
            Console.WriteLine("");

            // Create tab for each light
            uint[] groupOffsets = GetGroupPointers();
            for (int i = 1; i < LITMain.lightCount + 1; i++)
            {
                if (groupOffsets[i - 1] != 0)
                {
                    tabContainer.TabPages.Add(TabCreator.CreateTab(i));
                }
            }
        }

        private void spinRoomColorR_ValueChanged(object sender, EventArgs e)
        {
            colorBoxRoomColor.BackColor = Color.FromArgb(
                (int)spinRoomColorA.Value, (int)spinRoomColorR.Value, (int)spinRoomColorG.Value, (int)spinRoomColorB.Value);
        }
        private void spinRoomColorG_ValueChanged(object sender, EventArgs e)
        {
            colorBoxRoomColor.BackColor = Color.FromArgb(
                (int)spinRoomColorA.Value, (int)spinRoomColorR.Value, (int)spinRoomColorG.Value, (int)spinRoomColorB.Value);
        }
        private void spinRoomColorB_ValueChanged(object sender, EventArgs e)
        {
            colorBoxRoomColor.BackColor = Color.FromArgb(
                (int)spinRoomColorA.Value, (int)spinRoomColorR.Value, (int)spinRoomColorG.Value, (int)spinRoomColorB.Value);
        }
        private void spinRoomColorA_ValueChanged(object sender, EventArgs e)
        {
            colorBoxRoomColor.BackColor = Color.FromArgb(
                (int)spinRoomColorA.Value, (int)spinRoomColorR.Value, (int)spinRoomColorG.Value, (int)spinRoomColorB.Value);
        }

        private void colorBoxRoomColor_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();
            colorBoxRoomColor.BackColor = colorDialog1.Color;
            spinRoomColorR.Value = colorDialog1.Color.R;
            spinRoomColorG.Value = colorDialog1.Color.G;
            spinRoomColorB.Value = colorDialog1.Color.B;
            spinRoomColorA.Value = colorDialog1.Color.A;
        }

        private void roomBaseColorToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (roomBaseColorToolStripMenuItem.Checked) groupRoomBaseColor.Visible = true;
            else groupRoomBaseColor.Visible = false;
        }
        private void fogSettingsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (fogSettingsToolStripMenuItem.Checked) groupFogSettings.Visible = true;
            else groupFogSettings.Visible = false;
        }
        private void generalSettingsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (generalSettingsToolStripMenuItem.Checked) groupGeneralSettings.Visible = true;
            else groupGeneralSettings.Visible = false;
        }
        private void motionBlurSettingsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (motionBlurSettingsToolStripMenuItem.Checked) groupMotionBlurSettings.Visible = true;
            else groupMotionBlurSettings.Visible = false;
        }
        private void tuningColorsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (tuningColorsToolStripMenuItem.Checked) groupTuningColors.Visible = true;
            else groupTuningColors.Visible = false;
        }
        private void extraTuningColorsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (extraTuningColorsToolStripMenuItem.Checked) groupExtraTuningColors.Visible = true;
            else groupExtraTuningColors.Visible = false;
        }
        private void colorIntensityMultiplierToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (colorIntensityMultiplierToolStripMenuItem.Checked) groupColorIntensity.Visible = true;
            else groupColorIntensity.Visible = false;
        }
        private void lightingSettingsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (lightingSettingsToolStripMenuItem.Checked) groupLightingSettings.Visible = true;
            else groupLightingSettings.Visible = false;
        }
        private void windSettingsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (windSettingsToolStripMenuItem.Checked) groupWindSettings.Visible = true;
            else groupWindSettings.Visible = false;
        }

        private void spinBoxLightGroupIndex_ValueChanged(object sender, EventArgs e)
        {
            LoadMainLight((int)spinBoxLightGroupIndex.Value);
        }

        private void creditsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Credits creditsForm = new Credits();
            creditsForm.Show();
        }
    }
}
