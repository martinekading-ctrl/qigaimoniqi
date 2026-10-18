using System;
using Reborn1985.M1;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Reborn1985.M2
{
    // Camera-space Canvas is also captured by the Windows development verifier.
    public sealed class M2Hud : MonoBehaviour
    {
        [SerializeField] private M2Scavenging game;
        [SerializeField] private Camera view;
        [SerializeField] private string stageTitle = "重生80年代 · M2 拾荒原型";
        public bool InventoryInputBlocked { get; set; }
        public bool LifeInputBlocked { get; set; }
        public Func<string> ContextPrompt { get; set; }
        public Func<string> LifeContextPrompt { get; set; }
        private Action readLetter;
        public Transform CanvasRoot => canvasObject != null ? canvasObject.transform : null;
        public void SetStageTitle(string title) => stageTitle = title;
        private Font font;
        private GameObject canvasObject, bag;
        private Text region, weight, prompt, feedback, summary;
        private readonly Text[] rows = new Text[4];
        private readonly Button[] drops = new Button[4];
        private readonly Image[] rowBackgrounds = new Image[4];
        private int selected;
        public bool BagOpen => bag != null && bag.activeSelf;
        public string WeightText => weight != null ? weight.text : "";
        public string FeedbackText => feedback != null ? feedback.text : "";
        public void Configure(M2Scavenging service, Camera camera) { game = service; view = camera; }

        private void Start()
        {
            game.Initialize();
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 32);
            canvasObject = new GameObject("M2 HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = view; canvas.planeDistance = 1; canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("M2 UI Events", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(canvasObject.transform, false);
                // Arrow keys belong to movement; bag selection uses 1-4 instead.
                events.GetComponent<EventSystem>().sendNavigationEvents = false;
            }
            RectTransform title = Panel(canvasObject.transform, "Title", new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -18), new Vector2(485, 78));
            Label(title, stageTitle, 23, 14, 10, 460, 30, new Color(1, .88f, .62f));
            region = Label(title, "", 16, 14, 44, 460, 25);
            RectTransform chip = Panel(canvasObject.transform, "Weight", Vector2.one, Vector2.one, new Vector2(-18, -18), new Vector2(310, 78));
            weight = Label(chip, "", 22, 14, 9, 285, 30, new Color(.71f, .91f, .83f));
            Label(chip, "Tab / I：背包   ·   E：交互", 15, 14, 45, 285, 25);
            RectTransform bagRect = Panel(canvasObject.transform, "Backpack", Vector2.one, Vector2.one, new Vector2(-18, -106), new Vector2(520, 346));
            bag = bagRect.gameObject;
            Label(bagRect, "背包 · 每次丢弃一件", 22, 16, 10, 488, 32, new Color(1, .88f, .62f));
            Label(bagRect, "名称 / 数量       单件重量         合计", 15, 16, 47, 488, 25);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                RectTransform row = Panel(bagRect, "Item " + i, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -79 - i * 49), new Vector2(496, 45));
                rowBackgrounds[i] = row.GetComponent<Image>();
                rows[i] = Label(row, "", 16, 8, 9, 370, 28);
                RectTransform buttonRect = Rect(row, "Drop one", Vector2.one, Vector2.one, new Vector2(-5, -6), new Vector2(100, 33));
                Image image = buttonRect.gameObject.AddComponent<Image>(); image.color = new Color(.29f, .38f, .42f);
                Button button = buttonRect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                Label(buttonRect, i == 3 ? "受保护" : "丢弃 ×1", 15, 4, 3, 92, 27).alignment = TextAnchor.MiddleCenter;
                button.onClick.AddListener(() => { selected = index; if(index==3) readLetter?.Invoke(); else DropSelected(); });
                drops[i] = button;
            }
            summary = Label(bagRect, "", 17, 16, 281, 488, 25);
            Label(bagRect, "1—4 选择 · X 丢弃 · Esc 关闭；打开背包仍可移动", 14, 16, 313, 488, 25);
            RectTransform bottom = Panel(canvasObject.transform, "Interaction", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 18), new Vector2(1020, 86));
            prompt = Label(bottom, "", 18, 16, 10, 988, 28, new Color(1, .90f, .65f));
            Label(bottom, "WASD / 方向键 移动  ·  E 拾取/搜索  ·  Tab / I 背包  ·  黄色箭头为朝向", 14, 16, 49, 988, 25);
            RectTransform toast = Panel(canvasObject.transform, "Feedback", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 114), new Vector2(960, 40));
            feedback = Label(toast, "", 17, 14, 6, 932, 30);
            game.Changed += RefreshInventory;
            game.FeedbackChanged += RefreshFeedback;
            RefreshInventory(); RefreshFeedback(); SetBagOpen(false);
        }
        private void Update()
        {
            if (bag == null) return;
            if (!InventoryInputBlocked && !LifeInputBlocked && (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))) SetBagOpen(!BagOpen);
            if (!InventoryInputBlocked && !LifeInputBlocked && Input.GetKeyDown(KeyCode.Escape)) SetBagOpen(false);
            if (!InventoryInputBlocked && !LifeInputBlocked && BagOpen)
            {
                for (int i = 0; i < 4; i++)
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) { selected = i; RefreshInventory(); }
                if (Input.GetKeyDown(KeyCode.X)) DropSelected();
            }
            string nextRegion = game.World.RegionAt(game.Player.Body.position);
            if (region.text != nextRegion) region.text = nextRegion;
            string nextPrompt = LifeContextPrompt?.Invoke() ?? ContextPrompt?.Invoke() ?? game.Prompt(game.Nearest());
            if (prompt.text != nextPrompt) prompt.text = nextPrompt;
            feedback.transform.parent.gameObject.SetActive(Time.unscaledTime < game.FeedbackUntil);
        }
        public void SetBagOpen(bool open) { if (bag != null) bag.SetActive(open); }
        public void SelectItem(ItemKind kind) { selected = (int)kind; RefreshInventory(); }
        public InteractionResult DropSelected() => game.TryDrop((ItemKind)selected);
        public void SetLetterReader(Action reader) {readLetter=reader;if(drops[3]!=null){drops[3].GetComponentInChildren<Text>().text=reader==null?"受保护":"阅读";RefreshInventory();}}
        private void RefreshInventory()
        {
            if (weight == null) return;
            weight.text = "负重 " + M2Settings.Kg(game.State.TotalGrams) + " / " + M2Settings.Kg(game.Settings.CapacityGrams);
            summary.text = "总负重 " + M2Settings.Kg(game.State.TotalGrams) + " / " + M2Settings.Kg(game.Settings.CapacityGrams) + "（来信不计重）";
            for (int i = 0; i < 4; i++)
            {
                ItemKind kind = (ItemKind)i;
                int count = game.State.Count(kind), grams = game.Settings.Weight(kind);
                rows[i].text = (i + 1) + " " + M2Settings.Name(kind) + " ×" + count + "    " + M2Settings.Kg(grams) + "    " + M2Settings.Kg(count * grams);
                drops[i].interactable = i == 3 ? readLetter != null : count > 0;
                rowBackgrounds[i].color = i == selected ? new Color(.25f, .35f, .35f, .98f) : new Color(.12f, .17f, .20f, .96f);
            }
        }
        private void RefreshFeedback()
        {
            if (feedback == null) return;
            feedback.text = game.Feedback;
            feedback.color = game.FeedbackIsError ? new Color(1, .64f, .53f) : new Color(.75f, .95f, .81f);
        }
        private void OnDestroy()
        {
            if (game != null) { game.Changed -= RefreshInventory; game.FeedbackChanged -= RefreshFeedback; }
            if (font != null) Destroy(font);
        }
        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(parent, name, anchor, pivot, position, size);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.08f, .12f, .15f, .94f);
            return rect;
        }
        public Text Label(Transform parent, string value, int size, float x, float y, float width, float height, Color? color = null)
        {
            RectTransform rect = Rect(parent, value, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            Text text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size;
            text.color = color ?? new Color(.91f, .94f, .95f); text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
