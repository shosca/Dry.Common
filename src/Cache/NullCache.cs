#region using

using System;

#endregion

namespace Dry.Common.Cache {
    public class NullCache : ICache {
        public static ICache Instance = new NullCache();
        public bool HasKey(string key) { return false; }
        public object Get(string key) { return null; }
        public T Get<T>(string key) where T : class { return null; }
        public T Get<T>(string key, Func<T> action) where T : class { return null; }
        public void Store(string key, object data) { }
        public void Store(string key, object data, DateTime expiresAt) { }
        public void Store(string key, object data, TimeSpan walidFor) { }
        public void Delete(string key) { }
    }
}
