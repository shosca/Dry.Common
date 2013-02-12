#region using

using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Castle.Core.Logging;
using Castle.MonoRail.Framework;
using Enyim.Caching;
using Enyim.Caching.Memcached;

#endregion

namespace Dry.Common.Cache {
    public class MemcachedCache : ICache, ICacheProvider {
        static MemcachedClient _client;
        const string Region = "monorail";

        ILogger _logger = NullLogger.Instance;
        public ILogger Logger {
            get { return _logger; }
            set { _logger = value; }
        }

        int _expiry = 800;
        public int Expiry {
            get { return _expiry; }
            set { _expiry = value; }
        }


        [ThreadStatic] private static HashAlgorithm _hasher;
        private static HashAlgorithm Hasher {
            get { return _hasher ?? (_hasher = HashAlgorithm.Create()); }
        }

        [ThreadStatic] private static MD5 _md5;
        private static MD5 Md5 {
            get { return _md5 ?? (_md5 = MD5.Create()); }
        }


        public MemcachedCache() {
            if (_client == null) {
                _client = new MemcachedClient();
                _client.FlushAll();
            }
        }

        public void Service(IMonoRailServices provider) { }

        public bool HasKey(string key) {
            return Get(key) == null;
        }

        public object Get(string key) {
            if (key == null) {
                return null;
            }
            Logger.DebugFormat("fetching object {0} from the cache", key);
            var o = _client.Get(KeyAsString(key));
            if (o == null) return null;

            // From NH.MemcacheClient
            //we need to check here that the key that we stored is really the key that we got
            //the reason is that for long keys, we hash the value, and this mean that we may get
            //hash collisions. The chance is very low, but it is better to be safe
            var dictentry = (DictionaryEntry)o;
            return GetAlternateKeyHash(key).Equals(dictentry.Key) ? dictentry.Value : null;
        }

        public T Get<T>(string key) where T : class {
           return Get(key) as T;
        }

        public T Get<T>(string key, Func<T> action) where T : class {
            var o = Get(key) as T;
            if (o == null) {
                o = action();
                Store(key, o);
            }
            return o;
        }

        public void Store(string key, object data) {
            Store(key, data, DateTime.Now.AddSeconds(Expiry));
        }

        public void Store(string key, object data, DateTime expiresAt) {
            Store(key, data, new TimeSpan(expiresAt.Ticks - DateTime.Now.Ticks));
        }

        public void Store(string key, object data, TimeSpan validFor) {
            if (key == null) {
                throw new ArgumentNullException("key", "null key not allowed");
            }
            if (data == null) {
                throw new ArgumentNullException("data", "null value not allowed");
            }
            Logger.DebugFormat("setting value for item {0}", key);
            var returnOk = _client.Store(StoreMode.Set, KeyAsString(key), new DictionaryEntry(GetAlternateKeyHash(key), data), validFor);

            if (!returnOk) {
                Logger.WarnFormat("could not save: {0} => {1}", key, data);
            }
        }

        public void Delete(string key) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            Logger.DebugFormat("removing item {0}", key);
            _client.Remove(KeyAsString(key));
        }

        /// <summary>
        /// Turn the key obj into a string, preperably using human readable
        /// string, and if the string is too long (>=250) it will be hashed
        /// </summary>
        static string KeyAsString(object key) {
            var fullKey = FullKeyAsString(key);
            return fullKey.Length >= 250 ? ComputeHash(fullKey, Hasher) : fullKey.Replace(' ', '-');
        }

        /// <summary>
        /// Turn the key object into a human readable string.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        static string FullKeyAsString(object key) {
            return string.Format("{0}@{1}", Region, (key == null ? string.Empty : key.ToString()));
        }

        /// <summary>
        /// Compute the hash of the full key string using the given hash algorithm
        /// </summary>
        /// <param name="fullKeyString">The full key return by call FullKeyAsString</param>
        /// <param name="hashAlgorithm">The hash algorithm used to hash the key</param>
        /// <returns>The hashed key as a string</returns>
        static string ComputeHash(string fullKeyString, HashAlgorithm hashAlgorithm) {
            var bytes = Encoding.ASCII.GetBytes(fullKeyString);
            var computedHash = hashAlgorithm.ComputeHash(bytes);
            return Convert.ToBase64String(computedHash);
        }

        /// <summary>
        /// Compute an alternate key hash; used as a check that the looked-up value is
        /// in fact what has been put there in the first place.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The alternate key hash (using the MD5 algorithm)</returns>
        string GetAlternateKeyHash(object key) {
            var fullKey = FullKeyAsString(key);
            return fullKey.Length >= 250 ? ComputeHash(fullKey, Md5) : fullKey.Replace(' ', '-');
        }

    }
}
