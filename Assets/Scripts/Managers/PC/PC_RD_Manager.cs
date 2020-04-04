﻿using System;
using R1Engine.Serialize;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace R1Engine
{
    /// <summary>
    /// The game manager for Rayman Designer (PC)
    /// </summary>
    public class PC_RD_Manager : PC_Manager
    {
        #region Static Properties

        /// <summary>
        /// The events which are multi-colored
        /// </summary>
        public static EventType[] MultiColoredEvents => new[]
        {
            EventType.MS_compteur,
            EventType.MS_wiz_comptage,
            EventType.MS_pap,
        };

        /// <summary>
        /// The DES which are multi-colored
        /// </summary>
        public static string[] MultiColoredDES => new[]
        {
            "WIZCOMPT",
            "PCH",
        };

        #endregion

        #region Values and paths

        /// <summary>
        /// Gets the file path for the specified level
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The level file path</returns>
        public override string GetLevelFilePath(GameSettings settings) => GetDataPath() + $"{GetShortWorldName(settings.World)}{settings.Level:00}.LEV";

        /// <summary>
        /// Gets the file path for the specified world file
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The world file path</returns>
        public override string GetWorldFilePath(GameSettings settings) => GetDataPath() + $"RAY{((int)settings.World + 1):00}.WLD";

        /// <summary>
        /// Gets the file path for the vignette file
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The vignette file path</returns>
        public override string GetVignetteFilePath(GameSettings settings) => GetDataPath() + $"VIGNET.DAT";

        /// <summary>
        /// Indicates if the game has 3 palettes it swaps between
        /// </summary>
        public override bool Has3Palettes => false;

        /// <summary>
        /// Gets the levels for each world
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The levels</returns>
        public override KeyValuePair<World, int[]>[] GetLevels(GameSettings settings) => EnumHelpers.GetValues<World>().Select(w => new KeyValuePair<World, int[]>(w, Directory.EnumerateFiles(settings.GameDirectory + GetDataPath(), $"{GetShortWorldName(w)}??.LEV", SearchOption.TopDirectoryOnly)
            .Select(FileSystem.GetFileNameWithoutExtensions)
            .Select(x => Int32.Parse(x.Substring(3)))
            .ToArray())).ToArray();

        /// <summary>
        /// Gets the DES file names, in order, for the world
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>The DES file names</returns>
        public override IEnumerable<string> GetDESNames(Context context)
        {
            return EnumerateWLDManifest(context).Where(str => str.Contains("DES"));
        }

        /// <summary>
        /// Gets the ETA file names, in order, for the world
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>The ETA file names</returns>
        public override IEnumerable<string> GetETANames(Context context)
        {
            return EnumerateWLDManifest(context).Where(str => str.Contains("ETA"));
        }

        /// <summary>
        /// Enumerates the strings in a .wld manifest
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>The found strings</returns>
        protected IEnumerable<string> EnumerateWLDManifest(Context context)
        {
            // Get the encoding
            var e = Settings.StringEncoding;

            // TODO: Find better way to parse this
            // Read the world file and get the last data group
            var wld = FileFactory.Read<PC_WorldFile>(GetWorldFilePath(context.Settings), context,
                (s, data) => data.FileType = PC_WorldFile.Type.World).Unknown5;

            // Get the DES file names
            for (int i = 1; i < wld.Length; i += 13)
            {
                // Read the bytes until we reach NULL
                var length = 0;

                for (int j = 0; j < 13; j++, length++)
                {
                    if (wld[i + j] == 0x00)
                        break;
                }

                // Get the string
                var str = e.GetString(wld, i, length);

                // Return it
                yield return str;
            }
        }

        // Temp value for getting the des names while loading a level - find better solution?
        public IList<string> DESNames { get; set; }

        #endregion

        #region Manager Methods

        /// <summary>
        /// Gets a common design
        /// </summary>
        /// <param name="des">The DES</param>
        /// <param name="palette">The palette to use</param>
        /// <param name="desIndex">The DES index</param>
        /// <returns>The common design</returns>
        public override Common_Design GetCommonDesign(PC_DES des, IList<ARGBColor> palette, int desIndex)
        {
            // Check if the DES is multi-colored
            if (!MultiColoredDES.Contains(DESNames.ElementAtOrDefault(desIndex)?.Substring(0, DESNames[desIndex].Length - 4)))
                return base.GetCommonDesign(des, palette, desIndex);

            // Create the common design
            Common_Design commonDesign = new Common_Design
            {
                Sprites = new List<Sprite>(),
                Animations = new List<Common_Animation>()
            };

            // Process the image data
            var processedImageData = ProcessImageData(des.ImageData, des.RequiresBackgroundClearing);

            // Add sprites for each color
            for (int i = 0; i < 6; i++)
            {
                // Hack to get correct colors
                var p = palette.Skip(i * 8 + 1).ToList();
                
                p.Insert(0, new ARGBColor(0, 0, 0));

                if (i % 2 != 0)
                {
                    p[8] = palette[i * 8];
                }
                    
                // Sprites
                foreach (var s in des.ImageDescriptors)
                {
                    // Get the texture
                    Texture2D tex = GetSpriteTexture(s, p, processedImageData);

                    // Add it to the array
                    commonDesign.Sprites.Add(tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 1f), 16, 20));
                }
            }

            // Add animations for each color
            for (int i = 0; i < 6; i++)
            {
                // Animations
                foreach (var a in des.AnimationDescriptors)
                {
                    // Create a clone animation
                    var ca = new PC_AnimationDescriptor
                    {
                        LayersPerFrame = a.LayersPerFrame,
                        Unknown1 = a.Unknown1,
                        FrameCount = a.FrameCount,
                        Unknown2 = a.Unknown2,
                        Unknown3 = a.Unknown3,
                        FrameTableOffset = a.FrameTableOffset,
                        Layers = a.Layers.Select(x => new Common_AnimationLayer
                        {
                            IsFlipped = x.IsFlipped,
                            XPosition = x.XPosition,
                            YPosition = x.YPosition,
                            ImageIndex = (byte)(x.ImageIndex + (des.ImageDescriptors.Length * i))
                        }).ToArray(),
                        Frames = a.Frames
                    };

                    // Add the animation to list
                    commonDesign.Animations.Add(GetCommonAnimation(ca));
                }
            }

            return commonDesign;
        }

        /// <summary>
        /// Loads the specified level for the editor
        /// </summary>
        /// <param name="context">The serialization context</param>
        /// <returns>The editor manager</returns>
        public override Task<BaseEditorManager> LoadAsync(Context context)
        {
            // Get the DES names
            DESNames = GetDESNames(context).ToArray();

            // Load the level
            return base.LoadAsync(context);
        }

        /// <summary>
        /// Gets an editor manager from the specified objects
        /// </summary>
        /// <param name="level">The common level</param>
        /// <param name="context">The context</param>
        /// <param name="manager">The manager</param>
        /// <param name="designs">The common design</param>
        /// <returns>The editor manager</returns>
        public override PC_EditorManager GetEditorManager(Common_Lev level, Context context, PC_Manager manager, Common_Design[] designs) => new PC_RD_EditorManager(level, context, manager, designs);

        #endregion
    }
}