﻿using R1Engine.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace R1Engine
{
    /// <summary>
    /// Base game manager for PS1
    /// </summary>
    public abstract class R1_PS1BaseManager : IGameManager
    {
        #region Values and paths

        /// <summary>
        /// The width of the tile set in tiles
        /// </summary>
        public abstract int TileSetWidth { get; }

        protected virtual PS1MemoryMappedFile.InvalidPointerMode InvalidPointerMode => PS1MemoryMappedFile.InvalidPointerMode.DevPointerXOR;

        /// <summary>
        /// Gets the name for the world
        /// </summary>
        /// <returns>The world name</returns>
        public virtual string GetWorldName(R1_World world)
        {
            switch (world)
            {
                case R1_World.Jungle:
                    return "JUN";
                case R1_World.Music:
                    return "MUS";
                case R1_World.Mountain:
                    return "MON";
                case R1_World.Image:
                    return "IMG";
                case R1_World.Cave:
                    return "CAV";
                case R1_World.Cake:
                    return "CAK";
                default:
                    throw new ArgumentOutOfRangeException(nameof(world), world, null);
            }
        }

        /// <summary>
        /// Gets the levels for each world
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The levels</returns>
        public abstract GameInfo_Volume[] GetLevels(GameSettings settings);

        public abstract string ExeFilePath { get; }
        public abstract uint? ExeBaseAddress { get; }

        #endregion

        #region Manager Methods

        /// <summary>
        /// Gets the available game actions
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The game actions</returns>
        public virtual GameAction[] GetGameActions(GameSettings settings)
        {
            return new GameAction[]
            {
                new GameAction("Export Sprites", false, true, (input, output) => ExportAllSpritesAsync(settings, output)),
                new GameAction("Export Animation Frames", false, true, (input, output) => ExportAllAnimationFramesAsync(settings, output)),
                new GameAction("Export Vignette", false, true, (input, output) => ExportVignetteTextures(settings, output)),
                new GameAction("Export Menu Sprites", false, true, (input, output) => ExportMenuSpritesAsync(settings, output, false)),
                new GameAction("Export Menu Animation Frames", false, true, (input, output) => ExportMenuSpritesAsync(settings, output, true)),
            };
        }

        /// <summary>
        /// Gets the tile set to use
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>The tile set to use</returns>
        public abstract Unity_MapTileMap GetTileSet(Context context);

        /// <summary>
        /// Fills the PS1 v-ram and returns it
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="mode">The blocks to fill</param>
        /// <returns>The filled v-ram</returns>
        protected abstract void FillVRAM(Context context, VRAMMode mode);

        /// <summary>
        /// Gets the sprite texture for an event
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="imgBuffer">The image buffer, if available</param>
        /// <param name="s">The image descriptor to use</param>
        /// <returns>The texture</returns>
        public virtual Texture2D GetSpriteTexture(Context context, byte[] imgBuffer, R1_ImageDescriptor s)
        {
            // Get the loaded v-ram
            PS1_VRAM vram = context.GetStoredObject<PS1_VRAM>("vram");

            // Get the image properties
            var width = s.Width;
            var height = s.Height;
            var texturePageInfo = s.TexturePageInfo;
            var paletteInfo = s.PaletteInfo;

            // see http://hitmen.c02.at/files/docs/psx/psx.pdf page 37
            int pageX = BitHelpers.ExtractBits(texturePageInfo, 4, 0);
            int pageY = BitHelpers.ExtractBits(texturePageInfo, 1, 4);
            int abr = BitHelpers.ExtractBits(texturePageInfo, 2, 5);
            int tp = BitHelpers.ExtractBits(texturePageInfo, 2, 7); // 0: 4-bit, 1: 8-bit, 2: 15-bit direct

            if (pageX < 5)
                return null;

            // Get palette coordinates
            int paletteX = BitHelpers.ExtractBits(paletteInfo, 6, 0);
            int paletteY = BitHelpers.ExtractBits(paletteInfo, 10, 6);

            //Debug.Log(paletteX + " - " + paletteY + " - " + pageX + " - " + pageY + " - " + tp);

            // Get the palette size
            var palette = tp == 0 ? new ARGB1555Color[16] : new ARGB1555Color[256];

            // Create the texture
            Texture2D tex = TextureHelpers.CreateTexture2D(width, height, clear: true);

            // Set every pixel
            if (tp == 1)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var paletteIndex = vram.GetPixel8(pageX, pageY, s.ImageOffsetInPageX + x, s.ImageOffsetInPageY + y);

                        // Get the color from the palette
                        if (palette[paletteIndex] == null)
                            palette[paletteIndex] = vram.GetColor1555(0, 0, paletteX * 16 + paletteIndex, paletteY);

                        // Set the pixel
                        tex.SetPixel(x, height - 1 - y, palette[paletteIndex].GetColor());
                    }
                }
            }
            else if (tp == 0)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int actualX = (s.ImageOffsetInPageX + x) / 2;
                        var paletteIndex = vram.GetPixel8(pageX, pageY, actualX, s.ImageOffsetInPageY + y);

                        if (x % 2 == 0)
                            paletteIndex = (byte)BitHelpers.ExtractBits(paletteIndex, 4, 0);
                        else
                            paletteIndex = (byte)BitHelpers.ExtractBits(paletteIndex, 4, 4);

                        // Get the color from the palette
                        if (palette[paletteIndex] == null)
                            palette[paletteIndex] = vram.GetColor1555(0, 0, paletteX * 16 + paletteIndex, paletteY);

                        // Set the pixel
                        tex.SetPixel(x, height - 1 - y, palette[paletteIndex].GetColor());
                    }
                }
            }

            // Apply the changes
            tex.Apply();

            // Return the texture
            return tex;
        }

        public virtual async UniTask LoadExtraFile(Context context, string path, bool recreateOnWrite) 
        {
            await FileSystem.PrepareFile(context.BasePath + path);

            if (!FileSystem.FileExists(context.BasePath + path))
                return;

            var exe = FileFactory.Read<R1_PS1_Executable>(ExeFilePath, context);
            var entry = exe.FileTable.FirstOrDefault(x => x.ProcessedFilePath == path);

            if (entry == null)
                throw new Exception($"No file entry found for path: {path}");

            PS1MemoryMappedFile file = new PS1MemoryMappedFile(context, entry.MemoryAddress, InvalidPointerMode)
            {
                filePath = path,
                Length = entry.FileSize,
                RecreateOnWrite = recreateOnWrite
            };
            context.AddFile(file);
        }

        /// <summary>
        /// Loads the specified level for the editor from the specified blocks
        /// </summary>
        /// <param name="context">The context</param>
        /// <param name="map">The map data</param>
        /// <param name="events">The events</param>
        /// <param name="eventLinkingTable">The event linking table</param>
        /// <param name="loadTextures">Indicates if textures should be loaded</param>
        /// <param name="bg">The background block data if available</param>
        /// <returns>The level</returns>
        public async UniTask<Unity_Level> LoadAsync(Context context, MapData map, R1_EventData[] events, ushort[] eventLinkingTable, bool loadTextures, R1_PS1_BackgroundBlock bg = null)
        {
            Unity_MapTileMap tileSet = GetTileSet(context);

            var eventDesigns = new List<Unity_ObjectManager_R1.DataContainer<Unity_ObjectManager_R1.DESData>>();
            var eventETA = new List<Unity_ObjectManager_R1.DataContainer<R1_EventState[][]>>();

            // Only load the v-ram if we're loading textures
            if (loadTextures)
                // Get the v-ram
                FillVRAM(context, VRAMMode.Level);

            // Load background sprites
            if (bg != null && loadTextures)
            {
                Unity_ObjGraphics finalDesign = new Unity_ObjGraphics
                {
                    Sprites = new List<Sprite>(),
                    Animations = new List<Unity_ObjAnimation>(),
                    FilePath = bg.Offset.file.filePath
                };

                // Get every sprite
                foreach (R1_ImageDescriptor i in bg.BackgroundLayerInfos)
                {
                    // Get the texture for the sprite, or null if not loading textures
                    Texture2D tex = GetSpriteTexture(context, null, i);

                    // Add it to the array
                    finalDesign.Sprites.Add(tex == null ? null : tex.CreateSprite());
                }

                // Add to the designs
                eventDesigns.Add(new Unity_ObjectManager_R1.DataContainer<Unity_ObjectManager_R1.DESData>(new Unity_ObjectManager_R1.DESData(finalDesign, bg.BackgroundLayerInfos, bg.BackgroundLayerInfos.First().Offset, null, null), bg.Offset));
            }

            // Load Rayman
            var rayman = GetRaymanEvent(context);

            // Load graphics
            foreach (R1_EventData e in (events ?? (events = new R1_EventData[0])).Append(rayman).Where(x => x != null))
            {
                // Add if not found
                if (e.ImageDescriptorsPointer != null && eventDesigns.All(x => x.Data.ImageDescriptorPointer != e.ImageDescriptorsPointer))
                {
                    Unity_ObjGraphics finalDesign = new Unity_ObjGraphics
                    {
                        Sprites = new List<Sprite>(),
                        Animations = new List<Unity_ObjAnimation>(),
                        FilePath = e.ImageDescriptorsPointer.file.filePath
                    };

                    // Get every sprite
                    foreach (R1_ImageDescriptor i in e.ImageDescriptors)
                    {
                        // Get the texture for the sprite, or null if not loading textures
                        Texture2D tex = loadTextures ? GetSpriteTexture(context, e.ImageBuffer, i) : null;

                        // Add it to the array
                        finalDesign.Sprites.Add(tex == null ? null : tex.CreateSprite());
                    }

                    // Add animations
                    finalDesign.Animations.AddRange(e.AnimDescriptors.Select(x => x.ToCommonAnimation()));

                    var desName = LevelEditorData.NameTable_R1PS1DES.TryGetItem(e.ImageDescriptorsPointer?.file.filePath)?.FindItem(x =>
                        x.Value.ImageDescriptors == e.ImageDescriptorsPointer?.AbsoluteOffset &&
                        x.Value.AnimationDescriptors == e.AnimDescriptorsPointer?.AbsoluteOffset &&
                        x.Value.ImageBuffer == e.ImageBufferPointer?.AbsoluteOffset).Key;

                    // Add to the designs
                    eventDesigns.Add(new Unity_ObjectManager_R1.DataContainer<Unity_ObjectManager_R1.DESData>(new Unity_ObjectManager_R1.DESData(finalDesign, e.ImageDescriptors, e.ImageDescriptorsPointer, e.AnimDescriptorsPointer, e.ImageBufferPointer), e.ImageDescriptorsPointer, name: desName));
                }

                // Add if not found
                if (e.ETAPointer != null && eventETA.All(x => x.PrimaryPointer != e.ETAPointer))
                {
                    var etaName = LevelEditorData.NameTable_R1PS1ETA.TryGetItem(e.ETAPointer.file.filePath)?.FindItem(x => x.Value == e.ETAPointer.AbsoluteOffset).Key;

                    // Add to the ETA
                    eventETA.Add(new Unity_ObjectManager_R1.DataContainer<R1_EventState[][]>(e.ETA.EventStates, e.ETAPointer, name: etaName));
                }
            }

            // Read tables from exe
            var exe = FileFactory.Read<R1_PS1_Executable>(ExeFilePath, context);

            var objManager = new Unity_ObjectManager_R1(
                context: context, 
                des: eventDesigns.ToArray(), 
                eta: eventETA.ToArray(), 
                linkTable: eventLinkingTable,
                typeZDC: exe?.TypeZDC,
                zdcData: exe?.ZDCData,
                eventFlags: exe?.EventFlags);

            await Controller.WaitIfNecessary();

            var maps = new Unity_Map[]
            {
                new Unity_Map()
                {
                    // Set the dimensions
                    Width = map.Width,
                    Height = map.Height,

                    // Create the tile array
                    TileSet = new Unity_MapTileMap[]
                    {
                        tileSet
                    },
                    TileSetWidth = TileSetWidth,
                    MapTiles = map.Tiles.Select(x => new Unity_Tile(x)).ToArray()
                }
            };

            // Initialize Rayman
            rayman?.InitRayman(events.FirstOrDefault(x => x.Type == R1_EventType.TYPE_RAY_POS));

            // Convert levelData to common level format
            Unity_Level level = new Unity_Level(maps, objManager, eventData: events.Select(e => new Unity_Object_R1(e, objManager)).Cast<Unity_Object>().ToList(), rayman: rayman != null ? new Unity_Object_R1(rayman, objManager) : null, localization: await LoadLocalizationAsync(context));

            await Controller.WaitIfNecessary();

            // Return the level
            return level;
        }

        public virtual uint? TypeZDCOffset => null;
        public virtual long TypeZDCCount => 256;
        public virtual uint? ZDCDataOffset => null;
        public virtual long ZDCDataCount => 200;
        public virtual uint? EventFlagsOffset => null;
        public virtual long EventFlagsCount => 256;

        public abstract FileTableInfo[] FileTableInfos { get; }

        public virtual R1_EventData GetRaymanEvent(Context context) => null;

        /// <summary>
        /// Loads the specified level for the editor
        /// </summary>
        /// <param name="context">The serialization context</param>
        /// <param name="loadTextures">Indicates if textures should be loaded</param>
        /// <returns>The level</returns>
        public abstract UniTask<Unity_Level> LoadAsync(Context context, bool loadTextures);

        /// <summary>
        /// Saves the specified level
        /// </summary>
        /// <param name="context">The serialization context</param>
        /// <param name="level">The level</param>
        public virtual UniTask SaveLevelAsync(Context context, Unity_Level level) => throw new NotImplementedException();

        /// <summary>
        /// Preloads all the necessary files into the context
        /// </summary>
        /// <param name="context">The serialization context</param>
        public virtual UniTask LoadFilesAsync(Context context)
        {
            // Load the exe
            return ExeBaseAddress == null
                ? (UniTask)context.AddLinearSerializedFileAsync(ExeFilePath, recreateOnWrite: false)
                : context.AddMemoryMappedFile(ExeFilePath, ExeBaseAddress.Value, recreateOnWrite: false);
        }

        /// <summary>
        /// Gets the base directory name for exporting a common design
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="des">The design to export</param>
        /// <returns>The base directory name</returns>
        protected abstract string GetExportDirName(GameSettings settings, Unity_ObjGraphics des);

        /// <summary>
        /// Exports every sprite from the game
        /// </summary>
        /// <param name="baseGameSettings">The game settings</param>
        /// <param name="outputDir">The output directory</param>
        /// <returns>The task</returns>
        public async UniTask ExportAllSpritesAsync(GameSettings baseGameSettings, string outputDir)
        {
            // TODO: Extract BigRay from INI

            // Keep track of the hash for every DES
            var hashList = new List<string>();

            // Keep track of the DES index for each file
            var desIndexes = new Dictionary<string, int>();

            // Enumerate every world
            foreach (var world in GetLevels(baseGameSettings).First().Worlds)
            {
                baseGameSettings.World = world.Index;

                // Enumerate every level
                foreach (var lvl in world.Maps)
                {
                    baseGameSettings.Level = lvl;

                    // Create the context
                    using (var context = new Context(baseGameSettings))
                    {
                        // Load the editor manager
                        var level = await LoadAsync(context, true);

                        // Set up animations
                        level.ObjManager.InitObjects(level);

                        var objManager = (Unity_ObjectManager_R1)level.ObjManager;

                        // Enumerate every design
                        foreach (var des in objManager.DES)
                        {
                            // Get the export dir name
                            var exportDirName = GetExportDirName(baseGameSettings, des.Data.Graphics);

                            if (!desIndexes.ContainsKey(exportDirName))
                                desIndexes.Add(exportDirName, 0);

                            var spriteIndex = -1;

                            // Enumerate every sprite
                            foreach (var sprite in des.Data.Graphics.Sprites.Where(x => x != null).Select(x => x.texture))
                            {
                                spriteIndex++;

                                // Get the png encoded data
                                var encodedData = sprite.EncodeToPNG();

                                // Check the hash
                                using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
                                {
                                    // Get the hash
                                    var hash = Convert.ToBase64String(sha1.ComputeHash(encodedData));

                                    // Check if it's been used before
                                    if (hashList.Contains(hash))
                                        continue;

                                    // Add to the hash list
                                    hashList.Add(hash);
                                }

                                // Export it
                                Util.ByteArrayToFile(Path.Combine(outputDir, $"{exportDirName}{desIndexes[exportDirName]} - {spriteIndex}.png"), encodedData);
                            }

                            desIndexes[exportDirName]++;
                        }
                    }

                    // Unload textures
                    await Resources.UnloadUnusedAssets();
                }
            }
        }

        /// <summary>
        /// Exports every animation frame from the game
        /// </summary>
        /// <param name="baseGameSettings">The game settings</param>
        /// <param name="outputDir">The output directory</param>
        /// <returns>The task</returns>
        public virtual async UniTask ExportAllAnimationFramesAsync(GameSettings baseGameSettings, string outputDir)
        {
            // Keep track of the hash for every DES
            var hashList = new List<string>();

            // Keep track of the DES index for each file
            var desIndexes = new Dictionary<string, int>();

            // Enumerate every world
            foreach (var world in GetLevels(baseGameSettings).First().Worlds)
            {
                baseGameSettings.World = world.Index;

                // Enumerate every level
                foreach (var lvl in world.Maps)
                {
                    baseGameSettings.Level = lvl;

                    // Create the context
                    using (var context = new Context(baseGameSettings))
                    {
                        // Load the level
                        var level = await LoadAsync(context, true);

                        var objManager = (Unity_ObjectManager_R1)level.ObjManager;

                        // Set up animations
                        objManager.InitObjects(level);

                        // Enumerate every design
                        for (var i = 0; i < objManager.DES.Length; i++)
                        {
                            var des = objManager.DES[i];
                            
                            // Check the hash
                            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
                            {
                                // Get the hash
                                var hash = Convert.ToBase64String(sha1.ComputeHash(des.Data.Graphics.Sprites
                                    .SelectMany(x => x?.texture?.GetRawTextureData() ?? new byte[0])
                                    .Append((byte) des.Data.Graphics.Animations.Count).ToArray()));

                                // Check if it's been used before
                                if (hashList.Contains(hash))
                                    continue;

                                // Add to the hash list
                                hashList.Add(hash);
                            }

                            // Get the export dir name
                            var exportDirName = GetExportDirName(baseGameSettings, des.Data.Graphics);

                            if (!desIndexes.ContainsKey(exportDirName))
                                desIndexes.Add(exportDirName, 0);

                            // Find all events where this DES is used
                            var matchingEvents = level.EventData.Append(level.Rayman).Cast<Unity_Object_R1>().Where(x => x.DESIndex == i);

                            // Find matching ETA for this DES from the level events
                            var matchingStates = matchingEvents.SelectMany(lvlEvent => objManager.ETA[lvlEvent.ETAIndex].Data.SelectMany(x => x)).ToArray();

                            // Get the textures
                            var textures = des.Data.Graphics.Sprites?.Select(x => x?.texture).ToArray() ?? new Texture2D[0];

                            ExportAnimationFrames(textures, des.Data.Graphics.Animations.ToArray(),
                                Path.Combine(outputDir, $"{exportDirName}{desIndexes[exportDirName]}"), matchingStates);

                            // Unload textures
                            await Resources.UnloadUnusedAssets();

                            desIndexes[exportDirName]++;
                        }
                    }
                }
            }
        }

        public void ExportAnimationFrames(Texture2D[] textures, Unity_ObjAnimation[] spriteAnim, string outputDir, R1_EventState[] matchingStates)
        {
            // Enumerate the animations
            for (var j = 0; j < spriteAnim.Length; j++)
            {
                // Get the animation descriptor
                var anim = spriteAnim[j];

                // Get the speed
                var speed = String.Join("-", matchingStates.Where(x => x.AnimationIndex == j).Select(x => x.AnimationSpeed).Distinct());

                // Get the folder
                var animFolderPath = Path.Combine(outputDir, $"{j}-{speed}");

                int? frameWidth = null;
                int? frameHeight = null;

                var layersPerFrame = anim.Frames.First().SpriteLayers.Length;
                var frameCount = anim.Frames.Length;

                for (int dummyFrame = 0; dummyFrame < frameCount; dummyFrame++)
                {
                    for (int dummyLayer = 0; dummyLayer < layersPerFrame; dummyLayer++)
                    {
                        var l = anim.Frames[dummyFrame].SpriteLayers[dummyLayer];

                        if (l.ImageIndex < textures.Length)
                        {
                            var s = textures[l.ImageIndex];

                            if (s != null)
                            {
                                var w = s.width + l.XPosition;
                                var h = s.height + l.YPosition;

                                if (frameWidth == null || frameWidth < w)
                                    frameWidth = w;

                                if (frameHeight == null || frameHeight < h)
                                    frameHeight = h;
                            }
                        }
                    }
                }

                // Create each animation frame
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    var tex = TextureHelpers.CreateTexture2D(frameWidth ?? 1, frameHeight ?? 1, clear: true);

                    bool hasLayers = false;

                    // Write each layer
                    for (var layerIndex = 0; layerIndex < layersPerFrame; layerIndex++)
                    {
                        var animationLayer = anim.Frames[frameIndex].SpriteLayers[layerIndex];

                        if (animationLayer.ImageIndex >= textures.Length)
                            continue;

                        // Get the sprite
                        var sprite = textures[animationLayer.ImageIndex];

                        if (sprite == null)
                            continue;

                        // Set every pixel
                        for (int y = 0; y < sprite.height; y++)
                        {
                            for (int x = 0; x < sprite.width; x++)
                            {
                                var c = sprite.GetPixel(x, sprite.height - y - 1);

                                var xPosition = (animationLayer.IsFlippedHorizontally ? (sprite.width - 1 - x) : x) + animationLayer.XPosition;
                                var yPosition = (animationLayer.IsFlippedVertically ? (sprite.height - 1 - y) : y) + animationLayer.YPosition;

                                if (xPosition >= tex.width)
                                    throw new Exception("Horizontal overflow!");

                                if (c.a != 0)
                                    tex.SetPixel(xPosition, tex.height - 1 - yPosition, c);
                            }
                        }

                        hasLayers = true;
                    }

                    tex.Apply();

                    if (!hasLayers)
                        continue;

                    // Save the file
                    Util.ByteArrayToFile(Path.Combine(animFolderPath, $"{frameIndex}.png"), tex.EncodeToPNG());
                }
            }
        }

        /// <summary>
        /// Gets the vignette file info
        /// </summary>
        /// <returns>The vignette file info</returns>
        protected abstract PS1VignetteFileInfo[] GetVignetteInfo();

        /// <summary>
        /// Exports all vignette textures to the specified output directory
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="outputDir">The output directory</param>
        public virtual void ExportVignetteTextures(GameSettings settings, string outputDir)
        {
            // Create the context
            using (var context = new Context(settings))
            {
                // Enumerate every file
                foreach (var fileInfo in GetVignetteInfo().Where(x => File.Exists(settings.GameDirectory + x.FilePath)))
                {
                    // Add the file to the context
                    context.AddFile(new LinearSerializedFile(context)
                    {
                        filePath = fileInfo.FilePath
                    });

                    // Get the textures
                    var textures = new List<Texture2D>();

                    if (fileInfo.FileType == VignetteFileType.Raw16)
                    {
                        // Read the raw data
                        var rawData = FileFactory.Read<ObjectArray<ARGB1555Color>>(fileInfo.FilePath, context, onPreSerialize: (s, x) => x.Length = s.CurrentLength / 2);

                        // Create the texture
                        textures.Add(TextureHelpers.CreateTexture2D(fileInfo.Width, (int)(rawData.Length / fileInfo.Width)));

                        // Set the pixels
                        for (int y = 0; y < textures.First().height; y++)
                        {
                            for (int x = 0; x < textures.First().width; x++)
                            {
                                var c = rawData.Value[y * textures.First().width + x];
                                c.Alpha = Byte.MaxValue;
                                textures.First().SetPixel(x, textures.First().height - y - 1, c.GetColor());
                            }
                        }
                    }
                    else if (fileInfo.FileType == VignetteFileType.MultiXXX)
                    {
                        // Read the data
                        var multiData = FileFactory.Read<R1_PS1_MultiVignetteFile>(fileInfo.FilePath, context);

                        // Get the textures
                        for (int i = 0; i < multiData.ImageBlocks.Length; i++)
                        {
                            // Create the texture
                            var tex = TextureHelpers.CreateTexture2D(fileInfo.Widths[i], (int)(multiData.ImageBlocks[i].Length / fileInfo.Widths[i]));

                            // Set the pixels
                            for (int y = 0; y < tex.height; y++)
                            {
                                for (int x = 0; x < tex.width; x++)
                                {
                                    var c = multiData.ImageBlocks[i].Value[y * tex.width + x];
                                    c.Alpha = Byte.MaxValue;
                                    tex.SetPixel(x, tex.height - y - 1, c.GetColor());
                                }
                            }

                            // Add the texture
                            textures.Add(tex);
                        }
                    }
                    else
                    {
                        R1_PS1_VignetteBlockGroup imageBlock;

                        // Get the block
                        if (fileInfo.FileType == VignetteFileType.BlockedXXX)
                            imageBlock = FileFactory.Read<R1_PS1_BackgroundVignetteFile>(fileInfo.FilePath, context).ImageBlock;
                        else
                            imageBlock = FileFactory.Read<R1_PS1_VignetteBlockGroup>(fileInfo.FilePath, context, onPreSerialize: (s, x) => x.BlockGroupSize = (int)(s.CurrentLength / 2));

                        // Create the texture
                        textures.Add(TextureHelpers.CreateTexture2D(imageBlock.Width, imageBlock.Height));

                        // Get the block width
                        var blockWdith = imageBlock.GetBlockWidth(context.Settings.EngineVersion);

                        // Write each block
                        for (int blockIndex = 0; blockIndex < imageBlock.ImageBlocks.Length; blockIndex++)
                        {
                            // Get the block data
                            var blockData = imageBlock.ImageBlocks[blockIndex];

                            // Write the block
                            for (int y = 0; y < imageBlock.Height; y++)
                            {
                                for (int x = 0; x < blockWdith; x++)
                                {
                                    // Get the color
                                    var c = blockData[x + (y * blockWdith)];

                                    c.Alpha = Byte.MaxValue;

                                    // Set the pixel
                                    textures.First().SetPixel((x + (blockIndex * blockWdith)), textures.First().height - y - 1, c.GetColor());
                                }
                            }
                        }
                    }

                    // Apply the pixels
                    textures.ForEach(x => x.Apply());

                    // Write the textures
                    if (textures.Count == 1)
                    {
                        // Get the output file path
                        var outputPath = Path.Combine(outputDir, FileSystem.ChangeFilePathExtension(fileInfo.FilePath, ".png"));

                        // Create the directory
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                        // Write the texture
                        File.WriteAllBytes(outputPath, textures.First().EncodeToPNG());
                    }
                    else
                    {
                        var index = 0;

                        foreach (var tex in textures)
                        {
                            // Get the output file path
                            var outputPath = Path.Combine(outputDir, FileSystem.ChangeFilePathExtension(fileInfo.FilePath, $" - {index}.png"));

                            // Create the directory
                            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                            // Write the texture
                            File.WriteAllBytes(outputPath, tex.EncodeToPNG());

                            index++;
                        }
                    }
                }
            }
        }

        protected virtual UniTask<IReadOnlyDictionary<string, string[]>> LoadLocalizationAsync(Context context) => UniTask.FromResult<IReadOnlyDictionary<string, string[]>>(null);

        public abstract UniTask ExportMenuSpritesAsync(GameSettings settings, string outputPath, bool exportAnimFrames);

        protected async UniTask ExportMenuSpritesAsync(Context menuContext, Context bigRayContext, string outputPath, bool exportAnimFrames, R1_PS1_FontData[] fontData, R1_EventData[] fixEvents, R1_PS1_BigRayBlock bigRay)
        {
            // Fill the v-ram for each context
            FillVRAM(menuContext, VRAMMode.Menu);

            if (bigRayContext != null)
                FillVRAM(bigRayContext, VRAMMode.BigRay);

            // Export each font DES
            if (!exportAnimFrames)
            {
                for (int fontIndex = 0; fontIndex < fontData.Length; fontIndex++)
                {
                    // Export every sprite
                    for (int spriteIndex = 0; spriteIndex < fontData[fontIndex].ImageDescriptorsCount; spriteIndex++)
                    {
                        // Get the sprite texture
                        var tex = GetSpriteTexture(menuContext, fontData[fontIndex].ImageBuffer, fontData[fontIndex].ImageDescriptors[spriteIndex]);

                        // Make sure it's not null
                        if (tex == null)
                            continue;

                        // Export the font sprite
                        Util.ByteArrayToFile(Path.Combine(outputPath, "Font", $"{fontIndex} - {spriteIndex}.png"), tex.EncodeToPNG());
                    }
                }
            }

            // Export menu sprites from allfix
            var exportedImgDescr = new List<Pointer>();
            var index = 0;

            foreach (R1_EventData t in fixEvents)
            {
                if (exportedImgDescr.Contains(t.ImageDescriptorsPointer))
                    continue;

                exportedImgDescr.Add(t.ImageDescriptorsPointer);

                await ExportEventSpritesAsync(menuContext, t, Path.Combine(outputPath, "Menu"), index);

                index++;
            }

            // Export BigRay
            if (bigRay != null)
                await ExportEventSpritesAsync(bigRayContext, bigRay.BigRay, Path.Combine(outputPath, "BigRay"), 0);

            async UniTask ExportEventSpritesAsync(Context context, R1_EventData e, string eventOutputDir, int desIndex)
            {
                var sprites = e.ImageDescriptors.Select(x => GetSpriteTexture(context, e.ImageBuffer, x)).ToArray();

                if (!exportAnimFrames)
                {
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (sprites[i] == null)
                            continue;

                        Util.ByteArrayToFile(Path.Combine(eventOutputDir, $"{desIndex} - {i}.png"), sprites[i].EncodeToPNG());
                    }
                }
                else
                {
                    // Enumerate the animations
                    for (var j = 0; j < e.AnimDescriptors.Length; j++)
                    {
                        // Get the animation descriptor
                        var anim = e.AnimDescriptors[j];

                        // Get the speed
                        var speed = String.Join("-", e.ETA.EventStates.SelectMany(x => x).Where(x => x.AnimationIndex == j).Select(x => x.AnimationSpeed).Distinct());

                        // Get the folder
                        var animFolderPath = Path.Combine(eventOutputDir, desIndex.ToString(), $"{j}-{speed}");

                        int? frameWidth = null;
                        int? frameHeight = null;

                        for (int dummyFrame = 0; dummyFrame < anim.FrameCount; dummyFrame++)
                        {
                            for (int dummyLayer = 0; dummyLayer < anim.LayersPerFrame; dummyLayer++)
                            {
                                var l = anim.Layers[dummyFrame * anim.LayersPerFrame + dummyLayer];

                                if (l.ImageIndex < sprites.Length)
                                {
                                    var s = sprites[l.ImageIndex];

                                    if (s != null)
                                    {
                                        var w = s.width + l.XPosition;
                                        var h = s.height + l.YPosition;

                                        if (frameWidth == null || frameWidth < w)
                                            frameWidth = w;

                                        if (frameHeight == null || frameHeight < h)
                                            frameHeight = h;
                                    }
                                }
                            }
                        }

                        // Create each animation frame
                        for (int frameIndex = 0; frameIndex < anim.FrameCount; frameIndex++)
                        {
                            Texture2D tex = TextureHelpers.CreateTexture2D(frameWidth ?? 1, frameHeight ?? 1, clear: true);

                            bool hasLayers = false;

                            // Write each layer
                            for (var layerIndex = 0; layerIndex < anim.LayersPerFrame; layerIndex++)
                            {
                                var animationLayer = anim.Layers[frameIndex * anim.LayersPerFrame + layerIndex];

                                if (animationLayer.ImageIndex >= sprites.Length)
                                    continue;

                                // Get the sprite
                                var sprite = sprites[animationLayer.ImageIndex];

                                if (sprite == null)
                                    continue;

                                // Set every pixel
                                for (int y = 0; y < sprite.height; y++)
                                {
                                    for (int x = 0; x < sprite.width; x++)
                                    {
                                        var c = sprite.GetPixel(x, sprite.height - y - 1);

                                        var xPosition = (animationLayer.IsFlippedHorizontally ? (sprite.width - 1 - x) : x) + animationLayer.XPosition;
                                        var yPosition = (animationLayer.IsFlippedVertically ? (sprite.height - 1 - y) : y) + animationLayer.YPosition;

                                        if (xPosition >= tex.width)
                                            throw new Exception("Horizontal overflow!");

                                        if (c.a != 0)
                                            tex.SetPixel(xPosition, tex.height - 1 - yPosition, c);
                                    }
                                }

                                hasLayers = true;
                            }

                            tex.Apply();

                            if (!hasLayers)
                                continue;

                            // Save the file
                            Util.ByteArrayToFile(Path.Combine(animFolderPath, $"{frameIndex}.png"), tex.EncodeToPNG());
                        }
                    }
                }

                // Unload textures
                await Resources.UnloadUnusedAssets();
            }
        }

        #endregion

        #region Value Types

        protected class PS1VignetteFileInfo
        {
            public PS1VignetteFileInfo(string filePath, int width = 0)
            {
                FilePath = filePath;
                Width = width;

                if (width != 0)
                    FileType = VignetteFileType.Raw16;
                else if (filePath.EndsWith(".XXX", StringComparison.InvariantCultureIgnoreCase))
                    FileType = VignetteFileType.BlockedXXX;
                else
                    FileType = VignetteFileType.Blocked;
            }

            public PS1VignetteFileInfo(string filePath, params int[] widths)
            {
                FilePath = filePath;
                Widths = widths;
                FileType = VignetteFileType.MultiXXX;
            }

            public VignetteFileType FileType { get; }

            public string FilePath { get; }

            public int Width { get; }

            public int[] Widths { get; }
        }

        /// <summary>
        /// The available vignette file types
        /// </summary>
        protected enum VignetteFileType
        {
            Raw16,
            Blocked,
            BlockedXXX,
            MultiXXX
        }

        protected enum VRAMMode
        {
            Level,
            Menu,
            BigRay
        }

        public class FileTableInfo
        {
            public FileTableInfo(uint offset, uint count, string name)
            {
                Offset = offset;
                Count = count;
                Name = name;
            }

            public uint Offset { get; }
            public uint Count { get; }
            public string Name { get; }
        }

        #endregion
    }
}