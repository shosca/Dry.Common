using System;

namespace Dry.Common.Model {
    public interface IModifyAuditable<T> where T : class {
        DateTime ModifiedAt { get; set; }
        T ModifiedBy { get; set; }
    }
}