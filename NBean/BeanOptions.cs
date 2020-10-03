using NBean.Interfaces;

namespace NBean
{
    internal class BeanOptions : IBeanOptions
    {
        /// <summary>
        /// Specifies whether each Bean[column] or Bean.Get<T>(column) call 
        /// will throw ColumnNotFoundException if the column does not exist. Default True
        /// </summary>
        public bool ValidateGetColumns { get; set; } = true;
    }
}
