﻿using BinarySerializer;
using System.Text;
using UnityEngine;

namespace Ray1Map.Jade {
	public class SND_InsertChunk_YAxisFreq : SND_InsertChunk {
		public float Min { get; set; }
		public float Max { get; set; }

		public override void SerializeImpl(SerializerObject s) {
			Min = s.Serialize<float>(Min, name: nameof(Min));
			Max = s.Serialize<float>(Max, name: nameof(Max));
		}
	}
}
