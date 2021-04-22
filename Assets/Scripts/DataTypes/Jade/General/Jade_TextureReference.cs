﻿
using System;
using System.Collections.Generic;
using BinarySerializer;

namespace R1Engine.Jade {
	public class Jade_TextureReference : BinarySerializable {
		public Jade_Key Key { get; set; }
		public bool IsNull => Key.IsNull;
		public TEX_File Info { get; set; }
		public TEX_File Content { get; set; }

		public byte RRR2_Byte { get; set; }
		public bool RRR2_ReadByte { get; set; } = true;

		public override void SerializeImpl(SerializerObject s) {
			Key = s.SerializeObject<Jade_Key>(Key, name: nameof(Key));
			if (s.GetR1Settings().EngineVersion == EngineVersion.Jade_RRR2 && RRR2_ReadByte) {
				TEX_GlobalList lists = Context.GetStoredObject<TEX_GlobalList>(Jade_BaseManager.TextureListKey);
				if (!lists.ContainsTexture(this)) {
					RRR2_Byte = s.Serialize<byte>(RRR2_Byte, name: nameof(RRR2_Byte));
				}
			}
		}

		public Jade_TextureReference() { }
		public Jade_TextureReference(Context c, Jade_Key key) {
			Context = c;
			Key = key;
		}

		// Dummy resolve method for now
		public Jade_TextureReference Resolve() {
			if (IsNull) return this;
			TEX_GlobalList lists = Context.GetStoredObject<TEX_GlobalList>(Jade_BaseManager.TextureListKey);
			lists.AddTexture(this);
			return this;
		}

		public Jade_TextureReference LoadInfo(
			Action<SerializerObject, TEX_File> onPreSerialize = null,
			Action<SerializerObject, TEX_File> onPostSerialize = null) {
			if (IsNull) return this;
			LOA_Loader loader = Context.GetStoredObject<LOA_Loader>(Jade_BaseManager.LoaderKey);
			loader.RequestFile(Key, (s, configureAction) => {
				Info = s.SerializeObject<TEX_File>(Info, onPreSerialize: f => {
					configureAction(f); f.IsContent = false; onPreSerialize?.Invoke(s, f);
				}, name: nameof(Info));
				onPostSerialize?.Invoke(s, Info);
			}, (f) => {
				Info = f?.ConvertType<TEX_File>();
			}, immediate: false,
			queue: LOA_Loader.QueueType.Textures,
			cache: LOA_Loader.CacheType.TextureInfo,
			name: typeof(TEX_File).Name);
			return this;
		}

		public Jade_TextureReference LoadContent(
			Action<SerializerObject, TEX_File> onPreSerialize = null,
			Action<SerializerObject, TEX_File> onPostSerialize = null) {
			if (IsNull) return this;
			if (!Info.HasContent) return this;

			LOA_Loader loader = Context.GetStoredObject<LOA_Loader>(Jade_BaseManager.LoaderKey);
			loader.RequestFile(Key, (s, configureAction) => {
				Content = s.SerializeObject<TEX_File>(Content, onPreSerialize: f => {
					f.Info = Info;
					configureAction(f); f.IsContent = true; onPreSerialize?.Invoke(s, f);
				}, name: nameof(Content));
				onPostSerialize?.Invoke(s, Content);
			}, (f) => {
				Content = f?.ConvertType<TEX_File>(); ;
			}, immediate: false,
			queue: LOA_Loader.QueueType.Textures,
			cache: LOA_Loader.CacheType.TextureContent,
			name: typeof(TEX_File).Name);
			return this;
		}

		public override bool IsShortLog => true;
		public override string ShortLog => RRR2_Byte != 0 ? $"{Key},{RRR2_Byte}" : Key.ToString();
	}
}
