﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace NBean
{
    using Dict = Dictionary<string, object>;

    internal class Auditor : BeanObserver
    {

        public Auditor(BeanApi api)
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

            var audit = api.Dispense("AUDIT");

            audit.Put("Action", new string('X', 16))
                .Put("User", new string('X', 64))
                .Put("Object", new string('X', 64))
                .Put("ObjectId", new string('X', 64))
                .Put("Property", new string('X', 64))
                .Put("PropertyType", new string('X', 64))
                .Put("OldValue", new string('X', 1024))
                .Put("NewValue", new string('X', 1024))
                .Put("Notes", new string('X', 4096));
            api.Store(audit);

            if (exitFluidMode)
                api.ExitFluidMode();
        }


        private Dict GetChanges(Bean bean)
        {
            var changes = new Dict();

            var dirtyNames = bean.GetDirtyNames();

            if (dirtyNames.Any())
            {
                foreach (var dirtyKey in bean.GetDirtyNames())
                {
                    changes[dirtyKey] = bean[dirtyKey];
                }
            }

            return changes;
        }


        private void AuditChanges(string action, Bean bean)
        {
            if (bean.GetKind().ToUpper() == "AUDIT")
                return;

            var dirtyBackup = bean.GetDirtyBackup();
            var changes = GetChanges(bean);

            if (action != "DELETE" && !changes.Any())
                return;

            var api = bean.Api;

            if (!api.AuditChanges && (api.AuditChanges || !bean.AuditChanges))
                return;

            var kind = bean.GetKind();
            var keyName = api.GetKeyName(kind);

            if ("INSERT|UPDATE".Contains(action))
            {
                foreach (var change in changes)
                {
                    var audit = api.Dispense("AUDIT");

                    audit
                        .Put("AuditDate", DateTime.Now)
                        .Put("Action", action)
                        .Put("User", api.CurrentUser ?? string.Empty)
                        .Put("Object", bean.GetKind())
                        .Put("ObjectId", bean[keyName])
                        .Put("Property", change.Key)
                        .Put("PropertyType", api.GetDbTypeFromValue(change.Value))
                        .Put("OldValue", action == "UPDATE" ? dirtyBackup[change.Key].FormatValueToString() : string.Empty)
                        .Put("NewValue", bean[change.Key].FormatValueToString())
                        .Put("Notes", string.Empty)
                        .Store();
                }
            }
            
            else if (action == "DELETE")
            {
                var audit = api.Dispense("AUDIT");

                audit
                    .Put("AuditDate", DateTime.Now)
                    .Put("Action", action)
                    .Put("User", api.CurrentUser ?? string.Empty)
                    .Put("Object", bean.GetKind())
                    .Put("ObjectId", bean[keyName])
                    .Put("Property", string.Empty)
                    .Put("PropertyType", string.Empty)
                    .Put("OldValue", string.Empty)
                    .Put("NewValue", string.Empty)
                    .Put("Notes", api.ToJson(bean));

                api.Store(audit);
            }

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