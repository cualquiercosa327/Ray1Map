﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using PsychoPortal;
using PsychoPortal.Unity;
using UnityEngine;
using Context = BinarySerializer.Context;
using Debug = UnityEngine.Debug;
using Mesh = PsychoPortal.Mesh;
using UnityMesh = UnityEngine.Mesh;

namespace Ray1Map.Psychonauts
{
    public class Psychonauts_Manager : BaseGameManager
    {
        #region Manager

        public override GameInfo_Volume[] GetLevels(GameSettings settings)
        {
            Loader loader = new Loader(new PsychonautsSettings(GetVersion(settings)), settings.GameDirectory);

            if (loader.Version == PsychonautsVersion.PS2)
                loader.LoadFilePackages();

            return GameInfo_Volume.SingleVolume(Maps.
                Select((x, i) =>
                {
                    string world = x.Name.Substring(0, 2);

                    return new
                    {
                        Level = x.Name,
                        World = world,
                        WorldIndex = LevelNames.FindItemIndex(l => l.Name == world),
                        DisplayName = x.DisplayName,
                        Index = i
                    };
                }).
                Where(x => loader.FileManager.FileExists(loader.GetPackPackFilePath(x.Level))).
                GroupBy(x => x.World).
                Select(x => new GameInfo_World(
                    index: x.First().WorldIndex,
                    worldName: LevelNames[x.First().WorldIndex].DisplayName,
                    maps: x.Select(m => m.Index).ToArray(),
                    mapNames: x.Select(m => $"{m.Level} - {m.DisplayName}").ToArray())).
                ToArray());
        }

        private static (string Name, string DisplayName)[] LevelNames { get; } = 
        {
            ("ST", "Start"),
            ("CA", "Whispering Rock Summer Camp"),
            ("BB", "Coach Oleander's Basic Braining"),
            ("NI", "Nightmare in the Brain Tumbler"),
            ("SA", "Sasha's Shooting Gallery"),
            ("MI", "Milla's Dance Party"),
            ("LL", "Lair of the Lungfish"),
            ("LO", "Lungfishopolis"),
            ("AS", "Thorney Towers Home for the Disturbed"),
            ("MM", "The Milkman Conspiracy"),
            ("TH", "Gloria's Theater"),
            ("WW", "Waterloo World"),
            ("BV", "Black Velvetopia"),
            ("MC", "Meat Circus"),
        };

        private static (string Name, string DisplayName)[] Maps { get; } =
        {
            ("STMU", "Main Menu"),

            ("CARE", "Reception Area and Wilderness"),
            ("CARE_NIGHT", "Reception Area and Wilderness (Night)"),
            ("CAMA", "Campgrounds Main"),
            ("CAMA_NIGHT", "Campgrounds Main (Night)"),
            ("CAKC", "Kids' Cabins"),
            ("CAKC_NIGHT", "Kids' Cabins (Night)"),
            ("CABH", "Boathouse and Beach"),
            ("CABH_NIGHT", "Boathouse and Beach (Night)"),
            ("CALI", "Lodge"),
            ("CALI_NIGHT", "Lodge (Night)"),
            ("CAGP", "GPC and Wilderness"),
            ("CAGP_NIGHT", "GPC and Wilderness (Night)"),
            ("CAJA", "Ford's Sanctuary"),
            ("CASA", "Sasha's Underground Lab"),
            ("CABU", "Bunkhouse File Select UI"),

            ("BBA1", "Obstacle Course 1"),
            ("BBA2", "Obstacle Course 2"),
            ("BBLT", "Obstacle Course Finale"),

            ("NIMP", "The Woods"),
            ("NIBA", "The Braintank"),

            ("SACU", "Sasha's Shooting Gallery"),

            ("MIFL", "The Lounge"),
            ("MIMM", "The Race"),
            ("MILL", "The Party"),

            ("LLLL", "Lair of the Lungfish"),

            ("LOMA", "Lungfishopolis"),
            ("LOCB", "Kochamara"),

            ("ASGR", "Grounds"),
            ("ASCO", "Lower Floors"),
            ("ASUP", "Upper Floors"),
            ("ASLB", "The Lab of Dr. Lobato"),
            ("ASRU", "Ruins"),

            ("MMI1", "The Neighborhood"),
            ("MMI2", "Book Repository"),
            ("MMDM", "The Den Mother"),

            ("THMS", "The Stage"),
            ("THCW", "The Catwalks"),
            ("THFB", "Confrontation"),

            ("WWMA", "Waterloo World"),

            ("BVES", "Edgar's Sancuary"),
            ("BVRB", "Running Against the Bull"),
            ("BVWT", "Tiger"),
            ("BVWE", "Eagle"),
            ("BVWD", "Dragon"),
            ("BVWC", "Cobra"),
            ("BVMA", "Matador's Arena"),

            ("MCTC", "Tent City"),
            ("MCBB", "The Butcher"),
        };

        private static IBinarySerializerLogger GetLogger() => Settings.Log ? new BinarySerializerLogger(Settings.LogFile) : null;
        private static PsychonautsVersion GetVersion(GameSettings settings) => settings.GameModeSelection switch
        {
            GameModeSelection.Psychonauts_Xbox_Proto_20041217 => PsychonautsVersion.Xbox_Proto_20041217,
            GameModeSelection.Psychonauts_PC_Digital => PsychonautsVersion.PC_Digital,
            GameModeSelection.Psychonauts_PS2 => PsychonautsVersion.PS2,
            _ => throw new Exception("Invalid game mode"),
        };

        private static readonly float _scale = 1 / 32f;
        // Need to invert the z-axis
        private static readonly Vector3 _scaleVector = new Vector3(_scale, _scale, -_scale);

        #endregion

        #region Game Actions

        public override GameAction[] GetGameActions(GameSettings settings)
        {
            return new GameAction[]
            {
                new GameAction("Export All Level Textures", false, true, (_, output) => ExportAllLevelTextures(settings, output)),
                new GameAction("Export Current Level Textures", false, true, (_, output) => ExportCurrentLevelTextures(settings, output)),
                new GameAction("Export Current Level Model as OBJ", false, true, (_, output) => ExportCurrentLevelModelAsOBJ(settings, output)),
            };
        }

        public void ExportAllLevelTextures(GameSettings settings, string outputPath)
        {
            Loader loader = new Loader(new PsychonautsSettings(GetVersion(settings)), settings.GameDirectory);
            loader.UseNativeTextures = false;

            using IBinarySerializerLogger logger = GetLogger();

            loader.LoadCommonPackPack(logger);

            foreach (string lvl in Maps.Select(x => x.Name))
            {
                if (!loader.FileManager.FileExists(loader.GetPackPackFilePath(lvl)))
                    continue;

                loader.LoadLevelPackPack(lvl, logger);
                loader.TexturesManager.DumpTextures(outputPath);

                Debug.Log($"Exported {lvl}");
            }

            Debug.Log($"Finished exporting");
        }

        public void ExportCurrentLevelTextures(GameSettings settings, string outputPath)
        {
            string lvl = Maps[settings.Level].Name;

            Loader loader = new Loader(new PsychonautsSettings(GetVersion(settings)), settings.GameDirectory);
            loader.UseNativeTextures = false;

            using IBinarySerializerLogger logger = GetLogger();

            loader.LoadLevelPackPack(lvl, logger);

            loader.TexturesManager.DumpTextures(outputPath);

            Debug.Log($"Finished exporting");
        }

        public void ExportCurrentLevelModelAsOBJ(GameSettings settings, string outputPath)
        {
            string lvl = Maps[settings.Level].Name;

            Loader loader = new Loader(new PsychonautsSettings(GetVersion(settings)), settings.GameDirectory);
            loader.UseNativeTextures = false;

            using IBinarySerializerLogger logger = GetLogger();

            loader.LoadLevelPackPack(lvl, logger);

            var exp = new PsychonautsObjExporter();
            var textures = loader.TexturesManager.GetTextures(loader.LevelScene.TextureTranslationTable);

            exportDomain(loader.LevelScene.RootDomain);

            void exportDomain(Domain domain)
            {
                foreach (Mesh mesh in domain.Meshes)
                    exportMesh(mesh);

                foreach (Domain child in domain.Children)
                    exportDomain(child);

                void exportMesh(Mesh mesh)
                {
                    exp.Export(outputPath, $"{domain.Name}_{mesh.Name}", mesh, textures);

                    foreach (Mesh child in mesh.Children)
                        exportMesh(child);
                }
            }

            Debug.Log($"Finished exporting");
        }

        #endregion

        #region Load

        public override async UniTask<Unity_Level> LoadAsync(Context context)
        {
            // Get the settings
            GameSettings r1Settings = context.GetR1Settings();
            string lvl = Maps[r1Settings.Level].Name;

            // Create a loader
            Loader loader = new Loader(new PsychonautsSettings(GetVersion(r1Settings)), r1Settings.GameDirectory);

            // Use a PsychoPortal logger
            using IBinarySerializerLogger logger = GetLogger();

            Controller.DetailedState = "Loading common packs";
            await Controller.WaitIfNecessary();

            // Load common data
            loader.LoadFilePackages(logger);
            loader.LoadCommonPackPack(logger);
            loader.LoadCommonAnimPack(logger);

            Controller.DetailedState = "Loading level packs";
            await Controller.WaitIfNecessary();

            // Load level data
            loader.LoadLevelPackPack(lvl, logger);
            loader.LoadLevelAnimPack(lvl, logger);

            Controller.DetailedState = "Loading animations";
            await Controller.WaitIfNecessary();

            loader.AnimationManager.LoadAnimations(loader.LevelAnimPack.StubSharedAnims.
                Concat(loader.CommonAnimPack.StubSharedAnims).
                Select(x =>
                {
                    SharedSkelAnim jan = loader.FileManager.ReadFromFile<SharedSkelAnim>(x.JANFileName, logger, throwIfNotFound: true);
                    return new PsychonautsSkelAnim(x, jan);
                }));

            Controller.DetailedState = "Creating objects";
            await Controller.WaitIfNecessary();

            // Create the level
            var level = new Unity_Level()
            {
                FramesPerSecond = 60,
                IsometricData = new Unity_IsometricData
                {
                    CollisionMapWidth = 0,
                    CollisionMapHeight = 0,
                    TilesWidth = 0,
                    TilesHeight = 0,
                    CollisionMap = null,
                    Scale = Vector3.one,
                    ViewAngle = Quaternion.Euler(90, 0, 0),
                    CalculateYDisplacement = () => 0,
                    CalculateXDisplacement = () => 0,
                    ObjectScale = Vector3.one * 1
                },
                PixelsPerUnit = 1,
                CellSize = 1,
            };

            // Create the object manager
            level.ObjManager = new Unity_ObjectManager(context);

            GameObject world = LoadLevel(loader, level, Controller.obj.levelController.editor.layerTiles.transform, lvl);

            world.transform.localScale = _scaleVector;

            level.Layers = new Unity_Layer[]
            {
                new Unity_Layer_GameObject(true)
                {
                    Name = "Map",
                    ShortName = "MAP",
                    Graphics = world,
                    DisableGraphicsWhenCollisionIsActive = true
                }
            };

            Controller.obj.levelController.editor.cam.camera3D.farClipPlane = 10000f;

            return level;
        }

        public GameObject LoadLevel(Loader loader, Unity_Level level, Transform parent, string levelName)
        {
            GameObject gaoParent = new GameObject(levelName);
            gaoParent.transform.SetParent(parent, false);
            gaoParent.transform.localScale = Vector3.one;
            gaoParent.transform.localRotation = Quaternion.identity;
            gaoParent.transform.localPosition = Vector3.zero;
            
            // Load models outside the map for now
            Vector3 plbPos = new Vector3(-60000, 30000, 20000);

            foreach (PLB meshFile in loader.CommonMeshPack.MeshFiles.Concat(loader.LevelMeshPack.MeshFiles))
            {
                GameObject obj = LoadScene(loader, level, meshFile.Scene, gaoParent.transform, loader.TexturesManager, 
                    $"{meshFile.Name}, " +
                    $"Type: {meshFile.Type}");
                obj.transform.position = plbPos;
                plbPos += new Vector3(meshFile.Scene.RootDomain.Bounds.Max.X - meshFile.Scene.RootDomain.Bounds.Min.X, 0, 0);
            }

            // Load the level scene
            LoadScene(loader, level, loader.LevelScene, gaoParent.transform, loader.TexturesManager, "Level");

            return gaoParent;
        }

        public GameObject LoadScene(Loader loader, Unity_Level level, Scene scene, Transform parent, TexturesManager texManager, string name)
        {
            GameObject sceneObj = new GameObject(
                $"Scene: {name}, " +
                $"NavMeshes: {scene.NavMeshes.Length}, " +
                $"VisTree: {scene.VisibilityTree != null}");
            sceneObj.transform.SetParent(parent, false);
            sceneObj.transform.localScale = Vector3.one;
            sceneObj.transform.localRotation = Quaternion.identity;
            sceneObj.transform.localPosition = Vector3.zero;

            LoadDomain(loader, level, scene.RootDomain, sceneObj.transform, texManager.GetTextures(scene.TextureTranslationTable));

            // Load referenced scenes
            if (scene.ReferencedScenes != null)
                foreach (Scene refScene in scene.ReferencedScenes)
                    LoadScene(loader, level, refScene, sceneObj.transform, texManager, $"{name} References");

            return sceneObj;
        }

        public void LoadDomain(Loader loader, Unity_Level level, Domain domain, Transform parent, PsychonautsTexture[] textures)
        {
            GameObject domainObj = new GameObject(
                $"Domain: {domain.Name}, " +
                $"EntityInitData: {domain.EntityInitDatas?.Length ?? 0}, " +
                $"RuntimeRefs: ({String.Join(", ", domain.RuntimeReferences.Select(x => x.Value))})");
            domainObj.transform.SetParent(parent, false);
            domainObj.transform.localScale = Vector3.one;
            domainObj.transform.localRotation = Quaternion.identity;
            domainObj.transform.localPosition = Vector3.zero;

            // Load children
            foreach (Domain domainChild in domain.Children)
                LoadDomain(loader, level, domainChild, domainObj.transform, textures);

            // Load meshes
            foreach (Mesh mesh in domain.Meshes)
                LoadMesh(loader, level, mesh, domainObj.transform, textures);

            // Show entity positions
            foreach (DomainEntityInfo ei in domain.DomainEntityInfos)
            {
                Dictionary<string, string> editVars = ei.ParseEditVars();
                string plbName = editVars?.TryGetItem("self.meshname");

                if (plbName != null)
                {
                    //var n = plbName.ToLower().Replace('/', '\\');

                    //if (n.StartsWith("workresource"))
                    //    n = n.Substring("workresource".Length + 1);

                    //if (plbs.ContainsKey(n))
                    //{
                    //    GameObject entitiyObj = new GameObject($"Entity: {ei.Name}");
                    //    entitiyObj.transform.SetParent(domainObj.transform, false);
                    //    entitiyObj.transform.localPosition = ei.Position.ToVector3();
                    //    entitiyObj.transform.localRotation = ei.Rotation.ToQuaternionRad();
                    //    entitiyObj.transform.localScale = ei.Scale.ToVector3();

                    //    GameObject obj = Object.Instantiate(plbs[n]);
                    //    obj.transform.SetParent(entitiyObj.transform, false);
                    //    obj.transform.localPosition = Vector3.zero;
                    //    obj.transform.localRotation = Quaternion.identity;
                    //    obj.transform.localScale = Vector3.one;
                    //}
                }

                level.EventData.Add(new Unity_Object_Dummy(null, Unity_ObjectType.Object,
                    position: ei.Position.ToInvVector3() * _scale,
                    name: $"Entity: {ei.Name}",
                    debugText: $"Class: {ei.ScriptClass}{Environment.NewLine}" +
                               $"EditVars: {ei.EditVars}{Environment.NewLine}" +
                               $"{(editVars != null ? String.Join(Environment.NewLine, editVars.Select(x => $"{x.Key}: {x.Value}")) : null)}"));
            }
        }

        public void LoadMesh(Loader loader, Unity_Level level, Mesh mesh, Transform parent, PsychonautsTexture[] textures)
        {
            GameObject meshObj = new GameObject(
                $"Mesh: {mesh.Name}, " +
                $"LODs: {mesh.LODs?.Length ?? 0}, " +
                $"Lights: ({String.Join(", ", mesh.Lights.Select(x => x.Type))}), " +
                $"AnimAffectors: ({String.Join(", ", mesh.AnimAffectors.Select(x => x.Type))}), " +
                $"Collision: {mesh.CollisionTree != null}, " +
                $"EntityMeshInfo: {(mesh.EntityMeshInfo == null ? null : $"(Class: {mesh.EntityMeshInfo.ScriptClass}, EditVars: {mesh.EntityMeshInfo.EditVars})")}");
            meshObj.transform.SetParent(parent, false);
            meshObj.transform.localScale = Vector3.one;
            meshObj.transform.localRotation = Quaternion.identity;
            meshObj.transform.localPosition = Vector3.zero;

            foreach (Mesh meshChild in mesh.Children)
                LoadMesh(loader, level, meshChild, meshObj.transform, textures);

            meshObj.transform.localPosition = mesh.Position.ToVector3();
            meshObj.transform.localRotation = mesh.Rotation.ToQuaternionRad();
            meshObj.transform.localScale = mesh.Scale.ToVector3();

            GameObject visualMeshObj = new GameObject("Visual");
            visualMeshObj.transform.SetParent(meshObj.transform, false);
            visualMeshObj.transform.localScale = Vector3.one;
            visualMeshObj.transform.localRotation = Quaternion.identity;
            visualMeshObj.transform.localPosition = Vector3.zero;

            var mapObjComp = meshObj.AddComponent<MapObjectComponent>();
            mapObjComp.MapObject = visualMeshObj;

            int skeletonsCount = mesh.Skeletons.Length;

            PsychonautsSkeleton[] skeletons = new PsychonautsSkeleton[skeletonsCount];
            Matrix4x4[][] bindPoses = new Matrix4x4[skeletonsCount][];

            for (int skelIndex = 0; skelIndex < skeletonsCount; skelIndex++)
            {
                Skeleton s = mesh.Skeletons[skelIndex];

                GameObject skeletonObj = new GameObject($"Skeleton: {s.Name}");
                skeletonObj.transform.SetParent(visualMeshObj.transform, false);
                skeletonObj.transform.localPosition = Vector3.zero;
                skeletonObj.transform.localRotation = Quaternion.identity;
                skeletonObj.transform.localScale = Vector3.one;

                bindPoses[skelIndex] = new Matrix4x4[s.JointsCount];
                skeletons[skelIndex] = new PsychonautsSkeleton(s, skeletonObj.transform);

                for (int i = 0; i < s.JointsCount; i++)
                    bindPoses[skelIndex][i] = skeletons[skelIndex].Joints[i].Transform.worldToLocalMatrix * skeletonObj.transform.localToWorldMatrix;
            }

            if (skeletonsCount > 0)
            {
                var ja = visualMeshObj.AddComponent<PsychoPortal.Unity.SkeletonAnimationComponent>();
                ja.Skeletons = skeletons;
                ja.AnimationManager = loader.AnimationManager;
            }

            // Collision
            if (mesh.CollisionTree != null)
            {
                GameObject colObj = LoadCollisionTree(mesh.CollisionTree, meshObj.transform);
                var colObjComp = meshObj.AddComponent<CollisionObjectComponent>();
                colObjComp.CollisionObject = colObj;
            }

            for (var i = 0; i < mesh.MeshFrags.Length; i++)
            {
                MeshFrag meshFrag = mesh.MeshFrags[i];

                LoadMeshFrag(meshFrag, visualMeshObj.transform, i, textures, skeletons, bindPoses);
            }

            // Show trigger positions
            foreach (TriggerOBB t in mesh.Triggers)
            {
                level.EventData.Add(new Unity_Object_Dummy(null, Unity_ObjectType.Trigger,
                    position: t.Position.ToInvVector3() * _scale,
                    name: $"Trigger: {t.Name}",
                    debugText: t.Name));
            }
        }

        public void LoadMeshFrag(MeshFrag meshFrag, Transform parent, int index, PsychonautsTexture[] textures, PsychonautsSkeleton[] skeletons, Matrix4x4[][] bindPoses)
        {
            GameObject meshFragObj = new GameObject(
                $"Frag: {index}, " +
                $"Blend Shapes: {meshFrag.BlendshapeData?.Streams.Length ?? 0}, " +
                $"VertexStreamBasis: {meshFrag.VertexStreamBasis?.Length ?? 0}, " +
                $"Textures: {meshFrag.TextureIndices.Length}, " +
                $"Flags: {meshFrag.MaterialFlags}");
            meshFragObj.transform.SetParent(parent, false);

            UnityMesh unityMesh = new UnityMesh();

            // Set vertices and normals
            unityMesh.SetVertices(meshFrag);
            unityMesh.SetNormals(meshFrag);
            unityMesh.SetPolygons(meshFrag);
            unityMesh.SetVertexColors(meshFrag);

            MeshFilter mf = meshFragObj.AddComponent<MeshFilter>();
            meshFragObj.layer = LayerMask.NameToLayer("3D Collision");
            meshFragObj.transform.localScale = Vector3.one;
            meshFragObj.transform.localRotation = Quaternion.identity;
            meshFragObj.transform.localPosition = Vector3.zero;
            mf.sharedMesh = unityMesh;

            // Temporary code for visualizing the blend shapes
            //if (meshFrag.BlendshapeData != null)
            //{
            //    BlendTestComponent ba = meshFragObj.AddComponent<BlendTestComponent>();
            //    ba.mesh = unityMesh;
            //    ba.blendStreams = meshFrag.BlendshapeData.Streams;
            //    ba.vertices = meshFrag.Vertices.Select(x => x.Vertex.ToVector3()).ToArray();
            //    ba.speed = new AnimSpeed_SecondIncrease(1f);
            //}

            Material matSrc;

            if (meshFrag.MaterialFlags.HasFlag(MaterialFlags.AdditiveBlending))
                matSrc = Controller.obj.levelController.controllerTilemap.unlitAdditiveMaterial;
            else
                matSrc = Controller.obj.levelController.controllerTilemap.unlitTransparentCutoutMaterial;

            Material mat = new Material(matSrc);

            mat.color = meshFrag.MaterialColor.ToVector4();

            if (meshFrag.AnimInfo != null)
            {
                // TODO: Right now this is hard-coded to only ever use the first skeleton. However there can be multiple, like for Raz.

                BoneWeight[] weights = meshFrag.AnimInfo.OriginalSkinWeights.Select(x => new BoneWeight()
                {
                    boneIndex0 = meshFrag.AnimInfo.JointIDs[x.Joint1].JointIndex,
                    weight0 = x.Weight.X,
                    boneIndex1 = meshFrag.AnimInfo.JointIDs[x.Joint2].JointIndex,
                    weight1 = 1 - x.Weight.X,
                }).ToArray();
                unityMesh.boneWeights = weights;
                unityMesh.bindposes = bindPoses[0];

                SkinnedMeshRenderer smr = meshFragObj.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMaterial = mat;
                smr.sharedMesh = unityMesh;
                smr.bones = skeletons[0].Joints.Select(x => x.Transform).ToArray();
                smr.rootBone = skeletons[0].Joints[0].Transform;
            }
            else
            {
                MeshRenderer mr = meshFragObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
            }

            PsychonautsTexture tex = textures.ElementAtOrDefault((int?)meshFrag.TextureIndices?.FirstOrDefault() ?? -1);
            
            if (tex != null)
            {
                mat.mainTexture = tex.Texture;

                if (tex.IsAnimated)
                {
                    TextureAnimationComponent animTex = meshFragObj.AddComponent<TextureAnimationComponent>();
                    animTex.SetTexture(tex, mat);
                    //Debug.Log($"Texture {tex.GameTexture.FileName} is animated with {tex.AnimInfo.FramesCount} frames");
                }

                unityMesh.SetUVs(meshFrag, 0);
            }
        }

        public GameObject LoadCollisionTree(CollisionTree col, Transform parent)
        {
            GameObject colObj = new GameObject($"Collision");
            colObj.transform.SetParent(parent, false);

            colObj.transform.localPosition = Vector3.zero;
            colObj.transform.localRotation = Quaternion.identity;
            colObj.transform.localScale = Vector3.one;

            // Create a separate mesh for each type of collision polygons
            foreach (var v in col.CollisionPolys.GroupBy(x => x.SurfaceFlags))
            {
                SurfaceFlags flags = v.Key;
                Vector3[] vertices = v.SelectMany(x => x.VertexIndices).Select(x => col.Vertices[x].ToVector3()).ToArray();
                int[] triIndices = Enumerable.Range(0, vertices.Length / 3).Select(x =>
                {
                    int off = x * 3;
                    // TODO: Show double-sided?
                    //return new int[] { off + 0, off + 1, off + 2, off + 0, off + 2, off + 1 };
                    return new int[] { off + 0, off + 2, off + 1 };
                }).SelectMany(x => x).ToArray();

                GameObject polyObj = new GameObject(
                    $"CollisionPoly, " +
                    $"Surface: {flags}");
                polyObj.transform.SetParent(colObj.transform, false);

                polyObj.transform.localPosition = Vector3.zero;
                polyObj.transform.localRotation = Quaternion.identity;
                polyObj.transform.localScale = Vector3.one;

                UnityMesh unityMesh = new UnityMesh();

                // Set vertices
                unityMesh.SetVertices(vertices);
                unityMesh.SetTriangles(triIndices, 0);

                // TODO: Use better colors here
                Color color = new Color32(60, 120, 180, 255);

                for (int i = 0; i < 32; i++)
                {
                    if (((int)flags & (1 << i)) != 0)
                        color = Color.Lerp(color, new Color(i / 31f, i / 31f, i / 31f), 0.5f);
                }

                unityMesh.SetColors(Enumerable.Repeat(color, vertices.Length).ToArray());

                unityMesh.RecalculateNormals();

                MeshFilter mf = polyObj.AddComponent<MeshFilter>();
                polyObj.layer = LayerMask.NameToLayer("3D Collision");
                polyObj.transform.localScale = Vector3.one;
                polyObj.transform.localRotation = Quaternion.identity;
                polyObj.transform.localPosition = Vector3.zero;
                mf.sharedMesh = unityMesh;

                MeshRenderer mr = polyObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Controller.obj.levelController.controllerTilemap.isometricCollisionMaterial;

                // Add Collider GameObject
                GameObject gaoc = new GameObject($"Poly Collider");
                MeshCollider mc = gaoc.AddComponent<MeshCollider>();
                mc.sharedMesh = unityMesh;
                gaoc.layer = LayerMask.NameToLayer("3D Collision");
                gaoc.transform.SetParent(colObj.transform);
                gaoc.transform.localScale = Vector3.one;
                gaoc.transform.localRotation = Quaternion.identity;
                gaoc.transform.localPosition = Vector3.zero;
                var col3D = gaoc.AddComponent<Unity_Collision3DBehaviour>();
                col3D.Type = $"{flags}";
            }

            return colObj;
        }

        #endregion
    }

    // TODO: Remove once animations have been implemented
    public class BlendTestComponent : ObjectAnimationComponent
    {
        public UnityMesh mesh;
        public BlendshapeStream[] blendStreams;
        public Vector3[] vertices;

        protected override void UpdateAnimation()
        {
            if (mesh == null || blendStreams == null)
                return;

            speed.Update(blendStreams.Length, loopMode);

            int frameInt = speed.CurrentFrameInt;

            int nextFrameIndex = frameInt + 1 * speed.Direction;

            if (nextFrameIndex >= blendStreams.Length)
            {
                switch (loopMode)
                {
                    case AnimLoopMode.Repeat:
                        nextFrameIndex = 0;
                        break;

                    case AnimLoopMode.PingPong:
                        nextFrameIndex = blendStreams.Length - 1;
                        break;
                }
            }
            else if (nextFrameIndex < 0)
            {
                switch (loopMode)
                {
                    case AnimLoopMode.PingPong:
                        nextFrameIndex = 1;
                        break;
                }
            }

            BlendshapeStream currentFrame = blendStreams[frameInt];
            BlendshapeStream nextFrame = blendStreams[nextFrameIndex];

            float lerpFactor = speed.CurrentFrame - frameInt;

            Vector3[] newVertices = Enumerable.Range(0, vertices.Length)
                .Select(x =>
                {
                    Vector3 current = x >= currentFrame.Vertices.Length
                        ? new Vector3()
                        : currentFrame.Vertices[x].Vertex.ToVector3() * currentFrame.Scale;
                    Vector3 next = x >= nextFrame.Vertices.Length
                        ? new Vector3()
                        : nextFrame.Vertices[x].Vertex.ToVector3() * nextFrame.Scale;

                    return Vector3.Lerp(current, next, lerpFactor) + vertices[x];
                }).
                ToArray();

            mesh.SetVertices(newVertices);
        }
    }
}