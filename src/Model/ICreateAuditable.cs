using System;

namespace Dry.Common.Model {
    public interface ICreateAuditable<T> where T : class {
        DateTime CreatedAt { get; set; }
        T CreatedBy { get; set; }
    }
}