using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deckbuilder.EditorTools
{
    public static class DebugScenario
    {
        public static string State()
        {
            StringBuilder _builder = new StringBuilder();

            _builder.Append("playing=").Append(Application.isPlaying);

            List<string> _scenes = new List<string>();
            for (int _i = 0; _i < SceneManager.sceneCount; _i++)
                _scenes.Add(SceneManager.GetSceneAt(_i).name);

            _builder.Append(" scenes=[").Append(string.Join(",", _scenes)).Append(']');

            if (!Application.isPlaying)
                return _builder.ToString();

            _builder.Append(" seed=").Append(RunRandom.IsInitialized ? RunRandom.SeedString : "-");

            RunManager _runManager = Object.FindFirstObjectByType<RunManager>();

            if (_runManager == null || !_runManager.IsRunning)
                return _builder.Append(" run=none").ToString();

            _builder.Append(" run=").Append(_runManager.CurrentRun.phase);
            _builder.Append(" room=").Append(_runManager.CurrentRoomIndex);
            _builder.Append(" roomType=").Append(_runManager.CurrentMap != null ? _runManager.CurrentMap.GetRoomType(_runManager.CurrentRoomIndex).ToString() : "-");
            _builder.Append(" roomReady=").Append(_runManager.CurrentRoom != null);
            _builder.Append(" enemies=").Append(_runManager.CurrentEnemyGroup != null ? _runManager.CurrentEnemyGroup.Count : 0);

            return _builder.ToString();
        }

        public static string StartRun(string _seed = "DEBUG", string _characterId = null)
        {
            if (!Application.isPlaying)
                return "Not in play mode.";

            RunManager _runManager = Object.FindFirstObjectByType<RunManager>();

            if (_runManager == null)
                return "No RunManager in the loaded scenes.";

            CharacterConfig _character = FindCharacter(_characterId);

            if (_character == null)
                return "No CharacterConfig asset found in the project.";

            _runManager.StartNewRun(_character, _seed);

            return $"StartNewRun({_character.Id}, \"{_seed}\") called. Poll State() until room is ready.";
        }

        public static string ListCharacters()
        {
            List<string> _ids = new List<string>();

            foreach (string _guid in AssetDatabase.FindAssets("t:CharacterConfig"))
            {
                CharacterConfig _character = AssetDatabase.LoadAssetAtPath<CharacterConfig>(AssetDatabase.GUIDToAssetPath(_guid));

                if (_character != null)
                    _ids.Add(_character.Id);
            }

            return _ids.Count > 0 ? string.Join(", ", _ids) : "(none)";
        }

        private static CharacterConfig FindCharacter(string _characterId)
        {
            CharacterConfig _fallback = null;

            foreach (string _guid in AssetDatabase.FindAssets("t:CharacterConfig"))
            {
                CharacterConfig _character = AssetDatabase.LoadAssetAtPath<CharacterConfig>(AssetDatabase.GUIDToAssetPath(_guid));

                if (_character == null)
                    continue;

                if (string.IsNullOrEmpty(_characterId) || _character.Id == _characterId)
                    return _character;

                _fallback ??= _character;
            }

            return _fallback;
        }
    }
}
