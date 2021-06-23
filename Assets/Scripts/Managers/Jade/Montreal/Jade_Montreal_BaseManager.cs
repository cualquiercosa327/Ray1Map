﻿using BinarySerializer;
using Cysharp.Threading.Tasks;
using R1Engine.Jade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace R1Engine
{
    public abstract class Jade_Montreal_BaseManager : Jade_BaseManager {
		public override void CreateLevelList(LOA_Loader l) {
			var groups = l.FileInfos.GroupBy(f => Jade_Key.UncomposeBinKey(l.Context, f.Key)).OrderBy(f => f.Key);
			//List<KeyValuePair<uint, LOA_Loader.FileInfo>> levels = new List<KeyValuePair<uint, LOA_Loader.FileInfo>>();
			List<KeyValuePair<uint, LevelInfo>> levels = new List<KeyValuePair<uint, LevelInfo>>();
			foreach (var g in groups) {
				if (!g.Any(f => f.Key.Type == Jade_Key.KeyType.Map)) continue;
				var kvpair = g.FirstOrDefault(f => f.Value.FileName != null && f.Value.FileName.EndsWith(".wol"));
				string mapName = null;
				string worldName = null;
				LevelInfo.FileType? fileType = null;
				if (kvpair.Value == null) {
					kvpair = g.FirstOrDefault(f => f.Value.FileName != null && f.Key.Type == Jade_Key.KeyType.Map);

					if (kvpair.Value != null) {
						if(kvpair.Value.DirectoryName == "ROOT/Bin") {
							string FilenamePattern = @"^(?<name>.*)_(?<type>(wow|wol|oin))_(?<key>[0-9a-f]{1,8}).bin";
							Match m = Regex.Match(kvpair.Value.FileName, FilenamePattern, RegexOptions.IgnoreCase);
							if (m.Success) {
								var name = m.Groups["name"].Value;
								var keyStr = m.Groups["key"].Value;
								var type = m.Groups["type"].Value;
								if (type.ToLower() == "oin") {
									continue;
								} else {
									mapName = name;
									worldName = type.ToUpper();
									if (worldName == "WOW") {
										fileType = LevelInfo.FileType.WOW;
									} else if(worldName == "WOL") fileType = LevelInfo.FileType.WOL;
								}
							}
						}
					}
				}
				//if (kvpair.Value != null) {
				//	Debug.Log($"{g.Key:X8} - {kvpair.Value.FilePath }");
				//}
				levels.Add(new KeyValuePair<uint, LevelInfo>(g.Key, new LevelInfo(
					g.Key,
					kvpair.Value?.DirectoryName ?? "null",
					kvpair.Value?.FileName ?? "null",
					worldName: worldName,
					mapName: mapName,
					type: fileType)));
			}

			var str = new StringBuilder();

			foreach (var kv in levels.OrderBy(l => l.Value?.DirectoryPath).ThenBy(l => l.Value?.FilePath)) {
				str.AppendLine($"new LevelInfo(0x{kv.Key:X8}, \"{kv.Value?.DirectoryPath}\", \"{kv.Value?.FilePath}\"" +
					$"{(kv.Value?.OriginalWorldName != null ? $", worldName: \"{kv.Value.OriginalWorldName}\"" : "")}" +
					$"{(kv.Value?.OriginalMapName != null ? $", mapName: \"{kv.Value.OriginalMapName}\"" : "")}" +
					$"{(kv.Value?.OriginalType != null ? $", type: LevelInfo.FileType.{kv.Value.OriginalType}" : "")}" +
					$"),");
				//Debug.Log($"{kv.Key:X8} - {kv.Value }");
			}

			str.ToString().CopyToClipboard();
		}

		public static async UniTask LoadTextures_Montreal(SerializerObject s, WOR_World w) {
			LOA_Loader Loader = s.Context.GetStoredObject<LOA_Loader>(Jade_BaseManager.LoaderKey);
			TEX_GlobalList texList = s.Context.GetStoredObject<TEX_GlobalList>(Jade_BaseManager.TextureListKey);

			Controller.DetailedState = $"Loading textures: Info";
			texList.SortTexturesList_Montreal();
			for (int i = 0; i < (texList.Textures?.Count ?? 0); i++) {
				texList.Textures[i].LoadInfo();
				await Loader.LoadLoopBINAsync();
			}

			Controller.DetailedState = $"Loading textures: Palettes";
			texList.SortPalettesList_Montreal();
			if (texList.Palettes != null) {
				for (int i = 0; i < (texList.Palettes?.Count ?? 0); i++) {
					texList.Palettes[i].Load();
				}
				await Loader.LoadLoopBINAsync();
			}
			Controller.DetailedState = $"Loading textures: Content";
			for (int i = 0; i < (texList.Textures?.Count ?? 0); i++) {
				texList.Textures[i].LoadContent();
				await Loader.LoadLoopBINAsync();
				if (texList.Textures[i].Content != null && texList.Textures[i].Info.Type != TEX_File.TexFileType.RawPal) {
					if (texList.Textures[i].Content.Width != texList.Textures[i].Info.Width ||
						texList.Textures[i].Content.Height != texList.Textures[i].Info.Height ||
						texList.Textures[i].Content.Color != texList.Textures[i].Info.Color) {
						throw new Exception($"Info & Content width/height mismatch for texture with key {texList.Textures[i].Key}");
					}
				}
			}
			Controller.DetailedState = $"Loading textures: CubeMaps";
			for (int i = 0; i < (texList.CubeMaps?.Count ?? 0); i++) {
				texList.CubeMaps[i].Load();
				await Loader.LoadLoopBINAsync();
			}
			Controller.DetailedState = $"Loading textures: End";
			texList.FillInReferences();

			w.TextureList_Montreal = texList;

			texList = new TEX_GlobalList();
			s.Context.StoreObject<TEX_GlobalList>(Jade_BaseManager.TextureListKey, texList);
			Loader.Caches[LOA_Loader.CacheType.TextureInfo].Clear();
			Loader.Caches[LOA_Loader.CacheType.TextureContent].Clear();
		}

		public static async UniTask LoadWorld_Montreal(SerializerObject s, Jade_GenericReference world, int index, int count) {
			LOA_Loader Loader = s.Context.GetStoredObject<LOA_Loader>(Jade_BaseManager.LoaderKey);
			Jade_Reference<Jade_BinTerminator> terminator = new Jade_Reference<Jade_BinTerminator>(s.Context, new Jade_Key(s.Context, 0x0FF7C0DE));
			Loader.BeginSpeedMode(world.Key, serializeAction: async s => {
				world.Resolve(flags: LOA_Loader.ReferenceFlags.Log | LOA_Loader.ReferenceFlags.DontUseCachedFile);
				await Loader.LoadLoopBINAsync();

				if (world?.Value != null && world.Value is WOR_World w) {
					if (count == 1) {
						Controller.DetailedState = $"Loading world: {w.Name}";
					} else {
						Controller.DetailedState = $"Loading world {index + 1}/{count}: {w.Name}";
					}
					await w.JustAfterLoad_Montreal(s, false);
					await Jade_Montreal_BaseManager.LoadTextures_Montreal(s, w);
				}
				terminator.Resolve();
				await Loader.LoadLoopBINAsync();

			});
			await Loader.LoadLoop(s);
			Loader.CurrentCacheType = LOA_Loader.CacheType.Main;
			Loader.Cache.Clear();
			Loader.EndSpeedMode();
		}
	}
}
