using UnityEngine;

namespace Reborn1985.M1
{
    public sealed class GreyboxOverlay : MonoBehaviour
    {
        [SerializeField] private M1World world;
        [SerializeField] private PlayerMover player;
        [SerializeField] private Camera view;
        [SerializeField] private bool showScreenOverlay = true;
        private GUIStyle title, body, label, small;
        private Font font;
        public void Configure(M1World map, PlayerMover actor, Camera camera) { world = map; player = actor; view = camera; }
        public void SetScreenOverlay(bool visible) => showScreenOverlay = visible;

        private void Start()
        {
            EnsureFont();
            foreach (RegionDefinition zone in world.Settings.Regions)
            {
                var obj = new GameObject(zone.Id + " region label", typeof(TextMesh));
                obj.transform.SetParent(world.transform);
                obj.transform.position = world.Center(zone.Entry) + Vector2.up * .8f;
                TextMesh text = obj.GetComponent<TextMesh>();
                text.font = font; text.text = zone.Id + " · " + zone.Title;
                text.fontSize = 48; text.characterSize = .065f;
                text.anchor = TextAnchor.MiddleCenter;
                text.color = new Color(.12f, .18f, .22f);
                var renderer = obj.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = font.material; renderer.sortingOrder = 100;
            }
            foreach (GreyboxPoint point in world.Points)
            {
                string name = point.Id == "C-01" ? "林婶" : point.Id == "G-03" ? "陈伯" : point.Id == "E-02" ? "旧货居民" : null;
                if (name == null) continue;
                var obj = new GameObject(name + " NPC label", typeof(TextMesh));
                obj.transform.SetParent(world.transform);
                obj.transform.position = point.transform.position + Vector3.up * .85f;
                TextMesh text = obj.GetComponent<TextMesh>();
                text.font = font; text.text = name; text.fontSize = 48; text.characterSize = .05f;
                text.anchor = TextAnchor.MiddleCenter; text.color = new Color(.12f,.18f,.22f);
                var renderer = obj.GetComponent<MeshRenderer>(); renderer.sharedMaterial = font.material; renderer.sortingOrder = 100;
            }
        }

        private void EnsureFont()
        {
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 48);
        }

        private void OnDestroy() { if (font != null) Destroy(font); }

        private void InitStyles()
        {
            EnsureFont();
            title = new GUIStyle(GUI.skin.label) { font = font, fontSize = 23, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16 };
            small = new GUIStyle(body) { fontSize = 13 };
            label = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(.96f, .87f, .66f);
            body.normal.textColor = Color.white;
            label.normal.textColor = Color.white;
            small.normal.textColor = new Color(.8f, .83f, .85f);
        }

        private void OnGUI()
        {
            if (!showScreenOverlay) return;
            if (world == null || player == null || view == null) return;
            if (title == null) InitStyles();
            float scale = Mathf.Max(.65f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.Box(new Rect(18, 14, 590, 88), GUIContent.none);
            GUI.Label(new Rect(34, 22, 560, 32), "重生80年代 · M1 街区灰盒", title);
            GUI.Label(new Rect(34, 58, 560, 28), world.RegionAt(player.transform.position) + "  /  仅移动、镜头、碰撞与位置占位", body);
            GUI.Box(new Rect(width - 218, 14, 200, 230), GUIContent.none);
            GUI.Label(new Rect(width - 202, 24, 180, 28), "A—G 连续街区", body);
            for (int i = 0; i < world.Settings.Regions.Length; i++)
            {
                RegionDefinition zone = world.Settings.Regions[i];
                GUI.Label(new Rect(width - 202, 58 + i * 24, 184, 25), zone.Id + "  " + zone.Title, small);
            }

            foreach (GreyboxPoint point in world.Points)
                if (Vector2.Distance(player.transform.position, point.transform.position) < 2.5f)
                    WorldLabel((Vector2)point.transform.position + Vector2.up * .32f, point.Id, small, scale, 76, 20);

            GUI.Box(new Rect(18, height - 86, width - 36, 68), GUIContent.none);
            GUI.Label(new Rect(34, height - 80, width - 68, 26), "WASD / 方向键：移动  ·  黄色箭头：朝向  ·  沿主街自由往返七区", body);
            GreyboxPoint nearest = null;
            float distance = 1.15f;
            foreach (GreyboxPoint point in world.Points)
            {
                float next = Vector2.Distance(point.transform.position, player.transform.position);
                if (next < distance) { nearest = point; distance = next; }
            }
            string detail = nearest == null ? "灰色体块为不可通行建筑/障碍；所有尺寸为可调原型参数。"
                : nearest.Id + " · " + nearest.Description + "（M1仅位置占位，无拾取/交易/剧情功能）";
            GUI.Label(new Rect(34, height - 51, width - 68, 25), detail, small);
            GUI.matrix = Matrix4x4.identity;
        }

        private void WorldLabel(Vector2 position, string text, GUIStyle style, float scale, float width, float height)
        {
            Vector3 viewport = view.WorldToViewportPoint(position);
            if (viewport.z < 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < .14f || viewport.y > .82f) return;
            Vector2 screen = new Vector2(viewport.x * Screen.width / scale, (1 - viewport.y) * Screen.height / scale);
            Rect rect = new Rect(screen.x - width / 2, screen.y - height / 2, width, height);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, text, style);
        }
    }
}
