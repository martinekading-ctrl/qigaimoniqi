using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using UnityEngine;

namespace Reborn1985.M3
{
    // Same player/controller/state/UI handlers as an ordinary launch; explicit dev flag only.
    public sealed class M3SmokeTest : MonoBehaviour
    {
        [Serializable] private sealed class Check
        {
            public string name; public bool passed;
            public long cashCents, incomeCents;
            public int grams, paper, bottles, metal, letters, ground, sold, receipts;
        }
        [Serializable] private sealed class Result
        {
            public string unityVersion, platform, graphicsDevice;
            public bool passed;
            public Check[] checks; public string[] errors, screenshots;
            public M2RunSnapshot finalState;
            public string inputMethod="Actual WindowsPlayer: same Rigidbody2D controller, E edge and uGUI button callback; not physical keyboard/mouse operation.";
            public string captureMethod="Live main camera and camera-space uGUI rendered together to RenderTexture, no mock scene or compositing.";
        }
        private M2Scavenging game;
        private M2Hud bag;
        private M3Trade trade;
        private M3Hud hud;
        private string output;
        private bool stopped;
        private readonly List<Check> checks=new List<Check>();
        private readonly List<string> errors=new List<string>(),screenshots=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {
            if (Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(),"-m3-smoke")>=0)
                new GameObject("M3 Development Verification").AddComponent<M3SmokeTest>();
        }
        private IEnumerator Start()
        {
            string[] args=Environment.GetCommandLineArgs(); int i=Array.IndexOf(args,"-m3Output");
            output=i>=0 && i+1<args.Length?args[i+1]:Path.Combine(Application.temporaryCachePath,"M3Smoke");
            Directory.CreateDirectory(output); Application.logMessageReceived+=RecordError;
            game=FindFirstObjectByType<M2Scavenging>(); bag=FindFirstObjectByType<M2Hud>(); trade=FindFirstObjectByType<M3Trade>(); hud=FindFirstObjectByType<M3Hud>();
            Application.runInBackground=true; Application.targetFrameRate=60;
            yield return new WaitForSeconds(.5f); Time.timeScale=3;
            IEnumerator routine=Run();
            while(true)
            {
                bool next;
                try { next=routine.MoveNext(); } catch(Exception ex) { errors.Add(ex.ToString()); break; }
                if(!next) break;
                yield return routine.Current;
                if(stopped) break;
            }
            game.Player.SetVerificationInput(null); game.SetVerificationInteraction(null); Time.timeScale=1;
            var result=new Result { unityVersion=Application.unityVersion,platform=Application.platform.ToString(),graphicsDevice=SystemInfo.graphicsDeviceType.ToString(),
                passed=errors.Count==0 && checks.All(check=>check.passed) && game.State.CashCents==114 && game.State.SoldCount==7,
                checks=checks.ToArray(),errors=errors.ToArray(),screenshots=screenshots.ToArray(),finalState=game.State.Snapshot() };
            File.WriteAllText(Path.Combine(output,"runtime-result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("M3_RUNTIME_"+(result.passed?"PASS":"FAIL")+": "+checks.Count+" checks, "+errors.Count+" errors, cash="+game.State.CashCents);
            Application.logMessageReceived-=RecordError; Application.Quit(result.passed?0:2);
        }
        private IEnumerator Run()
        {
            M2Pickup Source(string id)=>game.Targets.Single(target=>!target.IsDropped && target.Id==id);
            Vector2 scale=game.World.RegionEntry("G");
            Require(game.State.CashCents==0 && game.State.Count(ItemKind.Letter)==1,"New run: zero cents and protected letter");
            Require(trade.Open()==SaleResult.OutOfReach && !trade.IsOpen,"Remote Chenbo interaction rejected from A");
            yield return Walk(scale); yield return PressE();
            Require(trade.IsOpen && trade.Pending.Status==SaleResult.Empty && !hud.ConfirmButton.interactable,"E-03 empty-handed Chenbo shows rates without sale");
            Require(trade.Confirm()==SaleResult.Empty && game.State.CashCents==0 && game.State.SoldCount==0,"Empty confirm changes no assets");
            yield return Capture("01-empty-inquiry.png"); trade.Close();
            foreach(string id in new[]{"B-02","D-01","D-02","D-03"}) { yield return Walk(Source(id).transform.position); yield return PressE(); }
            Require(game.State.TotalGrams==3000 && game.State.Count(ItemKind.Paper)==2 && game.State.Count(ItemKind.Bottle)==2,"M2 base pickup regression: paper2 bottle2 3000g");
            yield return Walk(scale); bag.SetBagOpen(true); yield return PressE();
            SaleQuote original=trade.Pending;
            Require(trade.IsOpen && !bag.BagOpen && bag.InventoryInputBlocked && original.TotalGrams==3000 && original.TotalCents==44,"E-01 weighing preview: 3000g 44c, bag unobstructed by duplicate panels");
            Require(original.Lines[0].Cents==24 && original.Lines[1].Cents==20 && game.State.CashCents==0,"Preview class totals 24+20, cash not credited before confirm");
            yield return Capture("02-3kg-44c-confirmation.png");
            yield return PressE(); Require(game.State.CashCents==0 && trade.Pending==original,"E only opens, repeated E never confirms");
            trade.Close(); Require(game.State.CashCents==0 && game.State.TotalGrams==3000 && !bag.InventoryInputBlocked,"E-03 cancel retains all goods and zero cash");
            trade.Open(); SaleQuote stale=trade.Pending;
            Require(game.TryDrop(ItemKind.Paper)==InteractionResult.Success && game.State.TotalGrams==2000,"Drop after quote frees 1kg and invalidates displayed ownership");
            Require(trade.Confirm()==SaleResult.StaleQuote && game.State.CashCents==0 && game.State.GroundCount==1,"E-03 stale quantity refused atomically");
            // The same E dispatcher must prefer a nearby dropped resource to opening the counter.
            yield return PressE();
            Require(game.State.TotalGrams==3000 && game.State.GroundCount==0 && !trade.IsOpen,"M2 dropped-paper E re-pick still works at merchant approach");
            Require(game.State.TrySell(stale,trade.Settings,out _)==SaleResult.StaleQuote && game.State.CashCents==0,"Restored counts cannot revive original quote");
            yield return PressE();
            yield return Walk(game.World.Center(new Vector2Int(38,28)));
            Require(!trade.IsOpen && trade.Confirm()==SaleResult.NoPending && game.State.TotalGrams==3000 && game.State.CashCents==0,"Walking away cancels quote and prevents remote confirmation");
            yield return Walk(scale); yield return PressE();
            SaleQuote confirmed=trade.Pending;
            hud.ConfirmButton.onClick.Invoke();
            Require(game.State.CashCents==44 && game.State.ScrapIncomeCents==44 && game.State.TotalGrams==0 && game.State.SoldCount==4,"E-01 real UI callback sells exactly four units for 44 cents");
            Require(trade.DisplayedReceipt.totalGrams==3000 && trade.DisplayedReceipt.cashBefore==0 && trade.DisplayedReceipt.cashAfter==44 && hud.CashText.Contains("0.44"),"Actual receipt and HUD match committed cash");
            Require(game.State.Count(ItemKind.Letter)==1 && !trade.DisplayedReceipt.unitIds.Contains(M2RunState.LetterId),"Protected letter never enters sale");
            yield return Capture("03-first-income-44c.png");
            for(int i=0;i<30;i++) { hud.ConfirmButton.onClick.Invoke(); trade.Confirm(); }
            Require(game.State.CashCents==44 && game.State.Snapshot().receipts.Length==1 && game.State.TrySell(confirmed,trade.Settings,out _)==SaleResult.StaleQuote,"E-03 30 double callbacks and old quote replay produce no extra cash");
            trade.Close(); yield return PressE();
            Require(trade.Pending.Status==SaleResult.Empty && trade.Confirm()==SaleResult.Empty && game.State.CashCents==44,"Reopening Chenbo after selling cannot sell already-sold units"); trade.Close();
            foreach(string id in new[]{"B-02","D-01","D-02","D-03"}) Require(!game.Available(Source(id)),"Sold original pickup stays claimed "+id);
            yield return Walk(Source("F-03").transform.position); yield return PressE();
            Require(game.State.TotalGrams==3000 && game.State.Count(ItemKind.Metal)==1,"M2 capacity freed: metal can now be carried");
            yield return Walk(scale); yield return PressE();
            Require(trade.Pending.TotalCents==48 && trade.Pending.TotalGrams==3000,"E-02 metal quote 3kg at 16 cents/kg =48c");
            hud.ConfirmButton.onClick.Invoke();
            Require(game.State.CashCents==92 && game.State.TotalGrams==0 && game.State.SoldCount==5,"E-02 metal commits once: cash44+48=92c");
            yield return Capture("04-metal-48c.png"); trade.Close();
            foreach(string id in new[]{"F-01","F-02"}) { yield return Walk(Source(id).transform.position); yield return PressE(); }
            Require(game.State.TotalGrams==1500 && game.State.Count(ItemKind.Paper)==1 && game.State.Count(ItemKind.Bottle)==1,"M2 finite search still produces one paper and one bottle");
            bag.SetBagOpen(true); bag.SelectItem(ItemKind.Bottle);
            Require(bag.DropSelected()==InteractionResult.Success && game.State.GroundCount==1,"M2 bag drop after sales retains unsold bottle on ground");
            M2Pickup bottle=game.Targets.Single(target=>target.IsDropped);
            yield return Walk(scale); yield return PressE();
            Require(trade.Pending.TotalCents==12 && trade.Pending.UnitIds.Count==1,"Sale quotes carried paper only, excludes dropped bottle");
            hud.ConfirmButton.onClick.Invoke();
            Require(game.State.CashCents==104 && game.State.GroundCount==1,"Paper sale preserves ground bottle and credits12c"); trade.Close();
            yield return Walk(bottle.transform.position); yield return PressE();
            Require(game.State.Count(ItemKind.Bottle)==1 && game.State.TotalGrams==500 && game.State.GroundCount==0,"Unsold ground bottle re-picks with original identity");
            yield return Walk(scale); yield return PressE();
            Require(trade.Pending.TotalCents==10 && trade.Pending.TotalGrams==500,"Half-kilogram bottle quotes10c, not20c per piece");
            hud.ConfirmButton.onClick.Invoke();
            M2RunSnapshot snapshot=game.State.Snapshot();
            Require(game.State.CashCents==114 && snapshot.receipts.Length==4 && snapshot.soldUnits.Length==7 && snapshot.inventory.Length==1,"Seven finite resources sold once:114c, four receipts, only letter in bag");
            Require(snapshot.soldUnits.Select(unit=>unit.unitId).Distinct().Count()==7 && snapshot.claimedSourceIds.Length==7,"No duplicated sold identity and no source claim reset");
            yield return Capture("05-finite-total-114c.png"); trade.Close();
            bag.SetBagOpen(true); bag.SelectItem(ItemKind.Letter);
            Require(bag.DropSelected()==InteractionResult.Protected && game.State.Count(ItemKind.Letter)==1 && game.State.TotalGrams==0,"M2 protected letter still cannot drop after all transactions");
            yield return Capture("06-cash-and-protected-bag.png");
            yield return Walk(game.World.RegionEntry("A"));
            Require(game.State.CashCents==114 && game.State.SoldCount==7 && !trade.IsOpen,"Return to A preserves exact cash without scene reset or movement lock");
        }
        private IEnumerator PressE()
        { game.SetVerificationInteraction(false); yield return null; game.SetVerificationInteraction(true); yield return null; yield return null; game.SetVerificationInteraction(false); yield return null; }
        private IEnumerator Walk(Vector2 goal)
        {
            List<Vector2> path=game.World.VerificationPath(game.Player.Body.position,goal);
            if(path==null) { errors.Add("No clear physical route to "+goal); stopped=true; yield break; }
            foreach(Vector2 waypoint in path)
            {
                int steps=0;
                while(Vector2.Distance(game.Player.Body.position,waypoint)>.045f)
                {
                    Vector2 delta=waypoint-game.Player.Body.position;
                    game.Player.SetVerificationInput(delta.normalized*Mathf.Clamp01(delta.magnitude/(game.World.Settings.MovementSpeed*Time.fixedDeltaTime)));
                    yield return new WaitForFixedUpdate();
                    if(++steps>180) { errors.Add("Physical movement stuck at "+waypoint); stopped=true; break; }
                }
                if(stopped) break;
            }
            game.Player.SetVerificationInput(Vector2.zero); yield return null;
        }
        private void Require(bool passed,string name)
        {
            checks.Add(new Check {name=name,passed=passed,cashCents=game.State.CashCents,incomeCents=game.State.ScrapIncomeCents,grams=game.State.TotalGrams,
                paper=game.State.Count(ItemKind.Paper),bottles=game.State.Count(ItemKind.Bottle),metal=game.State.Count(ItemKind.Metal),letters=game.State.Count(ItemKind.Letter),
                ground=game.State.GroundCount,sold=game.State.SoldCount,receipts=game.State.Snapshot().receipts.Length});
            if(!passed) errors.Add(name); Debug.Log("M3_CHECK_"+(passed?"PASS ":"FAIL ")+name);
        }
        private IEnumerator Capture(string name)
        {
            yield return null; Camera.main.GetComponent<CameraFollow>().Snap(); Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            Camera camera=Camera.main; RenderTexture original=camera.targetTexture,active=RenderTexture.active;
            RenderTexture target=RenderTexture.GetTemporary(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);
            var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); texture.Apply();
                var colors=new HashSet<Color32>(); Color32[] pixels=texture.GetPixels32(); for(int i=0;i<pixels.Length;i+=997) colors.Add(pixels[i]);
                if(colors.Count<8) errors.Add("Blank capture "+name);
                File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG()); screenshots.Add(name);
            }
            finally { camera.targetTexture=original; RenderTexture.active=active; RenderTexture.ReleaseTemporary(target); Destroy(texture); }
        }
        private void RecordError(string text,string trace,LogType type)
        { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) errors.Add(text); }
    }
}
