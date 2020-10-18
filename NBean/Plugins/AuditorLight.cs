using System;

namespace NBean.Plugins
{

    public class AuditorLight : BeanObserver
    {
        private void AuditChanges(string action, Bean bean)
        {
            var api = bean.Api;
            var columns = api.GetKindColumns(bean.GetKind());

            if (columns.Count == 0)
                return;

            if (action == "INSERT")
            {
                if (columns.Contains("CreatedBy"))
                    bean["CreatedBy"] = api.CurrentUser ?? string.Empty;

                if (columns.Contains("CreatedAt"))
                    bean["CreatedAt"] = DateTime.Now;
            }

            if (columns.Contains("ChangedBy"))
                bean["ChangedBy"] = api.CurrentUser ?? string.Empty;

            if (columns.Contains("ChangedAt"))
                bean["ChangedAt"] = DateTime.Now;
        }


        public override void BeforeInsert(Bean bean)
        {
            AuditChanges("INSERT", bean);
        }


        public override void BeforeUpdate(Bean bean)
        {
            AuditChanges("UPDATE", bean);
        }
    }

}
