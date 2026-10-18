using UnityEngine;

namespace Reborn1985.M1
{
    public enum PointRole { FunctionalPlaceholder, VisualGuide, Decoration, Passage, StandingSpace }

    public sealed class GreyboxPoint : MonoBehaviour
    {
        [SerializeField] private string pointId;
        [SerializeField] private string description;
        [SerializeField] private PointRole role;
        public string Id => pointId;
        public string Description => description;
        public PointRole Role => role;

        public void Configure(string id, string label, PointRole pointRole)
        {
            pointId = id; description = label; role = pointRole;
        }
    }
}
