namespace Stats
{
    public class EntityStatModule : EntityModule
    {
        protected override void OnInitialize()
        {
            base.OnInitialize();

            StatManager.Instance.RegisterInstance(gameObject, Owner.EntityType);
        }

        private void OnDestroy()
        {
            if (StatManager.Instance != null)
                StatManager.Instance.UnregisterInstance(gameObject);
        }

        public float GetValue(StatType type)         => StatManager.Instance.GetInstanceValue(gameObject, type);
        public void AddModifier(StatModifier mod)    => StatManager.Instance.AddInstanceModifier(gameObject, mod);
        public void RemoveModifier(StatModifier mod) => StatManager.Instance.RemoveInstanceModifier(gameObject, mod);
    }
}
