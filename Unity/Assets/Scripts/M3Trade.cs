using System;
using System.Linq;
using Reborn1985.M1;
using Reborn1985.M2;
using UnityEngine;

namespace Reborn1985.M3
{
    public sealed class M3Trade : MonoBehaviour
    {
        [SerializeField] private M2Scavenging game;
        [SerializeField] private M3TradeSettings settings;
        private GreyboxPoint counter;
        private bool initialized;
        public M3TradeSettings Settings => settings;
        public bool IsOpen { get; private set; }
        public SaleQuote Pending { get; private set; }
        public SaleReceipt DisplayedReceipt { get; private set; }
        public event Action Changed;
        public void Configure(M2Scavenging service, M3TradeSettings prices) { game = service; settings = prices; }
        private void Start() => Initialize();
        public void Initialize()
        {
            if (initialized) return;
            game.Initialize();
            counter = game.World.Points.Single(point => point.Id == "G-04");
            game.ContextInteraction = HandleInteraction;
            initialized = true;
        }
        public bool InRange
        {
            get
            {
                if (counter == null) return false;
                Vector2 delta = (Vector2)counter.transform.position - game.Player.Body.position;
                return delta.magnitude <= settings.CounterRange && !Physics2D.CircleCast(game.Player.Body.position,.025f,delta.normalized,delta.magnitude,M1World.SolidMask);
            }
        }
        public bool OffersInteraction
        {
            get { M2Pickup pickup = game.Nearest(); return InRange && (pickup == null || !game.Available(pickup)); }
        }
        public string ContextPrompt() => IsOpen ? DisplayedReceipt != null ? "本次已结算，钱货结果已保留 · Esc 关闭" : Pending != null && Pending.Status == SaleResult.Empty ? "暂无废品可售 · Esc 返回后继续拾荒" : "称重确认：Enter 出售全部废品 · Esc 取消 · 移开自动取消" : OffersInteraction ? "[E] 陈伯废品收购 · G-04 秤前称重 / 询价" : null;
        private bool HandleInteraction()
        {
            if (IsOpen) return true; // E only opens; it never confirms a sale.
            if (!OffersInteraction) return false;
            Open(); return true;
        }
        public SaleResult Open()
        {
            if (!InRange) { game.ShowFeedback("请走到陈伯收购站的 G-04 秤前站位。",true); return SaleResult.OutOfReach; }
            Pending = game.State.QuoteAllScrap(settings); DisplayedReceipt = null; IsOpen = true; Changed?.Invoke();
            return Pending.Status;
        }
        public SaleResult Confirm()
        {
            if (!IsOpen || Pending == null) return SaleResult.NoPending;
            if (!InRange) { Close(); return SaleResult.OutOfReach; }
            SaleResult result = game.State.TrySell(Pending,settings,out SaleReceipt receipt);
            if (result == SaleResult.Success)
            {
                Pending = null; DisplayedReceipt = receipt;
                game.NotifyStateChanged();
                game.ShowFeedback("已出售废品 " + M2Settings.Kg(receipt.totalGrams) + "，收入 " + M3TradeSettings.Yuan(receipt.incomeCents) + "；现金 " + M3TradeSettings.Yuan(receipt.cashAfter) + "。");
            }
            else if (result == SaleResult.Empty) game.ShowFeedback("陈伯：先找些弃置废纸、玻璃瓶或废金属，有货再上秤。",true);
            else
            {
                Pending = null; IsOpen = false;
                game.ShowFeedback(result == SaleResult.StaleQuote ? "背包或报价已变化，本次未成交，请重新称重确认。" : "测试参数无法精确结算到分，本次未成交，需核对参数。",true);
            }
            Changed?.Invoke();
            return result;
        }
        public void Close()
        {
            Pending = null; DisplayedReceipt = null; IsOpen = false; Changed?.Invoke();
        }
        private void Update()
        {
            if (IsOpen && !InRange)
            {
                bool completed = DisplayedReceipt != null;
                Close();
                game.ShowFeedback(completed ? "已离开收购站，结算结果已保留。" : "已离开秤前，取消本次交易；物品和现金未变化。");
            }
        }
        private void OnDisable()
        { if (IsOpen) Close(); if (game != null && game.ContextInteraction == HandleInteraction) game.ContextInteraction = null; }
        private void OnEnable() { if (initialized) game.ContextInteraction = HandleInteraction; }
    }
}
