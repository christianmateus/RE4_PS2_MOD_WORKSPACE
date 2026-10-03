// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Encoding
{
    public class EncodedBone
    {
        public EncodedBone()
        {
        }

        public EncodedBone(byte[] line)
        {
            Line = line;
        }

        public EncodedBone(byte boneId, byte boneParent, float posX, float posY, float posZ)
        {
            BoneId = boneId;
            BoneParent = boneParent;
            PosX = posX;
            PosY = posY;
            PosZ = posZ;
        }

        private byte[] _line = new byte[16];

        public byte[] Line
        {
            get
            {
                return _line.ToArray();
            }
            set
            {
                for (int i = 0; i < value.Length && i < _line.Length; i++)
                {
                    _line[i] = value[i];
                }
            }
        }

        public byte BoneId
        {
            get
            {
                return _line[0];
            }
            set
            {
                _line[0] = value;
            }
        }

        public byte BoneParent
        {
            get
            {
                return _line[1];
            }
            set
            {
                _line[1] = value;
            }
        }

        public float PosX
        {
            get
            {
                return BitConverter.ToSingle(_line, 0x4);
            }
            set
            {
                var bvalue = BitConverter.GetBytes(value);
                bvalue.CopyTo(_line, 0x4);
            }
        }

        public float PosY
        {
            get
            {
                return BitConverter.ToSingle(_line, 0x8);
            }
            set
            {
                var bvalue = BitConverter.GetBytes(value);
                bvalue.CopyTo(_line, 0x8);
            }
        }

        public float PosZ
        {
            get
            {
                return BitConverter.ToSingle(_line, 0xC);
            }
            set
            {
                var bvalue = BitConverter.GetBytes(value);
                bvalue.CopyTo(_line, 0xC);
            }
        }


    }
}
