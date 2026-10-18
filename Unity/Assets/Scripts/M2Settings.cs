using UnityEngine;
namespace Reborn1985.M2
{
    [CreateAssetMenu(menuName = "Reborn 1985/M2 test parameters")]
    public sealed class M2Settings : ScriptableObject
    {
        [SerializeField, Min(1)] private int capacityGrams = 5000;
        [SerializeField, Min(1)] private int paperGrams = 1000;
        [SerializeField, Min(1)] private int bottleGrams = 500;
        [SerializeField, Min(1)] private int metalGrams = 3000;
        [SerializeField, Range(.25f, 1.2f)] private float interactionRadius = .72f;
        [SerializeField, Range(.25f, 1f)] private float dropRadius = .8f;
        public int CapacityGrams => capacityGrams;
        public float InteractionRadius => interactionRadius;
        public float DropRadius => dropRadius;
        public int Weight(ItemKind kind) => kind == ItemKind.Paper ? paperGrams : kind == ItemKind.Bottle ? bottleGrams : kind == ItemKind.Metal ? metalGrams : 0;
        public static string Name(ItemKind kind) => kind == ItemKind.Paper ? "废纸" : kind == ItemKind.Bottle ? "玻璃瓶" : kind == ItemKind.Metal ? "废金属" : "母亲来信";
        public static string Kg(int grams) => (grams / 1000m).ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture) + " kg";
    }

}