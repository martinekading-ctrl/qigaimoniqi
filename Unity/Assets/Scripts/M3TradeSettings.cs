using Reborn1985.M2;
using UnityEngine;

namespace Reborn1985.M3
{
    [CreateAssetMenu(menuName = "Reborn 1985/M3 test prices")]
    public sealed class M3TradeSettings : ScriptableObject
    {
        [SerializeField, Min(1)] private int paperCentsPerKg = 12;
        [SerializeField, Min(1)] private int bottleCentsPerKg = 20;
        [SerializeField, Min(1)] private int metalCentsPerKg = 16;
        [SerializeField, Range(.3f, 1.2f)] private float counterRange = .8f;
        public float CounterRange => counterRange;
        public int CentsPerKg(ItemKind kind) => kind == ItemKind.Paper ? paperCentsPerKg : kind == ItemKind.Bottle ? bottleCentsPerKg : kind == ItemKind.Metal ? metalCentsPerKg : 0;
        public static string Yuan(long cents) => (cents / 100m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " 元";
    }
}
