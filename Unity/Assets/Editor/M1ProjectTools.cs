using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Reborn1985.Editor
{
    public static class M1ProjectTools
    {
        public const string ScenePath = "Assets/Scenes/M1Greybox.unity";
        private const string Art = "Assets/Art/Placeholders/";
        private const string SettingsPath = "Assets/Settings/M1Settings.asset";

        [MenuItem("Reborn 1985/M1/Rebuild greybox scene")]
        public static void Generate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string folder in new[] { "Assets/Scenes", "Assets/Art/Placeholders", "Assets/Settings" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var settings = AssetDatabase.LoadAssetAtPath<M1Settings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<M1Settings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            foreach (RegionDefinition region in settings.Regions)
                if (region.Cells.xMin < 1 || region.Cells.yMin < 1 || region.Cells.xMax >= settings.MapSize.x || region.Cells.yMax >= settings.MapSize.y)
                    throw new InvalidOperationException("Region exceeds map bounds: " + region.Id);

            Sprite floorSprite = MakeSprite("Floor", 64, 32, new Vector2(.5f, .5f), (x, y) =>
            {
                float diamond = Mathf.Abs(x - 31.5f) / 32f + Mathf.Abs(y - 15.5f) / 16f;
                return diamond > 1 ? Color.clear : (diamond > .90f ? new Color(.71f, .73f, .74f) : Color.white);
            });
            Sprite blockSprite = MakeSprite("Block", 64, 48, new Vector2(.5f, 1f / 3f), (x, y) =>
            {
                float dx = Mathf.Abs(x - 31.5f);
                if (y < dx * .5f || y > 47 - dx * .5f) return Color.clear;
                if (y >= 16 + dx * .5f) return new Color(.57f, .60f, .62f);
                return x < 32 ? new Color(.37f, .41f, .44f) : new Color(.30f, .34f, .38f);
            });
            Sprite person = MakeSprite("Person", 24, 40, new Vector2(.5f, .05f), (x, y) =>
            {
                if (y < 4 && x >= 5 && x <= 18) return new Color(.21f, .24f, .27f);
                if (y >= 4 && y < 14 && x >= 6 && x <= 17) return new Color(.31f, .36f, .40f);
                if (y >= 14 && y < 28 && x >= 3 && x <= 20) return Color.white;
                if (y >= 28 && y < 36 && x >= 6 && x <= 17) return new Color(.91f, .79f, .62f);
                if (y >= 36 && x >= 5 && x <= 18) return new Color(.18f, .21f, .24f);
                return Color.clear;
            });
            Sprite square = MakeSprite("Marker", 20, 20, new Vector2(.5f, .5f), (x, y) =>
                x < 2 || x > 17 || y < 2 || y > 17 ? Color.clear : Color.white);
            Sprite arrow = MakeSprite("Facing", 16, 16, new Vector2(.5f, .5f), (x, y) =>
                y >= 3 && y <= 13 && Mathf.Abs(x - 7.5f) < (14 - y) * .55f ? Color.white : Color.clear);
            Tile floorTile = MakeTile("FloorTile", floorSprite, Tile.ColliderType.None);
            Tile wallTile = MakeTile("ObstacleTile", blockSprite, Tile.ColliderType.Grid);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("M1 Continuous County Block");
            var gridObject = new GameObject("Isometric Grid", typeof(Grid));
            gridObject.transform.SetParent(root.transform);
            Grid grid = gridObject.GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(settings.CellSize.x, settings.CellSize.y, 1);
            Tilemap ground = MakeMap("Ground - A to G", gridObject.transform, -1000);
            Tilemap solids = MakeMap("Buildings Walls Counters - Solid", gridObject.transform, 0);
            solids.gameObject.layer = M1World.SolidLayer;
            solids.GetComponent<TilemapRenderer>().mode = TilemapRenderer.Mode.Individual;

            var floorCells = new HashSet<Vector2Int>();
            foreach (RegionDefinition region in settings.Regions)
                foreach (Vector2Int cell in region.Cells.allPositionsWithin) floorCells.Add(cell);
            var internalSolids = new HashSet<Vector2Int>();
            foreach (RectInt rect in new[] { new RectInt(16, 28, 5, 1), new RectInt(39, 30, 5, 1),
                new RectInt(36, 27, 2, 3), new RectInt(45, 27, 1, 3), new RectInt(26, 9, 2, 2), new RectInt(32, 9, 2, 2) })
                foreach (Vector2Int cell in rect.allPositionsWithin) internalSolids.Add(cell);
            Color[] tints = { new Color(.62f,.65f,.65f), new Color(.70f,.71f,.68f), new Color(.77f,.76f,.70f),
                new Color(.76f,.71f,.61f), new Color(.67f,.73f,.71f), new Color(.61f,.64f,.60f), new Color(.70f,.67f,.62f) };
            for (int x = 0; x < settings.MapSize.x; x++)
                for (int y = 0; y < settings.MapSize.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (floorCells.Contains(cell))
                    {
                        ground.SetTile((Vector3Int)cell, floorTile);
                        int index = Array.FindIndex(settings.Regions, region => region.Cells.Contains(cell));
                        ground.SetTileFlags((Vector3Int)cell, TileFlags.None);
                        ground.SetColor((Vector3Int)cell, tints[index]);
                    }
                    if (!floorCells.Contains(cell) || internalSolids.Contains(cell)) solids.SetTile((Vector3Int)cell, wallTile);
                }
            Rigidbody2D staticBody = solids.gameObject.AddComponent<Rigidbody2D>();
            staticBody.bodyType = RigidbodyType2D.Static;
            var composite = solids.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var tilesCollider = solids.gameObject.AddComponent<TilemapCollider2D>();
            tilesCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            tilesCollider.extrusionFactor = .005f;
            tilesCollider.ProcessTilemapChanges();

            var pointsRoot = new GameObject("Stable Points - placeholders only");
            pointsRoot.transform.SetParent(root.transform);
            var points = new List<GreyboxPoint>();
            AddPoint("A-01", "原破旧铺盖 / 出生；休息未实现", 6, 5);
            AddPoint("A-02", "年代线索占位", 4, 7);
            AddPoint("A-03", "积水观察占位", 6, 8);
            AddPoint("A-04", "始终开放的小巷出口", 6, 11, PointRole.Passage);
            AddPoint("B-01", "公告占位", 5, 14);
            AddPoint("B-02", "第一份弃置废纸占位", 6, 16);
            AddPoint("D-01", "第二份废纸占位", 15, 20);
            AddPoint("D-02", "玻璃瓶1占位", 26, 20);
            AddPoint("D-03", "玻璃瓶2占位", 34, 20);
            AddPoint("D-04", "自行车 / 街道环境占位", 10, 22, PointRole.Decoration);
            AddPoint("F-01", "废弃竹筐堆占位", 27, 11);
            AddPoint("F-02", "废包装堆占位", 33, 11);
            AddPoint("F-03", "废金属占位", 30, 10);
            AddPoint("C-01", "林婶 / 对话与餐食未实现", 18, 29, PointRole.FunctionalPlaceholder, true);
            AddPoint("C-04", "帮工位置占位 / 未实现", 20, 26);
            AddPoint("G-01", "陈伯收购站门头占位", 41, 32, PointRole.VisualGuide);
            AddPoint("G-03", "陈伯 / 交易未实现", 41, 31, PointRole.FunctionalPlaceholder, true);
            AddPoint("G-04", "秤前站位 / 保持可走", 41, 28, PointRole.StandingSpace);
            AddPoint("E-01", "旧搪瓷水壶位置 / 无拿取和交易", 52, 12);
            AddPoint("E-02", "旧货居民 / 对话未实现", 50, 11, PointRole.FunctionalPlaceholder, true);

            void AddPoint(string id, string text, int x, int y, PointRole role = PointRole.FunctionalPlaceholder, bool npc = false)
            {
                var obj = new GameObject(id + " " + text);
                obj.transform.SetParent(pointsRoot.transform);
                obj.transform.position = ground.GetCellCenterWorld(new Vector3Int(x, y, 0));
                GreyboxPoint point = obj.AddComponent<GreyboxPoint>();
                point.Configure(id, text, role);
                points.Add(point);
                SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = npc ? person : square;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                renderer.color = npc ? new Color(.75f,.54f,.39f) : new Color(.62f,.52f,.33f);
                if (!npc) obj.transform.localScale = id == "A-01" ? new Vector3(1.8f, .8f, 1) : Vector3.one * .55f;
                else
                {
                    obj.layer = M1World.SolidLayer;
                    obj.transform.localScale = Vector3.one * settings.CharacterScale;
                    var collider = obj.AddComponent<CircleCollider2D>(); collider.radius = .13f;
                }
            }

            var world = root.AddComponent<M1World>();
            world.Configure(settings, grid, ground, solids, points.ToArray());
            var player = new GameObject("阿生 - keyboard player", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SortingGroup), typeof(PlayerMover));
            player.transform.SetParent(root.transform);
            player.transform.position = world.RegionEntry("A");
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.GetComponent<CircleCollider2D>().radius = settings.FootRadius;
            var material = new PhysicsMaterial2D("M1 Frictionless");
            material.friction = 0; material.bounciness = 0;
            const string materialPath = "Assets/Settings/M1Foot.physicsMaterial2D";
            if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath) == null) AssetDatabase.CreateAsset(material, materialPath);
            else UnityEngine.Object.DestroyImmediate(material);
            player.GetComponent<CircleCollider2D>().sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
            SpriteRenderer actor = ChildSprite(player.transform, "Temporary Asheng", person, new Color(.47f,.68f,.84f));
            actor.transform.localScale = Vector3.one * settings.CharacterScale;
            SpriteRenderer facing = ChildSprite(player.transform, "Facing arrow", arrow, new Color(1f,.80f,.23f));
            facing.transform.localPosition = Vector3.up * .67f;
            facing.sortingOrder = 1;
            PlayerMover mover = player.GetComponent<PlayerMover>();
            mover.Configure(settings, facing.transform);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(CameraFollow));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.23f,.27f,.30f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            ground.CompressBounds();
            Bounds bounds = ground.localBounds;
            cameraObject.GetComponent<CameraFollow>().Configure(player.transform, settings,
                new Rect(bounds.min.x - 5, bounds.min.y - 4, bounds.size.x + 10, bounds.size.y + 8));
            root.AddComponent<GreyboxOverlay>().Configure(world, mover, camera);

            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            PlayerSettings.productName = "Reborn1985 M1";
            PlayerSettings.companyName = "QigaiPrototype";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var serializedSettings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            SerializedProperty activeInput = serializedSettings.FindProperty("activeInputHandler");
            if (activeInput != null) { activeInput.intValue = 0; serializedSettings.ApplyModifiedPropertiesWithoutUndo(); }
            Physics2D.gravity = Vector2.zero;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 1;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("M1_GENERATE_OK: serialized scene, isometric tilemaps, 20 markers, player and camera.");
        }

        private static SpriteRenderer ChildSprite(Transform parent, string name, Sprite sprite, Color color)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(parent, false);
            var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot; return renderer;
        }

        private static Tilemap MakeMap(string name, Transform parent, int sortingOrder)
        {
            var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); obj.transform.SetParent(parent, false);
            obj.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder; return obj.GetComponent<Tilemap>();
        }

        private static Tile MakeTile(string name, Sprite sprite, Tile.ColliderType collider)
        {
            string path = Art + name + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = sprite; tile.colliderType = collider; EditorUtility.SetDirty(tile); return tile;
        }

        private static Sprite MakeSprite(string name, int width, int height, Vector2 pivot, Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) texture.SetPixel(x, y, pixel(x, y));
            texture.Apply();
            string path = Art + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot; importer.SetTextureSettings(settings);
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [MenuItem("Reborn 1985/M1/Verify scene and physics")]
        public static void Verify()
        {
            EditorSceneManager.OpenScene(ScenePath);
            M1World world = UnityEngine.Object.FindFirstObjectByType<M1World>();
            PlayerMover player = UnityEngine.Object.FindFirstObjectByType<PlayerMover>();
            var checks = new List<string>();
            void Require(bool condition, string name) { if (!condition) throw new InvalidOperationException("M1_FAIL: " + name); checks.Add("PASS " + name); }
            Require(Application.unityVersion == "6000.3.25f1", "Pinned Unity editor 6000.3.25f1");
            Require(world.Grid.cellLayout == GridLayout.CellLayout.Isometric, "Real Isometric Grid");
            Require(world.Points.Length == 20 && world.Points.Select(p => p.Id).Distinct().Count() == 20, "20 unique point IDs");
            Physics2D.SyncTransforms();
            foreach (RegionDefinition region in world.Settings.Regions)
            {
                Require(world.CanStand(region.Entry), "Clear approach in " + region.Id);
                Require(world.VerificationPath(world.RegionEntry("A"), world.RegionEntry(region.Id)) != null, "A to " + region.Id + " reachable");
                Require(world.VerificationPath(world.RegionEntry(region.Id), world.RegionEntry("A")) != null, region.Id + " return to A reachable");
            }
            Require(world.Points.Single(p => p.Id == "D-04").Role == PointRole.Decoration, "D-04 decoration only");
            Require(world.Points.Single(p => p.Id == "G-01").Role == PointRole.VisualGuide, "G-01 visual guide only");
            Require(world.Points.Single(p => p.Id == "G-04").Role == PointRole.StandingSpace, "G-04 clear standing space");
            SimulationMode2D original = Physics2D.simulationMode;
            Vector2 originalPosition = player.Body.position;
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                player.Body.position = world.RegionEntry("A");
                player.SetVerificationInput(new Vector2(1, 1)); player.SendMessage("FixedUpdate");
                Require(Mathf.Abs(player.Body.linearVelocity.magnitude - world.Settings.MovementSpeed) < .001f, "Diagonal speed normalized");
                player.SetVerificationInput(Vector2.right); player.SendMessage("FixedUpdate");
                Require(player.Facing == Vector2.right, "Facing follows direction");
                Vector2 start = player.Body.position;
                for (int step = 0; step < 180; step++) { player.SendMessage("FixedUpdate"); Physics2D.Simulate(.02f); }
                Require(player.Body.position.x - start.x < world.Settings.MovementSpeed * 3.6f - .5f, "Actual 2D physics blocks building crossing");
                Require(!Physics2D.OverlapCircle(player.Body.position, world.Settings.FootRadius * .85f, M1World.SolidMask), "No solid penetration after wall push");
                player.SetVerificationInput(Vector2.zero); player.SendMessage("FixedUpdate");
                Require(player.Body.linearVelocity == Vector2.zero, "Releasing movement stops body");
            }
            finally
            {
                Physics2D.simulationMode = original; player.SetVerificationInput(null);
                player.Body.position = originalPosition; player.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
            }
            Require(Camera.main.orthographic, "Orthographic camera");
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/M1EditModeValidation.txt", checks);
            Debug.Log("M1_VERIFY_OK: " + checks.Count + " checks\n" + string.Join("\n", checks));
        }

        [MenuItem("Reborn 1985/M1/Build Windows x64 development player")]
        public static void BuildWindows()
        {
            Verify();
            // Build the saved scene, never the temporary body positions used by Verify.
            EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, target = BuildTarget.StandaloneWindows64,
                locationPathName = "Builds/Windows/Reborn1985.exe", options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("M1_BUILD_OK: Windows x64 Mono, " + report.summary.totalSize + " bytes");
        }
    }
}
