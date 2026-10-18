using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Reborn1985.M1
{
    // Opt-in development-player verification. Normal launches never create this object.
    public sealed class M1SmokeWalk : MonoBehaviour
    {
        [Serializable]
        private sealed class Result
        {
            public string unityVersion;
            public string platform;
            public bool passed;
            public string[] visitedRegions;
            public string[] screenshots;
            public string[] errors;
            public string inputMethod = "Development harness drives the same Rigidbody2D controller; not a physical-keyboard test.";
            public string captureMethod = "Windows player camera renders the live scene to a RenderTexture; OnGUI panels are not in these captures.";
        }

        private readonly List<string> errors = new List<string>();
        private readonly List<string> visited = new List<string>();
        private readonly List<string> screenshots = new List<string>();
        private string output;
        private M1World world;
        private PlayerMover player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {
            if (!Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-m1-smoke") < 0) return;
            new GameObject("M1 Development Verification").AddComponent<M1SmokeWalk>();
        }

        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-m1Output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.temporaryCachePath, "M1Smoke");
            Directory.CreateDirectory(output);
            Application.logMessageReceived += RecordError;
            world = FindFirstObjectByType<M1World>();
            player = FindFirstObjectByType<PlayerMover>();
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            yield return new WaitForSeconds(.5f);
            yield return Capture("01-A-start.png");
            Camera camera = Camera.main;
            CameraFollow follow = camera.GetComponent<CameraFollow>();
            float size = camera.orthographicSize;
            follow.enabled = false;
            Bounds bounds = world.Ground.localBounds;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
            camera.orthographicSize = Mathf.Max(bounds.size.y / 2 + 1, bounds.size.x / (2 * camera.aspect) + 1);
            yield return Capture("00-seven-zone-overview.png");
            camera.orthographicSize = size;
            follow.enabled = true;
            follow.Snap();

            Time.timeScale = 3;
            string[] route = { "A", "B", "D", "C", "D", "G", "D", "F", "D", "E", "D", "B", "A" };
            foreach (string region in route)
            {
                List<Vector2> path = world.VerificationPath(player.Body.position, world.RegionEntry(region));
                if (path == null) { errors.Add("No collision-clear route to " + region); break; }
                bool failed = false;
                foreach (Vector2 waypoint in path)
                {
                    int steps = 0;
                    while (Vector2.Distance(player.Body.position, waypoint) > .045f)
                    {
                        Vector2 delta = waypoint - player.Body.position;
                        player.SetVerificationInput(delta.normalized * Mathf.Clamp01(delta.magnitude / (world.Settings.MovementSpeed * Time.fixedDeltaTime)));
                        yield return new WaitForFixedUpdate();
                        if (++steps > 180) { errors.Add("Movement stuck while approaching " + region); failed = true; break; }
                    }
                    if (failed) break;
                }
                player.SetVerificationInput(Vector2.zero);
                if (failed) break;
                if (Vector2.Distance(player.Body.position, world.RegionEntry(region)) > .12f)
                { errors.Add("Arrival mismatch in " + region); break; }
                visited.Add(region);
                Debug.Log("M1_WALK_REACHED " + region);
                yield return new WaitForSeconds(.5f);
                if (region == "C" || region == "G" || region == "F" || region == "E") yield return Capture("zone-" + region + ".png");
            }

            player.SetVerificationInput(null);
            Time.timeScale = 1;
            var result = new Result
            {
                unityVersion = Application.unityVersion, platform = Application.platform.ToString(),
                passed = errors.Count == 0 && visited.Count == route.Length,
                visitedRegions = visited.ToArray(), screenshots = screenshots.ToArray(), errors = errors.ToArray()
            };
            File.WriteAllText(Path.Combine(output, "runtime-result.json"), JsonUtility.ToJson(result, true));
            Debug.Log("M1_RUNTIME_" + (result.passed ? "PASS" : "FAIL"));
            Application.logMessageReceived -= RecordError;
            Application.Quit(result.passed ? 0 : 2);
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            Camera camera = Camera.main;
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture originalTarget = camera.targetTexture, originalActive = RenderTexture.active;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                Color32[] pixels = texture.GetPixels32();
                var samples = new HashSet<Color32>();
                for (int i = 0; i < pixels.Length; i += 997) samples.Add(pixels[i]);
                if (samples.Count < 5) errors.Add("Capture appears blank: " + name);
                File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG());
                screenshots.Add(name);
            }
            finally
            {
                camera.targetTexture = originalTarget; RenderTexture.active = originalActive;
                RenderTexture.ReleaseTemporary(target); Destroy(texture);
            }
            yield return null;
        }

        private void RecordError(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        }
    }
}
