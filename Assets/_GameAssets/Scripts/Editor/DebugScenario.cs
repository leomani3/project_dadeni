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

            GameFlow _gameFlow = Object.FindFirstObjectByType<GameFlow>();

            _builder.Append(" flow=").Append(_gameFlow != null ? _gameFlow.State.ToString() : "-");

            Deckbuilder.Combat.CombatManager _combatManager = Object.FindFirstObjectByType<Deckbuilder.Combat.CombatManager>();

            if (_combatManager != null)
            {
                _builder.Append(" combat=").Append(_combatManager.Phase);
                _builder.Append(" active=").Append(_combatManager.ActiveEntity != null ? _combatManager.ActiveEntity.name : "-");
                _builder.Append(" aliveEnemies=").Append(_combatManager.Enemies.Count);
                _builder.Append(" hand=").Append(_combatManager.Deck.Hand.Count);
                _builder.Append(" draw=").Append(_combatManager.Deck.DrawPile.Count);
                _builder.Append(" discard=").Append(_combatManager.Deck.DiscardPile.Count);
            }

            RunManager _runManager = Object.FindFirstObjectByType<RunManager>();

            if (_runManager == null || !_runManager.IsRunning)
                return _builder.Append(" run=none").ToString();

            _builder.Append(" run=").Append(_runManager.CurrentRun.phase);
            _builder.Append(" room=").Append(_runManager.CurrentRoomIndex);
            _builder.Append(" roomType=").Append(_runManager.CurrentMap != null ? _runManager.CurrentMap.GetRoomType(_runManager.CurrentRoomIndex).ToString() : "-");
            _builder.Append(" roomReady=").Append(_runManager.CurrentRoom != null);
            _builder.Append(" enemies=").Append(_gameFlow.CombatEnemyPrefabs.Count);

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

        public static string StartSandboxCombat()
        {
            if (!Application.isPlaying)
                return "Not in play mode.";

            GameFlow _gameFlow = Object.FindFirstObjectByType<GameFlow>();

            if (_gameFlow == null)
                return "No GameFlow in the loaded scenes.";

            if (!_gameFlow.IsInSandboxCombat)
                return $"Game flow is in {_gameFlow.State}, sandbox combat can only be restarted from itself.";

            Deckbuilder.Combat.CombatManager _combatManager = Object.FindFirstObjectByType<Deckbuilder.Combat.CombatManager>();

            if (_combatManager != null && _combatManager.IsCombatRunning)
                _combatManager.EndCombat();
            else
                _gameFlow.RestartSandboxCombat();

            return $"RestartSandboxCombat() called on {CombatSandbox.Instance.ToDebugString()}. Poll State() until combat=Placement.";
        }

        public static string Sandbox()
        {
            return CombatSandbox.Instance.ToDebugString();
        }

        public static string ListEnemyPrefabs()
        {
            List<string> _names = new List<string>();

            foreach (string _guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_GameAssets/Prefabs/Entities" }))
                _names.Add(AssetDatabase.GUIDToAssetPath(_guid));

            return _names.Count > 0 ? string.Join(", ", _names) : "(none)";
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
