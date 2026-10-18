using System.Collections;
using System.Text;
using Reborn1985.M2;
using Reborn1985.M3;
using UnityEngine;
using UnityEngine.UI;

namespace Reborn1985.M4
{
    public sealed class M4Hud : MonoBehaviour
    {
        [SerializeField] private M4Session session;
        [SerializeField] private M2Hud hud;
        private GameObject panel;
        private Text date,title,body,primaryLabel;
        private Button primary,secondary;
        public bool Ready=>panel!=null;
        public Button PrimaryButton=>primary;
        public Button HelpButton=>secondary;
        public string BodyText=>body==null?"":body.text;
        public void Configure(M4Session life,M2Hud overlay) {session=life;hud=overlay;}
        private IEnumerator Start()
        {
            while(hud.CanvasRoot==null) yield return null;
            session.Initialize();
            RectTransform status=M2Hud.Panel(hud.CanvasRoot,"M4 date and save",new Vector2(0,1),new Vector2(0,1),new Vector2(18,-106),new Vector2(485,82));
            date=hud.Label(status,"",17,14,8,235,29,new Color(1,.88f,.62f));
            MakeButton(status,"保存",new Vector2(265,-8),new Vector2(95,31),()=>session.SaveNow());
            MakeButton(status,"读取",new Vector2(370,-8),new Vector2(95,31),()=>session.OpenLoad());
            hud.Label(status,"F5 保存 · F9 读取 · L 家书 · J 线索",14,14,48,456,25);
            RectTransform window=M2Hud.Panel(hud.CanvasRoot,"M4 life and opening",Vector2.one,Vector2.one,new Vector2(-18,-106),new Vector2(520,446));
            panel=window.gameObject;
            title=hud.Label(window,"",22,18,12,484,32,new Color(1,.88f,.62f));
            body=hud.Label(window,"",17,18,58,484,292);body.horizontalOverflow=HorizontalWrapMode.Wrap;body.alignment=TextAnchor.UpperLeft;
            primary=MakeButton(window,"确认",new Vector2(18,-362),new Vector2(235,34),()=>session.Confirm());
            primaryLabel=primary.GetComponentInChildren<Text>();
            secondary=MakeButton(window,"询问一次性帮工",new Vector2(269,-362),new Vector2(233,34),()=>session.AcceptHelp());
            MakeButton(window,"返回探索 [Esc]",new Vector2(18,-407),new Vector2(484,30),()=>session.Close());
            session.Changed+=Refresh;session.Game.Changed+=Refresh;
            hud.LifeContextPrompt=session.ContextPrompt;hud.SetLetterReader(session.OpenLetter);Refresh();
        }
        private void Update()
        {
            if(!Ready) return;
            if(Input.GetKeyDown(KeyCode.F5)) session.SaveNow();
            if(Input.GetKeyDown(KeyCode.F9)) session.OpenLoad();
            if(Input.GetKeyDown(KeyCode.L)) session.OpenLetter();
            if(Input.GetKeyDown(KeyCode.J)) session.OpenJournal();
            if(!session.IsOpen) return;
            if(Input.GetKeyDown(KeyCode.Escape)){session.Close();return;}
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))session.Confirm();
        }
        private void Refresh()
        {
            if(!Ready)return;
            M4LifeData state=session.Game.State.Life;
            date.text=session.DateText;
            panel.SetActive(session.IsOpen);hud.LifeInputBlocked=session.IsOpen;
            if(session.IsOpen)hud.SetBagOpen(false);
            primary.gameObject.SetActive(false);secondary.gameObject.SetActive(false);
            primary.interactable=!session.Committed;primaryLabel.text="确认 [Enter]";
            string text="";
            switch(session.View)
            {
                case LifeView.Breakfast:
                    title.text="林婶早餐摊";text="林婶：先吃点东西，身上才有力气。\n\n基础餐："+M3TradeSettings.Yuan(session.Settings.BreakfastCents)+"\n当前现金："+M3TradeSettings.Yuan(session.Game.State.CashCents)+"\n\n购买后直接进食并记录，不生成可倒卖的食物。\n"+(state.helpMealClaimed?"一次性帮工餐食已领取。":state.helpOffered?"帮工已接受，可到 C-04 整理摊具。":"钱不够时，可询问一次性帮工。")+"\n\n已购买餐食："+state.purchasedMeals+" 份";
                    primary.gameObject.SetActive(true);primaryLabel.text="购买基础餐 [Enter]";
                    secondary.gameObject.SetActive(true);secondary.interactable=!session.Committed&&!state.helpMealClaimed&&session.Game.State.CashCents<session.Settings.BreakfastCents;break;
                case LifeView.Help:
                    title.text="C-04 · 整理摊具";text=state.helpMealClaimed?"这次帮工已完成，基础餐食已领取。不会因再对话或读取存档重新发放。":state.helpOffered?"林婶请你帮忙整理摊具。\n\n确认完成这次简单整理，可领到并吃下一份基础餐。只领取一次，不增加现金。\n\n动作是M4原型交互，具体表现待验证。":"先到林婶面前询问一次性帮工。";
                    primary.gameObject.SetActive(state.helpOffered&&!state.helpMealClaimed);primaryLabel.text="完成整理并领取餐食";break;
                case LifeView.Rest:
                    title.text="原有破旧铺盖 · 免费休息";text="这里只是阿生在小巷中的临时落脚处。\n\n休息不收费，确认后进入下一日。\n\n上日仍留在地面的丢弃物会清理，背包中的物品保留。一次性帮工餐不会重置。\n\n七个原有来源点按有限周期刷新，每点每轮只产出一件。可在休息后按 F5 保存。";
                    primary.gameObject.SetActive(true);primaryLabel.text="免费休息到次日";break;
                case LifeView.Letter:title.text="母亲来信";text=session.Settings.LetterSummary;break;
                case LifeView.Year:title.text="旧报纸上的年份";text="纸面上的年份是：1985。\n\n县城的自行车、旧招牌和朴素街景，都与现代记忆不同。\n\n已记录年代线索。具体月日尚未确认。";break;
                case LifeView.Notice:title.text="巷口公告";text="公告与旧报纸留下了1985年的年代痕迹。\n\n你可以继续沿主街寻找谋生机会。\n\n公告终稿及历史细节待核查，本版只记录年代线索。";break;
                case LifeView.Puddle:title.text="积水里的身影";text="积水映出一张年轻而陌生的脸，身上的旧衣满是补丁。\n\n现代的记忆仍在，这具身体的过往却只有零碎印象。\n\n这条小巷不是自己的住宅。";break;
                case LifeView.Kettle:title.text="旧搪瓷水壶";text="一只旧搪瓷水壶被放在旧货角落。\n\n有些旧东西，也许会被不同的人赋予不同价值。先记下这条线索。\n\nP0只观察并记录，不能拿走、购买、议价或转卖。";break;
                case LifeView.Resident:title.text="处理旧物的居民";text="居民：这壶准备处理掉，放着也是占地方。\n\n你记下了旧壶的消息。是否存在商机，仍需以后调查；没有必赚答案。\n\n旧货交易属于P1，本版不开放。";break;
                case LifeView.Journal:
                    title.text="家书与线索";
                    var notes=new StringBuilder();
                    notes.AppendLine(state.letterRead?"家书：已查看母亲来信摘要。":"家书：尚未查看，可按 L 阅读。");
                    notes.AppendLine(state.yearRead||state.noticeRead?"年代：已发现1985年线索。":"年代：还可留意小巷报纸与巷口公告。");
                    notes.AppendLine(state.puddleObserved?"处境：已观察积水与陌生身影。":"处境：尚未观察小巷积水。");
                    notes.AppendLine(state.kettleObserved||state.residentTalked?"旧壶：已记录旧货角落消息，只作线索。":"旧壶：尚未记录线索。");
                    notes.AppendLine("\n"+session.DateText+"\n已购买餐食 "+state.purchasedMeals+" 份；帮工餐食 "+(state.helpMealClaimed?"已领取":"未领取"));text=notes.ToString();break;
                case LifeView.Load:
                    title.text="读取上次存档";text="读取会完整恢复上次保存的位置、朝向、现金、背包、地面物及事件进度。\n\n当前未保存的进度会被替换。\n\n主文件损坏时会尝试上一份有效备份；都无效时保留当前游戏状态。";
                    primary.gameObject.SetActive(true);primaryLabel.text="读取存档 [Enter]";break;
            }
            if(session.Committed)text=session.ResultText;
            body.text=text;
        }
        private Button MakeButton(Transform parent,string label,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            RectTransform rect=M2Hud.Rect(parent,label,new Vector2(0,1),new Vector2(0,1),position,size);
            Image image=rect.gameObject.AddComponent<Image>();image.color=new Color(.29f,.38f,.42f);
            Button button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);
            hud.Label(rect,label,15,5,2,size.x-10,size.y-4).alignment=TextAnchor.MiddleCenter;return button;
        }
        private void OnDestroy()
        {
            if(session!=null)session.Changed-=Refresh;if(session!=null&&session.Game!=null)session.Game.Changed-=Refresh;
            if(hud!=null){hud.LifeInputBlocked=false;hud.SetLetterReader(null);if(session!=null&&hud.LifeContextPrompt==session.ContextPrompt)hud.LifeContextPrompt=null;}
        }
    }
}
