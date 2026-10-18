using System;
using System.Collections.Generic;
using System.Linq;
using Reborn1985.M1;
using UnityEngine;

namespace Reborn1985.M2
{
    public enum InteractionResult { Success, Unavailable, OutOfReach, Overweight, Protected, NoRoom }

    public sealed class InteractionEdge
    {
        private bool previous;
        public bool Press(bool held) { bool down = held && !previous; previous = held; return down; }
    }

    public sealed class M2Scavenging : MonoBehaviour
    {
        [SerializeField] private M1World world;
        [SerializeField] private PlayerMover player;
        [SerializeField] private M2Settings settings;
        [SerializeField] private Sprite dropSprite;
        private readonly List<M2Pickup> targets = new List<M2Pickup>();
        private readonly InteractionEdge interaction = new InteractionEdge();
        private bool? verificationHeld;
        private Transform dropRoot;
        public M1World World => world;
        public PlayerMover Player => player;
        public M2Settings Settings => settings;
        public M2RunState State { get; private set; }
        public IReadOnlyList<M2Pickup> Targets => targets;
        public string Feedback { get; private set; } = "靠近黄色废纸或绿色玻璃瓶，按 E 拾取。";
        public bool FeedbackIsError { get; private set; }
        public float FeedbackUntil { get; private set; }
        public event Action Changed;
        public event Action FeedbackChanged;
        public Func<bool> ContextInteraction { get; set; }
        public Func<bool> LifeInteraction { get; set; }
        internal void NotifyStateChanged() => Changed?.Invoke();
        internal void ShowFeedback(string message, bool error = false) => Report(error ? InteractionResult.Unavailable : InteractionResult.Success, message);

        public void Configure(M1World map, PlayerMover actor, M2Settings parameters, Sprite marker)
        { world = map; player = actor; settings = parameters; dropSprite = marker; }
        private void Start() => Initialize();
        public void Initialize()
        {
            if (State != null) return;
            State = new M2RunState(settings);
            foreach (GreyboxPoint point in world.Points)
                if (point.TryGetComponent(out M2Pickup pickup)) targets.Add(pickup);
            dropRoot = new GameObject("M2 dropped items - no blocking colliders").transform;
            dropRoot.SetParent(world.transform, false);
            RefreshSources();
        }
        private void Update()
        {
            if (State == null) return;
            if (interaction.Press(verificationHeld ?? Input.GetKey(KeyCode.E)))
            {
                if (LifeInteraction?.Invoke() == true) return;
                if (ContextInteraction?.Invoke() == true) return;
                M2Pickup target = Nearest();
                if (target != null) TryInteract(target);
            }
        }
        public void SetVerificationInteraction(bool? held) => verificationHeld = held;
        public bool Available(M2Pickup target) => target != null && target.isActiveAndEnabled &&
            (target.IsDropped ? State.HasDrop(target.Id) : !State.Claimed(target.Id));
        public bool InReach(Vector2 position)
        {
            Vector2 origin = player.Body.position;
            Vector2 delta = position - origin;
            return delta.magnitude <= settings.InteractionRadius &&
                !Physics2D.CircleCast(origin, .025f, delta.normalized, delta.magnitude, M1World.SolidMask);
        }
        public M2Pickup Nearest()
        {
            M2Pickup nearest = null;
            float best = float.PositiveInfinity;
            foreach (M2Pickup target in targets)
            {
                if (target == null || (!Available(target) && !target.IsSearch) || !InReach(target.transform.position)) continue;
                // Available resources take priority over an already searched pile.
                float distance = Vector2.Distance(player.Body.position, target.transform.position) + (Available(target) ? 0 : 2);
                if (distance < best) { best = distance; nearest = target; }
            }
            return nearest;
        }
        public string Prompt(M2Pickup target)
        {
            if (target == null) return "沿主街自由探索；只有明确弃置物和指定搜索点可取。";
            if (!Available(target)) return target.Id + " · 本轮已搜索，休息后按有限周期刷新";
            string id = target.IsDropped ? "地面丢弃物" : target.Id;
            return "[E] " + (target.IsSearch ? "搜索" : "拾取") + " · " + id + " · " + M2Settings.Name(target.Item) + " ×1 / " + M2Settings.Kg(settings.Weight(target.Item));
        }
        public InteractionResult TryInteract(M2Pickup target)
        {
            if (target == null || !targets.Contains(target) || !Available(target)) return Report(InteractionResult.Unavailable, "这里已经取过了，没有额外产出。");
            if (!InReach(target.transform.position)) return Report(InteractionResult.OutOfReach, "距离太远或有障碍，不能拾取。");
            if (!State.CanCarry(target.Item))
                return Report(InteractionResult.Overweight, "超重：需要 " + M2Settings.Kg(State.TotalGrams + settings.Weight(target.Item)) + "，上限 " + M2Settings.Kg(settings.CapacityGrams) + "。物品留在原地。");
            bool success = target.IsDropped ? State.TryCollectDrop(target.Id) : State.TryCollectSource(target.Id, target.Item);
            if (!success) return Report(InteractionResult.Unavailable, "物品状态已变化，请重新查看。");
            string action = target.IsSearch ? "搜索获得" : "已拾取";
            if (target.IsDropped)
            {
                targets.Remove(target);
                target.gameObject.SetActive(false);
                Destroy(target.gameObject);
            }
            else target.ShowState(false);
            Changed?.Invoke();
            return Report(InteractionResult.Success, action + " " + M2Settings.Name(target.Item) + " ×1；当前 " + M2Settings.Kg(State.TotalGrams) + "。");
        }
        public InteractionResult TryDrop(ItemKind kind)
        {
            if (kind == ItemKind.Letter) return Report(InteractionResult.Protected, "母亲来信是受保护剧情物品，不能丢弃。");
            if (State.Count(kind) == 0) return Report(InteractionResult.Unavailable, "没有这种物品可以丢弃。");
            if (!FindDropPosition(out Vector2 position)) return Report(InteractionResult.NoRoom, "附近没有安全落点，请走到更宽敞的地面。");
            var obj = new GameObject("Dropped " + M2Settings.Name(kind), typeof(SpriteRenderer), typeof(M2Pickup));
            obj.SetActive(false);
            obj.transform.SetParent(dropRoot, false);
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * .65f;
            obj.GetComponent<SpriteRenderer>().sprite = dropSprite;
            if (!State.TryDropOne(kind, position, out GroundDrop drop))
            { Destroy(obj); return Report(InteractionResult.Unavailable, "物品状态已变化，没有扣除物品。"); }
            M2Pickup pickup = obj.GetComponent<M2Pickup>();
            pickup.Configure(drop.unit.unitId, kind, false, true);
            pickup.ShowState(true);
            targets.Add(pickup);
            obj.SetActive(true);
            Changed?.Invoke();
            return Report(InteractionResult.Success, "已放下 " + M2Settings.Name(kind) + " ×1，可在附近重新拾取。");
        }
        public bool FindDropPosition(out Vector2 position)
        {
            Physics2D.SyncTransforms();
            Vector2 start = player.Body.position;
            float baseAngle = Mathf.Atan2(player.Facing.y, player.Facing.x);
            float clearance = world.Settings.FootRadius + .025f;
            for (int ring = 0; ring < 3; ring++)
            {
                float radius = Mathf.Lerp(.32f, settings.DropRadius, ring / 2f);
                for (int i = 0; i < 16; i++)
                {
                    float angle = baseAngle + i * Mathf.PI / 8;
                    Vector2 candidate = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    Vector3Int cell = world.Ground.WorldToCell(candidate);
                    if (!world.Ground.HasTile(cell) || world.Obstacles.HasTile(cell)) continue;
                    if (Physics2D.OverlapCircle(candidate, clearance, M1World.SolidMask) ||
                        Physics2D.CircleCast(start, clearance, candidate - start, radius, M1World.SolidMask)) continue;
                    if (targets.Any(target => target != null && target.IsDropped && Available(target) && Vector2.Distance(target.transform.position, candidate) < .18f)) continue;
                    position = candidate;
                    return true;
                }
            }
            position = default;
            return false;
        }
        private void RefreshSources()
        { foreach (M2Pickup target in targets) target.ShowState(Available(target)); }
        internal bool RestToNextDay()
        {
            if(!State.Rest()) return false;
            foreach(M2Pickup old in targets.Where(target=>target.IsDropped).ToArray())
            { old.gameObject.SetActive(false); targets.Remove(old); Destroy(old.gameObject); }
            RefreshSources(); Changed?.Invoke(); return true;
        }
        internal void ReplaceRunState(M2RunState replacement)
        {
            var staged = new List<M2Pickup>();
            try
            {
                foreach (GroundDrop drop in replacement.Snapshot().groundDrops)
                {
                    var obj = new GameObject("Restored " + M2Settings.Name(drop.unit.kind),typeof(SpriteRenderer),typeof(M2Pickup));
                    obj.SetActive(false); obj.transform.SetParent(dropRoot,false);
                    obj.transform.position=drop.position; obj.transform.localScale=Vector3.one*.65f;
                    obj.GetComponent<SpriteRenderer>().sprite=dropSprite;
                    M2Pickup pickup=obj.GetComponent<M2Pickup>(); pickup.Configure(drop.unit.unitId,drop.unit.kind,false,true); staged.Add(pickup);
                }
            }
            catch { foreach (M2Pickup pickup in staged) Destroy(pickup.gameObject); throw; }
            foreach (M2Pickup old in targets.Where(target=>target.IsDropped).ToArray())
            { old.gameObject.SetActive(false); targets.Remove(old); Destroy(old.gameObject); }
            State=replacement;
            foreach (M2Pickup pickup in staged) { targets.Add(pickup); pickup.gameObject.SetActive(true); }
            RefreshSources(); Changed?.Invoke();
        }
        private InteractionResult Report(InteractionResult result, string message)
        {
            Feedback = message;
            FeedbackIsError = result != InteractionResult.Success;
            FeedbackUntil = Time.unscaledTime + 4;
            FeedbackChanged?.Invoke();
            return result;
        }
    }
}
