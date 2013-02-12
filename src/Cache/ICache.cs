#region using

using System;

#endregion

namespace Dry.Common.Cache {
    public interface ICache {
        bool HasKey(string key);
        object Get(string key);
        T Get<T>(string key) where T : class;
        T Get<T>(string key, Func<T> action) where T : class;
        void Store(string key, object data);
        void Store(string key, object data, DateTime expiresAt);
        void Store(string key, object data, TimeSpan validFor);
        void Delete(string key);
    }
}
