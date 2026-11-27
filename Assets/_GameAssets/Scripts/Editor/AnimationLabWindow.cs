using System;
using System.Collections.Generic;
using Deckbuilder.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Deckbuilder.EditorTools
{
    public class AnimationLabWindow : EditorWindow
    {
        private const string PrefabFolder = "Assets/_GameAssets/Prefabs";
        private const string SetFolder = "Assets/_GameAssets/Animations/Sets";
        private const string MoveFolder = "Assets/_GameAssets/Animations/Moves";
        private const string DefaultSetPath = SetFolder + "/AnimSet_Humanoid_Default.asset";
        private const float SidebarWidth = 300f;
        private const float ToolbarHeight = 22f;
        private const float TimelineHeight = 62f;
        private const float DetailsHeight = 132f;
        private const float FrontYaw = 180f;
        private const float FrontPitch = 12f;
        private const float GameYaw = 45f;
        private const float GamePitch = 55f;

        private static readonly Color PreviewBackground = new Color(0.2f, 0.21f, 0.23f);
        private static readonly Color PanelBackground = new Color(0.16f, 0.17f, 0.19f);
        private static readonly Color BarColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color FillColor = new Color(0.35f, 0.6f, 1f, 0.35f);
        private static readonly Color TickColor = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color ImpactColor = new Color(1f, 0.38f, 0.3f);
        private static readonly Color InheritedColor = new Color(1f, 1f, 1f, 0.45f);

        private enum SlotKind
        {
            Idle,
            Run,
            Death,
            Cast
        }

        private sealed class Slot
        {
            public SlotKind kind;
            public CastStyle style;
            public string label;
            public string clipProperty;
            public AnimationClip clip;
            public float clipSpeed = 1f;
            public AnimationMove move;
            public CharacterAnimationSet owner;

            public AnimationClip PreviewClip => kind == SlotKind.Cast ? (move != null ? move.Clip : null) : clip;
            public float Speed => kind == SlotKind.Cast ? (move != null ? move.Speed : 1f) : clipSpeed;
            public string SpeedProperty => clipProperty + "Speed";
        }

        [SerializeField] private string m_selectedEntityPath;
        [SerializeField] private int m_selectedSlot;
        [SerializeField] private float m_yaw = FrontYaw;
        [SerializeField] private float m_pitch = FrontPitch;
        [SerializeField] private float m_zoom = 1f;
        [SerializeField] private bool m_loop = true;
        [SerializeField] private bool m_showCards = true;

        private readonly List<EntityAnimationModule> m_entities = new List<EntityAnimationModule>();
        private readonly List<CardConfig> m_cards = new List<CardConfig>();
        private readonly List<Slot> m_slots = new List<Slot>();
        private List<string> m_problems;
        private EntityAnimationModule m_entity;
        private Vector2 m_sidebarScroll;
        private GUIStyle m_rightMiniLabel;

        private PreviewRenderUtility m_preview;
        private GameObject m_instance;
        private Animator m_instanceAnimator;
        private PlayableGraph m_graph;
        private AnimationClipPlayable m_clipPlayable;
        private AnimationClip m_graphClip;
        private Bounds m_bounds;
        private float m_time;
        private bool m_playing;
        private double m_lastTickTime;
        private bool m_draggingImpact;
        private bool m_scrubbing;

        [MenuItem("Deckbuilder/Animation Lab")]
        public static void Open()
        {
            AnimationLabWindow _window = GetWindow<AnimationLabWindow>();
            _window.titleContent = new GUIContent("Animation Lab");
            _window.minSize = new Vector2(760f, 520f);
            _window.Show();
        }

        [MenuItem("Deckbuilder/Animation/Validate")]
        public static void ValidateMenu()
        {
            List<string> _problems = Validate();
            Debug.Log(_problems.Count == 0 ? "Animation setup: no problems." : $"Animation setup: {_problems.Count} problem(s).\n  " + string.Join("\n  ", _problems));
        }

        public static List<EntityAnimationModule> FindEntityModules()
        {
            List<EntityAnimationModule> _modules = new List<EntityAnimationModule>();

            foreach (string _guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                GameObject _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(_guid));

                if (!_prefab.TryGetComponent(out EntityAnimationModule _module))
                    continue;

                if (_prefab.GetComponentInChildren<Animator>(true) == null)
                    continue;

                _modules.Add(_module);
            }

            _modules.Sort((_a, _b) => string.CompareOrdinal(_a.name, _b.name));
            return _modules;
        }

        public static List<string> Validate()
        {
            List<string> _problems = new List<string>();

            foreach (EntityAnimationModule _module in FindEntityModules())
            {
                SerializedObject _serialized = new SerializedObject(_module);
                string _prefabName = _module.gameObject.name;

                if (_serialized.FindProperty("m_animator").objectReferenceValue == null)
                    _problems.Add($"{_prefabName}: EntityAnimationModule has no Animator assigned.");

                if (_serialized.FindProperty("m_controller").objectReferenceValue == null)
                    _problems.Add($"{_prefabName}: EntityAnimationModule has no controller assigned.");

                CharacterAnimationSet _set = _module.AnimationSet;

                if (_set == null)
                {
                    _problems.Add($"{_prefabName}: EntityAnimationModule has no animation set assigned.");
                    continue;
                }

                try
                {
                    _ = _set.Idle;
                    _ = _set.Run;
                    _ = _set.Death;
                    _set.GetCastMove(CastStyle.Attack, CardType.Attack);
                    _set.GetCastMove(CastStyle.Utility, CardType.Skill);
                }
                catch (InvalidOperationException _exception)
                {
                    _problems.Add($"{_prefabName}: {_exception.Message}");
                }
            }

            foreach (string _guid in AssetDatabase.FindAssets("t:CharacterAnimationSet"))
            {
                CharacterAnimationSet _set = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>(AssetDatabase.GUIDToAssetPath(_guid));
                SerializedObject _serialized = new SerializedObject(_set);
                SerializedProperty _casts = _serialized.FindProperty("m_casts");
                HashSet<int> _styles = new HashSet<int>();

                foreach (string _clipProperty in new[] { "m_idle", "m_run", "m_death" })
                    CheckClipEvents(_serialized.FindProperty(_clipProperty).objectReferenceValue as AnimationClip, _set.name, _problems);

                for (int _i = 0; _i < _casts.arraySize; _i++)
                {
                    SerializedProperty _entry = _casts.GetArrayElementAtIndex(_i);
                    int _style = _entry.FindPropertyRelative("style").intValue;

                    if (_style == (int)CastStyle.Default)
                        _problems.Add($"{_set.name}: cast entry {_i} uses 'Default', which is not a move slot.");

                    if (!_styles.Add(_style))
                        _problems.Add($"{_set.name}: '{(CastStyle)_style}' is listed more than once.");

                    if (_entry.FindPropertyRelative("move").objectReferenceValue == null)
                        _problems.Add($"{_set.name}: '{(CastStyle)_style}' has no move assigned.");
                }
            }

            foreach (string _guid in AssetDatabase.FindAssets("t:AnimationMove"))
            {
                AnimationMove _move = AssetDatabase.LoadAssetAtPath<AnimationMove>(AssetDatabase.GUIDToAssetPath(_guid));

                if (_move.Clip == null)
                {
                    _problems.Add($"{_move.name}: no clip assigned.");
                    continue;
                }

                CheckClipEvents(_move.Clip, _move.name, _problems);

                if (_move.Delivery != ImpactDelivery.Instant && _move.Projectile == null)
                    _problems.Add($"{_move.name}: delivers by '{_move.Delivery}' but has no projectile assigned.");
            }

            foreach (EntityAnimationModule _module in FindEntityModules())
            {
                if (_module.CastOrigin == null)
                    _problems.Add($"{_module.gameObject.name}: EntityAnimationModule has no cast origin assigned.");

                if (_module.HitPoint == null)
                    _problems.Add($"{_module.gameObject.name}: EntityAnimationModule has no hit point assigned.");
            }

            return _problems;
        }

        private static void CheckClipEvents(AnimationClip _clip, string _owner, List<string> _problems)
        {
            if (_clip == null)
                return;

            if (AnimationUtility.GetAnimationEvents(_clip).Length > 0)
                _problems.Add($"{_owner}: clip '{_clip.name}' still carries animation events. Timing lives on the move now, and the events have no receiver.");
        }

        private void OnEnable()
        {
            EditorApplication.update += Tick;
            Undo.undoRedoPerformed += OnUndoRedo;
            RefreshAssets();
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            Undo.undoRedoPerformed -= OnUndoRedo;
            DestroyPreview();
        }

        private void OnUndoRedo()
        {
            BuildSlots();
            Repaint();
        }

        private void RefreshAssets()
        {
            m_entities.Clear();
            m_entities.AddRange(FindEntityModules());

            m_cards.Clear();

            foreach (string _guid in AssetDatabase.FindAssets("t:CardConfig"))
                m_cards.Add(AssetDatabase.LoadAssetAtPath<CardConfig>(AssetDatabase.GUIDToAssetPath(_guid)));

            m_cards.Sort((_a, _b) => string.CompareOrdinal(_a.Title, _b.Title));

            EntityAnimationModule _selected = m_entities.Find(_module => AssetDatabase.GetAssetPath(_module) == m_selectedEntityPath);

            if (_selected == null && m_entities.Count > 0)
                _selected = m_entities[0];

            SelectEntity(_selected, m_selectedSlot);
        }

        private void SelectEntity(EntityAnimationModule _module, int _slot)
        {
            m_entity = _module;
            m_selectedEntityPath = _module == null ? null : AssetDatabase.GetAssetPath(_module);
            DestroyPreview();
            BuildSlots();
            SelectSlot(Mathf.Clamp(_slot, 0, Mathf.Max(0, m_slots.Count - 1)));
        }

        private void SelectSlot(int _index)
        {
            m_selectedSlot = _index;
            Slot _slot = SelectedSlot();
            m_time = _slot != null && _slot.move != null ? _slot.move.ImpactTime * _slot.move.Clip.length : 0f;
            m_playing = _slot != null && _slot.PreviewClip != null;
            m_lastTickTime = EditorApplication.timeSinceStartup;
        }

        private Slot SelectedSlot()
        {
            return m_selectedSlot >= 0 && m_selectedSlot < m_slots.Count ? m_slots[m_selectedSlot] : null;
        }

        private void BuildSlots()
        {
            m_slots.Clear();

            if (m_entity == null || m_entity.AnimationSet == null)
                return;

            AddClipSlot(SlotKind.Idle, "Idle", "m_idle");
            AddClipSlot(SlotKind.Run, "Run", "m_run");
            AddClipSlot(SlotKind.Death, "Death", "m_death");

            foreach (CastStyle _style in (CastStyle[])Enum.GetValues(typeof(CastStyle)))
            {
                if (_style == CastStyle.Default)
                    continue;

                Slot _slot = new Slot { kind = SlotKind.Cast, style = _style, label = _style.ToString() };
                m_entity.AnimationSet.TryFindCastMove(_style, out _slot.move, out _slot.owner);
                m_slots.Add(_slot);
            }
        }

        private void AddClipSlot(SlotKind _kind, string _label, string _clipProperty)
        {
            Slot _slot = new Slot { kind = _kind, label = _label, clipProperty = _clipProperty };

            for (CharacterAnimationSet _set = m_entity.AnimationSet; _set != null; _set = _set.Parent)
            {
                SerializedObject _serialized = new SerializedObject(_set);
                AnimationClip _clip = _serialized.FindProperty(_clipProperty).objectReferenceValue as AnimationClip;

                if (_clip == null)
                    continue;

                _slot.clip = _clip;
                _slot.clipSpeed = _serialized.FindProperty(_slot.SpeedProperty).floatValue;
                _slot.owner = _set;
                break;
            }

            m_slots.Add(_slot);
        }

        private void Tick()
        {
            double _now = EditorApplication.timeSinceStartup;
            float _deltaTime = (float)(_now - m_lastTickTime);
            m_lastTickTime = _now;

            if (!m_playing)
                return;

            Slot _slot = SelectedSlot();
            AnimationClip _clip = _slot?.PreviewClip;

            if (_clip == null)
            {
                m_playing = false;
                return;
            }

            m_time += _deltaTime * _slot.Speed;

            if (m_time >= _clip.length)
            {
                if (m_loop)
                {
                    m_time %= _clip.length;
                }
                else
                {
                    m_time = _clip.length;
                    m_playing = false;
                }
            }

            Repaint();
        }

        private void OnGUI()
        {
            if (m_rightMiniLabel == null)
                m_rightMiniLabel = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };

            HandleShortcuts();

            DrawSidebar(new Rect(0f, 0f, SidebarWidth, position.height));

            Rect _main = new Rect(SidebarWidth, 0f, position.width - SidebarWidth, position.height);
            Rect _toolbar = new Rect(_main.x, _main.y, _main.width, ToolbarHeight);
            Rect _details = new Rect(_main.x, _main.yMax - DetailsHeight, _main.width, DetailsHeight);
            Rect _timeline = new Rect(_main.x, _details.y - TimelineHeight, _main.width, TimelineHeight);
            Rect _preview = new Rect(_main.x, _toolbar.yMax, _main.width, _timeline.y - _toolbar.yMax);

            Slot _slot = SelectedSlot();

            DrawToolbar(_toolbar);
            DrawPreview(_preview, _slot);
            DrawTimeline(_timeline, _slot);
            DrawDetails(_details, _slot);
        }

        private void HandleShortcuts()
        {
            Event _event = Event.current;

            if (_event.type != EventType.KeyDown)
                return;

            if (_event.keyCode == KeyCode.S && (_event.control || _event.command))
            {
                Save();
                _event.Use();
            }
            else if (_event.keyCode == KeyCode.Space)
            {
                TogglePlay();
                _event.Use();
            }
            else if (_event.keyCode == KeyCode.LeftArrow || _event.keyCode == KeyCode.RightArrow)
            {
                StepFrame(_event.keyCode == KeyCode.RightArrow ? 1 : -1);
                _event.Use();
            }
        }

        private void Save()
        {
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Saved animation assets"), 1d);
        }

        private void TogglePlay()
        {
            Slot _slot = SelectedSlot();
            AnimationClip _clip = _slot?.PreviewClip;

            if (_clip == null)
                return;

            if (!m_playing && m_time >= _clip.length)
                m_time = 0f;

            m_playing = !m_playing;
            m_lastTickTime = EditorApplication.timeSinceStartup;
        }

        private void StepFrame(int _direction)
        {
            AnimationClip _clip = SelectedSlot()?.PreviewClip;

            if (_clip == null)
                return;

            int _frameCount = Mathf.Max(1, Mathf.RoundToInt(_clip.length * _clip.frameRate));
            int _frame = Mathf.Clamp(Mathf.RoundToInt(m_time / _clip.length * _frameCount) + _direction, 0, _frameCount);

            m_playing = false;
            m_time = _frame / (float)_frameCount * _clip.length;
            Repaint();
        }

        private void DrawSidebar(Rect _rect)
        {
            EditorGUI.DrawRect(_rect, PanelBackground);

            GUILayout.BeginArea(_rect);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                    RefreshAssets();

                if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                    m_problems = Validate();

                GUILayout.FlexibleSpace();
            }

            m_sidebarScroll = EditorGUILayout.BeginScrollView(m_sidebarScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
            GUILayout.BeginVertical(GUILayout.Width(SidebarWidth - 18f));
            float _previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 96f;

            if (m_problems != null)
                DrawProblems();

            EditorGUILayout.LabelField("Characters", EditorStyles.boldLabel);

            foreach (EntityAnimationModule _module in m_entities)
            {
                bool _selected = _module == m_entity;

                if (GUILayout.Toggle(_selected, _module.gameObject.name, "Button") && !_selected)
                    SelectEntity(_module, m_selectedSlot);
            }

            if (m_entity != null)
            {
                EditorGUILayout.Space();
                DrawEntitySetup();
            }

            if (m_slots.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);

                for (int _i = 0; _i < m_slots.Count; _i++)
                    DrawSlotRow(_i);

                EditorGUILayout.Space();
                m_showCards = EditorGUILayout.Foldout(m_showCards, $"Cards ({m_cards.Count})", true);

                if (m_showCards)
                {
                    foreach (CardConfig _card in m_cards)
                        DrawCardRow(_card);
                }
            }

            EditorGUIUtility.labelWidth = _previousLabelWidth;
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawProblems()
        {
            if (m_problems.Count == 0)
            {
                EditorGUILayout.HelpBox("No problems found.", MessageType.Info);
                return;
            }

            foreach (string _problem in m_problems)
                EditorGUILayout.HelpBox(_problem, MessageType.Error);
        }

        private void DrawEntitySetup()
        {
            SerializedObject _serialized = new SerializedObject(m_entity);
            SerializedProperty _animator = _serialized.FindProperty("m_animator");
            SerializedProperty _set = _serialized.FindProperty("m_animationSet");

            EditorGUILayout.PropertyField(_set, new GUIContent("Animation set"));

            if (_animator.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No Animator assigned on the EntityAnimationModule.", MessageType.Error);

                if (GUILayout.Button("Find Animator in children"))
                    _animator.objectReferenceValue = m_entity.GetComponentInChildren<Animator>(true);
            }

            if (_set.objectReferenceValue == null && GUILayout.Button("Create a set for this character"))
                _set.objectReferenceValue = CreateSetFor(m_entity.gameObject.name);

            if (_serialized.ApplyModifiedProperties())
            {
                DestroyPreview();
                BuildSlots();
            }
        }

        private static CharacterAnimationSet CreateSetFor(string _entityName)
        {
            CharacterAnimationSet _set = CreateInstance<CharacterAnimationSet>();

            SerializedObject _serialized = new SerializedObject(_set);
            _serialized.FindProperty("m_parent").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>(DefaultSetPath);
            _serialized.ApplyModifiedPropertiesWithoutUndo();

            string _path = AssetDatabase.GenerateUniqueAssetPath($"{SetFolder}/AnimSet_{_entityName}.asset");
            AssetDatabase.CreateAsset(_set, _path);
            AssetDatabase.SaveAssets();
            return _set;
        }

        private void DrawSlotRow(int _index)
        {
            Slot _slot = m_slots[_index];
            bool _selected = _index == m_selectedSlot;

            Rect _row = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));

            if (GUI.Toggle(_row, _selected, GUIContent.none, "Button") && !_selected)
                SelectSlot(_index);

            Rect _label = new Rect(_row.x + 6f, _row.y, _row.width * 0.38f, _row.height);
            Rect _value = new Rect(_label.xMax, _row.y, _row.xMax - _label.xMax - 6f, _row.height);

            GUI.Label(_label, _slot.label, EditorStyles.miniBoldLabel);

            string _valueText;

            if (_slot.kind == SlotKind.Cast && _slot.move == null)
                _valueText = "falls back to card type";
            else
                _valueText = _slot.kind == SlotKind.Cast ? _slot.move.name : (_slot.clip != null ? _slot.clip.name : "MISSING");

            if (_slot.PreviewClip != null && !Mathf.Approximately(_slot.Speed, 1f))
                _valueText += $"  ×{_slot.Speed:0.##}";

            if (_slot.owner != null && _slot.owner != m_entity.AnimationSet)
                _valueText += "  ↑";

            Color _previous = GUI.color;

            if (_slot.owner != null && _slot.owner != m_entity.AnimationSet)
                GUI.color = InheritedColor;

            GUI.Label(_value, _valueText, m_rightMiniLabel);
            GUI.color = _previous;
        }

        private void DrawCardRow(CardConfig _card)
        {
            CastStyle _baseStyle = CharacterAnimationSet.GetBaseStyle(_card.Type);
            CastStyle _style = _card.CastStyle == CastStyle.Default ? _baseStyle : _card.CastStyle;
            int _slotIndex = m_slots.FindIndex(_slot => _slot.kind == SlotKind.Cast && _slot.style == _style);

            if (_slotIndex >= 0 && m_slots[_slotIndex].move == null)
                _slotIndex = m_slots.FindIndex(_slot => _slot.kind == SlotKind.Cast && _slot.style == _baseStyle);

            Slot _target = _slotIndex >= 0 ? m_slots[_slotIndex] : null;
            string _moveName = _target != null && _target.move != null ? _target.move.name : "MISSING";

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(_card.Title, EditorStyles.miniButtonLeft, GUILayout.Width(110f)) && _slotIndex >= 0)
                {
                    SelectSlot(_slotIndex);
                    EditorGUIUtility.PingObject(_card);
                }

                CastStyle _newStyle = (CastStyle)EditorGUILayout.EnumPopup(_card.CastStyle, EditorStyles.miniPullDown, GUILayout.Width(86f));

                if (_newStyle != _card.CastStyle)
                {
                    SerializedObject _serialized = new SerializedObject(_card);
                    _serialized.FindProperty("m_castStyle").intValue = (int)_newStyle;
                    _serialized.ApplyModifiedProperties();
                }

                GUILayout.Label(_moveName, EditorStyles.miniLabel, GUILayout.MinWidth(0f));
            }
        }

        private void DrawToolbar(Rect _rect)
        {
            GUILayout.BeginArea(_rect, EditorStyles.toolbar);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(m_entity != null ? m_entity.gameObject.name : "No character", EditorStyles.toolbarButton);

                if (GUILayout.Button(m_playing ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                    TogglePlay();

                m_loop = GUILayout.Toggle(m_loop, "Loop", EditorStyles.toolbarButton, GUILayout.Width(44f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Front", EditorStyles.toolbarButton))
                {
                    m_yaw = FrontYaw;
                    m_pitch = FrontPitch;
                }

                if (GUILayout.Button("Game angle", EditorStyles.toolbarButton))
                {
                    m_yaw = GameYaw;
                    m_pitch = GamePitch;
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                    Save();
            }

            GUILayout.EndArea();
        }

        private void DrawPreview(Rect _rect, Slot _slot)
        {
            AnimationClip _clip = _slot?.PreviewClip;

            HandlePreviewInput(_rect);

            if (Event.current.type != EventType.Repaint)
                return;

            if (m_entity == null || _clip == null)
            {
                EditorGUI.DrawRect(_rect, PreviewBackground);
                GUI.Label(_rect, m_entity == null ? "No character with an EntityAnimationModule and an Animator found." : "Nothing to preview for this slot.", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true });
                return;
            }

            EnsurePreview();

            if (m_instanceAnimator == null)
            {
                EditorGUI.DrawRect(_rect, PreviewBackground);
                GUI.Label(_rect, "The preview copy has no Animator assigned on its EntityAnimationModule.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EnsureGraph(_clip);
            m_clipPlayable.SetTime(m_time);
            m_graph.Evaluate();

            Camera _camera = m_preview.camera;
            float _radius = Mathf.Max(0.5f, m_bounds.extents.magnitude);
            float _distance = _radius / Mathf.Sin(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * m_zoom;
            Quaternion _rotation = Quaternion.Euler(m_pitch, m_yaw, 0f);

            _camera.transform.SetPositionAndRotation(m_bounds.center - _rotation * Vector3.forward * _distance, _rotation);
            _camera.nearClipPlane = Mathf.Max(0.01f, _distance - _radius * 4f);
            _camera.farClipPlane = _distance + _radius * 4f;

            m_preview.lights[0].intensity = 1.2f;
            m_preview.lights[0].transform.rotation = Quaternion.Euler(40f, m_yaw + 35f, 0f);
            m_preview.lights[1].intensity = 0.5f;
            m_preview.lights[1].transform.rotation = Quaternion.Euler(-20f, m_yaw - 140f, 0f);
            m_preview.ambientColor = new Color(0.32f, 0.33f, 0.36f);

            m_preview.BeginPreview(_rect, GUIStyle.none);
            m_preview.Render(true);
            GUI.DrawTexture(_rect, m_preview.EndPreview(), ScaleMode.StretchToFill, false);

            GUI.Label(new Rect(_rect.x + 8f, _rect.y + 6f, _rect.width - 16f, 18f), $"{_slot.label}  ·  {_clip.name}", EditorStyles.whiteMiniLabel);
            GUI.Label(new Rect(_rect.x + 8f, _rect.yMax - 22f, _rect.width - 16f, 18f), "Drag to orbit · wheel to zoom · Space play · ←/→ step a frame · Ctrl+S save", EditorStyles.whiteMiniLabel);
        }

        private void HandlePreviewInput(Rect _rect)
        {
            Event _event = Event.current;

            if (!_rect.Contains(_event.mousePosition))
                return;

            if (_event.type == EventType.MouseDrag && _event.button == 0 && !m_scrubbing && !m_draggingImpact)
            {
                m_yaw += _event.delta.x * 0.6f;
                m_pitch = Mathf.Clamp(m_pitch + _event.delta.y * 0.4f, -10f, 85f);
                _event.Use();
                Repaint();
            }
            else if (_event.type == EventType.ScrollWheel)
            {
                m_zoom = Mathf.Clamp(m_zoom * (1f + _event.delta.y * 0.04f), 0.3f, 3f);
                _event.Use();
                Repaint();
            }
        }

        private void DrawTimeline(Rect _rect, Slot _slot)
        {
            EditorGUI.DrawRect(_rect, PanelBackground);

            AnimationClip _clip = _slot?.PreviewClip;

            if (_clip == null)
                return;

            Rect _bar = new Rect(_rect.x + 14f, _rect.y + 22f, _rect.width - 28f, 14f);
            float _length = _clip.length;
            int _frameCount = Mathf.Max(1, Mathf.RoundToInt(_length * _clip.frameRate));
            float _progress = Mathf.Clamp01(m_time / _length);

            EditorGUI.DrawRect(_bar, BarColor);
            EditorGUI.DrawRect(new Rect(_bar.x, _bar.y, _bar.width * _progress, _bar.height), FillColor);

            if (_bar.width / _frameCount >= 4f)
            {
                for (int _frame = 0; _frame <= _frameCount; _frame++)
                    EditorGUI.DrawRect(new Rect(_bar.x + _bar.width * _frame / _frameCount, _bar.yMax - 4f, 1f, 4f), TickColor);
            }

            Rect _impactHandle = default;

            if (_slot.move != null)
            {
                float _impactX = _bar.x + _bar.width * _slot.move.ImpactTime;
                _impactHandle = new Rect(_impactX - 7f, _bar.y - 12f, 14f, _bar.height + 22f);

                EditorGUI.DrawRect(new Rect(_impactX - 1f, _bar.y - 8f, 3f, _bar.height + 14f), ImpactColor);
                GUI.Label(new Rect(_impactX + 5f, _bar.y - 19f, 120f, 16f), $"Impact {_slot.move.ImpactTime * _length:0.00}s", EditorStyles.whiteMiniLabel);
                EditorGUIUtility.AddCursorRect(_impactHandle, MouseCursor.ResizeHorizontal);
            }

            float _playheadX = _bar.x + _bar.width * _progress;
            EditorGUI.DrawRect(new Rect(_playheadX - 1f, _bar.y - 4f, 2f, _bar.height + 8f), Color.white);

            int _currentFrame = Mathf.RoundToInt(_progress * _frameCount);
            float _speed = _slot.Speed;
            GUI.Label(new Rect(_bar.x, _bar.yMax + 4f, _bar.width, 16f), $"{m_time:0.00}s / {_length:0.00}s   ·   frame {_currentFrame}/{_frameCount}   ·   plays in {_length / _speed:0.00}s", EditorStyles.miniLabel);

            HandleTimelineInput(_rect, _bar, _impactHandle, _slot, _clip, _frameCount);
        }

        private void HandleTimelineInput(Rect _rect, Rect _bar, Rect _impactHandle, Slot _slot, AnimationClip _clip, int _frameCount)
        {
            Event _event = Event.current;

            switch (_event.type)
            {
                case EventType.MouseDown when _event.button == 0 && _slot.move != null && _impactHandle.Contains(_event.mousePosition):
                    m_draggingImpact = true;
                    m_playing = false;
                    _event.Use();
                    break;

                case EventType.MouseDown when _event.button == 0 && _rect.Contains(_event.mousePosition):
                    m_scrubbing = true;
                    m_playing = false;
                    m_time = SnapToFrame(_bar, _event.mousePosition.x, _frameCount) * _clip.length;
                    _event.Use();
                    break;

                case EventType.MouseDrag when m_draggingImpact:
                    float _impact = SnapToFrame(_bar, _event.mousePosition.x, _frameCount);
                    SetMoveFloat(_slot.move, "m_impactTime", _impact);
                    m_time = _impact * _clip.length;
                    _event.Use();
                    break;

                case EventType.MouseDrag when m_scrubbing:
                    m_time = SnapToFrame(_bar, _event.mousePosition.x, _frameCount) * _clip.length;
                    _event.Use();
                    break;

                case EventType.MouseUp when m_draggingImpact || m_scrubbing:
                    m_draggingImpact = false;
                    m_scrubbing = false;
                    _event.Use();
                    break;
            }

            if (_event.type == EventType.Used)
                Repaint();
        }

        private static float SnapToFrame(Rect _bar, float _mouseX, int _frameCount)
        {
            float _normalized = Mathf.Clamp01((_mouseX - _bar.x) / _bar.width);
            return Mathf.Round(_normalized * _frameCount) / _frameCount;
        }

        private static void SetMoveFloat(AnimationMove _move, string _property, float _value)
        {
            SerializedObject _serialized = new SerializedObject(_move);
            _serialized.FindProperty(_property).floatValue = _value;
            _serialized.ApplyModifiedProperties();
        }

        private void DrawDetails(Rect _rect, Slot _slot)
        {
            EditorGUI.DrawRect(_rect, PanelBackground);

            if (_slot == null || m_entity == null || m_entity.AnimationSet == null)
                return;

            GUILayout.BeginArea(new Rect(_rect.x + 10f, _rect.y + 6f, _rect.width - 20f, _rect.height - 12f));

            CharacterAnimationSet _ownSet = m_entity.AnimationSet;
            string _ownership = _slot.owner == null ? "not set anywhere in the chain" : (_slot.owner == _ownSet ? $"set in {_ownSet.name}" : $"inherited from {_slot.owner.name}; assigning here overrides it in {_ownSet.name}");
            EditorGUILayout.LabelField(_ownership, EditorStyles.miniLabel);

            if (_slot.kind == SlotKind.Cast)
                DrawMoveDetails(_slot, _ownSet);
            else
                DrawClipDetails(_slot, _ownSet);

            GUILayout.EndArea();
        }

        private void DrawClipDetails(Slot _slot, CharacterAnimationSet _ownSet)
        {
            AnimationClip _clip = (AnimationClip)EditorGUILayout.ObjectField(_slot.label, _slot.clip, typeof(AnimationClip), false);
            float _speed = Mathf.Max(0.05f, EditorGUILayout.DelayedFloatField(new GUIContent("Speed", "Playback speed of this clip. It belongs with the clip, so changing an inherited slot's speed copies the clip into this character's set too."), _slot.clipSpeed));

            if (_clip == _slot.clip && Mathf.Approximately(_speed, _slot.clipSpeed))
                return;

            SerializedObject _serialized = new SerializedObject(_ownSet);
            _serialized.FindProperty(_slot.clipProperty).objectReferenceValue = _clip;
            _serialized.FindProperty(_slot.SpeedProperty).floatValue = _speed;
            _serialized.ApplyModifiedProperties();
            BuildSlots();
        }

        private void DrawMoveDetails(Slot _slot, CharacterAnimationSet _ownSet)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                AnimationMove _move = (AnimationMove)EditorGUILayout.ObjectField("Move", _slot.move, typeof(AnimationMove), false);

                if (_move != _slot.move)
                    AssignMove(_ownSet, _slot.style, _move);

                if (GUILayout.Button("New move…", GUILayout.Width(90f)))
                    CreateMoveFor(_slot, _ownSet);
            }

            if (_slot.move == null)
            {
                EditorGUILayout.HelpBox($"No {_slot.style} move: cards with this style play their card type's Attack or Utility move. Assign one to give this character its own.", MessageType.None);
                return;
            }

            SerializedObject _serialized = new SerializedObject(_slot.move);
            EditorGUILayout.PropertyField(_serialized.FindProperty("m_clip"));
            EditorGUILayout.Slider(_serialized.FindProperty("m_impactTime"), 0f, 1f, new GUIContent("Impact (normalized)"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("m_speed"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("m_delivery"));
            EditorGUILayout.PropertyField(_serialized.FindProperty("m_projectile"));

            if (_serialized.ApplyModifiedProperties())
                BuildSlots();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Set impact at playhead"))
                    SetMoveFloat(_slot.move, "m_impactTime", Mathf.Clamp01(m_time / _slot.move.Clip.length));

                if (GUILayout.Button("Jump to impact"))
                {
                    m_playing = false;
                    m_time = _slot.move.ImpactTime * _slot.move.Clip.length;
                }

                if (_slot.owner != _ownSet && GUILayout.Button($"Shared by {_slot.owner.name}"))
                    EditorGUIUtility.PingObject(_slot.owner);
            }
        }

        private void AssignMove(CharacterAnimationSet _set, CastStyle _style, AnimationMove _move)
        {
            SerializedObject _serialized = new SerializedObject(_set);
            SerializedProperty _casts = _serialized.FindProperty("m_casts");
            int _index = -1;

            for (int _i = 0; _i < _casts.arraySize; _i++)
            {
                if (_casts.GetArrayElementAtIndex(_i).FindPropertyRelative("style").intValue == (int)_style)
                    _index = _i;
            }

            if (_move == null)
            {
                if (_index >= 0)
                    _casts.DeleteArrayElementAtIndex(_index);
            }
            else
            {
                if (_index < 0)
                {
                    _index = _casts.arraySize;
                    _casts.InsertArrayElementAtIndex(_index);
                }

                SerializedProperty _entry = _casts.GetArrayElementAtIndex(_index);
                _entry.FindPropertyRelative("style").intValue = (int)_style;
                _entry.FindPropertyRelative("move").objectReferenceValue = _move;
            }

            _serialized.ApplyModifiedProperties();
            BuildSlots();
        }

        private void CreateMoveFor(Slot _slot, CharacterAnimationSet _ownSet)
        {
            string _path = EditorUtility.SaveFilePanelInProject("New move", $"Move_{_ownSet.name.Replace("AnimSet_", string.Empty)}_{_slot.style}", "asset", "Where to save the move", MoveFolder);

            if (string.IsNullOrEmpty(_path))
                return;

            AnimationMove _move = CreateInstance<AnimationMove>();
            AssetDatabase.CreateAsset(_move, _path);

            if (_slot.move != null)
            {
                SerializedObject _serialized = new SerializedObject(_move);
                _serialized.FindProperty("m_clip").objectReferenceValue = _slot.move.Clip;
                _serialized.FindProperty("m_impactTime").floatValue = _slot.move.ImpactTime;
                _serialized.FindProperty("m_speed").floatValue = _slot.move.Speed;
                _serialized.FindProperty("m_delivery").intValue = (int)_slot.move.Delivery;
                _serialized.FindProperty("m_projectile").objectReferenceValue = _slot.move.Projectile;
                _serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            AssignMove(_ownSet, _slot.style, _move);
        }

        private void EnsurePreview()
        {
            if (m_preview == null)
            {
                m_preview = new PreviewRenderUtility();
                m_preview.camera.fieldOfView = 30f;
                m_preview.camera.clearFlags = CameraClearFlags.SolidColor;
                m_preview.camera.backgroundColor = PreviewBackground;
            }

            if (m_instance != null)
                return;

            m_instance = m_preview.InstantiatePrefabInScene(m_entity.gameObject);
            m_instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_instanceAnimator = m_instance.GetComponent<EntityAnimationModule>().Animator;

            if (m_instanceAnimator != null)
                m_instanceAnimator.applyRootMotion = false;

            m_bounds = new Bounds(m_instance.transform.position + Vector3.up, Vector3.one);
            bool _hasBounds = false;

            foreach (Renderer _renderer in m_instance.GetComponentsInChildren<Renderer>())
            {
                if (!_hasBounds)
                {
                    m_bounds = _renderer.bounds;
                    _hasBounds = true;
                }
                else
                {
                    m_bounds.Encapsulate(_renderer.bounds);
                }
            }
        }

        private void EnsureGraph(AnimationClip _clip)
        {
            if (m_graph.IsValid() && m_graphClip == _clip)
                return;

            DestroyGraph();

            m_graph = PlayableGraph.Create("AnimationLab");
            m_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            AnimationPlayableOutput _output = AnimationPlayableOutput.Create(m_graph, "AnimationLab", m_instanceAnimator);
            m_clipPlayable = AnimationClipPlayable.Create(m_graph, _clip);
            m_clipPlayable.SetApplyFootIK(false);
            _output.SetSourcePlayable(m_clipPlayable);

            m_graphClip = _clip;
        }

        private void DestroyGraph()
        {
            if (m_graph.IsValid())
                m_graph.Destroy();

            m_graphClip = null;
        }

        private void DestroyPreview()
        {
            DestroyGraph();

            if (m_preview != null)
            {
                m_preview.Cleanup();
                m_preview = null;
            }

            m_instance = null;
            m_instanceAnimator = null;
        }
    }
}
