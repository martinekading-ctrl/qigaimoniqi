using System.Collections;
using Reborn1985.M2;
using UnityEngine;
using UnityEngine.UI;

namespace Reborn1985.M3
{
    public sealed class M3Hud : MonoBehaviour
    {
        [SerializeField] private M3Trade trade;
        [SerializeField] private M2Scavenging game;
        [SerializeField] private M2Hud hud;
        private GameObject panel;
        private Text cash, title, description, total, balance, buttonLabel, closeLabel, instructions;
        private readonly Text[] lines = new Text[3];
        public Button ConfirmButton { get; private set; }
        public string CashText => cash != null ? cash.text : "";
        public bool Ready => panel != null;
        public void Configure(M3Trade service, M2Scavenging inventory, M2Hud overlay) { trade = service; game = inventory; hud = overlay; }
        private IEnumerator Start()
        {
            while (hud.CanvasRoot == null) yield return null;
            trade.Initialize();
            RectTransform chip = M2Hud.Panel(hud.CanvasRoot,"M3 Cash",new Vector2(0,1),new Vector2(0,1),new Vector2(515,-18),new Vector2(250,78));
            cash = hud.Label(chip,"",22,14,10,224,30,new Color(1,.88f,.62f));
            hud.Label(chip,"按分结算 · 单次运行",14,14,47,224,24);
            RectTransform window = M2Hud.Panel(hud.CanvasRoot,"M3 Chenbo Weighing",Vector2.one,Vector2.one,new Vector2(-18,-106),new Vector2(520,446));
            panel = window.gameObject;
            title = hud.Label(window,"",22,18,12,484,32,new Color(1,.88f,.62f));
            description = hud.Label(window,"",15,18,50,484,45);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            for (int i=0; i<3; i++) lines[i] = hud.Label(window,"",17,18,104+i*55,484,49);
            total = hud.Label(window,"",22,18,277,484,30,new Color(.75f,.95f,.81f));
            balance = hud.Label(window,"",17,18,313,484,26);
            hud.Label(window,"仅出售背包内全部废品；母亲来信受保护，不上秤。",14,18,344,484,26);
            ConfirmButton = MakeButton(window,"出售全部废品",new Vector2(18,-377),new Vector2(245,34),() => trade.Confirm());
            buttonLabel = ConfirmButton.GetComponentInChildren<Text>();
            closeLabel = MakeButton(window,"取消",new Vector2(279,-377),new Vector2(223,34),() => trade.Close()).GetComponentInChildren<Text>();
            instructions = hud.Label(window,"",14,18,416,484,25);
            trade.Changed += Refresh;
            game.Changed += Refresh;
            hud.ContextPrompt = trade.ContextPrompt;
            Refresh();
        }
        private void Update()
        {
            if (!Ready || !trade.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { trade.Close(); return; }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) trade.Confirm();
        }
        private void Refresh()
        {
            if (!Ready) return;
            cash.text = "现金 " + M3TradeSettings.Yuan(game.State.CashCents);
            panel.SetActive(trade.IsOpen);
            hud.InventoryInputBlocked = trade.IsOpen;
            if (trade.IsOpen) hud.SetBagOpen(false);
            SaleQuote quote = trade.Pending;
            SaleReceipt receipt = trade.DisplayedReceipt;
            bool completed = receipt != null;
            bool invalid = quote != null && quote.Status != SaleResult.Success && quote.Status != SaleResult.Empty;
            title.text = completed ? "已成交 · 陈伯废品收购" : "陈伯废品收购 · 称重确认";
            description.text = completed ? "钱货已结清；重复按确认不会再次领取现金。" : invalid ? "测试参数无法精确结算到分，禁止成交；请核对价格和重量。" : quote != null && quote.Status == SaleResult.Empty ? "陈伯：收废纸、玻璃瓶和废金属。先找些弃置物，有货再上秤。" : "陈伯：按重量收。核对下面的数量与价钱，再决定是否出售。";
            for (int i=0; i<3; i++)
            {
                ItemKind kind = (ItemKind)i;
                int count = completed ? receipt.quantities[i] : quote != null ? quote.Lines[i].Quantity : 0;
                int grams = completed ? receipt.grams[i] : quote != null ? quote.Lines[i].Grams : 0;
                int rate = completed ? receipt.centsPerKg[i] : trade.Settings.CentsPerKg(kind);
                long cents = completed ? receipt.lineCents[i] : quote != null ? quote.Lines[i].Cents : 0;
                lines[i].text = M2Settings.Name(kind) + " ×" + count + "    " + M2Settings.Kg(grams) + "\n" + M3TradeSettings.Yuan(rate) + "/kg    分类金额 " + (invalid ? "待确认" : M3TradeSettings.Yuan(cents));
            }
            int weight = completed ? receipt.totalGrams : quote?.TotalGrams ?? 0;
            long income = completed ? receipt.incomeCents : quote?.TotalCents ?? 0;
            long before = completed ? receipt.cashBefore : game.State.CashCents;
            total.text = invalid ? "未生成有效报价，本次不可成交" : "合计 " + M2Settings.Kg(weight) + "    " + (completed ? "收入 " : "将收入 ") + M3TradeSettings.Yuan(income);
            balance.text = invalid ? "现金保持 " + M3TradeSettings.Yuan(before) : "现金 " + M3TradeSettings.Yuan(before) + " → " + M3TradeSettings.Yuan(completed ? receipt.cashAfter : before + income);
            ConfirmButton.interactable = !completed && quote != null && quote.Status == SaleResult.Success;
            buttonLabel.text = completed ? "已结算" : "出售全部废品 [Enter]";
            closeLabel.text = completed ? "关闭 [Esc]" : "取消 [Esc]";
            instructions.text = completed ? "本次交易已完成 · Esc 关闭后可查看背包" : "Enter 确认 · Esc 取消后可开背包 · 离开秤前自动关闭";
        }
        private Button MakeButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = M2Hud.Rect(parent,label,new Vector2(0,1),new Vector2(0,1),position,size);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.29f,.38f,.42f);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None }; button.onClick.AddListener(action);
            hud.Label(rect,label,16,6,3,size.x-12,size.y-6).alignment = TextAnchor.MiddleCenter;
            return button;
        }
        private void OnDestroy()
        {
            if (trade != null) trade.Changed -= Refresh;
            if (game != null) game.Changed -= Refresh;
            if (hud != null) { hud.InventoryInputBlocked = false; if (trade != null && hud.ContextPrompt == trade.ContextPrompt) hud.ContextPrompt = null; }
        }
    }
}
