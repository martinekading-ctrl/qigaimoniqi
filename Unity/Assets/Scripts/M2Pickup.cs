using UnityEngine;

namespace Reborn1985.M2
{
    // The seven original components stay on their M1 objects and keep their stable IDs.
    public sealed class M2Pickup : MonoBehaviour
    {
        [SerializeField] private string sourceId;
        [SerializeField] private ItemKind item;
        [SerializeField] private bool search;
        [SerializeField] private bool dropped;
        private SpriteRenderer visual;
        public string Id => sourceId;
        public ItemKind Item => item;
        public bool IsSearch => search;
        public bool IsDropped => dropped;
        public SpriteRenderer Visual => visual != null ? visual : (visual = GetComponent<SpriteRenderer>());
        public void Configure(string id, ItemKind kind, bool searchPoint, bool groundDrop = false)
        { sourceId = id; item = kind; search = searchPoint; dropped = groundDrop; }
        public void ShowState(bool available)
        {
            if (Visual == null) return;
            Visual.enabled = available || search;
            Visual.color = search && !available ? new Color(.36f, .38f, .39f) : Tint(item);
        }
        public static Color Tint(ItemKind kind) => kind == ItemKind.Paper ? new Color(.95f, .86f, .55f)
            : kind == ItemKind.Bottle ? new Color(.34f, .84f, .75f) : new Color(.61f, .71f, .83f);
    }
}
