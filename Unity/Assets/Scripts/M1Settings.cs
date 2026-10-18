using System;
using UnityEngine;

namespace Reborn1985.M1
{
    [CreateAssetMenu(menuName = "Reborn 1985/M1 Prototype Settings")]
    public sealed class M1Settings : ScriptableObject
    {
        [SerializeField] private Vector2Int mapSize = new Vector2Int(60, 42);
        [SerializeField] private Vector2 cellSize = new Vector2(1f, .5f);
        [SerializeField, Range(.5f, 5f)] private float movementSpeed = 2.4f;
        [SerializeField, Range(.08f, .22f)] private float footRadius = .13f;
        [SerializeField, Range(1.5f, 6f)] private float cameraSize = 3.25f;
        [SerializeField, Range(.02f, .5f)] private float cameraSmoothTime = .12f;
        [SerializeField, Range(.5f, 1.5f)] private float characterScale = 1f;
        [SerializeField] private RegionDefinition[] regions =
        {
            new RegionDefinition("A", "破败小巷", new RectInt(3, 3, 5, 9), new Vector2Int(6, 5)),
            new RegionDefinition("B", "巷口过渡", new RectInt(5, 12, 3, 5), new Vector2Int(6, 14)),
            new RegionDefinition("D", "主街", new RectInt(3, 17, 48, 7), new Vector2Int(12, 20)),
            new RegionDefinition("C", "林婶早餐摊", new RectInt(14, 24, 9, 7), new Vector2Int(18, 26)),
            new RegionDefinition("G", "陈伯收购站", new RectInt(35, 24, 12, 9), new Vector2Int(41, 28)),
            new RegionDefinition("F", "废弃物区", new RectInt(25, 7, 10, 10), new Vector2Int(29, 15)),
            new RegionDefinition("E", "旧货角落", new RectInt(46, 8, 10, 9), new Vector2Int(49, 13))
        };

        public Vector2Int MapSize => mapSize;
        public Vector2 CellSize => cellSize;
        public float MovementSpeed => movementSpeed;
        public float FootRadius => footRadius;
        public float CameraSize => cameraSize;
        public float CameraSmoothTime => cameraSmoothTime;
        public float CharacterScale => characterScale;
        public RegionDefinition[] Regions => regions;

        public RegionDefinition Region(string id) => Array.Find(regions, region => region.Id == id);
    }

    [Serializable]
    public sealed class RegionDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private RectInt cells;
        [SerializeField] private Vector2Int entry;
        public string Id => id;
        public string Title => title;
        public RectInt Cells => cells;
        public Vector2Int Entry => entry;

        public RegionDefinition(string id, string title, RectInt cells, Vector2Int entry)
        {
            this.id = id; this.title = title; this.cells = cells; this.entry = entry;
        }
    }
}
