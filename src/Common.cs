#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Castle.Components.Validator;
using Castle.Core.Internal;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;
using Iesi.Collections.Generic;
using Dry.Common.Cache;

#endregion

namespace Dry.Common {
    public static class CommonExtensions {
        public static decimal ParseDecimal(this object o) {
            return ParseDecimal(o, 0);
        }

        public static decimal ParseDecimal(this object o, decimal d) {
            if (o == null) return d;
            decimal.TryParse(o.ToString(), out d);
            return d;
        }

        public static DateTime ParseDateTime(this object o) {
            return ParseDateTime(o, DateTime.Now.Date);
        }

        public static DateTime ParseDateTime(this object o, DateTime d) {
            if (o == null) return d;
            DateTime.TryParse(o.ToString(), out d);
            return d;
        }

        public static bool IsEmpty(this object val) {
            if (val == null) return false;

            var s = val as string;
            if (s != null) {
                s.HasValue();
            }

            var en = val as IEnumerable;
            if (en != null) {
                return !HasItems(en);
            }

            return true;
        }

        public static bool HasValue(this string val) {
            return !string.IsNullOrEmpty(val);
        }

        public static bool HasItems(this IEnumerable enumerable) {
            return enumerable.GetEnumerator().MoveNext();
        }

        public static IValidatorRegistry ValidatorRegistry = new CachedValidationRegistry();

        public static string WriteToTempFile(this Stream input) {
            var tempfile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
            using (var fs = new FileStream(tempfile, FileMode.CreateNew))
                input.CopyTo(fs);

            return tempfile;
        }

        /// <summary>
        /// Determines whether the <paramref name="genericType"/> is assignable from
        /// <paramref name="givenType"/> taking into account generic definitions
        /// </summary>
        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            return Castle.ActiveRecord.Conventions.IsAssignableToGenericType(givenType, genericType);
        }

        public static Guid GenerateComb() {
            //from nhibernate
            var guidArray = Guid.NewGuid().ToByteArray();

            var baseDate = new DateTime(1900, 1, 1);
            var now = DateTime.Now;

            // Get the days and milliseconds which will be used to build the byte string
            var days = new TimeSpan(now.Ticks - baseDate.Ticks);
            var msecs = now.TimeOfDay;

            // Convert to a byte array
            // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333
            var daysArray = BitConverter.GetBytes(days.Days);
            var msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));

            // Reverse the bytes to match SQL Servers ordering
            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            // Copy the bytes into the guid
            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);
        }


        public static string ToCacheKey(this DictHelper.MonoRailDictionary dict) {
            var list = new System.Collections.Generic.SortedSet<string>();
            foreach(var k in dict.Keys) {
                list.Add(k + "=" + dict[k]);
            }
            return string.Join("&", list);
        }

        public static Iesi.Collections.Generic.ISet<T> ToSet<T>(this IEnumerable<T> list) {
            var set = new HashedSet<T>();
            if (list != null)
                set.AddAll(list.ToList());
            return set;
        }

        public static TValue TryGet<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key ) where TValue : class {
            return !dict.ContainsKey(key) ? null : dict[key];
        }

        public static TValue TryGet<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> func) where TValue : class {
            if (dict.ContainsKey(key))
                return dict[key];

            var o = func();
            if (o != null) {
                try {
                    dict.Add(key, o);
                } catch {
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    throw;
                }
            }
            return o;
        }

        public static TValue TryGet<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> func) where TValue : class {
            if (dict.ContainsKey(key))
                return dict[key];

            var o = func(key);
            if (o != null) {
                dict.Add(key, o);
            }
            return o;
        }

        public static void TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value ) where TValue : class {
            if (value == null) return;
            if (!dict.ContainsKey(key)) {
                dict.Add(key, value);
            } else if (dict[key] == null) {
                dict[key] = value;
            }
        }

        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> dest, IDictionary<TKey, TValue> source) where TValue : class {
            foreach (var key in source.Keys.Where(key => !dest.ContainsKey(key))) {
                dest.Add(key, source[key]);
            }
            return dest;
        }

        public static string ToBase64String(this byte[] bytes) {
            return Convert.ToBase64String(bytes);
        }

        public static string ComputeMD5(this byte[] bytes) {
            return MD5.Create().ComputeHash(bytes).ToBase64String();
        }

        public static string ComputeSHA1(this byte[] bytes) {
            return SHA1.Create().ComputeHash(bytes).ToBase64String();
        }

        public static string ComputeSHA512(this byte[] bytes) {
            return SHA512.Create().ComputeHash(bytes).ToBase64String();
        }

        public static object Get(this ICacheProvider cache, string key, Func<object> source) {
            var o = cache.Get(key);
            if (o == null) {
                o = source.Invoke();
                cache.Store(key, o);
            }
            return o;
        }

        public static object Get(this ICache cache, string key, Func<object> source) {
            return cache.Get(key, 800, source);
        }

        public static object Get(this ICache cache, string key, int duration, Func<object> source) {
            return cache.Get(key, DateTime.Now.AddSeconds(duration), source);
        }

        public static object Get(this ICache cache, string key, DateTime expiresat, Func<object> source) {
            var o = cache.Get(key);
            if (o == null) {
                o = source.Invoke();
                cache.Store(key, o, expiresat);
            }
            return o;
        }

        const double R = 6371; // km
        public static double Distance(double lat1, double lon1, double lat2, double lon2) {
            var dLat = (lat2-lat1).ToRad();
            var dLon = (lon2 - lon1).ToRad();
            var a = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
                    Math.Cos(lat1.ToRad()) * Math.Cos(lat2.ToRad()) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        public static double ToRad(this double deg) {
            return (deg * Math.PI / 180.0);
        }

        public static double ToDeg(this double rad) {
            return (rad / Math.PI * 180.0);
        }

        public static void Upsert<T, TK>(this IDictionary<T, TK> dict, T key, TK val) {
            if (dict.ContainsKey(key)) {
                dict[key] = val;
            } else {
                dict.Add(key, val);
            }
        }

        public static TK GetOrDefault<T, TK>(this IDictionary<T, TK> dict, T key, TK def) {
            return dict.ContainsKey(key) ? dict[key] : def;
        }

        public static string ToTitleCase(this string str) {
            return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(str);
        }

        public static string ToSentenceCase(this string str) {
            return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]));
        }

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> source, int size) {
            T[] array = null;
            int count = 0;

            foreach (T item in source) {
                if (array == null) array = new T[size];

                array[count] = item;
                count++;

                if (count == size) {
                    yield return new ReadOnlyCollection<T>(array);
                    array = null;
                    count = 0;
                }
            }
            if (array != null) {
                Array.Resize(ref array, count);
                yield return new ReadOnlyCollection<T>(array);
            }
        }

        internal static readonly long InitialJavaScriptDateTicks = 621355968000000000;

        public static long ToTimestamp(this DateTime value) {
            return ConvertDateTimeToJavaScriptTicks(value);
        }

        static TimeSpan GetUtcOffset(DateTime dateTime) {
            return TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
        }

        static long ToUniversalTicks(DateTime dateTime) {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime.Ticks;

            return ToUniversalTicks(dateTime, GetUtcOffset(dateTime));
        }

        static long ToUniversalTicks(DateTime dateTime, TimeSpan offset) {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime.Ticks;

            long ticks = dateTime.Ticks - offset.Ticks;
            if (ticks > 3155378975999999999L)
                return 3155378975999999999L;

            if (ticks < 0L)
                return 0L;

            return ticks;
        }

        internal static long ConvertDateTimeToJavaScriptTicks(DateTime dateTime) {
            return ConvertDateTimeToJavaScriptTicks(dateTime, true);
        }

        internal static long ConvertDateTimeToJavaScriptTicks(DateTime dateTime, bool convertToUtc) {
            long ticks = (convertToUtc) ? ToUniversalTicks(dateTime) : dateTime.Ticks;
            return UniversialTicksToJavaScriptTicks(ticks);
        }

        static long UniversialTicksToJavaScriptTicks(long universialTicks) {
            long javaScriptTicks = (universialTicks - InitialJavaScriptDateTicks) / 10000;
            return javaScriptTicks;
        }

        public static string SaveTemporaryFile(this HttpPostedFile postedfile) {
            var tmpdir = Path.GetTempPath();
            var tmpfilename = Path.Combine(tmpdir, Guid.NewGuid() + Path.GetExtension(postedfile.FileName));
            postedfile.SaveAs(tmpfilename);
            return tmpfilename;
        }

        public static IEnumerable<string> SplitAndTrim(this string s, params string[] delimiters) {
            return SplitAndTrim(s, StringSplitOptions.RemoveEmptyEntries, delimiters);
        }

        public static IEnumerable<string> SplitAndTrim(this string s, StringSplitOptions options, params string[] delimiters) {
            if (s == null) {
                return new string[] {};
            }
            var query = s.Split(delimiters, StringSplitOptions.None).Select(x => x.Trim());
            if (options == StringSplitOptions.RemoveEmptyEntries) {
                query = query.Where(x => x.Trim() != string.Empty);
            }
            return query.ToList();
        }

        public static IEnumerable<T> GetInstanceOf<T>(this Assembly a) {
            var types = a.GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract && typeof (T).IsAssignableFrom(t))
                .Select(t => (T) Activator.CreateInstance(t));
            return types.ToArray();
        }

        public static string Join(this string[] items, string seperator) {
            return string.Join(seperator, items);
        }

        public static string Join(this IEnumerable<string> items, string seperator) {
            return string.Join(seperator, items);
        }

        public static ICollection<T> AddRange<T>(this ICollection<T> enumeration, IEnumerable<T> elements) {
            if (elements == null) return enumeration;
            foreach (var item in elements) {
                enumeration.Add(item);
            }
            return enumeration;
        }

        public static IEnumerable<T> ForAll<T>(this IEnumerable<T> enumeration, Action<T> action) {
            enumeration.ForEach(action);
            return enumeration;
        }

        public static T GetAttr<T>(this Assembly assembly) {
            return GetAttr<T>(assembly, false);
        }

        public static T GetAttr<T>(this Assembly assembly, bool inherit) {
            return (T) assembly.GetCustomAttributes(typeof (T), inherit).FirstOrDefault();
        }

        public static T GetAttr<T>(this Type type) {
            return GetAttr<T>(type, false);
        }

        public static T GetAttr<T>(this Type type, bool inherit) {
            return (T) type.GetCustomAttributes(typeof (T), inherit).FirstOrDefault();
        }

        public static T[] GetAttrs<T>(this Assembly assembly) {
            return GetAttrs<T>(assembly, false);
        }

        public static T[] GetAttrs<T>(this Assembly assembly, bool inherit) {
            return assembly.GetCustomAttributes(typeof (T), inherit) as T[];
        }

        public static T[] GetAttrs<T>(this Type type) {
            return GetAttrs<T>(type, false);
        }

        public static T[] GetAttrs<T>(this Type type, bool inherit) {
            return type.GetCustomAttributes(typeof (T), inherit) as T[];
        }

        public static T GetAttr<T>(this MethodInfo action) {
            return GetAttr<T>(action, false);
        }

        public static T GetAttr<T>(this MethodInfo action, bool inherit) {
            return (T) action.GetCustomAttributes(typeof (T), inherit).FirstOrDefault();
        }

        public static T[] GetAttrs<T>(this MethodInfo action) {
            return GetAttrs<T>(action, false);
        }

        public static T[] GetAttrs<T>(this MethodInfo action, bool inherit) {
            return action.GetCustomAttributes(typeof (T), inherit) as T[];
        }

        public static T GetAttr<T>(this PropertyInfo prop) {
            return GetAttr<T>(prop, false);
        }

        public static T GetAttr<T>(this PropertyInfo prop, bool inherit) {
            return (T) prop.GetCustomAttributes(typeof (T), inherit).FirstOrDefault();
        }

        public static T[] GetAttrs<T>(this PropertyInfo prop) {
            return GetAttrs<T>(prop, false);
        }

        public static T[] GetAttrs<T>(this PropertyInfo prop, bool inherit) {
            return prop.GetCustomAttributes(typeof (T), inherit) as T[];
        }
    }
}
