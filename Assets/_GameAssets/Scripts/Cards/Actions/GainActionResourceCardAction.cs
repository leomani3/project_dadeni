using System;
using Deckbuilder.ActionResources;
using UnityEngine;

namespace Deckbuilder.Cards.Actions
{
    [Serializable]
    public class GainActionResourceCardAction : EntityCardAction
    {
        [SerializeField] private ActionResourceConfig m_resource;
        [SerializeField] private int m_amount = 1;

        public ActionResourceConfig Resource => m_resource;
        public int Amount => m_amount;

        public override string DescriptionFormat => "Gain {0} {1}";
        public override object[] DescriptionValues => new object[] { m_amount, m_resource.DisplayName };

        protected override void ApplyTo(Entity _entity, CardActionContext _context)
        {
            if (_entity.TryGetModule(out EntityActionResourceModule _resources) && _resources.Has(m_resource))
                _resources.Gain(m_resource, m_amount);
        }
    }
}
