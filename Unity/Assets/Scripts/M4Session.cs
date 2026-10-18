using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using Reborn1985.M3;
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
        public M4SaveStore Store { get; private set; }
        public LifeView View { get; private set; }
        public bool IsOpen=>View!=LifeView.None;
        public bool Committed=>committed;
        public string ResultText { get; private set; }
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
            Store=new M4SaveStore(Path.Combine(Application.persistentDataPath,"Saves"));
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
        public string ContextPrompt()
        {
            if(IsOpen) return "Enter 确认当前操作 · Esc 返回探索";
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
                    ResultText=ok?"付过钱，你吃下一份基础餐，身上渐渐暖了些。现金剩 "+M3TradeSettings.Yuan(game.State.CashCents)+"。":"钱还不够，本次未扣款。可以去拾荒，或询问一次性帮工。";break;
                case LifeView.Help:
                    ok=game.State.CompleteHelp();
                    ResultText=ok?"你整理好摊具，领到并吃下一份基础餐。这次帮工不发现金，只能领取一次。":game.State.Life.helpMealClaimed?"这次帮工餐食已经领取，不能再领。":"先到林婶面前了解帮工，再来整理。";break;
                case LifeView.Rest:
                    ok=game.State.Rest();ResultText=ok?"你在原有破旧铺盖上歇了一夜。当前第 "+game.State.Life.dayIndex+" 日，现金未扣除。\n\nM4只推进日序；资源刷新与上日丢弃物清理由M5接入。":"无法推进日序，状态未变化。";break;
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
            try{Store.Save(Capture(),Validate);game.ShowFeedback("已保存位置、现金、物品和事件进度。F9可读取。" );Changed?.Invoke();return true;}
            catch(Exception ex) when(ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {game.ShowFeedback("保存失败，原存档保留："+ex.Message,true);return false;}
        }
        public bool LoadNow()
        {
            if(!Store.Load(Validate,out M4SaveData data,out bool backup,out string error))
            {game.ShowFeedback("未能读取有效存档，当前进度保留："+error,true);return false;}
            M2RunState candidate=M2RunState.RestoreValidated(data.run,game.Settings,trade.Settings,settings);
            trade.Close();Close();game.ReplaceRunState(candidate);game.Player.RestorePose(data.position,data.facing);
            Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().Snap();
            game.ShowFeedback(backup?"主存档不可用，已恢复上一份有效备份。":"已恢复位置、现金、物品和事件进度。");Changed?.Invoke();return true;
        }
        // Tests use isolated directories and never touch the player's normal save slot.
        public void SetVerificationSaveDirectory(string directory)
        {if(Debug.isDebugBuild && Environment.GetCommandLineArgs().Any(arg=>arg=="-m4-smoke")) Store=new M4SaveStore(directory);}
        private void Update(){if(IsOpen && activePoint!=null && !Near(activePoint)) Close();}
        private void OnDisable(){if(game!=null && game.LifeInteraction==HandleInteraction)game.LifeInteraction=null;}
        private void OnEnable(){if(initialized)game.LifeInteraction=HandleInteraction;}
    }
}
