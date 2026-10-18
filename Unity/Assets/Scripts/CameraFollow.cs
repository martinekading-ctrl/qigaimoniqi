using UnityEngine;

namespace Reborn1985.M1
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private M1Settings settings;
        [SerializeField] private Rect worldBounds;
        private Vector3 velocity;
        private Camera view;
        private Camera View => view != null ? view : (view = GetComponent<Camera>());

        private void Awake() { view = GetComponent<Camera>(); }

        public void Configure(Transform player, M1Settings data, Rect bounds)
        {
            target = player; settings = data; worldBounds = bounds;
            View.orthographicSize = data.CameraSize;
            Snap();
        }

        public void Snap()
        {
            if (target == null) return;
            transform.position = Clamp(new Vector3(target.position.x, target.position.y, -10f));
            velocity = Vector3.zero;
        }

        private Vector3 Clamp(Vector3 point)
        {
            float halfHeight = View.orthographicSize, halfWidth = halfHeight * View.aspect;
            point.x = worldBounds.width <= halfWidth * 2 ? worldBounds.center.x
                : Mathf.Clamp(point.x, worldBounds.xMin + halfWidth, worldBounds.xMax - halfWidth);
            point.y = worldBounds.height <= halfHeight * 2 ? worldBounds.center.y
                : Mathf.Clamp(point.y, worldBounds.yMin + halfHeight, worldBounds.yMax - halfHeight);
            return point;
        }

        private void LateUpdate()
        {
            if (target == null || settings == null) return;
            Vector3 desired = Clamp(new Vector3(target.position.x, target.position.y, -10f));
            if ((desired - transform.position).sqrMagnitude < .0025f) return;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity,
                settings.CameraSmoothTime, Mathf.Infinity, Mathf.Max(Time.deltaTime, .001f));
        }
    }
}
