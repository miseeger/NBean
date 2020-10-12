using System.Collections.Generic;
using System.Data;
using System.Linq;
using NBean.Interfaces;

namespace NBean
{
    internal class Auditor : BeanObserver
    {
        private readonly Dictionary<string, object> _changes;

        public Auditor(IBeanApi api)
        {
            var exitFluidMode = false;

            if ((api.Database == string.Empty && api.Connection.State != ConnectionState.Open)
                || api.IsKnownKind("AUDIT"))
                return;

            if (!api.IsFluidMode())
            {
                api.EnterFluidMode();
                exitFluidMode = true;
            }

            var audit = api.Dispense("Audit");
            audit.Put("Action", new string('X', 16))
                .Put("Object", new string('X', 64))
                .Put("Table", new string('X', 64))
                .Put("ObjectId", new string('X', 64))
                .Put("Property", new string('X', 64))
                .Put("PropertyType", new string('X', 64))
                .Put("OldValue", new string('X', 1024))
                .Put("NewValue", new string('X', 1024))
                .Put("Note", new string('X', 4096));
            api.Store(audit);

            if (exitFluidMode)
                api.ExitFluidMode();

            _changes = new Dictionary<string, object>();
        }


        private void GatherChanges(Bean bean)
        {
            _changes.Clear();

            // TODO: Gather dirtyNames with their values and types
            //       to store them in the audit table after successfully
            //       storing the data
        }


        private void AuditChanges(string action, Bean bean)
        {
            if (!_changes.Any()) 
                return;

            if (bean.Api.AuditChanges || (!bean.Api.AuditChanges && bean.AuditChanges))
            {
                // TODO: Store changes from Changes property to Audit table
            }
            
        }


        public override void BeforeStore(Bean bean)
        {
            GatherChanges(bean);
        }


        public override void AfterInsert(Bean bean)
        {
            AuditChanges("INSERT", bean);
        }


        public override void AfterUpdate(Bean bean)
        {
            AuditChanges("UPDATE", bean);
        }


        public override void AfterTrash(Bean bean)
        {
            AuditChanges("DELETE", bean);
        }

    }
}