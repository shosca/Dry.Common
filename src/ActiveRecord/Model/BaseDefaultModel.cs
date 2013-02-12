#region using

using NHibernate.Search.Attributes;

#endregion

namespace Dry.Common.ActiveRecord.Model {
    public class BaseDefaultModel<T> : BaseModel<T> where T : class {
        [DocumentId]
        public virtual long Id { get; set; }
    }
}
