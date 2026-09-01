namespace Utils.Playable
{
    [System.Flags]
    public enum PlayFlags
    {
        None = 0,
        OnEnable = 1 << 0,  // 1
        OnStart = 1 << 1,   // 2
        Manual = 1 << 2,    // 4
        Click = 1 << 3,     // 8
        Production = 1 << 4, // 16
        Place = 1 << 5,      // 32
        

        // Combos utiles (optionnel)
        All = OnEnable | OnStart | Manual | Click | Production | Place       // 63
    }

    public interface IPlayable
    {
        public PlayFlags PlayFlags => PlayFlags.Manual; // Par défaut, les Playables sont manuels. Overridez cette propriété pour changer le comportement d'auto-play.
        // Cette méthode sera appelée lorsque l'objet est interagi avec
        void Play();
    }
}