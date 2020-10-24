using System.Collections.Generic;

namespace NBean.Interfaces
{
    
    public interface IRelationSupport
    {
        // ----- Relation 1:n (1:1) 
        IList<Bean> GetOwnedList(string ownedKind);
        bool AttachOwned(Bean bean);
        bool AttachOwned(IList<Bean> beans);
        bool DetachOwned(Bean bean, bool trashOwned);
        bool DetachOwned(IList<Bean> beans, bool trashOwned);

        // ----- Relation n:1 (1:1)
        Bean GetOwner(string ownerKind);
        bool AttachOwner(Bean bean);
        bool DetachOwner(string ownerKind, bool trashOwned);

        // ----- Relation m:n ("Link")
        Dictionary<Bean, Bean> GetLinkedListEx(string kind);
        IList<Bean> GetLinkedList(string kind);
        //bool LinkWith(Bean bean);
        bool LinkWith(Bean bean, IDictionary<string, object> linkProps);
        bool Unlink(Bean bean);
        //bool LinkWith(IList<Bean> beans);
        bool LinkWith(IList<Bean> beans, IDictionary<string, object> linkProps);
        bool Unlink(IList<Bean> beans);
    }

}
