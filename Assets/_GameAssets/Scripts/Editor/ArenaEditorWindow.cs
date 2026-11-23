using System;
using System.Collections.Generic;
using Deckbuilder.Grid;
using UnityEditor;
using UnityEngine;

namespace Deckbuilder.EditorTools
{
    public class ArenaEditorWindow : EditorWindow
    {
        private const float IsoHalfWidth = 0.70710678f;
        private const float IsoHalfHeight = 0.57922697f;
        private const float MinScale = 10f;
        private const float MaxScale = 160f;
        private const float ZoomSpeed = 0.04f;
        private const float CoordinateLabelMinScale = 34f;
        private const string DefaultLayoutFolder = "Assets/_GameAssets/Objects/Arenas";

        private static readonly Color BackgroundColor = new Color(0.16f, 0.17f, 0.19f);
        private static readonly Color LatticeColor = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color AreaBorderColor = new Color(1f, 1f, 1f, 0.3f);
        private static readonly Color OutlineColor = new Color(0f, 0f, 0f, 0.55f);

        [SerializeField] private ArenaLayout m_layout;
        [SerializeField] private int m_brush = 1;
        [SerializeField] private Vector2 m_pan;
        [SerializeField] private float m_scale = 42f;
        [SerializeField] private bool m_showCoordinates;

        private readonly Vector3[] m_quad = new Vector3[5];
        private readonly Vector3[] m_segment = new Vector3[2];
        private CellType[] m_cellTypes;
        private GUIStyle m_coordinateStyle;
        private Vector2Int m_hovered;
        private bool m_hasHover;
        private bool m_painting;
        private bool m_erasing;
        private Vector2 m_origin;

        [MenuItem("Deckbuilder/Arena Editor")]
        public static void Open()
        {
            ArenaEditorWindow _window = GetWindow<ArenaEditorWindow>();
            _window.titleContent = new GUIContent("Arena Editor");
            _window.minSize = new Vector2(520f, 380f);
            _window.Show();
        }

        private void OnEnable()
        {
            m_cellTypes = (CellType[])Enum.GetValues(typeof(CellType));
        }

        private void OnGUI()
        {
            if (m_cellTypes == null)
                m_cellTypes = (CellType[])Enum.GetValues(typeof(CellType));

            DrawAssetBar();

            if (m_layout == null)
            {
                DrawEmptyState();
                return;
            }

            DrawBrushBar();
            DrawShapeBar();

            Rect _canvas = GUILayoutUtility.GetRect(0f, 100000f, 0f, 100000f);

            GUI.BeginClip(_canvas);
            Rect _local = new Rect(0f, 0f, _canvas.width, _canvas.height);
            m_origin = _local.center + m_pan;
            HandleCanvasInput(_local);
            DrawCanvas(_local);
            GUI.EndClip();

            DrawFooter();
        }

        private void DrawAssetBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                ArenaLayout _picked = (ArenaLayout)EditorGUILayout.ObjectField(m_layout, typeof(ArenaLayout), false, GUILayout.Width(220f));
                if (EditorGUI.EndChangeCheck())
                {
                    m_layout = _picked;
                    m_pan = Vector2.zero;
                }

                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                    CreateLayout();

                if (m_layout != null)
                {
                    if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                        EditorGUIUtility.PingObject(m_layout);

                    if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                        SaveLayout();

                    GUILayout.Space(12f);

                    EditorGUIUtility.labelWidth = 60f;
                    EditorGUI.BeginChangeCheck();
                    float _cellSize = EditorGUILayout.FloatField("Cell size", m_layout.CellSize, EditorStyles.toolbarTextField, GUILayout.Width(120f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(m_layout, "Change Cell Size");
                        m_layout.SetCellSize(_cellSize);
                        EditorUtility.SetDirty(m_layout);
                    }
                    EditorGUIUtility.labelWidth = 0f;
                }

                GUILayout.FlexibleSpace();

                m_showCoordinates = GUILayout.Toggle(m_showCoordinates, "Coords", EditorStyles.toolbarButton, GUILayout.Width(56f));

                if (GUILayout.Button("Frame", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                    Frame();
            }
        }

        private void DrawBrushBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Brush", EditorStyles.miniLabel, GUILayout.Width(40f));

                DrawBrushButton(0, "No cell", new Color(0.3f, 0.3f, 0.32f));

                for (int _i = 0; _i < m_cellTypes.Length; _i++)
                    DrawBrushButton(_i + 1, ObjectNames.NicifyVariableName(m_cellTypes[_i].ToString()), GetCellColor(m_cellTypes[_i]));

                GUILayout.FlexibleSpace();
                GUILayout.Label("Left paint  ·  Right erase  ·  Middle or Alt drag pan  ·  Scroll zoom", EditorStyles.miniLabel);
            }
        }

        private void DrawBrushButton(int _index, string _label, Color _color)
        {
            Color _previous = GUI.backgroundColor;

            if (m_brush == _index)
                GUI.backgroundColor = _color;

            if (GUILayout.Button($"{_index + 1}  {_label}", EditorStyles.toolbarButton, GUILayout.Width(104f)))
                m_brush = _index;

            GUI.backgroundColor = _previous;
        }

        private void DrawShapeBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Area", EditorStyles.miniLabel, GUILayout.Width(40f));

                Vector2 _areaSize = m_layout.AreaSize;

                EditorGUIUtility.labelWidth = 14f;
                EditorGUI.BeginChangeCheck();
                float _sizeX = EditorGUILayout.DelayedFloatField("X", _areaSize.x, EditorStyles.toolbarTextField, GUILayout.Width(70f));
                float _sizeZ = EditorGUILayout.DelayedFloatField("Z", _areaSize.y, EditorStyles.toolbarTextField, GUILayout.Width(70f));
                EditorGUIUtility.labelWidth = 0f;

                if (EditorGUI.EndChangeCheck())
                    ResizeArea(new Vector2(_sizeX, _sizeZ));

                GUILayout.Label("units", EditorStyles.miniLabel, GUILayout.Width(32f));

                Vector2Int _count = m_layout.GetAreaCellCount();
                float _cellSize = m_layout.CellSize;
                GUILayout.Label($"= {_count.x} x {_count.y} cells  ({_count.x * _cellSize:0.##} x {_count.y * _cellSize:0.##} u)", EditorStyles.miniLabel, GUILayout.Width(190f));

                if (GUILayout.Button("Fill area", EditorStyles.toolbarButton, GUILayout.Width(74f)))
                    FillArea();

                GUILayout.Space(12f);

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(52f)) && ConfirmClear())
                {
                    Undo.RecordObject(m_layout, "Clear Arena");
                    m_layout.Clear();
                    EditorUtility.SetDirty(m_layout);
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("Pick an Arena Layout asset, or create one.", EditorStyles.boldLabel);

                    if (GUILayout.Button("New Arena Layout", GUILayout.Height(28f)))
                        CreateLayout();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(m_hasHover ? $"Cell {m_hovered.x}, {m_hovered.y}" : "—", EditorStyles.miniLabel, GUILayout.Width(110f));

                RectInt _bounds = m_layout.GetBounds();
                float _cellSize = m_layout.CellSize;
                GUILayout.Label($"{m_layout.CellCount} cells  ·  {_bounds.width} x {_bounds.height}  ·  {_bounds.width * _cellSize:0.##} x {_bounds.height * _cellSize:0.##} u", EditorStyles.miniLabel, GUILayout.Width(240f));

                int _allies = m_layout.CountOfType(CellType.AllySpawn);
                int _enemies = m_layout.CountOfType(CellType.EnemySpawn);
                GUILayout.Label($"{_allies} ally spawns  ·  {_enemies} enemy spawns", EditorStyles.miniLabel, GUILayout.Width(220f));

                GUILayout.FlexibleSpace();

                if (_allies == 0 || _enemies == 0)
                    GUILayout.Label("Needs at least one spawn of each side", EditorStyles.miniLabel);
            }
        }

        private void HandleCanvasInput(Rect _canvas)
        {
            Event _event = Event.current;
            bool _inside = _canvas.Contains(_event.mousePosition);

            if (_inside)
            {
                Vector2 _coordinate = ScreenToCoordinate(_event.mousePosition);
                m_hovered = new Vector2Int(Mathf.RoundToInt(_coordinate.x), Mathf.RoundToInt(_coordinate.y));
                m_hasHover = m_layout.IsInsideArea(m_hovered);
            }
            else if (!m_painting)
            {
                m_hasHover = false;
            }

            switch (_event.type)
            {
                case EventType.ScrollWheel when _inside:
                    Zoom(_event.mousePosition, _event.delta.y);
                    _event.Use();
                    break;

                case EventType.MouseDown when _inside && (_event.button == 2 || (_event.button == 0 && _event.alt)):
                    GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    _event.Use();
                    break;

                case EventType.MouseDown when _inside && (_event.button == 0 || _event.button == 1):
                    m_painting = true;
                    m_erasing = _event.button == 1 || m_brush == 0;
                    Undo.RecordObject(m_layout, m_erasing ? "Erase Arena Cells" : "Paint Arena Cells");
                    PaintHovered();
                    _event.Use();
                    break;

                case EventType.MouseDrag when _event.button == 2 || (_event.button == 0 && _event.alt):
                    m_pan += _event.delta;
                    Repaint();
                    _event.Use();
                    break;

                case EventType.MouseDrag when m_painting:
                    PaintHovered();
                    _event.Use();
                    break;

                case EventType.MouseUp when m_painting:
                    m_painting = false;
                    GUIUtility.hotControl = 0;
                    _event.Use();
                    break;

                case EventType.KeyDown when _event.keyCode == KeyCode.S && (_event.control || _event.command):
                    SaveLayout();
                    _event.Use();
                    break;

                case EventType.KeyDown when _event.keyCode >= KeyCode.Alpha1 && _event.keyCode <= KeyCode.Alpha9:
                    int _index = _event.keyCode - KeyCode.Alpha1;
                    if (_index <= m_cellTypes.Length)
                    {
                        m_brush = _index;
                        Repaint();
                        _event.Use();
                    }
                    break;

                case EventType.MouseMove when _inside:
                    Repaint();
                    break;
            }
        }

        private void SaveLayout()
        {
            EditorUtility.SetDirty(m_layout);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent($"Saved {m_layout.name}"), 1d);
        }

        private void PaintHovered()
        {
            if (!m_hasHover || !m_layout.IsInsideArea(m_hovered))
                return;

            bool _changed = m_erasing
                ? m_layout.RemoveCell(m_hovered)
                : m_layout.SetCell(m_hovered, m_cellTypes[m_brush - 1]);

            if (!_changed)
                return;

            EditorUtility.SetDirty(m_layout);
            Repaint();
        }

        private void Zoom(Vector2 _mousePosition, float _delta)
        {
            Vector2 _before = ScreenToCoordinate(_mousePosition);
            m_scale = Mathf.Clamp(m_scale * (1f - _delta * ZoomSpeed), MinScale, MaxScale);
            Vector2 _after = ScreenToCoordinate(_mousePosition);

            m_pan += CoordinateToScreenOffset(_after) - CoordinateToScreenOffset(_before);
            Repaint();
        }

        private void Frame()
        {
            m_pan = Vector2.zero;

            if (m_layout == null)
                return;

            RectInt _area = m_layout.GetAreaBounds();
            Vector2 _center = new Vector2(_area.x + (_area.width - 1) * 0.5f, _area.y + (_area.height - 1) * 0.5f);
            m_pan = -CoordinateToScreenOffset(_center);
            Repaint();
        }

        private void ResizeArea(Vector2 _areaSize)
        {
            Undo.RecordObject(m_layout, "Resize Arena Area");

            m_layout.SetAreaSize(_areaSize);
            m_layout.RemoveCellsOutsideArea();

            EditorUtility.SetDirty(m_layout);
            Frame();
        }

        private void FillArea()
        {
            RectInt _area = m_layout.GetAreaBounds();

            Undo.RecordObject(m_layout, "Fill Arena Area");

            Dictionary<Vector2Int, CellType> _kept = new Dictionary<Vector2Int, CellType>();

            foreach (ArenaLayout.CellEntry _entry in m_layout.Cells)
                _kept[_entry.coordinate] = _entry.type;

            m_layout.Clear();

            for (int _x = _area.xMin; _x < _area.xMax; _x++)
            {
                for (int _y = _area.yMin; _y < _area.yMax; _y++)
                {
                    Vector2Int _coordinate = new Vector2Int(_x, _y);
                    m_layout.SetCell(_coordinate, _kept.TryGetValue(_coordinate, out CellType _type) ? _type : CellType.Empty);
                }
            }

            EditorUtility.SetDirty(m_layout);
            Frame();
        }

        private bool ConfirmClear()
        {
            return EditorUtility.DisplayDialog("Clear arena", $"Remove all {m_layout.CellCount} cells from {m_layout.name}?", "Clear", "Cancel");
        }

        private void CreateLayout()
        {
            if (!AssetDatabase.IsValidFolder(DefaultLayoutFolder))
            {
                int _lastSlash = DefaultLayoutFolder.LastIndexOf('/');
                AssetDatabase.CreateFolder(DefaultLayoutFolder.Substring(0, _lastSlash), DefaultLayoutFolder.Substring(_lastSlash + 1));
            }

            string _path = EditorUtility.SaveFilePanelInProject("New Arena Layout", "ArenaLayout", "asset", "Where should the arena layout live?", DefaultLayoutFolder);

            if (string.IsNullOrEmpty(_path))
                return;

            ArenaLayout _layout = CreateInstance<ArenaLayout>();
            AssetDatabase.CreateAsset(_layout, _path);
            AssetDatabase.SaveAssets();

            m_layout = _layout;
            m_pan = Vector2.zero;
        }

        private void DrawCanvas(Rect _canvas)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(_canvas, BackgroundColor);

            DrawLattice();
            DrawCells();
        }

        private void DrawLattice()
        {
            RectInt _area = m_layout.GetAreaBounds();

            float _minX = _area.xMin - 0.5f;
            float _maxX = _area.xMax - 0.5f;
            float _minY = _area.yMin - 0.5f;
            float _maxY = _area.yMax - 0.5f;

            for (int _x = _area.xMin; _x <= _area.xMax; _x++)
            {
                Handles.color = _x == _area.xMin || _x == _area.xMax ? AreaBorderColor : LatticeColor;
                DrawLine(new Vector2(_x - 0.5f, _minY), new Vector2(_x - 0.5f, _maxY));
            }

            for (int _y = _area.yMin; _y <= _area.yMax; _y++)
            {
                Handles.color = _y == _area.yMin || _y == _area.yMax ? AreaBorderColor : LatticeColor;
                DrawLine(new Vector2(_minX, _y - 0.5f), new Vector2(_maxX, _y - 0.5f));
            }
        }

        private void DrawCells()
        {
            IReadOnlyList<ArenaLayout.CellEntry> _cells = m_layout.Cells;

            for (int _i = 0; _i < _cells.Count; _i++)
                DrawCell(_cells[_i].coordinate, GetCellColor(_cells[_i].type));

            if (!m_showCoordinates || m_scale < CoordinateLabelMinScale)
                return;

            if (m_coordinateStyle == null)
            {
                m_coordinateStyle = new GUIStyle(EditorStyles.miniLabel);
                m_coordinateStyle.alignment = TextAnchor.MiddleCenter;
                m_coordinateStyle.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            }

            for (int _i = 0; _i < _cells.Count; _i++)
            {
                Vector2 _center = CoordinateToScreen(_cells[_i].coordinate);
                Rect _label = new Rect(_center.x - 26f, _center.y - 8f, 52f, 16f);
                GUI.Label(_label, $"{_cells[_i].coordinate.x},{_cells[_i].coordinate.y}", m_coordinateStyle);
            }
        }

        private void DrawCell(Vector2Int _coordinate, Color _color)
        {
            m_quad[0] = CoordinateToScreen(new Vector2(_coordinate.x - 0.5f, _coordinate.y - 0.5f));
            m_quad[1] = CoordinateToScreen(new Vector2(_coordinate.x + 0.5f, _coordinate.y - 0.5f));
            m_quad[2] = CoordinateToScreen(new Vector2(_coordinate.x + 0.5f, _coordinate.y + 0.5f));
            m_quad[3] = CoordinateToScreen(new Vector2(_coordinate.x - 0.5f, _coordinate.y + 0.5f));
            m_quad[4] = m_quad[0];

            Handles.color = _color;
            Handles.DrawAAConvexPolygon(m_quad[0], m_quad[1], m_quad[2], m_quad[3]);

            Handles.color = OutlineColor;
            Handles.DrawAAPolyLine(1.5f, m_quad);
        }

        private void DrawLine(Vector2 _from, Vector2 _to)
        {
            m_segment[0] = CoordinateToScreen(_from);
            m_segment[1] = CoordinateToScreen(_to);

            Handles.DrawAAPolyLine(1f, m_segment);
        }

        private Vector2 CoordinateToScreen(Vector2 _coordinate)
        {
            return m_origin + CoordinateToScreenOffset(_coordinate);
        }

        private Vector2 CoordinateToScreenOffset(Vector2 _coordinate)
        {
            return new Vector2(
                (_coordinate.x - _coordinate.y) * IsoHalfWidth * m_scale,
                -(_coordinate.x + _coordinate.y) * IsoHalfHeight * m_scale);
        }

        private Vector2 ScreenToCoordinate(Vector2 _screenPosition)
        {
            Vector2 _delta = _screenPosition - m_origin;

            float _difference = _delta.x / (IsoHalfWidth * m_scale);
            float _sum = -_delta.y / (IsoHalfHeight * m_scale);

            return new Vector2((_sum + _difference) * 0.5f, (_sum - _difference) * 0.5f);
        }

        private static Color GetCellColor(CellType _type)
        {
            switch (_type)
            {
                case CellType.Empty:
                    return new Color(0.58f, 0.63f, 0.70f);

                case CellType.AllySpawn:
                    return new Color(0.24f, 0.74f, 0.42f);

                case CellType.EnemySpawn:
                    return new Color(0.86f, 0.31f, 0.27f);

                case CellType.Obstacle:
                    return new Color(0.36f, 0.31f, 0.28f);

                default:
                    return Color.HSVToRGB(Mathf.Repeat((int)_type * 0.137f, 1f), 0.55f, 0.85f);
            }
        }
    }
}
