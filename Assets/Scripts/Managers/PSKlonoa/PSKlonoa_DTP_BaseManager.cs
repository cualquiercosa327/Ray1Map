﻿using BinarySerializer;
using BinarySerializer.PS1;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BinarySerializer.Klonoa;
using BinarySerializer.Klonoa.DTP;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace R1Engine
{
    public abstract class PSKlonoa_DTP_BaseManager : BaseGameManager
    {
        public override GameInfo_Volume[] GetLevels(GameSettings settings) => GameInfo_Volume.SingleVolume(Levels.Select((x, i) => new GameInfo_World(i, x.Item1, Enumerable.Range(0, x.Item2).ToArray())).ToArray());

        public virtual (string, int)[] Levels => new (string, int)[]
        {
            ("FIX", 0),
            ("MENU", 0),
            ("CODE", 0),

            ("Vision 1-1", 3),
            ("Vision 1-2", 5),
            ("Rongo Lango", 2),

            ("Vision 2-1", 4),
            ("Vision 2-2", 6),
            ("Pamela", 2),

            ("Vision 3-1", 5),
            ("Vision 3-2", 10),
            ("Gelg Bolm", 1),

            ("Vision 4-1", 3), // TODO: 5 in proto
            ("Vision 4-2", 8),
            ("Baladium", 2),

            ("Vision 5-1", 7),
            ("Vision 5-2", 9),
            ("Joka", 1),

            ("Vision 6-1", 8),
            ("Vision 6-2", 8),
            ("", 2),
            ("", 2), // TODO: 1 in proto
            ("", 3),
            ("", 3),
            ("Klonoa's Grand Gale Strategy", 9),
        };

        public override GameAction[] GetGameActions(GameSettings settings)
        {
            return new GameAction[]
            {
                new GameAction("Extract BIN", false, true, (input, output) => Extract_BINAsync(settings, output, false)),
                new GameAction("Extract BIN (unpack archives)", false, true, (input, output) => Extract_BINAsync(settings, output, true)),
                new GameAction("Extract Code", false, true, (input, output) => Extract_CodeAsync(settings, output)),
                new GameAction("Extract Graphics", false, true, (input, output) => Extract_GraphicsAsync(settings, output)),
                new GameAction("Extract Backgrounds", false, true, (input, output) => Extract_BackgroundsAsync(settings, output)),
                new GameAction("Extract Cutscenes", false, true, (input, output) => Extract_Cutscenes(settings, output)),
            };
        }

        public async UniTask Extract_BINAsync(GameSettings settings, string outputPath, bool unpack)
        {
            using var context = new R1Context(settings);
            var config = GetLoaderConfig(settings);
            await LoadFilesAsync(context, config);

            // Load the IDX
            var idxData = Load_IDX(context, config);

            var s = context.Deserializer;

            var loader = Loader_DTP.Create(context, idxData, config);

            var archiveDepths = new Dictionary<IDXLoadCommand.FileType, int>()
            {
                [IDXLoadCommand.FileType.Unknown] = 0,

                [IDXLoadCommand.FileType.Archive_TIM_Generic] = 1,
                [IDXLoadCommand.FileType.Archive_TIM_SongsText] = 1,
                [IDXLoadCommand.FileType.Archive_TIM_SaveText] = 1,
                [IDXLoadCommand.FileType.Archive_TIM_SpriteSheets] = 1,

                [IDXLoadCommand.FileType.OA05] = 0,
                [IDXLoadCommand.FileType.SEQ] = 0,

                [IDXLoadCommand.FileType.Archive_BackgroundPack] = 2,

                [IDXLoadCommand.FileType.FixedSprites] = 1,
                [IDXLoadCommand.FileType.Archive_SpritePack] = 1,
                [IDXLoadCommand.FileType.Archive_LevelMenuSprites] = 2,
                
                [IDXLoadCommand.FileType.Archive_LevelPack] = 1,

                [IDXLoadCommand.FileType.Archive_WorldMap] = 1,
                
                [IDXLoadCommand.FileType.Archive_MenuSprites] = 2,
                [IDXLoadCommand.FileType.Proto_Archive_MenuSprites_0] = 1,
                [IDXLoadCommand.FileType.Proto_Archive_MenuSprites_1] = 1,
                [IDXLoadCommand.FileType.Proto_Archive_MenuSprites_2] = 1,
                [IDXLoadCommand.FileType.Font] = 0,
                [IDXLoadCommand.FileType.Archive_MenuBackgrounds] = 2,
                
                [IDXLoadCommand.FileType.Archive_Unk0] = 1,
                [IDXLoadCommand.FileType.Unk1] = 0,

                [IDXLoadCommand.FileType.Code] = 0,
                [IDXLoadCommand.FileType.CodeNoDest] = 0,
            };

            // Enumerate every entry
            for (var blockIndex = 0; blockIndex < idxData.Entries.Length; blockIndex++)
            {
                loader.SwitchBlocks(blockIndex);

                // Process each BIN file
                loader.LoadBINFiles((cmd, i) =>
                {
                    var type = cmd.FILE_Type;

                    if (unpack)
                    {
                        var archiveDepth = archiveDepths[type];

                        if (archiveDepth > 0)
                        {
                            // Be lazy and hard-code instead of making some recursive loop
                            if (archiveDepth == 1)
                            {
                                var archive = loader.LoadBINFile<RawData_ArchiveFile>(i);

                                for (int j = 0; j < archive.Files.Length; j++)
                                {
                                    var file = archive.Files[j];

                                    Util.ByteArrayToFile(Path.Combine(outputPath, $"{blockIndex}", $"{i} ({type})", $"{j}.bin"), file.Data);
                                }
                            }
                            else if (archiveDepth == 2)
                            {
                                var archives = loader.LoadBINFile<ArchiveFile<RawData_ArchiveFile>>(i);

                                for (int a = 0; a < archives.Files.Length; a++)
                                {
                                    var archive = archives.Files[a];

                                    for (int j = 0; j < archive.Files.Length; j++)
                                    {
                                        var file = archive.Files[j];

                                        Util.ByteArrayToFile(Path.Combine(outputPath, $"{blockIndex}", $"{i} ({type})", $"{a}_{j}.bin"), file.Data);
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception($"Unsupported archive depth");
                            }

                            return;
                        }
                    }

                    // Read the raw data
                    var data = s.SerializeArray<byte>(null, cmd.FILE_Length);

                    Util.ByteArrayToFile(Path.Combine(outputPath, $"{blockIndex}", $"{i} ({type})", $"Data.bin"), data);
                });
            }
        }

        public async UniTask Extract_CodeAsync(GameSettings settings, string outputPath)
        {
            using var context = new R1Context(settings);
            var config = GetLoaderConfig(settings);
            await LoadFilesAsync(context, config);

            // Load the IDX
            var idxData = Load_IDX(context, config);

            var loader = Loader_DTP.Create(context, idxData, config);

            // Enumerate every entry
            for (var blockIndex = 0; blockIndex < idxData.Entries.Length; blockIndex++)
            {
                loader.SwitchBlocks(blockIndex);

                // Process each BIN file
                loader.LoadBINFiles((cmd, i) =>
                {
                    // TODO: Also export no-destination code blocks

                    if (cmd.FILE_Type != IDXLoadCommand.FileType.Code)
                        return;
                    
                    var codeFile = loader.LoadBINFile<RawData_File>(i);

                    Util.ByteArrayToFile(Path.Combine(outputPath, $"{blockIndex} - {i} - 0x{cmd.FILE_Destination:X8}.dat"), codeFile.Data);
                });
            }
        }

        public async UniTask Extract_GraphicsAsync(GameSettings settings, string outputPath)
        {
            using var context = new R1Context(settings);
            var config = GetLoaderConfig(settings);
            await LoadFilesAsync(context, config);

            // Load the IDX
            var idxData = Load_IDX(context, config);

            // Create the loader
            var loader = Loader_DTP.Create(context, idxData, config);

            // Enumerate every bin block
            for (var blockIndex = 0; blockIndex < idxData.Entries.Length; blockIndex++)
            {
                // Load the BIN
                loader.SwitchBlocks(blockIndex);
                loader.LoadAndProcessBINBlock();

                // WORLD MAP SPRITES
                var wldMap = loader.GetLoadedFile<WorldMap_ArchiveFile>();
                if (wldMap != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (i == 1)
                            loader.AddToVRAM(wldMap.Palette2);

                        // Enumerate every frame
                        for (int frameIndex = 0; frameIndex < wldMap.AnimatedSprites.Sprites.Files.Length - 1; frameIndex++)
                            exportSprite(wldMap.AnimatedSprites.Sprites.Files[frameIndex].Textures, $"{blockIndex} - WorldMapSprites", i, frameIndex, -1, -1); // TODO: Correct pal
                    }
                }

                // WORLD MAP TEXTURES
                for (var i = 0; i < wldMap?.SpriteSheets.Files.Length; i++)
                {
                    var tim = wldMap.SpriteSheets.Files[i];

                    exportTex(
                        getTex: () => GetTexture(tim),
                        blockName: $"{blockIndex} - WorldMapTextures",
                        name: $"{i}");
                }

                // MENU SPRITES
                var menuSprites = loader.GetLoadedFile<MenuSprites_ArchiveFile>();

                var sprites_0 = menuSprites?.Sprites_0;
                var sprites_1 = menuSprites?.Sprites_1;
                var sprites_2 = menuSprites?.Sprites_2;

                if (loader.GameVersion == LoaderConfiguration.GameVersion.DTP_Prototype_19970717)
                {
                    for (int fileIndex = 0; fileIndex < loader.LoadedFiles[blockIndex].Length; fileIndex++)
                    {
                        switch (loader.IDX.Entries[blockIndex].LoadCommands[fileIndex].FILE_Type)
                        {
                            case IDXLoadCommand.FileType.Proto_Archive_MenuSprites_0:
                                sprites_0 = (Sprites_ArchiveFile)loader.LoadedFiles[blockIndex][fileIndex];
                                break;

                            case IDXLoadCommand.FileType.Proto_Archive_MenuSprites_1:
                                sprites_1 = (Sprites_ArchiveFile)loader.LoadedFiles[blockIndex][fileIndex];
                                break;

                            case IDXLoadCommand.FileType.Proto_Archive_MenuSprites_2:
                                sprites_2 = (Sprites_ArchiveFile)loader.LoadedFiles[blockIndex][fileIndex];
                                break;
                        }
                    }
                }

                for (int frameIndex = 0; frameIndex < sprites_0?.Files.Length - 1; frameIndex++)
                    exportSprite(sprites_0.Files[frameIndex].Textures, $"{blockIndex} - Menu", 0, frameIndex, 960, 442);

                for (int frameIndex = 0; frameIndex < sprites_1?.Files.Length - 1; frameIndex++)
                    exportSprite(sprites_1.Files[frameIndex].Textures, $"{blockIndex} - Menu", 1, frameIndex, 0, 480);

                for (int frameIndex = 0; frameIndex < sprites_2?.Files.Length - 1; frameIndex++)
                    exportSprite(sprites_2.Files[frameIndex].Textures, $"{blockIndex} - Menu", 2, frameIndex, 960, 440);

                for (int frameIndex = 0; frameIndex < menuSprites?.AnimatedSprites.Sprites.Files.Length - 1; frameIndex++)
                {
                    int palY;

                    if (frameIndex < 120)
                        palY = 490;
                    else if (frameIndex < 166)
                        palY = 480;
                    else if (frameIndex < 178)
                        palY = 490;
                    else if (frameIndex < 182)
                        palY = -1; // TODO: Correct pal
                    else
                        palY = 480;

                    exportSprite(menuSprites.AnimatedSprites.Sprites.Files[frameIndex].Textures, $"{blockIndex} - Menu", 3, frameIndex, 0, palY);
                }

                // MENU BACKGROUND TEXTURES
                var menuBg = loader.GetLoadedFile<ArchiveFile<TIM_ArchiveFile>>();
                for (var i = 0; i < menuBg?.Files.Length; i++)
                {
                    var tims = menuBg.Files[i];

                    for (var j = 0; j < tims.Files.Length; j++)
                    {
                        var tim = tims.Files[j];
                        exportTex(
                            getTex: () => GetTexture(tim, onlyFirstTransparent: true),
                            blockName: $"{blockIndex} - MenuBackgrounds",
                            name: $"{i} - {j}");
                    }
                }

                // TIM TEXTURES
                for (int fileIndex = 0; fileIndex < loader.LoadedFiles[blockIndex].Length; fileIndex++)
                {
                    if (!(loader.LoadedFiles[blockIndex][fileIndex] is TIM_ArchiveFile timArchive)) 
                        continue;
                    
                    for (var i = 0; i < timArchive.Files.Length; i++)
                    {
                        var tim = timArchive.Files[i];
                        exportTex(
                            getTex: () => GetTexture(tim),
                            blockName:
                            $"{blockIndex} - {loader.IDX.Entries[blockIndex].LoadCommands[fileIndex].FILE_Type.ToString().Replace("Archive_TIM_", String.Empty)}",
                            name: $"{fileIndex} - {i}");
                    }
                }

                // BACKGROUND TEXTURES
                var bgPack = loader.BackgroundPack;
                for (var i = 0; i < bgPack?.TIMFiles.Files.Length; i++)
                {
                    var tim = bgPack.TIMFiles.Files[i];
                    exportTex(
                        getTex: () => GetTexture(tim, noPal: true),
                        blockName: $"{blockIndex} - Background",
                        name: $"{i}");
                }

                // SPRITE SETS
                for (int spriteSetIndex = 0; spriteSetIndex < loader.SpriteSets.Length; spriteSetIndex++)
                {
                    var spriteSet = loader.SpriteSets[spriteSetIndex];

                    if (spriteSet == null)
                        continue;

                    // Enumerate every sprite
                    for (int spriteIndex = 0; spriteIndex < spriteSet.Files.Length - 1; spriteIndex++)
                        exportSprite(spriteSet.Files[spriteIndex].Textures, $"{blockIndex} - Sprites", spriteSetIndex, spriteIndex, 0, 500);
                }

                // SPRITE SET PLAYER SPRITES
                var spritePack = loader.GetLoadedFile<LevelSpritePack_ArchiveFile>();
                if (spritePack != null)
                {
                    var exportedPlayerSprites = new HashSet<PlayerSprite_File>();

                    var pal = spritePack.PlayerSprites.Files.FirstOrDefault(x => x?.TIM?.Clut != null)?.TIM.Clut.Palette.Select(x => x.GetColor()).ToArray();

                    for (var i = 0; i < spritePack.PlayerSprites.Files.Length; i++)
                    {
                        var file = spritePack.PlayerSprites.Files[i];
                        if (file != null && !exportedPlayerSprites.Contains(file))
                        {
                            exportedPlayerSprites.Add(file);

                            exportTex(() =>
                            {
                                if (file.TIM != null)
                                    return GetTexture(file.TIM, palette: pal);
                                else
                                    return GetTexture(file.Raw_ImgData, pal, file.Raw_Width, file.Raw_Height,
                                        PS1_TIM.TIM_ColorFormat.BPP_8);
                            }, $"{blockIndex} - PlayerSprites", $"{i}");
                        }
                    }
                }

                // LEVEL MENU SPRITES
                var lvlMenuSprites = loader.GetLoadedFile<LevelMenuSprites_ArchiveFile>();
                if (lvlMenuSprites != null)
                {
                    for (int frameIndex = 0; frameIndex < lvlMenuSprites.Sprites_0.Files.Length; frameIndex++)
                        exportSprite(lvlMenuSprites.Sprites_0.Files[frameIndex].Textures, $"{blockIndex} - Menu", 0, frameIndex, 960, 442);

                    for (int frameIndex = 0; frameIndex < lvlMenuSprites.Sprites_1.Files.Length; frameIndex++)
                        exportSprite(lvlMenuSprites.Sprites_1.Files[frameIndex].Textures, $"{blockIndex} - Menu", 1, frameIndex, -1, -1); // TODO: Correct pal
                }

                // CUTSCENE SPRITES
                var cutscenePack = loader.LevelPack?.CutscenePack;
                if (cutscenePack?.Sprites != null)
                {
                    for (int frameIndex = 0; frameIndex < cutscenePack.Sprites.Files.Length - 1; frameIndex++)
                        exportSprite(cutscenePack.Sprites.Files[frameIndex].Textures, $"{blockIndex} - CutsceneSprites", 0, frameIndex, 0, 500);
                }

                // CUTSCENE CHARACTER NAMES
                if (cutscenePack?.CharacterNamesImgData != null)
                    exportTex(
                        getTex: () => GetTexture(imgData: cutscenePack.CharacterNamesImgData.Data, pal: null, width: 0x0C, height: 0x50, colorFormat: PS1_TIM.TIM_ColorFormat.BPP_4), 
                        blockName: $"{blockIndex} - CutsceneCharacterNames",
                        name: $"0");

                // CUTSCENE PLAYER SPRITES
                if (cutscenePack?.PlayerFramesImgData != null)
                {
                    var playerPal = loader.VRAM.GetColors1555(0, 0, 160, 511, 256).Select(x => x.GetColor()).ToArray();

                    for (var i = 0; i < cutscenePack.PlayerFramesImgData.Files.Length; i++)
                    {
                        var file = cutscenePack.PlayerFramesImgData.Files[i];
                        exportTex(
                            getTex: () => GetTexture(file.ImgData, playerPal, file.Width, file.Height,
                                PS1_TIM.TIM_ColorFormat.BPP_8), 
                            blockName: $"{blockIndex} - CutscenePlayerSprites",
                            name: $"{i}");
                    }
                }

                // CUTSCENE ANIMATIONS
                if (cutscenePack?.SpriteAnimations != null)
                {
                    var playerPal = loader.VRAM.GetColors1555(0, 0, 160, 511, 256).Select(x => x.GetColor()).ToArray();

                    for (var i = 0; i < cutscenePack.SpriteAnimations.Animations.Length; i++)
                    {
                        var anim = cutscenePack.SpriteAnimations.Animations[i];

                        var isPlayerAnim = (cutscenePack.Cutscenes.SelectMany(x => x.Cutscene_Normal.Instructions).FirstOrDefault(x => x.Type == CutsceneInstruction.InstructionType.SetObjAnimation && ((CutsceneInstructionData_SetObjAnimation)x.Data).AnimIndex == i)?.Data as CutsceneInstructionData_SetObjAnimation)?.ObjIndex == 0;

                        var animFrames = GetAnimationFrames(
                            loader: loader, 
                            anim: anim, 
                            sprites: cutscenePack.Sprites, 
                            palX: 0, 
                            palY: 500, 
                            isCutscenePlayer: isPlayerAnim, 
                            playerSprites: cutscenePack.PlayerFramesImgData.Files, 
                            playerPalette: playerPal);

                        if (animFrames == null)
                            continue;

                        Util.ExportAnimAsGif(
                            frames: animFrames, 
                            speeds: anim.Frames.Select(x => (int)x.FrameDelay).ToArray(), 
                            center: true, 
                            trim: false, 
                            filePath: Path.Combine(outputPath, $"{blockIndex} - CutsceneAnimations", $"{i}.gif"));
                    }
                }

                PaletteHelpers.ExportVram(Path.Combine(outputPath, $"VRAM_{blockIndex}.png"), loader.VRAM);
            }

            void exportTex(Func<Texture2D> getTex, string blockName, string name)
            {
                try
                {
                    var tex = getTex();

                    if (tex != null)
                        Util.ByteArrayToFile(Path.Combine(outputPath, $"{blockName}", $"{name}.png"),
                            tex.EncodeToPNG());
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error exporting with ex: {ex}");
                }
            }

            void exportSprite(SpriteTexture[] spriteTextures, string blockName, int setIndex, int frameIndex, int palX, int palY)
            {
                try
                {
                    var tex = GetTexture(spriteTextures, loader.VRAM, palX, palY);

                    Util.ByteArrayToFile(Path.Combine(outputPath, blockName, $"{setIndex} - {frameIndex}.png"), tex.EncodeToPNG());
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error exporting sprite frame: {ex}");
                }
            }
        }

        public async UniTask Extract_BackgroundsAsync(GameSettings settings, string outputPath)
        {
            using var context = new R1Context(settings);
            var config = GetLoaderConfig(settings);
            await LoadFilesAsync(context, config);

            // Load the IDX
            var idxData = Load_IDX(context, config);

            var loader = Loader_DTP.Create(context, idxData, config);

            // Enumerate every entry
            for (var blockIndex = 3; blockIndex < idxData.Entries.Length; blockIndex++)
            {
                loader.SwitchBlocks(blockIndex);

                // Process each BIN file
                loader.LoadBINFiles((cmd, i) =>
                {
                    try
                    {
                        // Check the file type
                        if (cmd.FILE_Type != IDXLoadCommand.FileType.Archive_BackgroundPack)
                            return;

                        // Read the data
                        var bg = loader.LoadBINFile<BackgroundPack_ArchiveFile>(i);

                        var exportedMaps = new bool[bg.BGDFiles.Files.Length];

                        // Export for each sector
                        foreach (var modifiersFile in bg.BackgroundModifiersFiles.Files)
                            export(modifiersFile.Modifiers.Where(x => !x.IsLayer || !exportedMaps[x.BGDIndex]).ToArray());

                        // Export unreferenced backgrounds
                        for (int j = 0; j < exportedMaps.Length; j++)
                        {
                            if (!exportedMaps[j])
                            {
                                export(new BackgroundModifierObject[]
                                {
                                    new BackgroundModifierObject
                                    {
                                        Type = BackgroundModifierObject.BackgroundModifierType.BackgroundLayer_19,
                                        BGDIndex = j,
                                        CELIndex = 0,
                                    }
                                });
                            }
                        }

                        void export(BackgroundModifierObject[] modifiers)
                        {
                            Texture2D[][] textures;
                            int animSpeed;

                            (textures, animSpeed) = GetBackgrounds(loader, bg, modifiers);

                            for (int texIndex = 0; texIndex < textures.Length; texIndex++)
                            {
                                var bgTex = textures[texIndex];
                                var bgIndex = modifiers.Where(x => x.IsLayer).ElementAt(texIndex).BGDIndex;

                                exportedMaps[bgIndex] = true;

                                if (bgTex.Length > 1)
                                {
                                    Util.ExportAnimAsGif(bgTex, animSpeed, false, false, Path.Combine(outputPath, $"{blockIndex} - {i} - {bgIndex}.gif"));
                                }
                                else
                                {
                                    Util.ByteArrayToFile(Path.Combine(outputPath, $"{blockIndex} - {i} - {bgIndex}.png"), bgTex[0].EncodeToPNG());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error exporting with ex: {ex}");
                    }
                });

                PaletteHelpers.ExportVram(Path.Combine(outputPath, $"VRAM_{blockIndex}.png"), loader.VRAM);
            }
        }

        public async UniTask Extract_Cutscenes(GameSettings settings, string outputPath)
        {
            using var context = new R1Context(settings);
            var config = GetLoaderConfig(settings);
            await LoadFilesAsync(context, config);

            // Load the IDX
            var idxData = Load_IDX(context, config);

            var loader = Loader_DTP.Create(context, idxData, config);

            // Enumerate every entry
            for (var blockIndex = 3; blockIndex < idxData.Entries.Length; blockIndex++)
            {
                loader.SwitchBlocks(blockIndex);

                // Process each BIN file
                loader.LoadBINFiles((cmd, i) =>
                {
                    // Check the file type
                    if (cmd.FILE_Type != IDXLoadCommand.FileType.Archive_LevelPack)
                        return;

                    // Read the data
                    var lvl = loader.LoadBINFile<LevelPack_ArchiveFile>(i);

                    // Make sure it has normal cutscens
                    if (lvl.CutscenePack.Cutscenes == null)
                        return;

                    // Enumerate every cutscene
                    for (var cutsceneIndex = 0; cutsceneIndex < lvl.CutscenePack.Cutscenes.Length; cutsceneIndex++)
                    {
                        Cutscene cutscene = lvl.CutscenePack.Cutscenes[cutsceneIndex];
                        var normalCutscene = CutsceneTextTranslationTables.CutsceneToText(
                            cutscene: cutscene,
                            translationTable: GetCutsceneTranslationTable,
                            includeInstructionIndex: false,
                            normalCutscene: true);

                        File.WriteAllText(Path.Combine(outputPath, $"{blockIndex}_{cutsceneIndex}.txt"), normalCutscene);

                        if (cutscene.Cutscene_Skip != null)
                        {
                            var skipCutscene = CutsceneTextTranslationTables.CutsceneToText(
                                cutscene: cutscene,
                                translationTable: GetCutsceneTranslationTable,
                                includeInstructionIndex: false,
                                normalCutscene: false);

                            File.WriteAllText(Path.Combine(outputPath, $"{blockIndex}_{cutsceneIndex} (skip).txt"), skipCutscene);
                        }
                    }
                });
            }
        }

        public abstract LoaderConfiguration_DTP GetLoaderConfig(GameSettings settings);

        public abstract Dictionary<string, char> GetCutsceneTranslationTable { get; }

        public override async UniTask<Unity_Level> LoadAsync(Context context)
        {
            var stopWatch = Stopwatch.StartNew();
            var startupLog = new StringBuilder();

            // Get settings
            GameSettings settings = context.GetR1Settings();
            int lev = settings.World;
            int sector = settings.Level;
            LoaderConfiguration_DTP config = GetLoaderConfig(settings);

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Retrieved settings");

            // Load the files
            await LoadFilesAsync(context, config);

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded files");

            Controller.DetailedState = "Loading IDX";
            await Controller.WaitIfNecessary();

            // Load the IDX
            IDX idxData = Load_IDX(context, config);

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded IDX");

            ArchiveFile.AddToParsedArchiveFiles = true;

            Controller.DetailedState = "Loading BIN";
            await Controller.WaitIfNecessary();

            // Create the loader
            var loader = Loader_DTP.Create(context, idxData, config);

            // Only parse the selected sector
            loader.LevelSector = sector;

            var logAction = new Func<string, Task>(async x =>
            {
                Controller.DetailedState = x;
                await Controller.WaitIfNecessary();
            });

            // Load the fixed BIN
            loader.SwitchBlocks(loader.Config.BLOCK_Fix);
            await loader.FillCacheForBlockReadAsync();
            await loader.LoadAndProcessBINBlockAsync(logAction);

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded fixed BIN");

            // Load the level BIN
            loader.SwitchBlocks(lev);
            await loader.FillCacheForBlockReadAsync();
            await loader.LoadAndProcessBINBlockAsync(logAction);

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded level BIN");

            Controller.DetailedState = "Loading level data";
            await Controller.WaitIfNecessary();

            // Load hard-coded level data
            loader.ProcessLevelData();

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded hard-coded level data");

            const float scale = 64f;

            // Load the layers
            Unity_Layer[] layers = await Load_LayersAsync(loader, sector, scale);

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded layers");

            // Load object manager
            Unity_ObjectManager_PSKlonoa_DTP objManager = await Load_ObjManagerAsync(loader);

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded object manager");

            Controller.DetailedState = "Loading objects";
            await Controller.WaitIfNecessary();

            List<Unity_Object> objects = Load_Objects(loader, sector, scale, objManager);

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded objects");

            Controller.DetailedState = "Loading paths";
            await Controller.WaitIfNecessary();

            var paths = Load_MovementPaths(loader, sector, scale);

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded paths");

            Unity_CameraClear camClear = null;
            var bgClear = loader.BackgroundPack?.BackgroundModifiersFiles.Files.ElementAtOrDefault(sector)?.Modifiers.Where(x => x.Type == BackgroundModifierObject.BackgroundModifierType.Clear).Select(x => x.Data_20).ToArray();

            // TODO: Fully support camera clearing with gradient and multiple clear zones
            if (bgClear?.Any() == true)
                camClear = new Unity_CameraClear(bgClear.First().Entries[0].Color.GetColor());

            startupLog.AppendLine($"{stopWatch.ElapsedMilliseconds:0000}ms - Loaded camera clears");

            var str = new StringBuilder();

            foreach (var archive in ArchiveFile.ParsedArchiveFiles)
            {
                for (int i = 0; i < archive.Value.Length; i++)
                {
                    if (!archive.Value[i])
                        str.AppendLine($"{archive.Key.Offset}: ({archive.Key.GetType().Name}) File #{i}");
                }
            }

            Debug.Log($"Unparsed BIN files:{Environment.NewLine}" +
                      $"{str}");

            stopWatch.Stop();

            Debug.Log($"Startup in {stopWatch.ElapsedMilliseconds:0000}ms{Environment.NewLine}" +
                      $"{startupLog}");

            return new Unity_Level(
                layers: layers,
                cellSize: 16,
                objManager: objManager,
                eventData: objects,
                framesPerSecond: 60,
                collisionLines: paths,
                isometricData: new Unity_IsometricData
                {
                    CollisionWidth = 0,
                    CollisionHeight = 0,
                    TilesWidth = 0,
                    TilesHeight = 0,
                    Collision = null,
                    Scale = Vector3.one,
                    ViewAngle = Quaternion.Euler(90, 0, 0),
                    CalculateYDisplacement = () => 0,
                    CalculateXDisplacement = () => 0,
                    ObjectScale = Vector3.one * 1
                },
                ps1Vram: loader.VRAM,
                cameraClear: camClear);
        }

        public async UniTask<Unity_Layer[]> Load_LayersAsync(Loader_DTP loader, int sector, float scale)
        {
            var modifiers = loader.LevelData3D.SectorModifiers[sector].Modifiers;
            var texAnimations = modifiers.
                Where(x => x.PrimaryType == PrimaryObjectType.Modifier_3D_41).
                SelectMany(x => x.DataFiles).
                Where(x => x.TextureAnimation != null).
                Select(x => x.TextureAnimation.Files).
                ToArray();
            var uvScrollAnimations = modifiers.
                Where(x => x.PrimaryType == PrimaryObjectType.Modifier_3D_41).
                SelectMany(x => x.DataFiles).
                Where(x => x.UVScrollAnimation != null).
                SelectMany(x => x.UVScrollAnimation.UVOffsets).
                ToArray();

            var layers = new List<Unity_Layer>();

            Controller.DetailedState = "Loading backgrounds";
            await Controller.WaitIfNecessary();

            // Load backgrounds
            if (loader.BackgroundPack != null && loader.BackgroundPack.BackgroundModifiersFiles.Files.Length > sector)
            {
                var bgModifiers = loader.BackgroundPack.BackgroundModifiersFiles.Files[sector].Modifiers;

                Texture2D[][] bgTextures;
                int bgAnimSpeed;

                // Get the background textures
                (bgTextures, bgAnimSpeed) = GetBackgrounds(loader, loader.BackgroundPack, bgModifiers);

                // Add the backgrounds
                layers.AddRange(bgTextures.Select((t, i) => new Unity_Layer_Texture
                {
                    Name = $"Background {i}", 
                    ShortName = $"BG{i}", 
                    Textures = t, 
                    AnimSpeed = bgAnimSpeed,
                }));
            }

            Controller.DetailedState = "Loading level geometry";
            await Controller.WaitIfNecessary();

            GameObject obj;
            bool isAnimated;

            (obj, isAnimated) = CreateGameObject(loader.LevelPack.Sectors[sector].LevelModel, loader, scale, "Map", texAnimations, uvScrollAnimations);

            var levelBounds = GetDimensions(loader.LevelPack.Sectors[sector].LevelModel, scale);

            // Calculate actual level dimensions: switched axes for unity & multiplied by cellSize
            var size = levelBounds.size;
            var cellSize = 16;
            var layerDimensions = new Vector3(size.x, size.z, size.y) * cellSize;

            // Correctly center object
            //obj.transform.position = new Vector3(-levelBounds.min.x, 0, -size.z-levelBounds.min.z);

            var parent3d = Controller.obj.levelController.editor.layerTiles.transform;
            layers.Add(new Unity_Layer_GameObject(true, isAnimated: isAnimated)
            {
                Name = "Map",
                ShortName = "MAP",
                Graphics = obj,
                Collision = null,
                Dimensions = layerDimensions,
                DisableGraphicsWhenCollisionIsActive = true
            });
            obj.transform.SetParent(parent3d);

            Controller.DetailedState = "Loading 3D objects";
            await Controller.WaitIfNecessary();

            GameObject gao_3dObjParent = null;

            bool isObjAnimated = false;

            foreach (var modifier in modifiers)
            {
                if (modifier.PrimaryType == PrimaryObjectType.None || modifier.PrimaryType == PrimaryObjectType.Invalid)
                    continue;

                if (modifier.PrimaryType == PrimaryObjectType.Modifier_3D_40 || modifier.PrimaryType == PrimaryObjectType.Modifier_3D_41)
                {
                    bool animated;

                    (gao_3dObjParent, animated) = Load_ModifierObject(loader, modifier, gao_3dObjParent, scale, texAnimations);

                    if (animated)
                        isObjAnimated = true;
                }
                else
                {
                    Debug.LogWarning($"Skipped unsupported modifier object of primary type {modifier.PrimaryType}");
                }
            }

            Debug.Log($"MAP INFO{Environment.NewLine}" +
                      $"{texAnimations.Length} texture animations{Environment.NewLine}" +
                      $"{uvScrollAnimations.Length} UV scroll animations{Environment.NewLine}" +
                      $"Modifiers:{Environment.NewLine}\t" +
                      $"{String.Join($"{Environment.NewLine}\t", modifiers.Take(modifiers.Length - 1).Select(x => $"{x.Offset}: {(int)x.PrimaryType:00}-{x.SecondaryType:00} {String.Join(", ", x.DataFiles?.Select(d => d.DeterminedType.ToString()) ?? new string[0])}"))}");

            if (gao_3dObjParent != null)
            {
                layers.Add(new Unity_Layer_GameObject(true, isAnimated: isObjAnimated)
                {
                    Name = "3D Objects",
                    ShortName = $"3DO",
                    Graphics = gao_3dObjParent
                });
                gao_3dObjParent.transform.SetParent(parent3d);
            }

            Controller.DetailedState = "Loading collision";
            await Controller.WaitIfNecessary();

            // TODO: Load collision

            return layers.ToArray();
        }

        public (GameObject, bool) Load_ModifierObject(Loader_DTP loader, ModifierObject modifier, GameObject gao_3dObjParent, float scale, PS1_TIM[][] texAnimations)
        {
            bool isObjAnimated = false;

            // Some objects only have a single position without a rotation
            var pos = modifier.DataFiles.FirstOrDefault(x => x.Position != null)?.Position;
            
            // Objects can have transform info. This is 2-dimensional as it can effect individual objects in the TMD and have multiple "frames"
            // obj positions/rotations. We assume that if it effects multiple objects it does not animate and is only for their relative positions.
            var objTransform = modifier.DataFiles.FirstOrDefault(x => x.Transform?.Info?.ObjectsCount > 1)?.Transform;

            var transform = modifier.DataFiles.FirstOrDefault(x => x.Transform != null && !(x.Transform?.Info?.ObjectsCount > 1))?.Transform;

            var transformPositions = transform?.Positions.Positions;
            var transformRotations = transform?.Rotations.Rotations;

            // The minecart has multiple transforms with animations for how it travels, so we combine them
            if (transform == null)
            {
                var transforms = modifier.DataFiles.FirstOrDefault(x => x.Transforms != null)?.Transforms;

                transformPositions = transforms?.Files.SelectMany(x => x.Positions.Positions).ToArray();
                transformRotations = transforms?.Files.SelectMany(x => x.Rotations.Rotations).ToArray();
            }

            var tmdFiles = modifier.DataFiles.Where(x => x.TMD != null).Select(x => x.TMD);

            var constantRotation = modifier.PrimaryType == PrimaryObjectType.Modifier_3D_41
                ? ModifierObjectsRotations.TryGetItem(loader.BINBlock)?.TryGetItem(modifier.SecondaryType)
                : null;
            var posOffset = modifier.PrimaryType == PrimaryObjectType.Modifier_3D_41
                ? ModifierObjectsPositionOffsets.TryGetItem(loader.BINBlock)?.TryGetItem(modifier.SecondaryType)
                : null;

            var index = 0;

            foreach (var tmd in tmdFiles)
            {
                if (gao_3dObjParent == null)
                {
                    gao_3dObjParent = new GameObject("3D Objects");
                    gao_3dObjParent.transform.localPosition = Vector3.zero;
                    gao_3dObjParent.transform.localRotation = Quaternion.identity;
                    gao_3dObjParent.transform.localScale = Vector3.one;
                }

                GameObject gameObj;
                bool isAnimated;

                (gameObj, isAnimated) = CreateGameObject(
                    tmd: tmd,
                    loader: loader,
                    scale: scale,
                    name: $"Object3D Offset:{modifier.Offset} Index:{index} Type:{modifier.PrimaryType}-{modifier.SecondaryType}",
                    texAnimations: texAnimations,
                    scrollUVs: new int[0],
                    positions: objTransform?.Positions.Positions[0].Select(x => GetPositionVector(x, Vector3.zero, scale)).ToArray(),
                    rotations: objTransform?.Rotations.Rotations[0].Select(x => GetQuaternion(x)).ToArray());

                if (isAnimated)
                    isObjAnimated = true;

                Vector3 defaultPos = Vector3.zero;
                Quaternion defaultRotation = Quaternion.identity;

                if (pos != null)
                {
                    defaultPos = GetPositionVector(pos, posOffset, scale);
                }
                else if (transformPositions != null)
                {
                    defaultPos = GetPositionVector(transformPositions[0][0], posOffset, scale);
                    defaultRotation = GetQuaternion(transformRotations[0][0]);
                }

                gameObj.transform.localPosition = defaultPos;
                gameObj.transform.localRotation = defaultRotation;
                gameObj.transform.SetParent(gao_3dObjParent.transform);

                if (transformPositions?.Length > 1)
                {
                    var mtComponent = gameObj.AddComponent<AnimatedTransformComponent>();
                    mtComponent.animatedTransform = gameObj.transform;
                    var positions = transformPositions?.Select(x => GetPositionVector(x[0], posOffset, scale)).ToArray() ?? new Vector3[0];
                    var rotations = transformRotations?.Select(x => GetQuaternion(x[0])).ToArray() ?? new Quaternion[0];
                    var frameCount = Math.Max(positions.Length, rotations.Length);
                    mtComponent.frames = new AnimatedTransformComponent.Frame[frameCount];
                    for (int i = 0; i < frameCount; i++)
                    {
                        mtComponent.frames[i] = new AnimatedTransformComponent.Frame()
                        {
                            Position = positions.Length > i ? positions[i] : defaultPos,
                            Rotation = rotations.Length > i ? rotations[i] : defaultRotation,
                            Scale = Vector3.one
                        };
                    }
                }

                if (constantRotation != null)
                {
                    var mtComponent = gameObj.AddComponent<AnimatedTransformComponent>();
                    mtComponent.animatedTransform = gameObj.transform;

                    var rotationPerFrame = constantRotation.Value.Item2;
                    var count = 0x1000 / rotationPerFrame;

                    mtComponent.frames = new AnimatedTransformComponent.Frame[count];

                    for (int i = 0; i < count; i++)
                    {
                        var degrees = GetRotationInDegrees(i * rotationPerFrame);

                        mtComponent.frames[i] = new AnimatedTransformComponent.Frame()
                        {
                            Position = defaultPos,
                            Rotation = defaultRotation * Quaternion.Euler(
                                x: degrees * constantRotation.Value.Item1.x,
                                y: degrees * constantRotation.Value.Item1.y,
                                z: degrees * constantRotation.Value.Item1.z),
                            Scale = Vector3.one
                        };
                    }
                }

                index++;
            }

            return (gao_3dObjParent, isObjAnimated);
        }

        public async UniTask<Unity_ObjectManager_PSKlonoa_DTP> Load_ObjManagerAsync(Loader_DTP loader)
        {
            var frameSets = new List<Unity_ObjectManager_PSKlonoa_DTP.SpriteSet>();

            // Enumerate each frame set
            for (var i = 0; i < loader.SpriteSets.Length; i++)
            {
                Controller.DetailedState = $"Loading sprites {i + 1}/{loader.SpriteSets.Length}";
                await Controller.WaitIfNecessary();

                var frames = loader.SpriteSets[i];

                // Skip if null
                if (frames == null)
                    continue;

                // Create the frame textures
                var frameTextures = frames.Files.Take(frames.Files.Length - 1).Select(x => GetTexture(x.Textures, loader.VRAM, 0, 500).CreateSprite()).ToArray();

                frameSets.Add(new Unity_ObjectManager_PSKlonoa_DTP.SpriteSet(frameTextures, i));
            }

            return new Unity_ObjectManager_PSKlonoa_DTP(loader.Context, frameSets.ToArray());
        }

        public List<Unity_Object> Load_Objects(Loader_DTP loader, int sector, float scale, Unity_ObjectManager_PSKlonoa_DTP objManager)
        {
            var objects = new List<Unity_Object>();
            var movementPaths = loader.LevelPack.Sectors[sector].MovementPaths.Files;

            // Add enemies
            foreach (EnemyObject enemyObj in loader.LevelData2D.EnemyObjects.Where(x => x.GlobalSectorIndex == loader.GlobalSectorIndex))
            {
                // Add the enemy object
                addEnemyObj(enemyObj);

                // Add spawned objects from portals
                if (enemyObj.Data is EnemyData_02 data_02)
                {
                    foreach (var spawnObject in data_02.SpawnObjects)
                        addEnemyObj(spawnObject);
                }

                void addEnemyObj(EnemyObject obj)
                {
                    var spriteInfo = GetSprite_Enemy(obj);

                    if (spriteInfo.SpriteSet == -1 || spriteInfo.SpriteIndex == -1)
                        Debug.LogWarning($"Sprite could not be determined for enemy object of secondary type {obj.SecondaryType} and graphics index {obj.GraphicsIndex}");

                    objects.Add(new Unity_Object_PSKlonoa_DTP_Enemy(objManager, obj, scale, spriteInfo));
                }
            }

            // Add enemy spawn points
            for (int pathIndex = 0; pathIndex < movementPaths.Length; pathIndex++)
            {
                for (int objIndex = 0; objIndex < loader.LevelData2D.EnemyObjectIndexTables.IndexTables[pathIndex].Length; objIndex++)
                {
                    var obj = loader.LevelData2D.EnemyObjects[loader.LevelData2D.EnemyObjectIndexTables.IndexTables[pathIndex][objIndex]];

                    if (obj.GlobalSectorIndex != loader.GlobalSectorIndex)
                        continue;

                    var pos = GetPosition(movementPaths[pathIndex].Blocks, obj.MovementPathSpawnPosition, Vector3.zero, scale);

                    objects.Add(new Unity_Object_Dummy(obj, Unity_Object.ObjectType.Trigger, objLinks: new int[]
                    {
                        objects.OfType<Unity_Object_PSKlonoa_DTP_Enemy>().FindItemIndex(x => x.Object == obj)
                    })
                    {
                        Position = pos,
                    });
                }
            }

            // Add collectibles
            objects.AddRange(loader.LevelData2D.CollectibleObjects.Where(x => x.GlobalSectorIndex == loader.GlobalSectorIndex && x.SecondaryType != -1).Select(x =>
            {
                Vector3 pos;

                // If the path index is -1 then the position is absolute, otherwise it's relative
                if (x.MovementPath == -1)
                    pos = GetPosition(x.XPos.Value, x.YPos.Value, x.ZPos.Value, scale);
                else
                    pos = GetPosition(movementPaths[x.MovementPath].Blocks, x.MovementPathPosition, new Vector3(0, x.YPos.Value, 0), scale);

                var spriteInfo = GetSprite_Collectible(x);

                if (spriteInfo.SpriteSet == -1 || spriteInfo.SpriteIndex == -1)
                    Debug.LogWarning($"Sprite could not be determined for collectible object of secondary type {x.SecondaryType}");

                return new Unity_Object_PSKlonoa_DTP_Collectible(objManager, x, pos, spriteInfo);
            }));

            // Add scenery objects
            objects.AddRange(loader.LevelData3D.SectorModifiers[sector].Modifiers.Where(x => x.DataFiles != null).SelectMany(x => x.DataFiles).Where(x => x.ScenerySprites != null).SelectMany(x => x.ScenerySprites.Positions).Select(x => new Unity_Object_Dummy(x, Unity_Object.ObjectType.Object)
            {
                Position = GetPosition(x.XPos, x.YPos, x.ZPos, scale),
            }));

            var wpIndex = objects.Count;
            // Temporarily add waypoints at each path block to visualize them
            objects.AddRange(movementPaths.SelectMany((x, i) => x.Blocks.SelectMany(b => new Unity_Object[]
            {
                new Unity_Object_Dummy(b, Unity_Object.ObjectType.Waypoint, $"Path: {i}", objLinks: new int[]
                {
                    ++wpIndex
                })
                {
                    Position = GetPosition(b.XPos, b.YPos, b.ZPos, scale),
                },
                new Unity_Object_Dummy(b, Unity_Object.ObjectType.Waypoint, $"Path: {i}", objLinks: new []
                {
                    (wpIndex++) - 1
                })
                {
                    Position = GetPosition(
                        x: b.XPos + b.DirectionX * b.BlockLength,
                        y: b.YPos + b.DirectionY * b.BlockLength,
                        z: b.ZPos + b.DirectionZ * b.BlockLength,
                        scale: scale),
                }
            })));

            return objects;
        }

        public Unity_CollisionLine[] Load_MovementPaths(Loader_DTP loader, int sector, float scale)
        {
            var lines = new List<Unity_CollisionLine>();

            foreach (var path in loader.LevelPack.Sectors[sector].MovementPaths.Files)
            {
                foreach (var pathBlock in path.Blocks)
                {
                    var origin = GetPosition(pathBlock.XPos, pathBlock.YPos, pathBlock.ZPos, scale);
                    var end = GetPosition(
                        x: pathBlock.XPos + pathBlock.DirectionX * pathBlock.BlockLength, 
                        y: pathBlock.YPos + pathBlock.DirectionY * pathBlock.BlockLength, 
                        z: pathBlock.ZPos + pathBlock.DirectionZ * pathBlock.BlockLength, 
                        scale: scale);

                    lines.Add(new Unity_CollisionLine(origin, end));
                }
            }

            return lines.ToArray();
        }

        public (GameObject, bool) CreateGameObject(PS1_TMD tmd, Loader_DTP loader, float scale, string name, PS1_TIM[][] texAnimations, int[] scrollUVs, Vector3[] positions = null, Quaternion[] rotations = null)
        {
            bool isAnimated = false;
            var textureCache = new Dictionary<int, Texture2D>();
            var textureAnimCache = new Dictionary<long, Texture2D[]>();

            GameObject gaoParent = new GameObject(name);
            gaoParent.transform.position = Vector3.zero;

            // Create each object
            for (var objIndex = 0; objIndex < tmd.Objects.Length; objIndex++)
            {
                var obj = tmd.Objects[objIndex];

                // Helper methods
                Vector3 toVertex(PS1_TMD_Vertex v) => new Vector3(v.X / scale, -v.Y / scale, v.Z / scale);
                Vector3 toNormal(PS1_TMD_Normal n) => new Vector3(n.X, -n.Y , n.Z);
                Vector2 toUV(PS1_TMD_UV uv) => new Vector2(uv.U / 255f, uv.V / 255f);

                RectInt getRect(PS1_TMD_UV[] uv)
                {
                    int xMin = uv.Min(x => x.U);
                    int xMax = uv.Max(x => x.U) + 1;
                    int yMin = uv.Min(x => x.V);
                    int yMax = uv.Max(x => x.V) + 1;
                    int w = xMax - xMin;
                    int h = yMax - yMin;

                    return new RectInt(xMin, yMin, w, h);
                }

                GameObject gameObject = new GameObject($"Object_{objIndex} Offset:{obj.Offset}");

                gameObject.transform.SetParent(gaoParent.transform);
                gameObject.transform.localScale = Vector3.one;

                gameObject.transform.localPosition = positions?[objIndex] ?? Vector3.zero;
                gameObject.transform.rotation = rotations?[objIndex] ?? Quaternion.identity;

                // Add each primitive
                for (var packetIndex = 0; packetIndex < obj.Primitives.Length; packetIndex++)
                {
                    var packet = obj.Primitives[packetIndex];

                    //if (!packet.Flags.HasFlag(PS1_TMD_Packet.PacketFlags.LGT))
                    //    Debug.LogWarning($"Packet has light source");

                    // TODO: Implement other types
                    if (packet.Mode.Code != PS1_TMD_PacketMode.PacketModeCODE.Polygon)
                    {
                        Debug.LogWarning($"Skipped packet with code {packet.Mode.Code}");
                        continue;
                    }

                    Mesh unityMesh = new Mesh();

                    var vertices = packet.Vertices.Select(x => toVertex(obj.Vertices[x])).ToArray();

                    Vector3[] normals = null;

                    if (packet.Normals != null) 
                    {
                        normals = packet.Normals.Select(x => toNormal(obj.Normals[x])).ToArray();
                        if(normals.Length == 1)
                            normals = Enumerable.Repeat(normals[0], vertices.Length).ToArray();
                    }
                    int[] triangles;

                    // Set vertices
                    unityMesh.SetVertices(vertices);

                    // Set normals
                    if (normals != null) 
                        unityMesh.SetNormals(normals);

                    if (packet.Mode.IsQuad)
                    {
                        if (packet.Flags.HasFlag(PS1_TMD_Packet.PacketFlags.FCE))
                        {
                            triangles = new int[]
                            {
                                // Lower left triangle
                                0, 1, 2, 0, 2, 1,
                                // Upper right triangle
                                3, 2, 1, 3, 1, 2,
                            };
                        }
                        else
                        {
                            triangles = new int[]
                            {
                                // Lower left triangle
                                0, 1, 2,
                                // Upper right triangle
                                3, 2, 1,
                            };
                        }
                    }
                    else
                    {
                        if (packet.Flags.HasFlag(PS1_TMD_Packet.PacketFlags.FCE))
                        {
                            triangles = new int[]
                            {
                                0, 1, 2, 0, 2, 1,
                            };
                        }
                        else
                        {
                            triangles = new int[]
                            {
                                0, 1, 2,
                            };
                        }
                    }

                    unityMesh.SetTriangles(triangles, 0);

                    var colors = packet.RGB.Select(x => x.Color.GetColor()).ToArray();

                    if (colors.Length == 1)
                        colors = Enumerable.Repeat(colors[0], vertices.Length).ToArray();

                    unityMesh.SetColors(colors);

                    if (packet.UV != null)
                        unityMesh.SetUVs(0, packet.UV.Select(toUV).ToArray());

                    if(normals == null) unityMesh.RecalculateNormals();

                    GameObject gao = new GameObject($"Packet_{packetIndex} Offset:{packet.Offset} Flags:{packet.Flags}");

                    MeshFilter mf = gao.AddComponent<MeshFilter>();
                    MeshRenderer mr = gao.AddComponent<MeshRenderer>();
                    gao.layer = LayerMask.NameToLayer("3D Collision");
                    gao.transform.SetParent(gameObject.transform);
                    gao.transform.localScale = Vector3.one;
                    gao.transform.localPosition = Vector3.zero;
                    mf.mesh = unityMesh;

                    if (packet.Mode.ABE)
                        mr.material = Controller.obj.levelController.controllerTilemap.unlitAdditiveMaterial;
                    else
                        mr.material = Controller.obj.levelController.controllerTilemap.unlitTransparentCutoutMaterial;

                    // Add texture
                    if (packet.Mode.TME)
                    {
                        var rect = getRect(packet.UV);

                        var key = packet.CBA.ClutX | packet.CBA.ClutY << 6 | packet.TSB.TX << 16 | packet.TSB.TY << 24;
                        long animKey = (long) key | (long) rect.x << 32 | (long) rect.y << 40;

                        if (!textureAnimCache.ContainsKey(animKey))
                        {
                            PS1_TIM[] foundAnim = null;

                            // Check if the texture region falls within an animated area
                            foreach (var anim in texAnimations)
                            {
                                foreach (var tim in anim)
                                {
                                    // Check the page
                                    if ((tim.XPos * 2) / PS1_VRAM.PageWidth == packet.TSB.TX &&
                                        tim.YPos / PS1_VRAM.PageHeight == packet.TSB.TY)
                                    {
                                        var is4Bit = tim.ColorFormat == PS1_TIM.TIM_ColorFormat.BPP_4;

                                        var timRect = new RectInt(
                                            xMin: ((tim.XPos * 2) % PS1_VRAM.PageWidth) * (is4Bit ? 2 : 1),
                                            yMin: (tim.YPos % PS1_VRAM.PageHeight),
                                            width: tim.Width * 2 * (is4Bit ? 2 : 1),
                                            height: tim.Height);

                                        // Check page offset
                                        if (rect.Overlaps(timRect))
                                        {
                                            foundAnim = anim;
                                            break;
                                        }
                                    }
                                }

                                if (foundAnim != null)
                                    break;
                            }

                            if (foundAnim != null)
                            {
                                var textures = new Texture2D[foundAnim.Length];

                                for (int i = 0; i < textures.Length; i++)
                                {
                                    loader.AddToVRAM(foundAnim[i]);
                                    textures[i] = GetTexture(packet, loader.VRAM);
                                }

                                textureAnimCache.Add(animKey, textures);
                            }
                            else
                            {
                                textureAnimCache.Add(animKey, null);
                            }
                        }

                        if (!textureCache.ContainsKey(key))
                            textureCache.Add(key, GetTexture(packet, loader.VRAM));

                        var t = textureCache[key];

                        var animTextures = textureAnimCache[animKey];

                        if (animTextures != null)
                        {
                            isAnimated = true;
                            var animTex = gao.AddComponent<AnimatedTextureComponent>();
                            animTex.material = mr.material;
                            animTex.animatedTextures = animTextures;
                        }

                        t.wrapMode = TextureWrapMode.Repeat;
                        mr.material.SetTexture("_MainTex", t);
                        mr.name = $"{objIndex}-{packetIndex} TX: {packet.TSB.TX}, TY:{packet.TSB.TY}, X:{rect.x}, Y:{rect.y}, W:{rect.width}, H:{rect.height}, F:{packet.Flags}, ABE:{packet.Mode.ABE}, TGE:{packet.Mode.TGE}, UVOffset:{packet.UV.First().Offset.FileOffset - tmd.Offset.FileOffset}";

                        // Check for UV scroll animations
                        if (packet.UV.Any(x => scrollUVs.Contains((int)(x.Offset.FileOffset - tmd.Objects[0].Offset.FileOffset))))
                        {
                            isAnimated = true;
                            var animTex = gao.AddComponent<AnimatedTextureComponent>();
                            animTex.material = mr.material;
                            animTex.scrollV = -0.5f;
                        }
                    }
                }
            }

            return (gaoParent, isAnimated);
        }

        public Bounds GetDimensions(PS1_TMD tmd, float scale) {
            Bounds b = new Bounds();
            var verts = tmd.Objects.SelectMany(x => x.Vertices).ToArray();
            var min = new Vector3(verts.Min(v => v.X), verts.Min(v => v.Y), verts.Min(v => v.Z)) / scale;
            var max = new Vector3(verts.Max(v => v.X), verts.Max(v => v.Y), verts.Max(v => v.Z)) / scale;
            var center = Vector3.Lerp(min, max, 0.5f);

            return new Bounds(center, max-min);
        }

        public Vector3 GetPositionVector(ObjPosition pos, Vector3? posOffset, float scale)
        {
            if (posOffset == null)
                return new Vector3(pos.XPos / scale, -pos.YPos / scale, pos.ZPos / scale);
            else
                return new Vector3((pos.XPos + posOffset.Value.x) / scale, -(pos.YPos + posOffset.Value.y) / scale, (pos.ZPos + posOffset.Value.z) / scale);
        }

        public float GetRotationInDegrees(int value)
        {
            if (value > 0x800)
                value -= 0x1000;

            return value * (360f / 0x1000);
        }

        public Quaternion GetQuaternion(ObjRotation rot)
        {
            return GetQuaternion(rot.RotationX, rot.RotationY, rot.RotationZ);
        }

        public Quaternion GetQuaternion(int rotX, int rotY, int rotZ)
        {
            return Quaternion.Euler(
                x: -GetRotationInDegrees(rotX),
                y: GetRotationInDegrees(rotY),
                z: -(GetRotationInDegrees(rotZ) - GetRotationInDegrees(rotX)));
        }

        public IDX Load_IDX(Context context, LoaderConfiguration_DTP config)
        {
            return FileFactory.Read<IDX>(config.FilePath_IDX, context, (s, idx) => idx.Pre_LoaderConfig = config);
        }

        public void FillTextureFromVRAM(
            Texture2D tex,
            PS1_VRAM vram,
            int width, int height,
            PS1_TIM.TIM_ColorFormat colorFormat,
            int texX, int texY,
            int clutX, int clutY,
            int texturePageOriginX = 0, int texturePageOriginY = 0,
            int texturePageOffsetX = 0, int texturePageOffsetY = 0,
            int texturePageX = 0, int texturePageY = 0,
            bool flipX = false, bool flipY = false,
            bool useDummyPal = false)
        {
            var dummyPal = useDummyPal ? Util.CreateDummyPalette(colorFormat == PS1_TIM.TIM_ColorFormat.BPP_8 ? 256 : 16) : null;

            texturePageOriginX *= 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte paletteIndex;

                    if (colorFormat == PS1_TIM.TIM_ColorFormat.BPP_8)
                    {
                        paletteIndex = vram.GetPixel8(texturePageX, texturePageY,
                            texturePageOriginX + texturePageOffsetX + x,
                            texturePageOriginY + texturePageOffsetY + y);
                    }
                    else if (colorFormat == PS1_TIM.TIM_ColorFormat.BPP_4)
                    {
                        paletteIndex = vram.GetPixel8(texturePageX, texturePageY,
                            texturePageOriginX + (texturePageOffsetX + x) / 2,
                            texturePageOriginY + texturePageOffsetY + y);

                        if (x % 2 == 0)
                            paletteIndex = (byte)BitHelpers.ExtractBits(paletteIndex, 4, 0);
                        else
                            paletteIndex = (byte)BitHelpers.ExtractBits(paletteIndex, 4, 4);
                    }
                    else
                    {
                        throw new Exception($"Non-supported color format");
                    }

                    // Get the color from the palette
                    var c = useDummyPal ? dummyPal[paletteIndex] : vram.GetColor1555(0, 0, clutX + paletteIndex, clutY);

                    if (c.Alpha == 0)
                        continue;

                    var texOffsetX = flipX ? width - x - 1 : x;
                    var texOffsetY = flipY ? height - y - 1 : y;

                    // Set the pixel
                    tex.SetPixel(texX + texOffsetX, tex.height - (texY + texOffsetY) - 1, c.GetColor());
                }
            }
        }

        public Texture2D GetTexture(PS1_TIM tim, bool flipTextureY = true, Color[] palette = null, bool onlyFirstTransparent = false, bool noPal = false)
        {
            if (tim.XPos == 0 && tim.YPos == 0)
                return null;

            var pal = noPal ? null : palette ?? tim.Clut?.Palette?.Select(x => x.GetColor()).ToArray();

            if (onlyFirstTransparent && pal != null)
                for (int i = 0; i < pal.Length; i++)
                    pal[i].a = i == 0 ? 0 : 1;

            return GetTexture(tim.ImgData, pal, tim.Width, tim.Height, tim.ColorFormat, flipTextureY);
        }

        public Texture2D GetTexture(PS1_TMD_Packet packet, PS1_VRAM vram)
        {
            if (!packet.Mode.TME)
                throw new Exception($"Packet has no texture");

            PS1_TIM.TIM_ColorFormat colFormat = packet.TSB.TP switch
            {
                PS1_TSB.TexturePageTP.CLUT_4Bit => PS1_TIM.TIM_ColorFormat.BPP_4,
                PS1_TSB.TexturePageTP.CLUT_8Bit => PS1_TIM.TIM_ColorFormat.BPP_8,
                PS1_TSB.TexturePageTP.Direct_15Bit => PS1_TIM.TIM_ColorFormat.BPP_16,
                _ => throw new InvalidDataException($"PS1 TSB TexturePageTP was {packet.TSB.TP}")
            };
            int width = packet.TSB.TP switch
            {
                PS1_TSB.TexturePageTP.CLUT_4Bit => 256,
                PS1_TSB.TexturePageTP.CLUT_8Bit => 128,
                PS1_TSB.TexturePageTP.Direct_15Bit => 64,
                _ => throw new InvalidDataException($"PS1 TSB TexturePageTP was {packet.TSB.TP}")
            };

            var tex = TextureHelpers.CreateTexture2D(width, 256, clear: true);

            FillTextureFromVRAM(
                tex: tex,
                vram: vram,
                width: width,
                height: 256,
                colorFormat: colFormat,
                texX: 0,
                texY: 0,
                clutX: packet.CBA.ClutX * 16,
                clutY: packet.CBA.ClutY,
                texturePageX: packet.TSB.TX,
                texturePageY: packet.TSB.TY,
                texturePageOriginX: 0,
                texturePageOriginY: 0,
                texturePageOffsetX: 0,
                texturePageOffsetY: 0,
                flipY: true);

            tex.Apply();

            return tex;
        }

        public Texture2D GetTexture(byte[] imgData, Color[] pal, int width, int height, PS1_TIM.TIM_ColorFormat colorFormat, bool flipTextureY = true)
        {
            Util.TileEncoding encoding;

            int palLength;

            switch (colorFormat)
            {
                case PS1_TIM.TIM_ColorFormat.BPP_4:
                    width *= 2 * 2;
                    encoding = Util.TileEncoding.Linear_4bpp;
                    palLength = 16;
                    break;

                case PS1_TIM.TIM_ColorFormat.BPP_8:
                    width *= 2;
                    encoding = Util.TileEncoding.Linear_8bpp;
                    palLength = 256;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            pal ??= Util.CreateDummyPalette(palLength).Select(x => x.GetColor()).ToArray();

            var tex = TextureHelpers.CreateTexture2D(width, height);

            tex.FillRegion(
                imgData: imgData,
                imgDataOffset: 0,
                pal: pal,
                encoding: encoding,
                regionX: 0,
                regionY: 0,
                regionWidth: tex.width,
                regionHeight: tex.height,
                flipTextureY: flipTextureY);

            return tex;
        }

        public Texture2D GetTexture(SpriteTexture[] spriteTextures, PS1_VRAM vram, int palX, int palY)
        {
            if (spriteTextures?.Any() != true)
                return null;

            var rects = spriteTextures.Select(s => 
                new RectInt(
                    xMin: s.FlipX ? s.XPos - s.Width - 1 : s.XPos, 
                    yMin: s.FlipY ? s.YPos - s.Height - 1 : s.YPos, 
                    width: s.Width, 
                    height: s.Height)).ToArray();

            var minX = rects.Min(x => x.x);
            var minY = rects.Min(x => x.y);
            var maxX = rects.Max(x => x.x + x.width);
            var maxY = rects.Max(x => x.y + x.height);

            var width = maxX - minX;
            var height = maxY - minY;

            var tex = TextureHelpers.CreateTexture2D(width, height, clear: true);

            for (var i = 0; i < spriteTextures.Length; i++)
            {
                var index = spriteTextures.Length - i - 1;

                var sprite = spriteTextures[index];
                var texPage = sprite.TexturePage;

                try
                {
                    FillTextureFromVRAM(
                        tex: tex,
                        vram: vram,
                        width: sprite.Width,
                        height: sprite.Height,
                        colorFormat: PS1_TIM.TIM_ColorFormat.BPP_4,
                        texX: rects[index].x - minX,
                        texY: rects[index].y - minY,
                        clutX: palX + sprite.PalOffsetX,
                        clutY: palY + sprite.PalOffsetY,
                        texturePageX: texPage % 16,
                        texturePageY: texPage / 16,
                        texturePageOffsetX: sprite.TexturePageOffsetX,
                        texturePageOffsetY: sprite.TexturePageOffsetY,
                        flipX: sprite.FlipX,
                        flipY: sprite.FlipY,
                        useDummyPal: palX == -1 || palY == -1);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error filling sprite texture data: {ex.Message}");
                }
            }

            tex.Apply();

            return tex;
        }

        public (Texture2D[][], int) GetBackgrounds(Loader_DTP loader, BackgroundPack_ArchiveFile bg, BackgroundModifierObject[] modifiers)
        {
            // Get the modifiers
            var layers = modifiers.Where(x => x.IsLayer).ToArray();
            var palScrolls = modifiers.Where(x => x.Type == BackgroundModifierObject.BackgroundModifierType.PaletteScroll).ToArray();

            // Get the amount of frames each animation will have. We assume if there are multiple palette scroll modifiers that they all share
            // the same length and speed (which they do in Klonoa)
            var framesLength = palScrolls.Any() ? palScrolls.First().Data_23.Length : 1;
            var animSpeed = palScrolls.Any() ? palScrolls.First().Data_23.Speed : 0;

            // Create the textures array for the backgrounds and their frames
            var textures = new Texture2D[layers.Length][];

            for (int i = 0; i < textures.Length; i++)
                textures[i] = new Texture2D[framesLength];

            // Load VRAM data
            for (int tileSetIndex = 0; tileSetIndex < bg.TIMFiles.Files.Length; tileSetIndex++)
            {
                var tim = bg.TIMFiles.Files[tileSetIndex];

                // The game hard-codes this
                if (tileSetIndex == 0)
                {
                    tim.Clut.XPos = 0x130;
                    tim.Clut.YPos = 0x1F0;
                    tim.Clut.Width = 0x10;
                    tim.Clut.Height = 0x10;
                }
                else if (tileSetIndex == 1)
                {
                    tim.XPos = 0x1C0;
                    tim.YPos = 0x100;
                    tim.Width = 0x40;
                    tim.Height = 0x100;

                    tim.Clut.XPos = 0x120;
                    tim.Clut.YPos = 0x1F0;
                    tim.Clut.Width = 0x10;
                    tim.Clut.Height = 0x10;
                }

                loader.AddToVRAM(tim);
            }

            // Create the background frames
            for (int frameIndex = 0; frameIndex < framesLength; frameIndex++)
            {
                // Update VRAM with each palette scroll modifier
                if (frameIndex > 0)
                {
                    foreach (var palMod in palScrolls.Select(x => x.Data_23))
                    {
                        var pal = loader.VRAM.GetPixels8(0, 0, palMod.XPosition * 2, palMod.YPosition, 32);

                        var firstColor_0 = pal[0];
                        var firstColor_1 = pal[1];
                        
                        var index = palMod.StartIndex;
                        var endIndex = index + palMod.Length;
                        
                        do
                        {
                            pal[index * 2] = pal[(index + 1) * 2];
                            pal[index * 2 + 1] = pal[(index + 1) * 2 + 1];

                            index += 1;
                        } while (index < endIndex);

                        pal[(endIndex - 1) * 2] = firstColor_0;
                        pal[(endIndex - 1) * 2 + 1] = firstColor_1;

                        loader.VRAM.AddDataAt(0, 0, palMod.XPosition * 2, palMod.YPosition, pal, 32, 1);
                    }
                }

                // Create the frame for each background
                for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
                {
                    var celIndex = layers[layerIndex].CELIndex;
                    var bgIndex = layers[layerIndex].BGDIndex;

                    var tim = bg.TIMFiles.Files[celIndex];
                    var cel = bg.CELFiles.Files[celIndex];
                    var map = bg.BGDFiles.Files[bgIndex];

                    var tex = TextureHelpers.CreateTexture2D(map.MapWidth * map.CellWidth, map.MapHeight * map.CellHeight, clear: true);

                    for (int mapY = 0; mapY < map.MapHeight; mapY++)
                    {
                        for (int mapX = 0; mapX < map.MapWidth; mapX++)
                        {
                            var cellIndex = map.Map[mapY * map.MapWidth + mapX];

                            if (cellIndex == 0xFF)
                                continue;

                            var cell = cel.Cells[cellIndex];

                            if (cell.ABE)
                                Debug.LogWarning($"CEL ABE flag is set!");

                            FillTextureFromVRAM(
                                tex: tex,
                                vram: loader.VRAM,
                                width: map.CellWidth,
                                height: map.CellHeight,
                                colorFormat: tim.ColorFormat,
                                texX: mapX * map.CellWidth,
                                texY: mapY * map.CellHeight,
                                clutX: cell.ClutX * 16,
                                clutY: cell.ClutY,
                                texturePageOriginX: tim.XPos,
                                texturePageOriginY: tim.YPos,
                                texturePageOffsetX: cell.XOffset,
                                texturePageOffsetY: cell.YOffset);
                        }
                    }

                    tex.Apply();

                    textures[layerIndex][frameIndex] = tex;
                }
            }

            return (textures, animSpeed);
        }
        
        public Texture2D[] GetAnimationFrames(Loader_DTP loader, SpriteAnimation anim, Sprites_ArchiveFile sprites, int palX, int palY, bool isCutscenePlayer = false, CutscenePlayerSprite_File[] playerSprites = null, Color[] playerPalette = null)
        {
            var textures = new Texture2D[anim.FramesCount];

            for (int i = 0; i < anim.FramesCount; i++)
            {
                var frame = anim.Frames[i];

                try
                {
                    if (isCutscenePlayer && playerSprites != null && playerPalette != null)
                    {
                        if (frame.Byte_02 != 0xFF)
                        {
                            if (frame.Byte_02 == 0x99)
                            {
                                var playerSprite = playerSprites[frame.SpriteIndex - 1];

                                textures[i] = GetTexture(playerSprite.ImgData, playerPalette, playerSprite.Width,
                                    playerSprite.Height, PS1_TIM.TIM_ColorFormat.BPP_8);
                            }
                            else
                            {
                                // Animate Klonoa normally - nothing we can do sadly, so set to null
                                textures[i] = null;
                            }

                            continue;
                        }
                    }

                    var sprite = sprites.Files[frame.SpriteIndex];

                    // TODO: Center sprite correctly since sprites can have different sizes
                    textures[i] = GetTexture(sprite.Textures, loader.VRAM, palX, palY);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error exporting animation. IsPlayer: {isCutscenePlayer}, Sprite: {frame.SpriteIndex}, Byte_02: {frame.Byte_02}, Frame: {i}, Offset: {frame.Offset}, Exception: {ex}");

                    return null;
                }
            }

            return textures;
        }

        public async UniTask LoadFilesAsync(Context context, LoaderConfiguration_DTP config)
        {
            // The game only loads portions of the BIN at a time
            await context.AddLinearFileAsync(config.FilePath_BIN);
            
            // The IDX gets loaded into a fixed memory location
            await context.AddMemoryMappedFile(config.FilePath_IDX, config.Address_IDX);

            // The exe has to be loaded to read certain data from it
            await context.AddMemoryMappedFile(config.FilePath_EXE, config.Address_EXE, memoryMappedPriority: 0); // Give lower prio to prioritize IDX
        }

        // [BINBlock][SecondaryType (for type 41)]
        public Dictionary<int, Dictionary<int, (Vector3Int, int)?>> ModifierObjectsRotations { get; } = new Dictionary<int, Dictionary<int, (Vector3Int, int)?>>()
        {
            [3] = new Dictionary<int, (Vector3Int, int)?>()
            {
                [1] = (new Vector3Int(0, 1, 0), 128), // Wind // TODO: Reverse dir
                [5] = (new Vector3Int(0, 0, 1), 27), // Small windmill
                [6] = (new Vector3Int(0, 0, 1), 8), // Big windmill
            }
        };

        // [BINBlock][SecondaryType (for type 41)]
        // Some objects offset their positions
        public Dictionary<int, Dictionary<int, Vector3Int>> ModifierObjectsPositionOffsets { get; } = new Dictionary<int, Dictionary<int, Vector3Int>>()
        {
            [3] = new Dictionary<int, Vector3Int>()
            {
                [1] = new Vector3Int(0, 182, 0), // Wind
            }
        };

        public static ObjSpriteInfo GetSprite_Enemy(EnemyObject obj)
        {
            // TODO: Some enemies have palette swaps. This is done by modifying the x and y palette offsets (by default they are 0 and 500). For example the shielded Moo enemies in Vision 1-2

            // There are 42 object types (0-41). The graphics index is an index to an array of functions for displaying the graphics. The game
            // normally doesn't directly use the graphics index as it sometimes modifies it, but it appears the value initially set in the object
            // data will always match the correct sprite to show, so we can use that.
            var graphicsIndex = obj.GraphicsIndex;

            // Usually the graphics index matches the sprite set index (minus 1), but some are special cases, and since we don't want to show the
            // first sprite we hard-code this. Ideally we would animate them, but that is sadly entirely hard-coded :(
            return graphicsIndex switch
            {
                01 => new ObjSpriteInfo(0, 81), // Moo
                02 => new ObjSpriteInfo(1, 0),
                03 => new ObjSpriteInfo(2, 36), // Pinkie

                05 => new ObjSpriteInfo(4, 0), // Portal
                06 => new ObjSpriteInfo(5, 12),
                07 => new ObjSpriteInfo(6, 36), // Flying Moo
                08 => new ObjSpriteInfo(7, 16),
                09 => new ObjSpriteInfo(8, 4), // Spiker
                10 => new ObjSpriteInfo(9, 68),
                11 => new ObjSpriteInfo(10, 4),
                12 => new ObjSpriteInfo(11, 72),
                13 => new ObjSpriteInfo(12, 54),
                14 => new ObjSpriteInfo(13, 24),
                15 => new ObjSpriteInfo(14, 0), // Moo with shield
                16 => new ObjSpriteInfo(15, 154),
                17 => new ObjSpriteInfo(16, 0), // Moo with spiky shield
                18 => new ObjSpriteInfo(17, 0),
                19 => new ObjSpriteInfo(18, 8),
                20 => new ObjSpriteInfo(19, 28),
                21 => new ObjSpriteInfo(20, 0),

                23 => new ObjSpriteInfo(22, 44),
                24 => new ObjSpriteInfo(23, 76),
                25 => new ObjSpriteInfo(24, 0), // Big spiky ball
                26 => new ObjSpriteInfo(25, 36),
                
                28 => new ObjSpriteInfo(27, 118),
                29 => new ObjSpriteInfo(28, 165),
                30 => new ObjSpriteInfo(29, 41),
                31 => new ObjSpriteInfo(30, 157),
                32 => new ObjSpriteInfo(31, 16),

                35 => new ObjSpriteInfo(14, 0, scale: 2), // Big Moo
                36 => new ObjSpriteInfo(1, 0, scale: 2),
                37 => new ObjSpriteInfo(0, 81, scale: 2), // Big Moo

                39 => new ObjSpriteInfo(14, 0, scale: 2), // Big Moo with shield

                112 => new ObjSpriteInfo(11, 149),
                137 => new ObjSpriteInfo(11, 149, scale: 2),
                _ => new ObjSpriteInfo(-1, -1)
            };
        }

        public static ObjSpriteInfo GetSprite_Collectible(CollectibleObject obj)
        {
            switch (obj.SecondaryType)
            {
                // Switch
                case 1:
                    return new ObjSpriteInfo(68, 10);

                // Dream Stone
                case 2:
                    return obj.Ushort_14 == 0 ? new ObjSpriteInfo(68, 0) : new ObjSpriteInfo(68, 5);

                // Heart, life
                case 3:
                case 4:
                    return obj.Short_0E switch
                    {
                        3 => new ObjSpriteInfo(68, 30),
                        4 => new ObjSpriteInfo(68, 22),
                        15 => new ObjSpriteInfo(68, 57),
                        _ => new ObjSpriteInfo(-1, -1)
                    };

                // Bubble
                case 5:
                case 6:
                case 16:
                case 17:
                    return obj.Short_0E switch
                    {
                        5 => new ObjSpriteInfo(68, 42), // Checkpoint
                        9 => new ObjSpriteInfo(68, 43), // Item
                        13 => new ObjSpriteInfo(68, 44), // x2
                        _ => new ObjSpriteInfo(-1, -1)
                    };

                // Nagapoko Egg
                case 8:
                case 9:
                    return new ObjSpriteInfo(68, 76);

                // Bouncy spring
                case 10:
                    return new ObjSpriteInfo(21, 2);

                // Colored orb (Vision 5-1)
                case 15:
                    return new ObjSpriteInfo(68, 81 + (6 * (obj.Ushort_14 - 2)));

                default:
                    return new ObjSpriteInfo(-1, -1);
            }
        }

        public static Vector3 GetPosition(float x, float y, float z, float scale) => new Vector3(x / scale, -z / scale, -y / scale);

        public static Vector3 GetPosition(MovementPathBlock[] path, int position, Vector3 relativePos, float scale)
        {
            var blockIndex = 0;
            int blockPosOffset;

            if (position < 0)
            {
                blockIndex = 0;
                blockPosOffset = position;
            }
            else
            {
                var iVar6 = 0;

                do
                {
                    var iVar2 = path[blockIndex].BlockLength;

                    if (iVar2 == 0x7ffe)
                    {
                        blockIndex = 0;
                    }
                    else
                    {
                        if (iVar2 == 0x7fff)
                        {
                            iVar6 -= path[blockIndex - 1].BlockLength;
                            break;
                        }

                        iVar6 += iVar2;
                        blockIndex++;
                    }
                } while (iVar6 <= position);

                iVar6 -= position;

                blockIndex--;

                if (iVar6 < 0)
                    blockPosOffset = -iVar6;
                else
                    blockPosOffset = path[blockIndex].BlockLength - iVar6;
            }

            var block = path[blockIndex];

            float xPos = block.XPos + block.DirectionX * blockPosOffset + relativePos.x;
            float yPos = block.YPos + block.DirectionY * blockPosOffset + relativePos.y;
            float zPos = block.ZPos + block.DirectionZ * blockPosOffset + relativePos.z;

            return GetPosition(xPos, yPos, zPos, scale);
        }

        public static void Helper_GenerateCutsceneTextTranslation(Loader_DTP loader, Dictionary<string, char> d, int cutscene, int instruction, string text)
        {
            var c = loader.LevelPack.CutscenePack.Cutscenes[cutscene];
            var i = (CutsceneInstructionData_DrawText)c.Cutscene_Normal.Instructions[instruction].Data;

            var textIndex = 0;

            foreach (var cmd in i.TextCommands)
            {
                if (cmd.Type != CutsceneInstructionData_DrawText.TextCommand.CommandType.DrawChar)    
                    continue;

                var charImgData = c.Font.CharactersImgData[cmd.Command];

                using SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();
                
                var hash = Convert.ToBase64String(sha1.ComputeHash(charImgData));

                if (d.ContainsKey(hash))
                {
                    if (d[hash] != text[textIndex])
                        Debug.LogWarning($"Character {text[textIndex]} has multiple font textures!");
                }
                else
                {
                    d[hash] = text[textIndex];
                }

                textIndex++;
            }

            var str = new StringBuilder();

            foreach (var v in d.OrderBy(x => x.Value))
            {
                var value = v.Value.ToString();

                if (value == "\"" || value == "\'")
                    value = $"\\{value}";

                str.AppendLine($"[\"{v.Key}\"] = '{value}',");
            }

            str.ToString().CopyToClipboard();
        }

        public class ObjSpriteInfo
        {
            public ObjSpriteInfo(int spriteSet, int spriteIndex, int scale = 1, int palOffsetX = 0, int palOffsetY = 500)
            {
                SpriteSet = spriteSet;
                SpriteIndex = spriteIndex;
                Scale = scale;
                PalOffsetX = palOffsetX;
                PalOffsetY = palOffsetY;
            }

            public int SpriteSet { get; }
            public int SpriteIndex { get; }
            public int Scale { get; }
            public int PalOffsetX { get; }
            public int PalOffsetY { get; }
        }
    }
}