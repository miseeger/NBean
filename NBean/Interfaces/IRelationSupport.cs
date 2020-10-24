using System.Collections.Generic;

namespace NBean.Interfaces
{
    
    public interface IRelationSupport
    {
        // ----- Relation 1:n (also: 1:1) 
        IList<Bean> GetOwnedList(string ownedKind);
        IList<T> GetOwnedList<T>() where T : Bean, new();
        bool AttachOwned(Bean bean);
        bool AttachOwned(IList<Bean> beans);
        bool DetachOwned(Bean bean, bool trashOwned);
        bool DetachOwned(IList<Bean> beans, bool trashOwned);

        // ----- Relation n:1 (also: 1:1)
        Bean GetOwner(string ownerKind);
        T GetOwner<T>() where T : Bean, new();
        bool AttachOwner(Bean bean);
        bool DetachOwner(string ownerKind, bool trashOwned);
        bool DetachOwner<T>(bool trashOwned) where T : Bean, new();

        // ----- Relation m:n ("Link")
        IList<Bean> GetLinkedList(string kind);
        IList<T> GetLinkedList<T>() where T : Bean, new();
        Dictionary<Bean, Bean> GetLinkedListEx(string kind);
        Dictionary<T, Bean> GetLinkedListEx<T>() where T : Bean, new();
        bool LinkWith(Bean bean, IDictionary<string, object> linkProps);
        bool Unlink(Bean bean);
        bool LinkWith(IList<Bean> beans, IDictionary<string, object> linkProps);
        bool Unlink(IList<Bean> beans);
    }

}
