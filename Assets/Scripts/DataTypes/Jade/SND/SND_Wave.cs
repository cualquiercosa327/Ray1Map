﻿using BinarySerializer;
using System.Text;

namespace R1Engine.Jade
{
    public class SND_Wave : Jade_File 
    {
		public byte[] WaveData { get; set; }
		public byte[] WaveData_Content { get; set; }


		public string Magic { get; set; }
		public uint OriginalFileSize { get; set; }
		public string FileTypeHeader { get; set; }
		public SND_WaveChunk_Container[] Chunks { get; set; }

		public override void SerializeImpl(SerializerObject s) {
			SND_GlobalList list = Context.GetStoredObject<SND_GlobalList>(Jade_BaseManager.SoundListKey);
			list.AddWave(this);

			if (FileSize > 8 && s.GetR1Settings().EngineVersion == EngineVersion.Jade_RRR && s.GetR1Settings().Platform == Platform.PC) {
				// Serialize wave content
				Magic = s.SerializeString(Magic, 4, Encoding.ASCII, name: nameof(Magic));
				OriginalFileSize = s.Serialize<uint>(OriginalFileSize, name: nameof(OriginalFileSize));
				FileTypeHeader = s.SerializeString(FileTypeHeader, 4, Encoding.ASCII, name: nameof(FileTypeHeader));
				uint EditorSize = 4;
				if (EditorSize < OriginalFileSize) {
					Chunks = s.SerializeObjectArrayUntil<SND_WaveChunk_Container>(Chunks, c => {
						EditorSize += c.EditorSize;
						return EditorSize >= OriginalFileSize;
					}, name: nameof(Chunks));
				}
			} else {
				// TODO: Maybe properly parse this later on
				WaveData = s.SerializeArray<byte>(WaveData, FileSize, name: nameof(WaveData));
			}
		}

		public enum Type {
			Ambience,
			Dialog,
			LoadingSound,
			Music,
			Sound,
		}
		public Type SoundType { get; set; }
		public override string Export_Extension {
			get {
				switch(SoundType) {
					case Type.Ambience: return "waa";
					case Type.Dialog: return "wad";
					case Type.LoadingSound: return "wac";
					case Type.Music: return "wam";
					case Type.Sound: return "wav";
					//case Type.Sound: return "wac";
					default: return null;
				}
			}
		}

		public void SerializeData(SerializerObject s) {
			s.Log($"Serializing data for SND_Wave: {Key}");
			if (Chunks == null)
				throw new BinarySerializableException(this, $"{GetType()}: Tried to serialize data, but Chunks was null");

			var dataChunk = Chunks.FindItem(c => c.Type == SND_WaveChunk_Container.ChunkType.Data);

			if(dataChunk == null)
				throw new BinarySerializableException(this, $"{GetType()}: Tried to serialize data, but Data chunk was not found");

			((SND_WaveChunk_Data)dataChunk.Chunk).SerializeData(s);
		}
	}
}