﻿using System.Linq;

namespace R1Engine
{
    public class GBAVV_Map2D_AnimationFrame : R1Serializable
    {
        public Pointer TilePositionsPointer { get; set; }
        public Pointer TileShapesPointer { get; set; }
        public UInt24 TileOffset { get; set; } // Offset in the global tileset
        public byte TilesCount { get; set; }

        // Fusion
        public Pointer Fusion_TileSetPointer { get; set; }
        public short Fusion_Short_08 { get; set; }
        public short Fusion_Short_0A { get; set; }
        public byte[] Fusion_Data_0C { get; set; }
        public byte[] Fusion_Data_11 { get; set; }
        public Pointer Fusion_Pointer_14 { get; set; } // 8 bytes
        public Pointer Fusion_HitBox1Pointer { get; set; }
        public Pointer Fusion_HitBox2Pointer { get; set; }
        public byte[] Fusion_Data_20 { get; set; }

        // Serialized from pointers
        public TilePosition[] TilePositions { get; set; }
        public TileShape[] TileShapes { get; set; }

        // Fusion
        public byte[] Fusion_TileSet { get; set; }
        public GBAVV_Map2D_AnimationRect Fusion_HitBox1 { get; set; }
        public GBAVV_Map2D_AnimationRect Fusion_HitBox2 { get; set; }

        // Helpers
        public int GetTileShape(int index)
        {
            if (Context.Settings.GBAVV_IsFusion)
                return TilePositions[index].ShapeIndex;
            else
                return TileShapes[index].ShapeIndex;
        }

        public override void SerializeImpl(SerializerObject s)
        {
            if (s.GameSettings.GBAVV_IsFusion)
            {
                TilePositionsPointer = s.SerializePointer(TilePositionsPointer, name: nameof(TilePositionsPointer));
                Fusion_TileSetPointer = s.SerializePointer(Fusion_TileSetPointer, name: nameof(Fusion_TileSetPointer));
                Fusion_Short_08 = s.Serialize<short>(Fusion_Short_08, name: nameof(Fusion_Short_08));
                Fusion_Short_0A = s.Serialize<short>(Fusion_Short_0A, name: nameof(Fusion_Short_0A));
                Fusion_Data_0C = s.SerializeArray<byte>(Fusion_Data_0C, 4, name: nameof(Fusion_Data_0C));
                TilesCount = s.Serialize<byte>(TilesCount, name: nameof(TilesCount));
                Fusion_Data_11 = s.SerializeArray<byte>(Fusion_Data_11, 3, name: nameof(Fusion_Data_11));
                Fusion_Pointer_14 = s.SerializePointer(Fusion_Pointer_14, name: nameof(Fusion_Pointer_14));
                Fusion_HitBox1Pointer = s.SerializePointer(Fusion_HitBox1Pointer, name: nameof(Fusion_HitBox1Pointer));
                Fusion_HitBox2Pointer = s.SerializePointer(Fusion_HitBox2Pointer, name: nameof(Fusion_HitBox2Pointer));
                Fusion_Data_20 = s.SerializeArray<byte>(Fusion_Data_20, 12, name: nameof(Fusion_Data_20));

                TilePositions = s.DoAt(TilePositionsPointer, () => s.SerializeObjectArray<TilePosition>(TilePositions, TilesCount, name: nameof(TilePositions)));

                if (TilePositions != null)
                    Fusion_TileSet = s.DoAt(Fusion_TileSetPointer, () => s.SerializeArray<byte>(Fusion_TileSet, TilePositions.Select(x => GBAVV_BaseManager.TileShapes[x.ShapeIndex]).Sum(x => x.x * x.y / 2), name: nameof(Fusion_TileSet)));

                Fusion_HitBox1 = s.DoAt(Fusion_HitBox1Pointer, () => s.SerializeObject<GBAVV_Map2D_AnimationRect>(Fusion_HitBox1, name: nameof(Fusion_HitBox1)));
                Fusion_HitBox2 = s.DoAt(Fusion_HitBox2Pointer, () => s.SerializeObject<GBAVV_Map2D_AnimationRect>(Fusion_HitBox2, name: nameof(Fusion_HitBox2)));
            }
            else
            {
                TilePositionsPointer = s.SerializePointer(TilePositionsPointer, name: nameof(TilePositionsPointer));
                TileShapesPointer = s.SerializePointer(TileShapesPointer, name: nameof(TileShapesPointer));
                TileOffset = s.Serialize<UInt24>(TileOffset, name: nameof(TileOffset));
                TilesCount = s.Serialize<byte>(TilesCount, name: nameof(TilesCount));

                TilePositions = s.DoAt(TilePositionsPointer, () => s.SerializeObjectArray<TilePosition>(TilePositions, TilesCount, name: nameof(TilePositions)));
                TileShapes = s.DoAt(TileShapesPointer, () => s.SerializeObjectArray<TileShape>(TileShapes, TilesCount, name: nameof(TileShapes)));
            }
        }

        public class TilePosition : R1Serializable
        {
            public short XPos { get; set; }
            public short YPos { get; set; }
            public int ShapeIndex { get; set; }

            public override void SerializeImpl(SerializerObject s)
            {
                XPos = s.Serialize<short>(XPos, name: nameof(XPos));
                YPos = s.Serialize<short>(YPos, name: nameof(YPos));

                if (s.GameSettings.GBAVV_IsFusion)
                    ShapeIndex = s.Serialize<int>(ShapeIndex, name: nameof(ShapeIndex));
            }
        }

        public class TileShape : R1Serializable
        {
            public byte ShapeIndex { get; set; }
            public byte Unknown { get; set; }

            public override void SerializeImpl(SerializerObject s)
            {
                s.SerializeBitValues<byte>(bitFunc =>
                {
                    ShapeIndex = (byte)bitFunc(ShapeIndex, 4, name: nameof(ShapeIndex));
                    Unknown = (byte)bitFunc(Unknown, 4, name: nameof(Unknown));
                });
            }
        }
    }
}