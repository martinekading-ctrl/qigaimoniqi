using System;
using Reborn1985.M2;
using Reborn1985.M3;
using Reborn1985.M4;
using UnityEngine;
using UnityEngine.UI;

namespace Reborn1985.P0
{
    // Presentation only: target selection reuses the same distance/LOS/priority as gameplay.
    public sealed class P0UsabilityHud : IDisposable
    {
        private readonly M4Session session;
        private readonly M2Hud hud;
        private readonly M3Trade trade;
        private readonly Camera camera;
        private readonly RectTransform canvas,controls,hint;
        private readonly Text saveStatus,hintLabel,controlsBody;
        private readonly SpriteRenderer[] playerSprites;
        public Button ControlsButton { get; }
        public bool ControlsOpen=>controls.gameObject.activeSelf;
        public bool HintVisible=>hint.gameObject.activeSelf;
        public string HintText=>hintLabel.text;
        public RectTransform HintRect=>hint;
        public string SaveText=>saveStatus.text;
        public string HelpText { get; }
        public bool ControlsTextFits=>controlsBody.preferredHeight<=controlsBody.rectTransform.rect.height+.5f;

        public P0UsabilityHud(M4Session life,M2Hud overlay,RectTransform status,Camera view)
        {
            session=life;hud=overlay;trade=life.Trade;camera=view;canvas=(RectTransform)hud.CanvasRoot;
            playerSprites=session.Game.Player.GetComponentsInChildren<SpriteRenderer>();
            saveStatus=hud.Label(status,"",15,14,68,338,25,new Color(.75f,.95f,.81f));
            ControlsButton=Button(status,"操作 [F1]",new Vector2(365,-67),new Vector2(100,27),ToggleControls);
            controls=M2Hud.Panel(canvas,"P0 optional controls",new Vector2(0,1),new Vector2(0,1),new Vector2(18,-218),new Vector2(485,330));
            hud.Label(controls,"操作与街区 · 随时可以关闭",20,15,10,455,30,new Color(1,.88f,.62f));
            HelpText="WASD/方向键移动；E近距离交互。\nTab/I背包；1—4选择；X丢弃一件。\nEnter确认；Esc返回当前界面。\nL家书；J线索；F5保存；F9读取。\n\nA 小巷/免费休息；B 巷口；D 主街\nC 林婶早餐；G 陈伯收购\nF 废弃物搜索；E 旧货角落（只观察）\n可先探索任何区域，不必按顺序完成。\n钱不够可问林婶一次帮工；当天资源耗尽，\n回A原铺盖免费休息后继续拾荒。";
            controlsBody=hud.Label(controls,HelpText,15,15,49,455,229);controlsBody.alignment=TextAnchor.UpperLeft;controlsBody.horizontalOverflow=HorizontalWrapMode.Wrap;controlsBody.verticalOverflow=VerticalWrapMode.Truncate;
            Button(controls,"返回探索 [F1 / Esc]",new Vector2(15,-286),new Vector2(455,29),HideControls);
            controls.gameObject.SetActive(false);
            hint=M2Hud.Panel(canvas,"P0 nearby interaction",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(300,32));
            hint.GetComponent<Image>().raycastTarget=false;
            hintLabel=hud.Label(hint,"",16,8,2,284,28,new Color(1,.90f,.65f));hintLabel.alignment=TextAnchor.MiddleCenter;
            hint.gameObject.SetActive(false);
            trade.Changed+=ContextChanged;
            Canvas.willRenderCanvases+=Tick;
        }
        public void ToggleControls()
        {
            bool open=!ControlsOpen;
            if(open){session.Close();trade.Close();hud.SetBagOpen(false);}
            controls.gameObject.SetActive(open);
        }
        public void HideControls()=>controls.gameObject.SetActive(false);
        public void ContextChanged(){if(session.IsOpen||trade.IsOpen||hud.BagOpen)HideControls();}
        public bool TryTarget(out Vector2 position,out string label)
        {
            M2Scavenging game=session.Game;M2Pickup pickup=game.Nearest();
            if(pickup!=null&&game.Available(pickup))
            {position=pickup.transform.position;label="E  "+M2Settings.Name(pickup.Item)+(pickup.IsSearch?" · 搜索":" · 拾取");return true;}
            string id=session.NearbyPointId;
            if(id!=null)
            {
                position=session.InteractionPosition(id);
                label="E  "+(id=="A-01"?"原铺盖 · 免费休息":id=="A-02"?"旧报纸":id=="A-03"?"积水":id=="B-01"?"巷口公告":id=="C-01"?"林婶早餐摊":id=="C-04"?"整理摊具":id=="E-01"?"旧搪瓷水壶 · 观察":"旧货居民 · 交谈");return true;
            }
            if(trade.OffersInteraction){position=game.World.RegionEntry("G");label="E  陈伯 · 称重收购";return true;}
            if(pickup!=null){position=pickup.transform.position;label="本轮已搜索 · 休息后再来";return true;}
            position=Vector2.zero;label="";return false;
        }
        public void Tick()
        {
            if(saveStatus==null)return;
            ContextChanged();
            string status=session.SaveStatusText;if(saveStatus.text!=status)saveStatus.text=status;
            if(session.IsOpen||trade.IsOpen||hud.BagOpen||ControlsOpen||!TryTarget(out Vector2 position,out string label))
            {hint.gameObject.SetActive(false);return;}
            Vector3 screen=camera.WorldToScreenPoint(position+Vector2.up*1.05f);
            float playerTop=session.Game.Player.Body.position.y;
            foreach(SpriteRenderer sprite in playerSprites)if(sprite!=null&&sprite.enabled)playerTop=Mathf.Max(playerTop,sprite.bounds.max.y);
            Vector3 playerScreen=camera.WorldToScreenPoint(new Vector3(session.Game.Player.Body.position.x,playerTop,0));
            if(screen.z<=0){hint.gameObject.SetActive(false);return;}
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,camera,out Vector2 local);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,playerScreen,camera,out Vector2 playerLocal);
            Rect bounds=canvas.rect;
            local.x=Mathf.Clamp(local.x,bounds.xMin+166,bounds.xMax-166);
            local.y=Mathf.Clamp(Mathf.Max(local.y,playerLocal.y+26),bounds.yMin+196,bounds.yMax-234);
            if((hint.anchoredPosition-local).sqrMagnitude>.01f)hint.anchoredPosition=local;
            if(hintLabel.text!=label)hintLabel.text=label;
            hint.gameObject.SetActive(true);
        }
        private Button Button(Transform parent,string label,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            RectTransform rect=M2Hud.Rect(parent,label,new Vector2(0,1),new Vector2(0,1),position,size);
            Image image=rect.gameObject.AddComponent<Image>();image.color=new Color(.29f,.38f,.42f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);
            hud.Label(rect,label,14,3,1,size.x-6,size.y-2).alignment=TextAnchor.MiddleCenter;return button;
        }
        public void Dispose()
        {
            Canvas.willRenderCanvases-=Tick;trade.Changed-=ContextChanged;
            if(controls!=null)UnityEngine.Object.Destroy(controls.gameObject);if(hint!=null)UnityEngine.Object.Destroy(hint.gameObject);
        }
    }
}
