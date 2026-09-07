using System.Collections.Generic;

namespace RE4_PS2_LIT_Editor
{
    public class LIT
    {
        public struct LitMainLightStruct
        {
            /*
             * Main light always have a length of 100 bytes (0x64)
             */
            public byte smd_R;
            public byte smd_G;
            public byte smd_B;
            public byte smd_A;
            public uint lightCount;
            // Fog
            public uint fogType;
            public float fogStart;
            public float fogEnd;
            public byte fog_R;
            public byte fog_G;
            public byte fog_B;
            public byte fog_A;
            // Sky
            public uint mirrorFogType;
            public float mirrorFogStart;
            public float mirrorFogEnd;
            public byte mirrorFog_R;
            public byte mirrorFog_G;
            public byte mirrorFog_B;
            public byte mirrorFog_A;
            // Focus
            public int focalDistance;
            public byte focusLevel;
            public byte unk1;
            public byte focusMode;
            public byte blurRate;
            public int unk2;
            // Tuning colors (SMD)
            public byte tuningSMD_R;
            public byte tuningSMD_G;
            public byte tuningSMD_B;
            public byte tuningSMD_A;
            // Tuning colors (Enemy)
            public byte tuningEnemy_R;
            public byte tuningEnemy_G;
            public byte tuningEnemy_B;
            public byte tuningEnemy_A;
            // Tuning colors (Player)
            public byte tuningPlayer_R;
            public byte tuningPlayer_G;
            public byte tuningPlayer_B;
            public byte tuningPlayer_A;
            // Multipliers and other
            public byte smdMultiplier;
            public byte playerMultiplier;
            public byte unk3;
            public byte unk4;
            public float distanceCulling;
            public float hokan; // Don't know what this is
            // Wind
            public byte windDirection;
            public byte windPower;
            public byte windFrequency;
            public byte windUnk;
            // Blur
            public byte blurType;
            public byte blurPower;
            public byte mipMapMinLevel;
            public byte mipMapMaxLevel;
            // Contrast
            public byte filteringFormat;
            public byte contrastLevel;
            public byte contrastPower;
            public byte contrastBias;
            public float levelOfDetailBias; // Not sure
            // Extra Tuning colors (Enemy)
            public byte extraTuningEnemy_R;
            public byte extraTuningEnemy_G;
            public byte extraTuningEnemy_B;
            public byte extraTuningEnemy_A;
            // Extra Tuning colors (Player)
            public byte extraTuningPlayer_R;
            public byte extraTuningPlayer_G;
            public byte extraTuningPlayer_B;
            public byte extraTuningPlayer_A;
        }

        public struct LitIndividualLightStruct
        {
            public byte active; // 07 = active; 05 = inactive
            public byte lightType;
            public byte attributes;
            public byte lightMask;

            public float range;
            public byte color_R;
            public byte color_G;
            public byte color_B;
            public byte color_A;
            public float intensity;
            public float posX;
            public float posY;
            public float posZ;

            public byte unk2;
            public byte unk3;
            public byte unk4;
            public byte unk5;

            public float unk6;
            public float unk7;
            public float unk8;

            public float posX2;
            public float posY2;
            public float posZ2;

            public byte origin; // 0 = static; 1 = follows movement

        }
        enum LightType : byte
        {
            Constant = 0,
            Linear = 1,
            Quadratic = 2,
            Spotlight = 3,
            Custom = 4,
            Sky = 5,
            SpotQuad = 6,
            LocalAmbient = 7
        }
        enum Attributes : byte
        {
            Normal = 0,
            Flicker = 1,
            Wave = 2,
            SpotRotate = 3,
            Shadow = 4,
            Path = 5,
            Fade = 6,
            Shine = 7
        }

        Dictionary<string, byte> ParentList = new Dictionary<string, byte>
        {
            {"World", 0},
            {"Enemy", 1},
            {"SMD", 2},
            {"ETM", 3},
            {"Object", 4}
        };

        Dictionary<string, byte> Flags = new Dictionary<string, byte>
        {
            {"None", 0},
            {"Ignore Tuning", 1},
            {"Eletric Light", 2},
            {"Flashlight", 3}
        };
    }

}
