using System.Collections;

namespace Deckbuilder.Actions
{
    public interface IEntityAction
    {
        IEnumerator Execute();
    }
}
