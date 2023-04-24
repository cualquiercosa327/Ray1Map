﻿using BinarySerializer;

namespace Ray1Map.Jade {
	public class WAY_Network : Jade_File {
		public override string Export_Extension => "way";
		public override bool HasHeaderBFFile => true;

		public Jade_Reference<OBJ_GameObject> Root { get; set; }
		public uint Flags { get; set; }

		public uint TVP_UInt0 { get; set; }
		public uint TVP_UInt1 { get; set; }
		public uint TVP_UInt2 { get; set; }

		protected override void SerializeFile(SerializerObject s) {
			Root = s.SerializeObject<Jade_Reference<OBJ_GameObject>>(Root, name: nameof(Root))?.Resolve();
			Flags = s.Serialize<uint>(Flags, name: nameof(Flags));
			if (s.GetR1Settings().EngineVersionTree.HasParent(EngineVersion.Jade_RRRTVParty) && BitHelpers.ExtractBits64(Flags, 1, 31) != 0) {
				TVP_UInt0 = s.Serialize<uint>(TVP_UInt0, name: nameof(TVP_UInt0));
				TVP_UInt1 = s.Serialize<uint>(TVP_UInt1, name: nameof(TVP_UInt1));
				TVP_UInt2 = s.Serialize<uint>(TVP_UInt2, name: nameof(TVP_UInt2));
			}
		}
	}
}
