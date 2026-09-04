using System.Collections.Generic;
using UnityEngine;

namespace Utils.Rendering
{
    [CreateAssetMenu(menuName = "Art Direction/Palette", fileName = "Palette")]
    public class PaletteAsset : ScriptableObject
    {
        public const int MaxColors = 64;

        [SerializeField] private List<Color> _environmentColors = new List<Color>();
        [SerializeField] private Color _playerAccent = new Color(1f, 0.690f, 0.231f, 1f);
        [SerializeField] private Color _enemyAccent = new Color(0.839f, 0.271f, 0.243f, 1f);
        [SerializeField] private Color _interactableAccent = new Color(0.275f, 0.780f, 0.706f, 1f);

        public Color PlayerAccent => _playerAccent;
        public Color EnemyAccent => _enemyAccent;
        public Color InteractableAccent => _interactableAccent;
        public IReadOnlyList<Color> EnvironmentColors => _environmentColors;

        public int FillShaderColors(Vector4[] _buffer)
        {
            int count = 0;

            count = Append(_buffer, count, _playerAccent);
            count = Append(_buffer, count, _enemyAccent);
            count = Append(_buffer, count, _interactableAccent);

            for (int i = 0; i < _environmentColors.Count; i++)
                count = Append(_buffer, count, _environmentColors[i]);

            return count;
        }

        public bool IsGameplayAccent(Color _color, float _tolerance)
        {
            return Matches(_color, _playerAccent, _tolerance)
                || Matches(_color, _enemyAccent, _tolerance)
                || Matches(_color, _interactableAccent, _tolerance);
        }

        public bool ContainsColor(Color _color, float _tolerance)
        {
            if (IsGameplayAccent(_color, _tolerance))
                return true;

            for (int i = 0; i < _environmentColors.Count; i++)
            {
                if (Matches(_color, _environmentColors[i], _tolerance))
                    return true;
            }

            return false;
        }

        public void SetEnvironmentColors(IEnumerable<Color> _colors)
        {
            _environmentColors.Clear();
            _environmentColors.AddRange(_colors);
        }

        public void SetAccents(Color _player, Color _enemy, Color _interactable)
        {
            _playerAccent = _player;
            _enemyAccent = _enemy;
            _interactableAccent = _interactable;
        }

        private static int Append(Vector4[] _buffer, int _count, Color _color)
        {
            if (_count >= MaxColors || _count >= _buffer.Length)
                return _count;

            _buffer[_count] = new Vector4(_color.r, _color.g, _color.b, 1f);
            return _count + 1;
        }

        private static bool Matches(Color _a, Color _b, float _tolerance)
        {
            return Mathf.Abs(_a.r - _b.r) <= _tolerance
                && Mathf.Abs(_a.g - _b.g) <= _tolerance
                && Mathf.Abs(_a.b - _b.b) <= _tolerance;
        }
    }
}
