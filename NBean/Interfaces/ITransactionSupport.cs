using System;
using System.Data;

namespace NBean.Interfaces
{
    public interface ITransactionSupport
    {
        bool ImplicitTransactions { get; set; }
        bool InTransaction { get; }
        IsolationLevel TransactionIsolation { get; set; }
        void Transaction(Func<bool> action);
    }
}
