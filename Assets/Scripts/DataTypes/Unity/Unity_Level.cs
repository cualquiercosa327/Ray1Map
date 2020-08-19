﻿using System.Collections.Generic;
using System.Linq;

namespace R1Engine
{
    /// <summary>
    /// Level data
    /// </summary>
    public class Unity_Level
    {
        #region Public Properties

        // TODO: Replace this with toggle in editor
        public int DefaultMap { get; set; }
        public int DefaultCollisionMap { get; set; }

        /// <summary>
        /// The level maps
        /// </summary>
        public Unity_Map[] Maps { get; set; }

        /// <summary>
        /// The event data for every event
        /// </summary>
        public List<Unity_Obj> EventData { get; set; }

        /// <summary>
        /// Rayman's event
        /// </summary>
        public Unity_Obj Rayman { get; set; }

        /// <summary>
        /// Localization data, currently only for web
        /// </summary>
        public Dictionary<string, string[]> Localization { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Auto applies the palette to the tiles in the level
        /// </summary>
        public void AutoApplyPalette()
        {
            // Get the palette changers
            var paletteXChangers = EventData.Where(x => (R1_EventType)x.Type == R1_EventType.TYPE_PALETTE_SWAPPER && x.Data.SubEtat < 6).ToDictionary(x => x.Data.XPosition, x => (R1_PaletteChangerMode)x.Data.SubEtat);
            var paletteYChangers = EventData.Where(x => (R1_EventType)x.Type == R1_EventType.TYPE_PALETTE_SWAPPER && x.Data.SubEtat >= 6).ToDictionary(x => x.Data.YPosition, x => (R1_PaletteChangerMode)x.Data.SubEtat);

            // NOTE: The auto system won't always work since it just checks one type of palette swapper and doesn't take into account that the palette swappers only trigger when on-screen, rather than based on the axis. Because of this some levels, like Music 5, won't work. More are messed up in the EDU games. There is sadly no solution to this since it depends on the players movement.
            // Check which type of palette changer we have
            bool isPaletteHorizontal = paletteXChangers.Any();

            // Keep track of the default palette
            int defaultPalette = 1;

            // Get the default palette
            if (isPaletteHorizontal && paletteXChangers.Any())
            {
                switch (paletteXChangers.OrderBy(x => x.Key).First().Value)
                {
                    case R1_PaletteChangerMode.Left1toRight2:
                    case R1_PaletteChangerMode.Left1toRight3:
                        defaultPalette = 1;
                        break;
                    case R1_PaletteChangerMode.Left2toRight1:
                    case R1_PaletteChangerMode.Left2toRight3:
                        defaultPalette = 2;
                        break;
                    case R1_PaletteChangerMode.Left3toRight1:
                    case R1_PaletteChangerMode.Left3toRight2:
                        defaultPalette = 3;
                        break;
                }
            }
            else if (!isPaletteHorizontal && paletteYChangers.Any())
            {
                switch (paletteYChangers.OrderByDescending(x => x.Key).First().Value)
                {
                    case R1_PaletteChangerMode.Top1tobottom2:
                    case R1_PaletteChangerMode.Top1tobottom3:
                        defaultPalette = 1;
                        break;
                    case R1_PaletteChangerMode.Top2tobottom1:
                    case R1_PaletteChangerMode.Top2tobottom3:
                        defaultPalette = 2;
                        break;
                    case R1_PaletteChangerMode.Top3tobottom1:
                    case R1_PaletteChangerMode.Top3tobottom2:
                        defaultPalette = 3;
                        break;
                }
            }

            // Keep track of the current palette
            int currentPalette = defaultPalette;

            // Enumerate each cell (PC only has 1 map per level)
            for (int cellY = 0; cellY < Maps[0].Height; cellY++)
            {
                // Reset the palette on each row if we have a horizontal changer
                if (isPaletteHorizontal)
                    currentPalette = defaultPalette;
                // Otherwise check the y position
                else
                {
                    // Check every pixel 16 steps forward
                    for (int y = 0; y < Settings.CellSize; y++)
                    {
                        // Attempt to find a matching palette changer on this pixel
                        var py = paletteYChangers.TryGetValue((short)(Settings.CellSize * cellY + y), out R1_PaletteChangerMode pm) ? (R1_PaletteChangerMode?)pm : null;

                        // If one was found, change the palette based on type
                        if (py != null)
                        {
                            switch (py)
                            {
                                case R1_PaletteChangerMode.Top2tobottom1:
                                case R1_PaletteChangerMode.Top3tobottom1:
                                    currentPalette = 1;
                                    break;
                                case R1_PaletteChangerMode.Top1tobottom2:
                                case R1_PaletteChangerMode.Top3tobottom2:
                                    currentPalette = 2;
                                    break;
                                case R1_PaletteChangerMode.Top1tobottom3:
                                case R1_PaletteChangerMode.Top2tobottom3:
                                    currentPalette = 3;
                                    break;
                            }
                        }
                    }
                }

                for (int cellX = 0; cellX < Maps[0].Width; cellX++)
                {
                    // Check the x position for palette changing
                    if (isPaletteHorizontal)
                    {
                        // Check every pixel 16 steps forward
                        for (int x = 0; x < Settings.CellSize; x++)
                        {
                            // Attempt to find a matching palette changer on this pixel
                            var px = paletteXChangers.TryGetValue((short)(Settings.CellSize * cellX + x), out R1_PaletteChangerMode pm) ? (R1_PaletteChangerMode?)pm : null;

                            // If one was found, change the palette based on type
                            if (px != null)
                            {
                                switch (px)
                                {
                                    case R1_PaletteChangerMode.Left3toRight1:
                                    case R1_PaletteChangerMode.Left2toRight1:
                                        currentPalette = 1;
                                        break;
                                    case R1_PaletteChangerMode.Left1toRight2:
                                    case R1_PaletteChangerMode.Left3toRight2:
                                        currentPalette = 2;
                                        break;
                                    case R1_PaletteChangerMode.Left1toRight3:
                                    case R1_PaletteChangerMode.Left2toRight3:
                                        currentPalette = 3;
                                        break;
                                }
                            }
                        }
                    }

                    // Set the common tile
                    Maps[0].MapTiles[cellY * Maps[0].Width + cellX].PaletteIndex = currentPalette;
                }
            }
        }

        #endregion
    }
}