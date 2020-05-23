﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using R1Engine.Serialize;
using UnityEngine.Tilemaps;

namespace R1Engine
{
    /// <summary>
    /// The game manager for Rayman Educational (PS1)
    /// </summary>
    public class PS1_EDU_Manager : PC_EDU_Manager
    {
        #region Values and paths

        /// <summary>
        /// Gets the file path for the specified level
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The level file path</returns>
        public override string GetLevelFilePath(GameSettings settings) =>
            $"{GetVolumePath(settings)}{GetShortWorldName(settings.World)}/{GetShortWorldName(settings.World)}{Math.Ceiling(settings.Level / 19d):00}/{GetShortWorldName(settings.World)}{settings.Level:00}.NEW";

        /// <summary>
        /// Gets the file path for the specified world file
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The world file path</returns>
        public override string GetWorldFilePath(GameSettings settings) => GetVolumePath(settings) + $"RAY{((int)settings.World + 1):00}.NEW";

        /// <summary>
        /// Gets the file path for the allfix file
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The allfix file path</returns>
        public override string GetAllfixFilePath(GameSettings settings) => GetVolumePath(settings) + $"ALLFIX.NEW";

        /// <summary>
        /// Gets the file path for the big ray file
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The big ray file path</returns>
        public override string GetBigRayFilePath(GameSettings settings) => GetVolumePath(settings) + $"BIGRAY.DAT";

        /// <summary>
        /// Gets the levels for each world
        /// </summary>
        /// <param name="settings">The game settings</param>
        /// <returns>The levels</returns>
        public override KeyValuePair<World, int[]>[] GetLevels(GameSettings settings) => EnumHelpers.GetValues<World>().Select(w => new KeyValuePair<World, int[]>(w, Directory.EnumerateFiles(settings.GameDirectory + GetVolumePath(settings), $"{GetShortWorldName(w)}??.NEW", SearchOption.AllDirectories)
            .Select(FileSystem.GetFileNameWithoutExtensions)
            .Select(x => Int32.Parse(x.Substring(3)))
            .ToArray())).ToArray();

        /// <summary>
        /// Gets the archive files which can be extracted
        /// </summary>
        public override ArchiveFile[] GetArchiveFiles(GameSettings settings)
        {
            return GetEduVolumes(settings).SelectMany(x => new ArchiveFile[]
            {
                new ArchiveFile($"PCMAP/{x}/COMMON.DAT"),
                new ArchiveFile($"PCMAP/{x}/SPECIAL.DAT"),
                new ArchiveFile($"PCMAP/{x}/VIGNET.DAT", ".pcx"),
            }).ToArray();
        }

        #endregion

        #region Manager Methods

        /// <summary>
        /// Loads the specified level for the editor
        /// </summary>
        /// <param name="context">The serialization context</param>
        /// <param name="loadTextures">Indicates if textures should be loaded</param>
        /// <returns>The editor manager</returns>
        public override async Task<BaseEditorManager> LoadAsync(Context context, bool loadTextures)
        {
            // TODO: Set to true once we parse world files!
            loadTextures = false;

            Controller.status = $"Loading map data for {context.Settings.EduVolume}: {context.Settings.World} {context.Settings.Level}";

            // Load the level
            var levelData = FileFactory.Read<PS1_EDU_LevFile>(GetLevelFilePath(context.Settings), context);

            await Controller.WaitIfNecessary();

            // Convert levelData to common level format
            Common_Lev commonLev = new Common_Lev
            {
                // Create the map
                Maps = new Common_LevelMap[]
                {
                    new Common_LevelMap()
                    {
                        // Set the dimensions
                        Width = levelData.Width,
                        Height = levelData.Height,

                        // Create the tile arrays
                        TileSet = new Common_Tileset[3],
                        Tiles = new Common_Tile[levelData.Width * levelData.Height],
                    }
                },

                // Create the events list
                EventData = new List<Common_EventData>(),
            };

            // TODO: Just for testing...
            FileFactory.Read<PS1_EDU_AllfixFile>(GetAllfixFilePath(context.Settings), context);

            // Load the sprites
            var eventDesigns = loadTextures ? await LoadSpritesAsync(context, levelData.ColorPalettes.First()) : new Common_Design[0];

            var index = 0;

            foreach (PC_Event e in levelData.Events)
            {
                // Get the file keys
                var desKey = e.DES.ToString();
                var etaKey = e.ETA.ToString();

                // Add the event
                commonLev.EventData.Add(new Common_EventData
                {
                    Type = e.Type,
                    Etat = e.Etat,
                    SubEtat = e.SubEtat,
                    XPosition = e.XPosition,
                    YPosition = e.YPosition,
                    DESKey = desKey,
                    ETAKey = etaKey,
                    OffsetBX = e.OffsetBX,
                    OffsetBY = e.OffsetBY,
                    OffsetHY = e.OffsetHY,
                    FollowSprite = e.FollowSprite,
                    HitPoints = e.HitPoints,
                    Layer = e.Layer,
                    HitSprite = e.HitSprite,
                    FollowEnabled = e.FollowEnabled,

                    // TODO: Is this correct?
                    LabelOffsets = levelData.EventCommands[index].LabelOffsetTable,
                    CommandCollection = levelData.EventCommands[index].Commands,
                    
                    LinkIndex = levelData.EventLinkTable[index],
                    DebugText = $"Flags: {String.Join(", ", e.Flags.GetFlags())}{Environment.NewLine}"
                });

                index++;
            }

            await Controller.WaitIfNecessary();

            Controller.status = $"Loading tile set";

            // Read the 3 tile sets (one for each palette)
            var tileSets = ReadTileSets(levelData);

            // Set the tile sets
            commonLev.Maps[0].TileSet[0] = tileSets[0];
            commonLev.Maps[0].TileSet[1] = tileSets[1];
            commonLev.Maps[0].TileSet[2] = tileSets[2];

            // Enumerate each cell
            for (int cellY = 0; cellY < levelData.Height; cellY++)
            {
                for (int cellX = 0; cellX < levelData.Width; cellX++)
                {
                    // Get the cell
                    var cell = levelData.MapTiles[cellY * levelData.Width + cellX];

                    // Set the common tile
                    commonLev.Maps[0].Tiles[cellY * levelData.Width + cellX] = new Common_Tile()
                    {
                        TileSetGraphicIndex = cell.TextureIndex,
                        CollisionType = cell.CollisionType,
                        PaletteIndex = 1,
                        XPosition = cellX,
                        YPosition = cellY
                    };
                }
            }

            // Return an editor manager
            return GetEditorManager(commonLev, context, eventDesigns);
        }

        /// <summary>
        /// Reads 3 tile-sets, one for each palette
        /// </summary>
        /// <param name="levData">The level data to get the tile-set for</param>
        /// <returns>The 3 tile-sets</returns>
        public Common_Tileset[] ReadTileSets(PS1_EDU_LevFile levData)
        {
            // Create the output array
            var output = new Common_Tileset[levData.ColorPalettes.Length];

            // Enumerate every palette
            for (int i = 0; i < levData.ColorPalettes.Length; i++)
                output[i] = new Common_Tileset(levData.TileTextures.Select(x => x == 0 ? new RGB666Color(0, 0, 0, 0) : levData.ColorPalettes[i][x]).ToArray(), 512 / Settings.CellSize, Settings.CellSize);

            return output;
        }

        /// <summary>
        /// Gets the event states for the current context
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>The event states</returns>
        public override IEnumerable<PC_ETA> GetCurrentEventStates(Context context)
        {
            // TODO: Read ETA from allfix + world
            return new PC_ETA[0];
        }

        #endregion
    }
}