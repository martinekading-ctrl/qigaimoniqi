using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using Reborn1985.M3;
using Reborn1985.M5;
using UnityEngine;

namespace Reborn1985.M4
{
    public sealed class M4Session : MonoBehaviour
    {
        [SerializeField] private M2Scavenging game;
        [SerializeField] private M3Trade trade;
        [SerializeField] private M4Settings settings;
        private readonly Dictionary<string,GreyboxPoint> points=new Dictionary<string,GreyboxPoint>();
        private bool initialized,committed;
        private string activePoint;
        public M2Scavenging Game=>game;
        public M4Settings Settings=>settings;
        public M3Trade Trade=>trade;
        public M4SaveStore Store { get; private set; }
        public LifeView View { get; private set; }
        public bool IsOpen=>View!=LifeView.None;
        public bool Committed=>committed;
        public string ResultText { get; private set; }
        private string persistedRun,persistenceAction,persistenceIssue;
        private Vector2 persistedPosition,persistedFacing;
        private int persistedDay;
        private bool runDirty,backupLoaded;
        public bool HasUnsavedChanges=>persistedRun==null || runDirty || Vector2.Distance(game.Player.Body.position,persistedPosition)>.025f || Vector2.Dot(game.Player.Facing,persistedFacing)<.999f;
        public string SaveStatusText=>persistenceIssue??(persistedRun==null?"尚未手动保存 · F5保存":backupLoaded?"已从备份读取 · 建议F5保存":HasUnsavedChanges?"有未保存进度 · F5保存":persistenceAction+" · 第"+persistedDay+"日");
        public string DateText=>"1985年 · 第"+game.State.Life.dayIndex+"日 · 清晨";
        public event Action Changed;
        public void Configure(M2Scavenging inventory,M3Trade merchant,M4Settings content) {game=inventory;trade=merchant;settings=content;}
        private void Start()
        {
            Initialize();
            if (!game.State.Life.openingSeen)
            {
                game.State.MarkOpeningSeen();
                game.ShowFeedback("潮湿的小巷里，你醒了过来。现代创业失败的记忆还在，眼前却全然陌生。可以自由行动，L查看家书。");
            }
        }
        public void Initialize()
        {
            if(initialized) return;
            game.Initialize();trade.Initialize();
            foreach(GreyboxPoint point in game.World.Points) points.Add(point.Id,point);
            Store=new M4SaveStore(Path.Combine(Application.persistentDataPath,"Saves"),NormalizeSave);
            game.Changed+=UpdateRunDirty;
            game.LifeInteraction=HandleInteraction; initialized=true;
        }
        public Vector2 InteractionPosition(string id)=>id=="C-01"?game.World.RegionEntry("C"):(Vector2)points[id].transform.position;
        public bool Near(string id)
        {
            Vector2 delta=InteractionPosition(id)-game.Player.Body.position;
            if(delta.magnitude>settings.InteractionRange) return false;
            return !Physics2D.CircleCastAll(game.Player.Body.position,.025f,delta.normalized,delta.magnitude,M1World.SolidMask)
                .Any(hit=>hit.collider.gameObject!=points[id].gameObject);
        }
        private string NearestPoint()
        {
            M2Pickup pickup=game.Nearest();
            if(trade.IsOpen || (pickup!=null && game.Available(pickup))) return null;
            string best=null;float distance=float.PositiveInfinity;
            foreach(string id in new[]{"A-01","A-02","A-03","B-01","C-01","C-04","E-01","E-02"})
            { if(!Near(id)) continue; float next=Vector2.Distance(game.Player.Body.position,InteractionPosition(id)); if(next<distance){distance=next;best=id;} }
            return best;
        }
        public string NearbyPointId=>NearestPoint();
        public string ContextPrompt()
        {
            if(IsOpen)
            {
                if(committed) return "本次操作已完成 · Esc 返回探索";
                if(View==LifeView.Load) return "Enter 读取存档 · Esc 取消";
                if(View==LifeView.Breakfast) return "Enter 购买基础餐 · "+(!game.State.Life.helpMealClaimed&&game.State.CashCents<settings.BreakfastCents?"可用鼠标询问帮工 · ":"")+"Esc 返回";
                if(View==LifeView.Rest) return "Enter 免费休息到次日 · Esc 取消";
                if(View==LifeView.Help && game.State.Life.helpOffered && !game.State.Life.helpMealClaimed) return "Enter 完成整理并领取一次餐食 · Esc 返回";
                return "Esc 返回探索 · 不需要完成阅读才能离开";
            }
            string id=NearestPoint();
            return id==null?null:"[E] "+id+" · "+(id=="A-01"?"原铺盖，免费休息":id=="A-02"?"查看旧报纸":id=="A-03"?"观察积水":id=="B-01"?"查看公告":id=="C-01"?"林婶早餐摊":id=="C-04"?"整理摊具 / 帮工":id=="E-01"?"观察旧搪瓷水壶":"与旧货居民交谈");
        }
        private bool HandleInteraction()
        {
            if(IsOpen) return true;
            string id=NearestPoint(); if(id==null) return false;
            OpenPoint(id); return true;
        }
        public bool OpenPoint(string id)
        {
            if(!points.ContainsKey(id) || !Near(id)) {game.ShowFeedback("请靠近对应位置，并避开障碍。",true);return false;}
            LifeView view=id=="A-01"?LifeView.Rest:id=="A-02"?LifeView.Year:id=="A-03"?LifeView.Puddle:id=="B-01"?LifeView.Notice:id=="C-01"?LifeView.Breakfast:id=="C-04"?LifeView.Help:id=="E-01"?LifeView.Kettle:id=="E-02"?LifeView.Resident:LifeView.None;
            if(view==LifeView.None) return false;
            Open(view,id);game.State.MarkSeen(view);game.NotifyStateChanged();return true;
        }
        public void OpenLetter() { if(game.State.Count(ItemKind.Letter)==1){Open(LifeView.Letter,null);game.State.MarkSeen(LifeView.Letter);game.NotifyStateChanged();} }
        public void OpenJournal()=>Open(LifeView.Journal,null);
        public void OpenLoad()=>Open(LifeView.Load,null);
        private void Open(LifeView view,string point)
        {trade.Close();View=view;activePoint=point;committed=false;ResultText="";Changed?.Invoke();}
        public void Close() {View=LifeView.None;activePoint=null;committed=false;ResultText="";Changed?.Invoke();}
        public bool AcceptHelp()
        {
            if(View!=LifeView.Breakfast || committed || !Near("C-01") || !game.State.OfferHelp(settings.BreakfastCents)) return false;
            committed=true;ResultText="林婶：去 C-04 帮我整理一下摊具，收拾好了就来吃一份基础餐。";game.NotifyStateChanged();Changed?.Invoke();return true;
        }
        public bool Confirm()
        {
            if(!IsOpen || committed) return false;
            if(activePoint!=null && !Near(activePoint)) {Close();return false;}
            bool ok=false;
            switch(View)
            {
                case LifeView.Breakfast:
                    ok=game.State.TryBuyBreakfast(settings.BreakfastCents);
                    ResultText=ok?"付过钱，你吃下一份基础餐，身上渐渐暖了些。现金剩 "+M3TradeSettings.Yuan(game.State.CashCents)+"。":game.State.Life.helpMealClaimed?"钱还不够，本次未扣款。可继续拾荒；当天资源耗尽时，回原铺盖免费休息。":game.State.Life.helpOffered?"钱还不够，本次未扣款。可先到 C-04 完成已接受的帮工换餐。":"钱还不够，本次未扣款。可以去拾荒，或询问一次性帮工。";break;
                case LifeView.Help:
                    ok=game.State.CompleteHelp();
                    ResultText=ok?"你整理好摊具，领到并吃下一份基础餐。这次帮工不发现金，只能领取一次。":game.State.Life.helpMealClaimed?"这次帮工餐食已经领取，不能再领。":"先到林婶面前了解帮工，再来整理。";break;
                case LifeView.Rest:
                    ok=game.RestToNextDay();ResultText=ok?"你在原有破旧铺盖上歇了一夜。当前第 "+game.State.Life.dayIndex+" 日，现金未扣除。\n\n上日地面丢弃物已清理，背包与一次性帮工状态保留。七个来源点按有限周期刷新，可继续拾荒。":"无法推进日序，状态未变化。";break;
                case LifeView.Load:return LoadNow();
                default:return false;
            }
            if(ok) committed=true;
            game.NotifyStateChanged();game.ShowFeedback(ResultText.Split('\n')[0],!ok);Changed?.Invoke();return ok;
        }
        public M4SaveData Capture()=>new M4SaveData {parameters=M4SaveValidation.Signature(game.Settings,trade.Settings,settings),position=game.Player.Body.position,facing=game.Player.Facing,run=game.State.Snapshot()};
        public bool Validate(M4SaveData data)
        {
            if(data==null || data.version!=1 || data.scene!="M1Greybox" || data.parameters!=M4SaveValidation.Signature(game.Settings,trade.Settings,settings) || !M4SaveValidation.Run(data.run,game.Settings,trade.Settings,settings)) return false;
            if(!Finite(data.position) || !Finite(data.facing) || Mathf.Abs(data.facing.magnitude-1)>.01f || !SafePosition(data.position,game.World.Settings.FootRadius*.9f)) return false;
            return data.run.groundDrops.All(drop=>Finite(drop.position) && SafePosition(drop.position,game.World.Settings.FootRadius*.95f));
        }
        private static bool Finite(Vector2 value)=>!float.IsNaN(value.x)&&!float.IsNaN(value.y)&&!float.IsInfinity(value.x)&&!float.IsInfinity(value.y);
        private bool SafePosition(Vector2 value,float radius)
        {var cell=game.World.Ground.WorldToCell(value);return game.World.Ground.HasTile(cell)&&!game.World.Obstacles.HasTile(cell)&&!Physics2D.OverlapCircle(value,radius,M1World.SolidMask);}
        public bool SaveNow()
        {
            try{M4SaveData data=Capture();Store.Save(data,Validate);MarkPersisted(data,false,"已保存");game.ShowFeedback("已保存位置、现金、物品和事件进度。F9可读取。" );Changed?.Invoke();return true;}
            catch(Exception ex) when(ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {persistenceIssue="保存失败 · 原存档保留";game.ShowFeedback("保存失败，原存档保留："+ex.Message,true);Changed?.Invoke();return false;}
        }
        public bool LoadNow()
        {
            if(!Store.Load(Validate,out M4SaveData data,out bool backup,out string error))
            {
                Debug.LogWarning("P0 load refused: "+error);
                persistenceIssue="读取失败 · 当前进度保留";Changed?.Invoke();
                game.ShowFeedback(File.Exists(Store.Path)||File.Exists(Store.Path+".bak")?"存档和备份无法读取，当前进度保留。可以另行检查文件，或按F5保存当前进度。":"还没有可读取的存档，当前进度保留。请先按F5保存。",true);return false;
            }
            M2RunState candidate=M2RunState.RestoreValidated(data.run,game.Settings,trade.Settings,settings);
            trade.Close();Close();game.ReplaceRunState(candidate);game.Player.RestorePose(data.position,data.facing);
            Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().Snap();
            MarkPersisted(data,backup,"已读取存档");
            game.ShowFeedback(backup?"主存档不可用，已恢复上一份有效备份。":"已恢复位置、现金、物品和事件进度。");Changed?.Invoke();return true;
        }
        private M4SaveData NormalizeSave(M4SaveData data)=>M5SaveUpgrade.Normalize(data,game.Settings,trade.Settings,settings);
        private void MarkPersisted(M4SaveData data,bool backup,string action)
        {persistedRun=JsonUtility.ToJson(data.run);persistedPosition=data.position;persistedFacing=data.facing;persistedDay=data.run.life.dayIndex;runDirty=false;backupLoaded=backup;persistenceAction=action;persistenceIssue=null;}
        private void UpdateRunDirty(){if(persistedRun!=null)runDirty=JsonUtility.ToJson(game.State.Snapshot())!=persistedRun;}
        // Tests use isolated directories and never touch the player's normal save slot.
        public void SetVerificationSaveDirectory(string directory)
        {if(Debug.isDebugBuild && Environment.GetCommandLineArgs().Any(arg=>arg=="-m4-smoke"||arg=="-m5-smoke"||arg=="-p0-usability-smoke")) Store=new M4SaveStore(directory,NormalizeSave);}
        private void Update(){if(IsOpen && activePoint!=null && !Near(activePoint)) Close();}
        private void OnDisable(){if(game!=null && game.LifeInteraction==HandleInteraction)game.LifeInteraction=null;}
        private void OnEnable(){if(initialized)game.LifeInteraction=HandleInteraction;}
        private void OnDestroy(){if(game!=null)game.Changed-=UpdateRunDirty;}
    }
}
