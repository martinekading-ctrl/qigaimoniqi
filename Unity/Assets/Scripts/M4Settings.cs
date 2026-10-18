using UnityEngine;

namespace Reborn1985.M4
{
    [CreateAssetMenu(menuName = "Reborn 1985/M4 prototype content")]
    public sealed class M4Settings : ScriptableObject
    {
        [SerializeField, Min(1)] private int breakfastCents = 26;
        [SerializeField, Range(.3f,1f)] private float interactionRange = .72f;
        [SerializeField, TextArea(3,8)] private string letterSummary = "母亲来信 · 原型摘要\n\n家书来自广东乡下的母亲。这具身体的主人曾外出谋生，母亲仍留在乡下。\n\n原主的过往仍只有零碎印象。\n\n当前仅提供已确认背景的摘要，信件正文尚待确认，不将摘要当作终稿。";
        public int BreakfastCents => breakfastCents;
        public float InteractionRange => interactionRange;
        public string LetterSummary => letterSummary;
    }
}
