#region using

using System;
using NHibernate.Search.Attributes;

#endregion

namespace Dry.Common.ActiveRecord.Model {
    public class BaseGuidModel<T> : BaseModel<T> where T : class {
        [DocumentId]
        public virtual Guid Id { get; set; }
    }
}
