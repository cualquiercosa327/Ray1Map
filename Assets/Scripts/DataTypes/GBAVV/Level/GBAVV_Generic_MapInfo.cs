﻿namespace R1Engine
{
    public class GBAVV_Generic_MapInfo : R1Serializable
    {
        public bool SerializeData { get; set; } = true; // Set before serializing

        public Pointer TilePalette2DPointer { get; set; }
        public Pointer MapData2DPointer { get; set; }
        public GBAVV_MapType MapType { get; set; }
        public uint Uint_0C { get; set; } // Set to 1 for certain bonus maps
        public short Index3D { get; set; } // For Mode7 and isometric levels
        public byte Alpha_BG3 { get; set; }
        public byte Alpha_BG2 { get; set; } // Might not be BG2
        public byte[] Frogger_Bytes { get; set; }

        // Serialized from pointers

        public RGBA5551Color[] TilePalette2D { get; set; }
        public GBAVV_Map2D_Data MapData2D { get; set; }

        public override void SerializeImpl(SerializerObject s)
        {
            TilePalette2DPointer = s.SerializePointer(TilePalette2DPointer, name: nameof(TilePalette2DPointer));
            MapData2DPointer = s.SerializePointer(MapData2DPointer, name: nameof(MapData2DPointer));

            if (s.GameSettings.EngineVersion == EngineVersion.GBAVV_FroggerAdvance)
            {
                Frogger_Bytes = s.SerializeArray<byte>(Frogger_Bytes, 8, name: nameof(Frogger_Bytes));
            }
            else
            {
                MapType = s.Serialize<GBAVV_MapType>(MapType, name: nameof(MapType));
                Uint_0C = s.Serialize<uint>(Uint_0C, name: nameof(Uint_0C));
                Index3D = s.Serialize<short>(Index3D, name: nameof(Index3D));
                Alpha_BG3 = s.Serialize<byte>(Alpha_BG3, name: nameof(Alpha_BG3));
                Alpha_BG2 = s.Serialize<byte>(Alpha_BG2, name: nameof(Alpha_BG2));
            }

            if (!SerializeData)
                return;

            TilePalette2D = s.DoAt(TilePalette2DPointer, () => s.SerializeObjectArray<RGBA5551Color>(TilePalette2D, 256, name: nameof(TilePalette2D)));
            MapData2D = s.DoAt(MapData2DPointer, () => s.SerializeObject<GBAVV_Map2D_Data>(MapData2D, name: nameof(MapData2D)));
        }

        public enum GBAVV_MapType : int
        {
            // 2D
            Normal = 0,
            Normal_Vehicle_0 = 1, // Underwater in Crash 1, flying carpet in Crash 2
            Normal_Vehicle_1 = 2, // Motorcycle in Crash 1, copter in Crash 2

            // 3D
            Mode7 = 3,
            Isometric = 4,
        }
    }
}