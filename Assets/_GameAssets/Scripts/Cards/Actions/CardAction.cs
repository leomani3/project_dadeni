using System;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public abstract class CardAction
    {
        public virtual string ActionName => GetType().Name;

        public abstract Type DefinitionType { get; }
        protected abstract float Magnitude { get; }

        public float Weight
        {
            get
            {
                CardActionDefinition[] _definitions = GameAssets.Instance?.cardActionDefinitions;
                if (_definitions == null)
                    return 0f;

                foreach (CardActionDefinition _candidate in _definitions)
                {
                    if (_candidate != null && _candidate.GetType() == DefinitionType)
                        return _candidate.WeightPerUnit * Magnitude;
                }

                return 0f;
            }
        }
    }
}
