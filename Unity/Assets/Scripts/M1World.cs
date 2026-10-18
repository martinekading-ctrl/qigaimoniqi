using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Reborn1985.M1
{
    public sealed class M1World : MonoBehaviour
    {
        public const int SolidLayer = 8;
        public const int SolidMask = 1 << SolidLayer;
        [SerializeField] private M1Settings settings;
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap ground;
        [SerializeField] private Tilemap obstacles;
        [SerializeField] private GreyboxPoint[] points;
        public M1Settings Settings => settings;
        public Tilemap Ground => ground;
        public Tilemap Obstacles => obstacles;
        public GreyboxPoint[] Points => points;
        public Grid Grid => grid;
        public Vector2 Center(Vector2Int cell) => ground.GetCellCenterWorld((Vector3Int)cell);
        public Vector2 RegionEntry(string id) => Center(settings.Region(id).Entry);

        public void Configure(M1Settings data, Grid worldGrid, Tilemap floor, Tilemap solids, GreyboxPoint[] markers)
        {
            settings = data; grid = worldGrid; ground = floor; obstacles = solids; points = markers;
        }

        public string RegionAt(Vector2 position)
        {
            Vector2Int cell = (Vector2Int)ground.WorldToCell(position);
            foreach (RegionDefinition region in settings.Regions)
                if (region.Cells.Contains(cell)) return region.Id + " · " + region.Title;
            return "街区边缘";
        }

        public bool CanStand(Vector2Int cell)
        {
            return ground.HasTile((Vector3Int)cell) && !obstacles.HasTile((Vector3Int)cell)
                && !Physics2D.OverlapCircle(Center(cell), settings.FootRadius + .015f, SolidMask);
        }

        // Navigation belongs to development verification, not an auto-walk gameplay feature.
        public List<Vector2> VerificationPath(Vector2 start, Vector2 goal)
        {
            Vector2Int from = (Vector2Int)ground.WorldToCell(start);
            Vector2Int to = (Vector2Int)ground.WorldToCell(goal);
            var previous = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (queue.Count > 0 && !previous.ContainsKey(to))
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = current + direction;
                    if (previous.ContainsKey(next) || !CanStand(next)) continue;
                    Vector2 delta = Center(next) - Center(current);
                    if (Physics2D.CircleCast(Center(current), settings.FootRadius + .01f,
                            delta.normalized, delta.magnitude, SolidMask)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(to)) return null;
            var path = new List<Vector2>();
            for (Vector2Int cell = to; cell != from; cell = previous[cell]) path.Add(Center(cell));
            path.Reverse();
            return path;
        }
    }
}
