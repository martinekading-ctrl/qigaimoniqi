using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using UnityEngine;

namespace Reborn1985.M2
{
    // Opt-in Windows development-player checks. Never runs on an ordinary launch.
    public sealed class M2SmokeTest : MonoBehaviour
    {
        [Serializable] private sealed class Check
        {
            public string name;
            public bool passed;
            public int paper, bottles, metal, letters, totalGrams, groundUnits;
            public string details;
        }
        [Serializable] private sealed class Result
        {
            public string unityVersion, platform, graphicsDevice;
            public bool passed;
            public string[] visitedRegions, screenshots, errors;
            public Check[] checks;
            public M2RunSnapshot finalState;
            public string inputMethod = "Actual WindowsPlayer; same Rigidbody2D mover and E-key edge path via development input, plus same UI drop handler. Not a physical keyboard/mouse test.";
            public string captureMethod = "Live Windows camera renders scene AND camera-space uGUI into RenderTexture. No mock UI, compositing or AI images.";
        }
        private readonly List<Check> checks = new List<Check>();
        private readonly List<string> errors = new List<string>(), visited = new List<string>(), screenshots = new List<string>();
        private M2Scavenging game;
        private M2Hud hud;
        private string output;
        private bool walkFailed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-m2-smoke") >= 0)
                new GameObject("M2 Development Verification").AddComponent<M2SmokeTest>();
        }
        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-m2Output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.temporaryCachePath, "M2Smoke");
            Directory.CreateDirectory(output);
            Application.logMessageReceived += RecordError;
            Application.runInBackground = true; Application.targetFrameRate = 60;
            game = FindFirstObjectByType<M2Scavenging>(); hud = FindFirstObjectByType<M2Hud>();
            yield return new WaitForSeconds(.5f);
            Time.timeScale = 3;
            IEnumerator test = RunChecks();
            while (true)
            {
                bool more;
                try { more = test.MoveNext(); }
                catch (Exception ex) { errors.Add(ex.ToString()); break; }
                if (!more) break;
                yield return test.Current;
                if (walkFailed) break;
            }
            game.Player.SetVerificationInput(null); game.SetVerificationInteraction(null); Time.timeScale = 1;
            var result = new Result
            {
                unityVersion = Application.unityVersion, platform = Application.platform.ToString(), graphicsDevice = SystemInfo.graphicsDeviceType.ToString(),
                passed = errors.Count == 0 && checks.All(check => check.passed) && visited.Count == 13,
                visitedRegions = visited.ToArray(), screenshots = screenshots.ToArray(), errors = errors.ToArray(), checks = checks.ToArray(), finalState = game.State.Snapshot()
            };
            File.WriteAllText(Path.Combine(output, "runtime-result.json"), JsonUtility.ToJson(result, true));
            Debug.Log("M2_RUNTIME_" + (result.passed ? "PASS" : "FAIL") + ": " + checks.Count + " checks, " + visited.Count + " route stops, " + errors.Count + " errors");
            Application.logMessageReceived -= RecordError;
            Application.Quit(result.passed ? 0 : 2);
        }
        private IEnumerator RunChecks()
        {
            M2Pickup Source(string id) => game.Targets.Single(target => !target.IsDropped && target.Id == id);
            Require(game.State.Count(ItemKind.Letter) == 1 && game.State.TotalGrams == 0, "TEST6 new run has one weightless letter");
            hud.SetBagOpen(true); hud.SelectItem(ItemKind.Letter);
            Require(hud.DropSelected() == InteractionResult.Protected && game.State.Count(ItemKind.Letter) == 1, "TEST6 letter destructive operation rejected through HUD");
            yield return Capture("00-protected-letter.png");
            hud.SetBagOpen(false);
            Require(game.TryInteract(Source("B-02")) == InteractionResult.OutOfReach && game.State.TotalGrams == 0, "Remote pickup refused from A");

            yield return Walk(game.World.Center(new Vector2Int(6,15)));
            yield return Capture("01-nearby-pickup.png");
            Vector2 midpoint = (game.Player.Body.position + (Vector2)Source("B-02").transform.position) / 2;
            var wall = new GameObject("Temporary verification-only LOS blocker", typeof(BoxCollider2D));
            wall.layer = M1World.SolidLayer; wall.transform.position = midpoint;
            wall.GetComponent<BoxCollider2D>().size = new Vector2(.10f,.10f);
            Physics2D.SyncTransforms();
            Require(game.TryInteract(Source("B-02")) == InteractionResult.OutOfReach && !game.State.Claimed("B-02"), "Wall blocks pickup at otherwise legal distance");
            Destroy(wall); yield return null;
            Require(game.Nearest() == Source("B-02"), "Nearby prompt selects legal B-02");
            game.SetVerificationInteraction(true);
            yield return null; yield return null;
            Require(game.State.Count(ItemKind.Paper) == 1 && !Source("B-02").Visual.enabled, "E press collects B-02 and removes its ground visual");
            // Keep E held while walking to another source: arriving must not auto-collect it.
            yield return Walk(Source("D-01").transform.position);
            Require(game.State.Count(ItemKind.Paper) == 1 && !game.State.Claimed("D-01"), "Held E does not auto-collect on leaving and returning");
            game.SetVerificationInteraction(false); yield return null;
            yield return PressE();
            foreach (string id in new[] { "D-02", "D-03" }) { yield return Walk(Source(id).transform.position); yield return PressE(); }
            Require(game.State.Count(ItemKind.Paper) == 2 && game.State.Count(ItemKind.Bottle) == 2 && game.State.TotalGrams == 3000 &&
                new[] { "B-02", "D-01", "D-02", "D-03" }.All(id => game.State.Claimed(id) && !Source(id).Visual.enabled), "TEST1 base route: paper2 bottle2 = 3000g, all four points claimed");
            hud.SetBagOpen(true);
            Require(hud.WeightText.Contains("3.0 kg") && hud.WeightText.Contains("5.0 kg"), "HUD displays actual 3.0 / 5.0 kg");
            yield return Capture("02-backpack-3kg.png");
            yield return Walk(Source("F-03").transform.position);
            yield return PressE();
            Require(game.State.TotalGrams == 3000 && game.State.Count(ItemKind.Metal) == 0 && game.Available(Source("F-03")) && hud.FeedbackText.Contains("超重"), "TEST2 6000g rejected; metal stays and backpack unchanged");
            yield return Capture("03-overweight-metal.png");
            hud.SelectItem(ItemKind.Paper);
            Require(hud.DropSelected() == InteractionResult.Success && game.State.TotalGrams == 2000 && game.State.GroundCount == 1, "Drop one paper immediately removes 1000g and creates one ground unit");
            CheckDropsSafe();
            yield return PressE();
            Require(game.State.Count(ItemKind.Paper) == 1 && game.State.Count(ItemKind.Bottle) == 2 && game.State.Count(ItemKind.Metal) == 1 && game.State.TotalGrams == 5000 && game.State.Claimed("F-03"), "TEST3 capacity choice: metal succeeds at exactly 5000g");
            yield return Capture("04-metal-at-capacity.png");
            M2Pickup paperDrop = game.Targets.Single(target => target.IsDropped);
            Require(game.TryInteract(paperDrop) == InteractionResult.Overweight && game.State.HasDrop(paperDrop.Id), "Re-pick applies capacity limit and retains rejected ground item");
            yield return Walk(Source("F-01").transform.position);
            yield return PressE();
            Require(!game.State.Claimed("F-01") && game.State.TotalGrams == 5000, "Overweight search does not consume finite yield");
            hud.SelectItem(ItemKind.Metal);
            Require(hud.DropSelected() == InteractionResult.Success && game.State.TotalGrams == 2000, "Drop metal to free search capacity");
            yield return PressE();
            yield return Walk(Source("F-02").transform.position);
            yield return PressE();
            int before = game.State.TotalGrams;
            for (int i = 0; i < 20; i++)
            {
                game.TryInteract(Source("F-01")); game.TryInteract(Source("F-02"));
            }
            Require(before == 3500 && game.State.TotalGrams == before && game.State.Count(ItemKind.Paper) == 2 && game.State.Count(ItemKind.Bottle) == 3 &&
                game.State.Claimed("F-01") && game.State.Claimed("F-02"), "TEST4 each search yields once; 20 repeat pairs produce zero extras");
            yield return Capture("05-finite-search.png");
            for (int i = 0; i < 30; i++)
            {
                ItemKind kind = i % 2 == 0 ? ItemKind.Paper : ItemKind.Bottle;
                hud.SelectItem(kind);
                string[] existing = game.Targets.Where(target => target.IsDropped).Select(target => target.Id).ToArray();
                if (hud.DropSelected() != InteractionResult.Success) { Require(false, "Cycle drop " + i); break; }
                M2Pickup dropped = game.Targets.Single(target => target.IsDropped && !existing.Contains(target.Id));
                CheckDropsSafe();
                if (game.TryInteract(dropped) != InteractionResult.Success) { Require(false, "Cycle re-pick " + i); break; }
                Require(game.TryInteract(dropped) == InteractionResult.Unavailable, "Duplicate same-frame re-pick refused " + i);
                hud.SetBagOpen(false); hud.SetBagOpen(true);
                yield return null;
            }
            M2RunSnapshot snapshot = game.State.Snapshot();
            Require(game.State.TotalGrams == 3500 && game.State.Count(ItemKind.Paper) == 2 && game.State.Count(ItemKind.Bottle) == 3 && game.State.GroundCount == 2 &&
                snapshot.inventory.Concat(snapshot.groundDrops.Select(drop => drop.unit)).Select(unit => unit.unitId).Distinct().Count() == 8, "TEST5 30 drop/re-pick and bag-toggle cycles conserve all seven resources plus letter");
            Require(snapshot.claimedSourceIds.Length == 7 && game.State.Count(ItemKind.Letter) == 1 && game.State.Count(ItemKind.Metal) == 0, "Claims and protected letter survive all exchanges");
            yield return Capture("06-drop-and-repick.png");
            yield return Walk(game.World.RegionEntry("A"));
            string[] route = { "A", "B", "D", "C", "D", "G", "D", "F", "D", "E", "D", "B", "A" };
            foreach (string zone in route)
            {
                yield return Walk(game.World.RegionEntry(zone));
                if (walkFailed) yield break;
                visited.Add(zone);
                Debug.Log("M2_REGRESSION_REACHED " + zone);
                Require(Vector2.Distance(game.Player.Body.position, game.World.RegionEntry(zone)) < .12f && Camera.main.orthographic, "TEST7 move/camera regression " + visited.Count + ": " + zone);
            }
            Require(game.State.TotalGrams == 3500 && game.State.GroundCount == 2, "Full route with bag open does not duplicate, lose or block resources");
            yield return Capture("07-return-to-A.png");
        }
        private IEnumerator PressE()
        {
            game.SetVerificationInteraction(false); yield return null;
            game.SetVerificationInteraction(true); yield return null; yield return null;
            game.SetVerificationInteraction(false); yield return null;
        }
        private IEnumerator Walk(Vector2 goal)
        {
            List<Vector2> path = game.World.VerificationPath(game.Player.Body.position, goal);
            if (path == null) { errors.Add("No collision-clear path to " + goal); walkFailed = true; yield break; }
            foreach (Vector2 waypoint in path)
            {
                int steps = 0;
                while (Vector2.Distance(game.Player.Body.position, waypoint) > .045f)
                {
                    Vector2 delta = waypoint - game.Player.Body.position;
                    game.Player.SetVerificationInput(delta.normalized * Mathf.Clamp01(delta.magnitude / (game.World.Settings.MovementSpeed * Time.fixedDeltaTime)));
                    yield return new WaitForFixedUpdate();
                    if (++steps > 180) { errors.Add("Movement stuck at " + waypoint); walkFailed = true; break; }
                }
                if (walkFailed) break;
            }
            game.Player.SetVerificationInput(Vector2.zero);
            yield return null;
        }
        private void CheckDropsSafe()
        {
            bool safe = game.Targets.Where(target => target.IsDropped).All(target => target.GetComponent<Collider2D>() == null &&
                !Physics2D.OverlapCircle(target.transform.position, game.World.Settings.FootRadius + .02f, M1World.SolidMask) &&
                game.World.Ground.HasTile(game.World.Ground.WorldToCell(target.transform.position)));
            Require(safe, "Ground drops are on floor, clear of NPC/wall colliders and non-blocking");
        }
        private void Require(bool passed, string name)
        {
            checks.Add(new Check { name = name, passed = passed, paper = game.State.Count(ItemKind.Paper), bottles = game.State.Count(ItemKind.Bottle),
                metal = game.State.Count(ItemKind.Metal), letters = game.State.Count(ItemKind.Letter), totalGrams = game.State.TotalGrams, groundUnits = game.State.GroundCount,
                details = "claimCount=" + game.State.Snapshot().claimedSourceIds.Length + "; feedback=" + game.Feedback });
            if (!passed) errors.Add(name);
            Debug.Log("M2_CHECK_" + (passed ? "PASS " : "FAIL ") + name);
        }
        private IEnumerator Capture(string name)
        {
            yield return null;
            Camera.main.GetComponent<CameraFollow>().Snap();
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Camera camera = Camera.main;
            RenderTexture original = camera.targetTexture, active = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); texture.Apply();
                Color32[] pixels = texture.GetPixels32(); var samples = new HashSet<Color32>();
                for (int i = 0; i < pixels.Length; i += 997) samples.Add(pixels[i]);
                if (samples.Count < 8) errors.Add("Blank capture " + name);
                File.WriteAllBytes(Path.Combine(output,name), texture.EncodeToPNG()); screenshots.Add(name);
            }
            finally { camera.targetTexture = original; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); Destroy(texture); }
        }
        private void RecordError(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }
    }
}
