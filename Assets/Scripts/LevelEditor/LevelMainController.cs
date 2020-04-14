﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace R1Engine
{
    public class LevelMainController : MonoBehaviour {

        /// <summary>
        /// The editor manager
        /// </summary>
        public BaseEditorManager EditorManager;

        /// <summary>
        /// The events
        /// </summary>
        public List<Common_Event> Events { get; set; }

        // The current level we are operating with
        public Common_Lev currentLevel => EditorManager?.Level;

        // The context, to reuse when writing
        private Serialize.Context serializeContext;

        // References to specific level controller gameObjects in inspector
        public LevelTilemapController controllerTilemap;
        public LevelEventController controllerEvents;

        // Reference to the background ting
        public MeshFilter backgroundTint;

        // Render camera things
        public Camera renderCamera;
        public Texture2D tex;

        public async Task LoadLevelAsync(IGameManager manager, Serialize.Context context) 
        {
            // Create the context
            serializeContext = context;

            // Make sure all the necessary files are downloaded
            await manager.LoadFilesAsync(serializeContext);

            using (serializeContext) {
                // Load the level
                EditorManager = await manager.LoadAsync(serializeContext, true);

                await Controller.WaitIfNecessary();

                Controller.status = $"Initializing tile maps";

                // Init tilemaps
                controllerTilemap.InitializeTilemaps();

                await Controller.WaitIfNecessary();

                Controller.status = $"Initializing events";

                // Add events
                Events = currentLevel.EventData.Select(x => controllerEvents.AddEvent(x)).ToList();

                // Init event things
                controllerEvents.InitializeEvents();

                await Controller.WaitIfNecessary();

                // Draw the background tint
                var mo = new Mesh {
                    vertices = new Vector3[]
                    {
                    new Vector3(0, 0), new Vector3(currentLevel.Width, 0), new Vector3(currentLevel.Width, -currentLevel.Height),
                    new Vector3(0, -currentLevel.Height)
                    }
                };

                mo.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
                backgroundTint.sharedMesh = mo;

                if (Settings.ScreenshotEnumeration)
                    ConvertLevelToPNG();
            }
        }

        public void SaveLevelTEMP() {
            // Set events
            Controller.obj.levelEventController.CalculateLinkIndexes();

            using (serializeContext) {
                Settings.GetGameManager.SaveLevel(serializeContext, currentLevel);
            }
            Debug.Log("Saved.");
        }

        public void ExportTileset() 
        {
            var tileSetIndex = 0;

            // Export every tile set
            foreach (var tileSet in currentLevel.TileSet.Where(x => x?.Tiles?.Any(y => y != null) == true))
            {
                // Get values
                var tileCount = tileSet.Tiles.Length;
                const int tileSetWidth = 16;
                var tileSetHeight = (int)Math.Ceiling(tileCount / (double)tileSetWidth);
                var tileSize = (int)tileSet.Tiles.First().sprite.rect.width;

                // Create the texture
                var tileTex = new Texture2D(tileSetWidth * tileSize, tileSetHeight * tileSize, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                // Default to fully transparent
                tileTex.SetPixels(Enumerable.Repeat(new Color(0, 0, 0, 0), tileTex.width * tileTex.height).ToArray());

                // Add every tile to it
                for (int i = 0; i < tileCount; i++)
                {
                    // Get the tile texture
                    var tile = tileSet.Tiles[i].sprite;

                    // Get the texture offsets
                    var offsetY = (int)Math.Floor(i / (double)tileSetWidth) * tileSize;
                    var offsetX = (i - (offsetY)) * tileSize;

                    // Set the pixels
                    for (int y = 0; y < tile.rect.height; y++)
                    {
                        for (int x = 0; x < tile.rect.width; x++)
                        {
                            tileTex.SetPixel(x + offsetX, tileTex.height - (y + offsetY) - 1, tile.texture.GetPixel((int)tile.rect.x + x, (int)tile.rect.y + y));
                        }
                    }
                }

                tileTex.Apply();

                var destPath = $@"Tilemaps\{Controller.CurrentSettings.GameModeSelection}\{Controller.CurrentSettings.GameModeSelection} - {Controller.CurrentSettings.World} {Controller.CurrentSettings.Level:00} ({tileSetIndex}).png";

                Directory.CreateDirectory(Path.GetDirectoryName(destPath));

                // Save the tile map
                File.WriteAllBytes(destPath, tileTex.EncodeToPNG());

                tileSetIndex++;
            }
        }

        public void ConvertLevelToPNG() {

            // Get the path to save to
            //var destPath = EditorUtility.SaveFilePanel("Select file destination", null, $"{Settings.GetGameSettings.GameModeSelection} - {Settings.World} {Settings.Level:00}.png", "png");

            var destPath = $@"Screenshots\{Controller.CurrentSettings.GameModeSelection}\{Controller.CurrentSettings.GameModeSelection} - {Controller.CurrentSettings.World} {Controller.CurrentSettings.Level:00}.png";

            Directory.CreateDirectory(Path.GetDirectoryName(destPath));

            // TODO: Allow this to be configured | THIS whole part should be refactored, the foreach after is bad
            // Set settings
            //Settings.ShowAlwaysEvents = false;
            //Settings.ShowEditorEvents = false;

            // Hide unused links and show gendoors
            foreach (var e in Events) 
            {
                if (e.Flag==EventFlag.Always || 
                    e.Flag == EventFlag.Editor)
                    e.gameObject.SetActive(false);

                if (e.Data.Type == EventType.TYPE_GENERATING_DOOR || 
                    e.Data.Type == EventType.TYPE_DESTROYING_DOOR || 
                    e.Data.Type==EventType.MS_scintillement || 
                    e.Data.Type==EventType.MS_super_gendoor ||
                    e.Data.Type == EventType.MS_super_kildoor || 
                    e.Data.Type==EventType.MS_compteur)
                    e.gameObject.SetActive(true);

                e.ChangeLinksVisibility(true);

                if (e.LinkID == 0) 
                {
                    e.lineRend.enabled = false;
                    e.linkCube.gameObject.SetActive(false);
                }
                else {
                    //Hide link if not linked to gendoor
                    bool gendoorFound = e.Data.Type == EventType.TYPE_GENERATING_DOOR || 
                                        e.Data.Type == EventType.TYPE_DESTROYING_DOOR || 
                                        e.Data.Type == EventType.MS_scintillement || 
                                        e.Data.Type == EventType.MS_super_gendoor || 
                                        e.Data.Type == EventType.MS_super_kildoor ||
                                        e.Data.Type == EventType.MS_compteur;
                    var allofSame = new List<Common_Event> {
                        e
                    };
                    foreach (Common_Event f in Events.Where(f => f.LinkID == e.LinkID)) {
                        allofSame.Add(f);
                        if (f.Data.Type == EventType.TYPE_GENERATING_DOOR || 
                            f.Data.Type == EventType.TYPE_DESTROYING_DOOR || 
                            f.Data.Type == EventType.MS_scintillement || 
                            f.Data.Type == EventType.MS_super_gendoor || 
                            f.Data.Type == EventType.MS_super_kildoor || 
                            f.Data.Type == EventType.MS_compteur)
                            gendoorFound = true;
                    }
                    if (!gendoorFound) {
                        foreach(var a in allofSame) {
                            a.lineRend.enabled = false;
                            a.linkCube.gameObject.SetActive(false);
                        }
                    }
                }
            }

            RenderTexture renderTex = new RenderTexture(currentLevel.Width*16, currentLevel.Height*16, 24);
            renderCamera.targetTexture = renderTex;
            //Set camera pos
            renderCamera.transform.position = new Vector3((currentLevel.Width) / 2f, -(currentLevel.Height) / 2f, renderCamera.transform.position.z);
            renderCamera.orthographicSize = (currentLevel.Height / 2f);
            renderCamera.rect = new Rect(0, 0, 1, 1);
            renderCamera.Render();

            //Save to picture
            RenderTexture.active = renderTex;

            tex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();

            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(destPath, bytes);

            Destroy(tex);
            RenderTexture.active = null;
            renderCamera.rect = new Rect(0, 0, 0, 0);

            Debug.Log("Level saved as PNG");

            //Unsub events
            Settings.OnShowAlwaysEventsChanged -= controllerEvents.ChangeEventsVisibility;
            Settings.OnShowEditorEventsChanged -= controllerEvents.ChangeEventsVisibility;

            if (Settings.ScreenshotEnumeration)
                SceneManager.LoadScene("Dummy");
        }
    }
}
