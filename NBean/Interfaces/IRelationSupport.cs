using System.Collections.Generic;

namespace NBean.Interfaces
{
    
    public interface IRelationSupport
    {
        // ----- Relation 1:n (also: 1:1) 
        IList<Bean> GetOwnedList(string ownedKind, string fkAlias = "");
        IList<T> GetOwnedList<T>(string fkAlias = "") where T : Bean, new();
        bool AttachOwned(Bean bean, string fkAlias = "");
        bool AttachOwned(IList<Bean> beans, string fkAlias = "");
        bool DetachOwned(Bean bean, bool trashOwned, string fkAlias = "");
        bool DetachOwned(IList<Bean> beans, bool trashOwned, string fkAlias = "");

        // ----- Relation n:1 (also: 1:1)
        Bean GetOwner(string ownerKind, string fkAlias = "");
        T GetOwner<T>(string fkAlias = "") where T : Bean, new();
        bool AttachOwner(Bean bean, string fkAlias = "");
        bool DetachOwner(string ownerKind, bool trashOwned, string fkAlias = "");
        bool DetachOwner<T>(bool trashOwned, string fkAlias = "") where T : Bean, new();

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
