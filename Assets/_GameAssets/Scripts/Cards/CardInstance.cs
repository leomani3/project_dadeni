using System.Collections.Generic;
using Deckbuilder.ActionResources;
using Deckbuilder.Cards.Actions;

namespace Deckbuilder.Cards
{
    public class CardInstance
    {
        public CardConfig Config { get; }
        public bool IsUpgraded { get; private set; }

        public string Title => Config.Title;
        public CardType Type => Config.Type;
        public int Cost => Config.GetCost(IsUpgraded);
        public ActionResourceConfig CostResource => Config.GetCostResource(IsUpgraded);
        public bool Exhausts => Config.GetExhausts(IsUpgraded);
        public ZoneDefinition TargetZone => Config.GetTargetZone(IsUpgraded);
        public bool RequiresLineOfSight => Config.GetRequiresLineOfSight(IsUpgraded);
        public ZoneDefinition EffectZone => Config.GetEffectZone(IsUpgraded);
        public IReadOnlyList<CardAction> Actions => Config.GetActions(IsUpgraded);

        public CardInstance(CardConfig _config, bool _isUpgraded)
        {
            Config = _config;
            IsUpgraded = _isUpgraded;
        }

        public void Upgrade()
        {
            IsUpgraded = true;
        }

        public override string ToString()
        {
            return IsUpgraded ? Title + "+" : Title;
        }
    }
}
