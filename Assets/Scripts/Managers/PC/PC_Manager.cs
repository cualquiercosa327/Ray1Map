﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;

namespace R1Engine
{
    /// <summary>
    /// Base game manager for PC
    /// </summary>
    public abstract class PC_Manager : IGameManager {
        #region Values and paths

        /// <summary>
        /// The size of one cell
        /// </summary>
        public const int CellSize = 16;

        /// <summary>
        /// The file mode to use
        /// </summary>
        public virtual FileMode FileMode => FileMode.None;

        /// <summary>
        /// Gets the base path for the game data
        /// </summary>
        /// <param name="basePath">The game base path</param>
        /// <returns>The data path</returns>
        public string GetDataPath(string basePath) => Path.Combine(basePath, "PCMAP");

        /// <summary>
        /// Gets the name for the world
        /// </summary>
        /// <returns>The world name</returns>
        public string GetWorldName(World world) {
            switch (world) {
                case World.Jungle:
                    return "JUNGLE";
                case World.Music:
                    return "MUSIC";
                case World.Mountain:
                    return "MOUNTAIN";
                case World.Image:
                    return "IMAGE";
                case World.Cave:
                    return "CAVE";
                case World.Cake:
                    return "CAKE";
                default:
                    throw new ArgumentOutOfRangeException(nameof(world), world, null);
            }
        }

        /// <summary>
        /// Gets the short name for the world
        /// </summary>
        /// <returns>The short world name</returns>
        public string GetShortWorldName(World world) {
            switch (world) {
                case World.Jungle:
                    return "JUN";
                case World.Music:
                    return "MUS";
                case World.Mountain:
                    return "MON";
                case World.Image:
                    return "IMA";
                case World.Cave:
                    return "CAV";
                case World.Cake:
                    return "CAK";
                default:
                    throw new ArgumentOutOfRangeException(nameof(world), world, null);
            }
        }

        /// <summary>
        /// Gets the file path for the specified level
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The level file path</returns>
        public abstract string GetLevelFilePath(GameSettings settings);

        /// <summary>
        /// Gets the file path for the allfix file
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The allfix file path</returns>
        public virtual string GetAllfixFilePath(GameSettings settings) => Path.Combine(GetDataPath(settings.GameDirectory), $"ALLFIX.DAT");

        /// <summary>
        /// Gets the file path for the big ray file
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The big ray file path</returns>
        public virtual string GetBigRayFilePath(GameSettings settings) => Path.Combine(GetDataPath(settings.GameDirectory), $"BIGRAY.DAT");

        /// <summary>
        /// Gets the file path for the vignette file
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The vignette file path</returns>
        public abstract string GetVignetteFilePath(GameSettings settings);

        /// <summary>
        /// Gets the file path for the specified world file
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The world file path</returns>
        public abstract string GetWorldFilePath(GameSettings settings);

        /// <summary>
        /// Indicates if the game has 3 palettes it swaps between
        /// </summary>
        public abstract bool Has3Palettes { get; }

        /// <summary>
        /// Gets the level count for the specified world
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The level count</returns>
        public abstract int[] GetLevels(GameSettings settings);

        /// <summary>
        /// Gets the available educational volumes
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The available educational volumes</returns>
        public virtual string[] GetEduVolumes(GameSettings settings) => new string[0];

        /// <summary>
        /// Reverse searches an animation
        /// </summary>
        /// <param name="wld">The world file</param>
        /// <param name="eta">The ETA index</param>
        /// <param name="animationIndex">The animation index</param>
        public void ReverseSearchAnimation(PC_WorldFile wld, int eta, int animationIndex)
        {
            foreach (var e in wld.Eta[eta].SelectMany(x => x).Where(x => x.AnimationIndex == animationIndex))
                Debug.Log($"Etat: {e.Etat}, SubEtat: {e.SubEtat}, Speed: {e.AnimationSpeed}");
        }

        /// <summary>
        /// Gets the DES file names, in order, for the world
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The DES file names</returns>
        public virtual IEnumerable<string> GetDESNames(GameSettings settings) => new string[0];

        /// <summary>
        /// Gets the ETA file names, in order, for the world
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The ETA file names</returns>
        public virtual IEnumerable<string> GetETANames(GameSettings settings) => new string[0];

        #endregion

        #region Texture Methods

        /// <summary>
        /// Extracts all found .pcx from an xor encrypted file
        /// </summary>
        /// <param name="filePath">The path of the file to extract from</param>
        /// <param name="outputDir">The directory to output the files to</param>
        public void ExtractEncryptedPCX(string filePath, string outputDir)
        {
            // Create the directory
            Directory.CreateDirectory(outputDir);

            // Read the file bytes
            var originalBytes = File.ReadAllBytes(filePath);

            // Enumerate every possible xor key
            for (int i = 0; i < 255; i++)
            {
                // Create a buffer
                var buffer = new byte[originalBytes.Length];

                // Decrypt the bytes to the buffer
                for (int j = 0; j < buffer.Length; j++)
                    buffer[j] = (byte)(originalBytes[j] ^ i);

                // Enumerate every byte
                for (int j = 0; j < buffer.Length - 100; j++)
                {
                    // Check if a valid PCX header is found
                    if (buffer[j + 0] != 0x0A || buffer[j + 1] != 0x05 || buffer[j + 2] != 0x01 ||
                        buffer[j + 3] != 0x08 || buffer[j + 4] != 0x00 || buffer[j + 5] != 0x00 ||
                        buffer[j + 6] != 0x00 || buffer[j + 7] != 0x00)
                        continue;

                    // Attempt to read the PCX file
                    try
                    {
                        // Create the PCX data
                        var pcx = new PCX();

                        // Serialize the data
                        using (var stream = new MemoryStream(buffer.Skip(j).ToArray()))
                            pcx.Serialize(new BinarySerializer(SerializerMode.Read, stream, pcx, "", Settings.GetGameSettings));

                        // Convert to a texture
                        var tex = pcx.ToTexture();

                        // Flip the texture
                        var flippedTex = new Texture2D(tex.width, tex.height);

                        for (int x = 0; x < tex.width; x++)
                        {
                            for (int y = 0; y < tex.height; y++)
                            {
                                flippedTex.SetPixel(x, tex.height - y - 1, tex.GetPixel(x, y));
                            }
                        }

                        // Apply the pixels
                        flippedTex.Apply();

                        // Save the file
                        File.WriteAllBytes(Path.Combine(outputDir, $"{i} - {j}.png"), flippedTex.EncodeToPNG());

                        Debug.Log("Exported PCX");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to create PCX: {ex.Message}");
                    }
                }
            }

        }

        /// <summary>
        /// Exports all sprite textures to the specified output directory
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="outputDir">The output directory</param>
        public void ExportSpriteTextures(GameSettings settings, string outputDir)
        {
            // Get the DES names for every world
            var desNames = EnumHelpers.GetValues<World>().ToDictionary(x => x, x =>
            {
                settings.World = x;
                var a = GetDESNames(settings).ToArray();
                return a.Any() ? a : null;
            });

            // Read the big ray file
            var brayFile = FileFactory.Read<PC_WorldFile>(GetBigRayFilePath(settings), settings, FileMode);

            // Read the allfix file
            var allfix = FileFactory.Read<PC_WorldFile>(GetAllfixFilePath(settings), settings, FileMode);

            // Export the sprite textures
            ExportSpriteTextures(settings, brayFile, Path.Combine(outputDir, "Bigray"), 0, null, GetBigRayPalette(settings));

            // Export the sprite textures
            ExportSpriteTextures(settings, allfix, Path.Combine(outputDir, "Allfix"), 0, desNames.Values.FirstOrDefault());

            // Enumerate every world
            foreach (World world in EnumHelpers.GetValues<World>())
            {
                // Set the world
                settings.World = world;

                // Get the world file path
                var worldPath = GetWorldFilePath(settings);

                if (!File.Exists(worldPath))
                    continue;

                // Read the world file
                var worldFile = FileFactory.Read<PC_WorldFile>(worldPath, settings, FileMode);

                // Export the sprite textures
                ExportSpriteTextures(settings, worldFile, Path.Combine(outputDir, world.ToString()), allfix.DesItemCount, desNames.TryGetValue(world, out var d) ? d : null);
            }
        }

        /// <summary>
        /// Exports all sprite textures from the world file to the specified output directory
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="worldFile">The world file</param>
        /// <param name="outputDir">The output directory</param>
        /// <param name="desOffset">The amount of textures in the allfix to use as the DES offset if a world texture</param>
        /// <param name="desNames">The DES names, if available</param>
        /// <param name="palette">Optional palette to use</param>
        public void ExportSpriteTextures(GameSettings settings, PC_WorldFile worldFile, string outputDir, int desOffset, string[] desNames, IList<ARGBColor> palette = null)
        {
            // Create the directory
            Directory.CreateDirectory(outputDir);

            var levels = new List<PC_LevFile>();

            // Load the levels to get the palettes
            foreach (var i in GetLevels(settings))
            {
                // Set the level number
                settings.Level = i;

                // Load the level
                levels.Add(FileFactory.Read<PC_LevFile>(GetLevelFilePath(settings), settings, FileMode));
            }

            // Enumerate each sprite group
            for (int i = 0; i < worldFile.DesItems.Length; i++)
            {
                int index = -1;

                // Enumerate each image
                foreach (var tex in GetSpriteTextures(settings, levels, worldFile.DesItems[i], worldFile, desOffset + 1 + i, palette))
                {
                    index++;

                    // Skip if null
                    if (tex == null)
                        continue;

                    // Get the DES name
                    var desName = desNames != null ? $" ({desNames[desOffset + i]})" : String.Empty;

                    // Write the texture
                    File.WriteAllBytes(Path.Combine(outputDir, $"{i.ToString()}{desName} - {index}.png"), tex.EncodeToPNG());
                }
            }
        }

        /// <summary>
        /// Gets all sprite textures for a DES item
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="levels">The levels in the world to check for the palette</param>
        /// <param name="desItem">The DES item</param>
        /// <param name="worldFile">The world data</param>
        /// <param name="desIndex">The DES index</param>
        /// <param name="palette">Optional palette to use</param>
        /// <returns>The sprite textures</returns>
        public Texture2D[] GetSpriteTextures(GameSettings settings, List<PC_LevFile> levels, PC_DesItem desItem, PC_WorldFile worldFile, int desIndex, IList<ARGBColor> palette = null)
        {
            // Create the output array
            var output = new Texture2D[desItem.ImageDescriptors.Length];

            bool foundForSpriteGroup = false;
            var defaultPalette = levels.First();

            Beginning:

            // Process the image data
            var processedImageData = ProcessImageData(desItem.ImageData, desItem.RequiresBackgroundClearing);

            // Enumerate each image
            for (int i = 0; i < desItem.ImageDescriptors.Length; i++)
            {
                // Get the image descriptor
                var imgDescriptor = desItem.ImageDescriptors[i];

                // Ignore garbage sprites
                if (imgDescriptor.InnerHeight == 0 || imgDescriptor.InnerWidth == 0)
                    continue;

                // Default to the first level
                var lvl = defaultPalette;

                bool foundCorrectPalette = false;

                // Check all matching animation descriptor
                foreach (var animDesc in desItem.AnimationDescriptors.Where(x => x.Layers.Any(y => y.ImageIndex == i)).Select(x => desItem.AnimationDescriptors.FindItemIndex(y => y == x)))
                {
                    // Check all ETA's where it appears
                    foreach (var eta in worldFile.Eta.SelectMany(x => x).SelectMany(x => x).Where(x => x.AnimationIndex == animDesc))
                    {
                        // Attempt to find the level where it appears
                        var lvlMatch = levels.FindLast(x => x.Events.Any(y =>
                            y.DES == desIndex &&
                            y.Etat == eta.Etat &&
                            y.SubEtat == eta.SubEtat &&
                            y.ETA == worldFile.Eta.FindItemIndex(z => z.SelectMany(h => h).Contains(eta))));

                        if (lvlMatch != null)
                        {
                            lvl = lvlMatch;
                            foundCorrectPalette = true;

                            if (!foundForSpriteGroup)
                            {
                                foundForSpriteGroup = true;
                                defaultPalette = lvlMatch;
                                goto Beginning;
                            }

                            break;
                        }
                    }
                }

                // Check background DES
                if (!foundCorrectPalette)
                {
                    var lvlMatch = levels.FindLast(x => x.BackgroundSpritesDES == desIndex);

                    if (lvlMatch != null)
                        lvl = lvlMatch;
                }

                // Hard-code palette for certain DES groups
                if (settings.GameMode == GameMode.RayPC && settings.World == World.Music && desIndex == 16)
                    lvl = levels[11];
                else if (settings.GameMode == GameMode.RayPC && settings.World == World.Image && desIndex == 19)
                    lvl = levels[10];

                // Get the texture
                Texture2D tex = GetSpriteTexture(imgDescriptor, palette ?? lvl.ColorPalettes.First(), processedImageData);

                // Set the texture
                output[i] = tex;
            }

            // Return the output
            return output;
        }

        /// <summary>
        /// Exports all animation frames to the specified directory
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="outputDir">The directory to export to</param>
        public void ExportAnimationFrames(GameSettings settings, string outputDir)
        {
            // Get the DES names for every world
            var desNames = EnumHelpers.GetValues<World>().ToDictionary(x => x, x =>
            {
                settings.World = x;
                var a = GetDESNames(settings).ToArray();
                return a.Any() ? a : null;
            });

            // Read the big ray file
            var brayFile = FileFactory.Read<PC_WorldFile>(GetBigRayFilePath(settings), settings, FileMode);

            // Read the allfix file
            var allfix = FileFactory.Read<PC_WorldFile>(GetAllfixFilePath(settings), settings, FileMode);

            // Export the sprite textures
            ExportAnimationFrames(settings, brayFile, Path.Combine(outputDir, "Bigray"), 0, null, GetBigRayPalette(settings));

            // Export the sprite textures
            ExportAnimationFrames(settings, allfix, Path.Combine(outputDir, "Allfix"), 0, desNames.Values.FirstOrDefault());
            
            // Enumerate every world
            foreach (World world in EnumHelpers.GetValues<World>())
            {
                // Set the world
                settings.World = world;

                // Get the world file path
                var worldPath = GetWorldFilePath(settings);

                if (!File.Exists(worldPath))
                    continue;

                // Read the world file
                var worldFile = FileFactory.Read<PC_WorldFile>(worldPath, settings, FileMode);

                // Export the sprite textures
                ExportAnimationFrames(settings, worldFile, Path.Combine(outputDir, world.ToString()), allfix.DesItemCount, desNames.TryGetValue(world, out var d) ? d : null);
            }
        }

        /// <summary>
        /// Exports the animation frames
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="worldFile">The world file to export from</param>
        /// <param name="outputDir">The directory to export to</param>
        /// <param name="desOffset">The amount of textures in the allfix to use as the DES offset if a world texture</param>
        /// <param name="desNames">The DES names, if available</param>
        /// <param name="palette">Optional palette to use</param>
        public void ExportAnimationFrames(GameSettings settings, PC_WorldFile worldFile, string outputDir, int desOffset, string[] desNames, IList<ARGBColor> palette = null)
        {
            // Create the directory
            Directory.CreateDirectory(outputDir);

            var levels = new List<PC_LevFile>();

            // Load the levels to get the palettes
            foreach (var i in GetLevels(settings))
            {
                // Set the level number
                settings.Level = i;

                // Load the level
                levels.Add(FileFactory.Read<PC_LevFile>(GetLevelFilePath(settings), settings, FileMode));
            }

            // Enumerate each sprite group
            for (int i = 0; i < worldFile.DesItems.Length; i++)
            {
                // Get the DES item
                var des = worldFile.DesItems[i];

                // Get the DES index
                var desIndex = desOffset + 1 + i;

                // Get the textures
                var textures = GetSpriteTextures(settings, levels, des, worldFile, desIndex, palette);

                // Get the DES name
                var desName = desNames != null ? $" ({desNames[desIndex - 1]})" : String.Empty;

                // Get the folder
                var desFolderPath = Path.Combine(outputDir, $"{i}{desName}");

                byte prevSpeed = 4;

                // Enumerate the animations
                for (var j = 0; j < des.AnimationDescriptors.Length; j++)
                {
                    // Get the animation descriptor
                    var anim = des.AnimationDescriptors[j];

                    byte? speed = null;

                    IEnumerable<PC_Eta> GetEtaMatches(int etaIndex) => worldFile.Eta[etaIndex].SelectMany(x => x).Where(x => x.AnimationIndex == j);

                    // Attempt to find a perfect match
                    for (int etaIndex = 0; etaIndex < worldFile.Eta.Length; etaIndex++)
                    {
                        if (speed != null)
                            break;

                        foreach (var etaMatch in GetEtaMatches(etaIndex))
                        {
                            // Check if it's a perfect match
                            if (levels.SelectMany(x => x.Events).Any(x => x.DES == desIndex &&
                                                                          x.ETA == etaIndex &&
                                                                          x.Etat == etaMatch.Etat &&
                                                                          x.SubEtat == etaMatch.SubEtat))
                            {
                                speed = etaMatch.AnimationSpeed;
                                break;
                            }
                        }
                    }

                    // If still null, find semi-perfect match
                    if (speed == null)
                    {
                        // Attempt to find a semi-perfect match
                        for (int etaIndex = 0; etaIndex < worldFile.Eta.Length; etaIndex++)
                        {
                            if (speed != null)
                                break;

                            foreach (var etaMatch in GetEtaMatches(etaIndex))
                            {
                                // Check if it's a semi-perfect match
                                if (levels.SelectMany(x => x.Events).Any(x => x.DES == desIndex && x.ETA == etaIndex))
                                {
                                    speed = etaMatch.AnimationSpeed;
                                    break;
                                }
                            }
                        }

                        // If still null, use previous eta
                        if (speed == null)
                            speed = prevSpeed;
                    }

                    // Set the previous eta
                    prevSpeed = speed.Value;

                    // Get the folder
                    var animFolderPath = Path.Combine(desFolderPath, $"{j}-{speed}");

                    // The layer index
                    var layer = 0;

                    var frameWidth = anim.Frames.Max(x => x.XPosition + x.Width) + 1;
                    var frameHeight = anim.Frames.Max(x => x.YPosition + x.Height) + 1;

                    // Create each animation frame
                    for (int frameIndex = 0; frameIndex < anim.FrameCount; frameIndex++)
                    {
                        Texture2D tex = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false)
                        {
                            filterMode = FilterMode.Point
                        };

                        // Default to fully transparent
                        for (int y = 0; y < tex.height; y++)
                        {
                            for (int x = 0; x < tex.width; x++)
                            {
                                tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                            }
                        }

                        bool hasLayers = false;

                        // Write each layer
                        for (var layerIndex = 0; layerIndex < anim.LayersPerFrame; layerIndex++)
                        {
                            var animationLayer = anim.Layers[layer];

                            layer++;

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

                                    var xStart = animationLayer.XPosition;
                                    var yStart = animationLayer.YPosition;
                                    var xPosition = (animationLayer.IsFlipped ? (sprite.width - 1 - x) : x) + xStart;
                                    var yPosition = -(y + yStart + 1);

                                    if (c.a != 0)
                                        tex.SetPixel(xPosition, yPosition, c);
                                }
                            }

                            hasLayers = true;
                        }

                        tex.Apply();

                        if (!hasLayers)
                            continue;

                        // Create the directory
                        Directory.CreateDirectory(animFolderPath);

                        // Save the file
                        File.WriteAllBytes(Path.Combine(animFolderPath, $"{frameIndex}.png"), tex.EncodeToPNG());
                    }
                }
            }
        }

        /// <summary>
        /// Processes the image data
        /// </summary>
        /// <param name="imageData">The image data to process</param>
        /// <param name="requiresBackgroundClearing">Indicates if the data requires background clearing</param>
        /// <returns>The processed image data</returns>
        public byte[] ProcessImageData(byte[] imageData, bool requiresBackgroundClearing)
        {
            // Create the output array
            var processedData = new byte[imageData.Length];

            int flag = -1;

            for (int i = imageData.Length - 1; i >= 0; i--)
            {
                // Decrypt the byte
                processedData[i] = (byte)(imageData[i] ^ 143);

                // Continue to next if we don't need to do background clearing
                if (!requiresBackgroundClearing)
                    continue;

                int num6 = (flag < 255) ? (flag + 1) : 255;

                if (processedData[i] == 161 || processedData[i] == 250)
                {
                    flag = processedData[i];
                    processedData[i] = 0;
                }
                else if (flag != -1)
                {
                    if (processedData[i] == num6)
                    {
                        processedData[i] = 0;
                        flag = num6;
                    }
                    else
                    {
                        flag = -1;
                    }
                }
            }

            return processedData;
        }

        /// <summary>
        /// Gets the texture for a sprite
        /// </summary>
        /// <param name="s">The image descriptor</param>
        /// <param name="palette">The palette to use</param>
        /// <param name="processedImageData">The processed image data to use</param>
        /// <returns>The sprite texture</returns>
        public Texture2D GetSpriteTexture(PC_ImageDescriptor s, IList<ARGBColor> palette, byte[] processedImageData)
        {
            // Get the image properties
            var width = s.OuterWidth;
            var height = s.OuterHeight;
            var offset = s.ImageOffset;

            // Create the texture
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            // Default to fully transparent
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }

            try
            {
                // Set every pixel
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Get the pixel offset
                        var pixelOffset = y * width + x + offset;

                        // Get the palette index
                        var pixel = processedImageData[pixelOffset];

                        // Ignore if 0
                        if (pixel == 0)
                            continue;

                        // Get the color from the palette
                        var color = palette[pixel];

                        // Set the pixel
                        tex.SetPixel(x, -(y + 1), color.GetColor());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Couldn't load sprite for DES: {ex.Message}");

                return null;
            }

            // Apply the changes
            tex.Apply();

            // Return the texture
            return tex;
        }

        #endregion

        #region Manager Methods

        /// <summary>
        /// Gets the BigRay color palette, if available
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The color palette or null if not available</returns>
        protected virtual IList<ARGBColor> GetBigRayPalette(GameSettings settings) => null;

        /// <summary>
        /// Exports all vignette textures to the specified output directory
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="outputDir">The output directory</param>
        public void ExportVignetteTextures(GameSettings settings, string outputDir)
        {
            // Get the file path
            var vigFilePath = GetVignetteFilePath(settings);

            // Extract the file
            ExtractEncryptedPCX(vigFilePath, outputDir);
        }

        /// <summary>
        /// Auto applies the palette to the tiles in the level
        /// </summary>
        /// <param name="level">The level to auto-apply the palette to</param>
        public void AutoApplyPalette(Common_Lev level)
        {
            // Get the palette changers
            var paletteXChangers = level.Events.Where(x => x.EventInfoData.Type == 158 && x.EventInfoData.SubEtat < 6).ToDictionary(x => x.XPosition, x => (PC_PaletteChangerMode)x.EventInfoData.SubEtat);
            var paletteYChangers = level.Events.Where(x => x.EventInfoData.Type == 158 && x.EventInfoData.SubEtat >= 6).ToDictionary(x => x.YPosition, x => (PC_PaletteChangerMode)x.EventInfoData.SubEtat);

            // TODO: The auto system won't always work since it just checks one type of palette swapper and doesn't take into account that the palette swappers only trigger when on-screen, rather than based on the axis. Because of this some levels, like Music 5, won't work. More are messed up in the EDU games. There is sadly no solution to this since it depends on the players movement.
            // Check which type of palette changer we have
            bool isPaletteHorizontal = paletteXChangers.Any();

            // Keep track of the default palette
            int defaultPalette = 1;

            // Get the default palette
            if (isPaletteHorizontal && paletteXChangers.Any())
            {
                switch (paletteXChangers.OrderBy(x => x.Key).First().Value)
                {
                    case PC_PaletteChangerMode.Left1toRight2:
                    case PC_PaletteChangerMode.Left1toRight3:
                        defaultPalette = 1;
                        break;
                    case PC_PaletteChangerMode.Left2toRight1:
                    case PC_PaletteChangerMode.Left2toRight3:
                        defaultPalette = 2;
                        break;
                    case PC_PaletteChangerMode.Left3toRight1:
                    case PC_PaletteChangerMode.Left3toRight2:
                        defaultPalette = 3;
                        break;
                }
            }
            else if (!isPaletteHorizontal && paletteYChangers.Any())
            {
                switch (paletteYChangers.OrderByDescending(x => x.Key).First().Value)
                {
                    case PC_PaletteChangerMode.Top1tobottom2:
                    case PC_PaletteChangerMode.Top1tobottom3:
                        defaultPalette = 1;
                        break;
                    case PC_PaletteChangerMode.Top2tobottom1:
                    case PC_PaletteChangerMode.Top2tobottom3:
                        defaultPalette = 2;
                        break;
                    case PC_PaletteChangerMode.Top3tobottom1:
                    case PC_PaletteChangerMode.Top3tobottom2:
                        defaultPalette = 3;
                        break;
                }
            }

            // Keep track of the current palette
            int currentPalette = defaultPalette;

            // Enumerate each cell
            for (int cellY = 0; cellY < level.Height; cellY++)
            {
                // Reset the palette on each row if we have a horizontal changer
                if (isPaletteHorizontal)
                    currentPalette = defaultPalette;
                // Otherwise check the y position
                else
                {
                    // Check every pixel 16 steps forward
                    for (int y = 0; y < CellSize; y++)
                    {
                        // Attempt to find a matching palette changer on this pixel
                        var py = paletteYChangers.TryGetValue((uint)(CellSize * cellY + y), out PC_PaletteChangerMode pm) ? (PC_PaletteChangerMode?)pm : null;

                        // If one was found, change the palette based on type
                        if (py != null)
                        {
                            switch (py)
                            {
                                case PC_PaletteChangerMode.Top2tobottom1:
                                case PC_PaletteChangerMode.Top3tobottom1:
                                    currentPalette = 1;
                                    break;
                                case PC_PaletteChangerMode.Top1tobottom2:
                                case PC_PaletteChangerMode.Top3tobottom2:
                                    currentPalette = 2;
                                    break;
                                case PC_PaletteChangerMode.Top1tobottom3:
                                case PC_PaletteChangerMode.Top2tobottom3:
                                    currentPalette = 3;
                                    break;
                            }
                        }
                    }
                }

                for (int cellX = 0; cellX < level.Width; cellX++)
                {
                    // Check the x position for palette changing
                    if (isPaletteHorizontal)
                    {
                        // Check every pixel 16 steps forward
                        for (int x = 0; x < CellSize; x++)
                        {
                            // Attempt to find a matching palette changer on this pixel
                            var px = paletteXChangers.TryGetValue((uint)(CellSize * cellX + x), out PC_PaletteChangerMode pm) ? (PC_PaletteChangerMode?)pm : null;

                            // If one was found, change the palette based on type
                            if (px != null)
                            {
                                switch (px)
                                {
                                    case PC_PaletteChangerMode.Left3toRight1:
                                    case PC_PaletteChangerMode.Left2toRight1:
                                        currentPalette = 1;
                                        break;
                                    case PC_PaletteChangerMode.Left1toRight2:
                                    case PC_PaletteChangerMode.Left3toRight2:
                                        currentPalette = 2;
                                        break;
                                    case PC_PaletteChangerMode.Left1toRight3:
                                    case PC_PaletteChangerMode.Left2toRight3:
                                        currentPalette = 3;
                                        break;
                                }
                            }
                        }
                    }

                    // Set the common tile
                    level.Tiles[cellY * level.Width + cellX].PaletteIndex = currentPalette;
                }
            }
        }

        /// <summary>
        /// Loads the sprites for the level
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="palette">The palette to use</param>
        /// <param name="eventDesigns">The list of event designs to populate</param>
        /// <returns>The ETA</returns>
        public async Task<PC_Eta[][][]> LoadSpritesAsync(GameSettings settings, IList<ARGBColor> palette, List<Common_Design> eventDesigns)
        {
            Controller.status = $"Loading allfix";

            // Read the fixed data
            var allfix = FileFactory.Read<PC_WorldFile>(GetAllfixFilePath(settings), settings, FileMode);

            await Controller.WaitIfNecessary();

            Controller.status = $"Loading world";

            // Read the world data
            var worldData = FileFactory.Read<PC_WorldFile>(GetWorldFilePath(settings), settings, FileMode);

            await Controller.WaitIfNecessary();

            Controller.status = $"Loading big ray";

            // NOTE: This is not loaded into normal levels and is purely loaded here so the animation can be viewed!
            // Read the big ray data
            var bigRayData = FileFactory.Read<PC_WorldFile>(GetBigRayFilePath(settings), settings, FileMode);

            await Controller.WaitIfNecessary();

            // Get the DES and ETA
            var des = allfix.DesItems.Concat(worldData.DesItems).Concat(bigRayData.DesItems).ToArray();
            var eta = allfix.Eta.Concat(worldData.Eta).Concat(bigRayData.Eta).ToArray();

            int desIndex = 0;

            // Read every DES item
            foreach (var d in des)
            {
                Controller.status = $"Loading DES {desIndex}/{des.Length}";

                await Controller.WaitIfNecessary();

                Common_Design finalDesign = new Common_Design
                {
                    Sprites = new List<Sprite>(),
                    Animations = new List<Common_Animation>()
                };

                // Process the image data
                var processedImageData = ProcessImageData(d.ImageData, d.RequiresBackgroundClearing);

                // Sprites
                foreach (var s in d.ImageDescriptors)
                {
                    // Ignore garbage sprites
                    var isGarbage = s.InnerHeight == 0 || s.InnerWidth == 0;

                    // Get the palette to use
                    var p = palette;

                    // Use BigRay palette if the last item
                    if (desIndex == des.Length - 1)
                        p = GetBigRayPalette(settings) ?? palette;

                    // Get the texture
                    Texture2D tex = isGarbage ? null : GetSpriteTexture(s, p, processedImageData);

                    // Add it to the array
                    finalDesign.Sprites.Add(tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 1f), 16, 20));
                }

                // Animations
                foreach (var a in d.AnimationDescriptors)
                {
                    // Create the animation
                    var animation = new Common_Animation
                    {
                        Frames = new Common_AnimationPart[a.FrameCount, a.LayersPerFrame],
                    };
                    // The layer index
                    var layer = 0;
                    // Create each frame
                    for (int i = 0; i < a.FrameCount; i++)
                    {
                        // Create each layer
                        for (var layerIndex = 0; layerIndex < a.LayersPerFrame; layerIndex++)
                        {
                            var animationLayer = a.Layers[layer];
                            layer++;

                            // Create the animation part
                            var part = new Common_AnimationPart
                            {
                                SpriteIndex = animationLayer.ImageIndex,
                                X = animationLayer.XPosition,
                                Y = animationLayer.YPosition,
                                Flipped = animationLayer.IsFlipped
                            };

                            // Add the texture
                            animation.Frames[i, layerIndex] = part;
                        }
                    }
                    // Add the animation to list
                    finalDesign.Animations.Add(animation);
                }

                // Add to the designs
                eventDesigns.Add(finalDesign);
                desIndex++;
            }

            // Return the ETA
            return eta;
        }

        /// <summary>
        /// Loads the specified level
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="eventDesigns">The list of event designs to populate</param>
        /// <returns>The level</returns>
        public virtual async Task<Common_Lev> LoadLevelAsync(GameSettings settings, List<Common_Design> eventDesigns) {
            Controller.status = $"Loading map data for {settings.World} {settings.Level}";

            // Read the level data
            var levelData = FileFactory.Read<PC_LevFile>(GetLevelFilePath(settings), settings, FileMode);

            await Controller.WaitIfNecessary();

            // Convert levelData to common level format
            Common_Lev commonLev = new Common_Lev {
                // Set the dimensions
                Width = levelData.Width,
                Height = levelData.Height,

                // Create the events list
                Events = new List<Common_Event>(),

                // Create the tile arrays
                TileSet = new Common_Tileset[3],
                Tiles = new Common_Tile[levelData.Width * levelData.Height],
            };

            // Load the sprites
            var eta = await LoadSpritesAsync(settings, levelData.ColorPalettes.First(), eventDesigns);

            // Add the events
            commonLev.Events = new List<Common_Event>();

            var index = 0;

            foreach (PC_Event e in levelData.Events) {
                Controller.status = $"Loading event {index}/{levelData.EventCount}";

                await Controller.WaitIfNecessary();

                //Get animation index from the eta item
                var etaItem = eta[e.ETA].SelectMany(x => x).FindItem(x => x.Etat == e.Etat && x.SubEtat == e.SubEtat);
                int animIndex = etaItem?.AnimationIndex ?? 0;
                int animSpeed = etaItem?.AnimationSpeed ?? 0;

                // Instantiate event prefab using LevelEventController
                var ee = Controller.obj.levelEventController.AddEvent(
                    EventInfoManager.GetPCEventInfo(settings.GameModeSelection, settings.World, (int)e.Type, e.Etat, e.SubEtat, (int)e.DES, (int)e.ETA, e.OffsetBX, e.OffsetBY, e.OffsetHY, e.FollowSprite, e.HitPoints, e.HitSprite, e.FollowEnabled, levelData.EventCommands[index].LabelOffsetTable, levelData.EventCommands[index].EventCode),
                    e.XPosition,
                    e.YPosition,
                    levelData.EventLinkingTable[index],
                    animIndex,
                    animSpeed);

                // Add the event
                commonLev.Events.Add(ee);

                index++;
            }

            Controller.status = $"Loading tile set";

            // Read the 3 tile sets (one for each palette)
            var tileSets = ReadTileSets(levelData);

            // Set the tile sets
            commonLev.TileSet[0] = tileSets[0];
            commonLev.TileSet[1] = tileSets[1];
            commonLev.TileSet[2] = tileSets[2];

            // Enumerate each cell
            for (int cellY = 0; cellY < levelData.Height; cellY++) 
            {
                for (int cellX = 0; cellX < levelData.Width; cellX++) 
                {
                    // Get the cell
                    var cell = levelData.Tiles[cellY * levelData.Width + cellX];

                    // Get the texture index, default to 0 for fully transparent (no texture)
                    var textureIndex = 0;

                    // Ignore if fully transparent
                    if (cell.TransparencyMode != PC_MapTileTransparencyMode.FullyTransparent) {
                        // Get the offset for the texture
                        var texOffset = levelData.TexturesOffsetTable[cell.TextureIndex];

                        // Get the texture
                        var texture = cell.TransparencyMode == PC_MapTileTransparencyMode.NoTransparency ? levelData.NonTransparentTextures.FindItem(x => x.Offset == texOffset) : levelData.TransparentTextures.FindItem(x => x.Offset == texOffset);

                        // Get the index
                        textureIndex = levelData.NonTransparentTextures.Concat(levelData.TransparentTextures).FindItemIndex(x => x == texture);
                    }

                    // Set the common tile
                    commonLev.Tiles[cellY * levelData.Width + cellX] = new Common_Tile() 
                    {
                        TileSetGraphicIndex = textureIndex,
                        CollisionType = cell.CollisionType,
                        PaletteIndex = 1,
                        XPosition = cellX,
                        YPosition = cellY
                    };
                }
            }

            // Return the common level data
            return commonLev;
        }

        /// <summary>
        /// Reads 3 tile-sets, one for each palette
        /// </summary>
        /// <param name="levData">The level data to get the tile-set for</param>
        /// <returns>The 3 tile-sets</returns>
        public Common_Tileset[] ReadTileSets(PC_LevFile levData) {
            // Create the output array
            var output = new Common_Tileset[]
            {
                new Common_Tileset(new Tile[levData.TexturesCount]),
                new Common_Tileset(new Tile[levData.TexturesCount]),
                new Common_Tileset(new Tile[levData.TexturesCount]),
            };

            // Keep track of the tile index
            int index = 0;

            // Enumerate every texture
            foreach (var texture in levData.NonTransparentTextures.Concat(levData.TransparentTextures)) {
                // Enumerate every palette
                for (int i = 0; i < levData.ColorPalettes.Length; i++) {
                    // Create the texture to use for the tile
                    var tileTexture = new Texture2D(CellSize, CellSize, TextureFormat.RGBA32, false) {
                        filterMode = FilterMode.Point
                    };

                    // Write each pixel to the texture
                    for (int y = 0; y < CellSize; y++) {
                        for (int x = 0; x < CellSize; x++) {
                            // Get the index
                            var cellIndex = CellSize * y + x;

                            // Get the color from the current palette (or default to fully transparent if it's the first tile)
                            var c = index == 0 ? new Color(0, 0, 0, 0) : levData.ColorPalettes[i][255 - texture.ColorIndexes[cellIndex]].GetColor();

                            // If the texture is transparent, add the alpha channel
                            if (texture is PC_TransparentTileTexture tt)
                                c.a = (float)tt.Alpha[cellIndex] / Byte.MaxValue;

                            // Set the pixel
                            tileTexture.SetPixel(x, y, c);
                        }
                    }

                    // Apply the pixels to the texture
                    tileTexture.Apply();

                    // Create and set up the tile
                    output[i].SetTile(tileTexture, CellSize, index);
                }

                index++;
            }

            return output;
        }

        /// <summary>
        /// Saves the specified level
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <param name="commonLevelData">The common level data</param>
        public void SaveLevel(GameSettings settings, Common_Lev commonLevelData) {
            // Get the level file path
            var lvlPath = GetLevelFilePath(settings);

            // Get the level data
            var lvlData = FileFactory.Read<PC_LevFile>(lvlPath, settings, FileMode);

            // Update the tiles
            for (int y = 0; y < lvlData.Height; y++) {
                for (int x = 0; x < lvlData.Width; x++) {
                    // Get the tiles
                    var tile = lvlData.Tiles[y * lvlData.Width + x];
                    var commonTile = commonLevelData.Tiles[y * lvlData.Width + x];

                    // Update the tile
                    tile.CollisionType = commonTile.CollisionType;

                    if (commonTile.TileSetGraphicIndex == 0) {
                        tile.TextureIndex = 0;
                        tile.TransparencyMode = PC_MapTileTransparencyMode.FullyTransparent;
                    }
                    else if (commonTile.TileSetGraphicIndex < lvlData.NonTransparentTexturesCount) {
                        tile.TextureIndex = (ushort)lvlData.TexturesOffsetTable.FindItemIndex(z => z == lvlData.NonTransparentTextures[commonTile.TileSetGraphicIndex].Offset);
                        tile.TransparencyMode = PC_MapTileTransparencyMode.NoTransparency;
                    }
                    else {
                        tile.TextureIndex = (ushort)lvlData.TexturesOffsetTable.FindItemIndex(z => z == lvlData.TransparentTextures[(commonTile.TileSetGraphicIndex - lvlData.NonTransparentTexturesCount)].Offset);
                        tile.TransparencyMode = PC_MapTileTransparencyMode.PartiallyTransparent;
                    }
                }
            }

            // Temporary event lists
            var events = new List<PC_Event>();
            var eventCommands = new List<PC_EventCommand>();
            var eventLinkingTable = new List<ushort>();

            // Set events
            // First correct their linkIndexes based on their linkID
            int currentId = 1;
            List<int> alreadyChained = new List<int>();
            foreach (Common_Event ee in commonLevelData.Events)
            {
                // No link
                if (ee.LinkID == 0) {
                    ee.LinkIndex = commonLevelData.Events.IndexOf(ee);
                }
                else {
                    // Skip if already chained
                    if (alreadyChained.Contains(commonLevelData.Events.IndexOf(ee)))
                        continue;

                    // Find all the events with the same linkId and store their indexes
                    List<int> indexesOfSameId = new List<int>();
                    foreach (Common_Event e in commonLevelData.Events.Where(e => e.LinkID == currentId))
                    {
                        indexesOfSameId.Add(commonLevelData.Events.IndexOf(e));
                        alreadyChained.Add(commonLevelData.Events.IndexOf(e));
                    }
                    // Loop through and chain them
                    for (int j = 0; j < indexesOfSameId.Count; j++) {
                        int next = j + 1;
                        if (next == indexesOfSameId.Count)
                            next = 0;

                        commonLevelData.Events[indexesOfSameId[j]].LinkIndex = indexesOfSameId[next];
                    }

                    currentId++;
                }
            }

            int indexx = 0; //Only for debugging
            foreach (var e in commonLevelData.Events) {
                Debug.Log(indexx + ":" + e.LinkIndex + " - " + e.name);
                indexx++;
                // Create the event
                var r1Event = new PC_Event
                {
                    DES = (uint)e.EventInfoData.DES,
                    DES2 = (uint)e.EventInfoData.DES,
                    DES3 = (uint)e.EventInfoData.DES,
                    ETA = (uint)e.EventInfoData.ETA,
                    Unknown1 = 0,
                    Unknown2 = 0,
                    Unknown3 = new byte[16],
                    XPosition = e.XPosition,
                    YPosition = e.YPosition,
                    Unknown13 = 0,
                    Unknown4 = new byte[20],
                    Unknown5 = new byte[28],
                    Type = (uint)e.EventInfoData.Type,
                    Unknown6 = 0,
                    OffsetBX = (byte)e.EventInfoData.OffsetBX,
                    OffsetBY = (byte)e.EventInfoData.OffsetBY,
                    Unknown7 = 0,
                    SubEtat = (byte)e.EventInfoData.SubEtat,
                    Etat = (byte)e.EventInfoData.Etat,
                    Unknown8 = 0,
                    Unknown9 = 0,
                    OffsetHY = (byte)e.EventInfoData.OffsetHY,
                    FollowSprite = (byte)e.EventInfoData.FollowSprite,
                    HitPoints = (byte)e.EventInfoData.HitPoints,
                    UnkGroup = 0,
                    HitSprite = (byte)e.EventInfoData.HitSprite,
                    Unknown10 = new byte[6],
                    Unknown11 = 0,
                    FollowEnabled = e.EventInfoData.FollowEnabled,
                    Unknown12 = 0
                };

                // Add the event
                events.Add(r1Event);

                // Add the event commands
                eventCommands.Add(new PC_EventCommand()
                {
                    CodeCount = (ushort)e.EventInfoData.Commands.Length,
                    EventCode = e.EventInfoData.Commands,
                    LabelOffsetCount = (ushort)e.EventInfoData.LabelOffsets.Length,
                    LabelOffsetTable = e.EventInfoData.LabelOffsets
                });

                // Add the event links
                eventLinkingTable.Add((ushort)e.LinkIndex);
            }

            // Update event values
            lvlData.EventCount = (ushort)events.Count;
            lvlData.Events = events.ToArray();
            lvlData.EventCommands = eventCommands.ToArray();
            lvlData.EventLinkingTable = eventLinkingTable.ToArray();

            // Save the file
            FileFactory.Write(lvlPath, settings);
        }

        #endregion
    }
}