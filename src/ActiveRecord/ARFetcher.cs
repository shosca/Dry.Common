using System.Collections.Specialized;
using Dry.Common.Monorail;
using NHibernate.Type;

namespace Dry.Common.ActiveRecord
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using NHibernate;
    using NHibernate.Criterion;

    using Castle.ActiveRecord;
    using Castle.Components.Binder;

    public class ARFetcher
    {
        private readonly IConverter _converter;

        public ARFetcher(IConverter converter)
        {
            _converter = converter;
        }

        public object FetchActiveRecord(ParameterInfo param,
            ARFetchAttribute attr,
            NameValueCollection dict,
            IDictionary<string, object> customActionParameters) {

            var type = param.ParameterType;

            var isArray = type.IsArray;

            if (isArray) type = type.GetElementType();

            var model = AR.Holder.GetModel(type);

            if (model == null) {
                throw new ActiveRecordException(String.Format("'{0}' is not an ActiveRecord " +
                    "class. It could not be bound.", type.Name));
            }

            var webParamName = attr.RequestParameterName ?? param.Name;

            if (!isArray) {
                var value = GetParameterValue(webParamName, customActionParameters, dict);
                return LoadActiveRecord(type, value, attr, model);
            }

            var pks = GetParameterValues(webParamName, customActionParameters, dict);

            var objs = Array.CreateInstance(type, pks.Length);

            for(var i = 0; i < objs.Length; i++) {
                objs.SetValue(LoadActiveRecord(type, pks[i], attr, model), i);
            }

            return objs;
        }

        private static object[] GetParameterValues(string webParamName, IDictionary<string, object> customActionParameters, NameValueCollection dict) {
            object tmp;
            object[] pks;

            if (customActionParameters.TryGetValue(webParamName, out tmp) == false || (tmp is Array) == false) {
                pks = dict.GetValues(webParamName);
            } else {
                pks = (object[]) tmp;
            }

            return pks ?? (pks = new object[0]);
        }

        private static string GetParameterValue(string webParamName, IDictionary<string, object> customActionParameters, NameValueCollection dict) {
            string value;
            object tmp;
            if (customActionParameters.TryGetValue(webParamName, out tmp) == false || tmp == null) {
                value = dict[webParamName];
            } else {
                value = tmp.ToString();
            }
            return value;
        }

        private object LoadActiveRecord(Type type, object pk, ARFetchAttribute attr, Castle.ActiveRecord.Model model)
        {
            object instance = null;

            if (pk != null && !String.Empty.Equals(pk)) {
                var pkModel = ObtainPrimaryKey(model);

                var pkType = pkModel.Value.ReturnedClass;

                bool conversionSucceeded;
                var convertedPk = _converter.Convert(pkType, pk.GetType(), pk, out conversionSucceeded);

                if (!conversionSucceeded) {
                    throw new ActiveRecordException(string.Format("ARFetcher could not convert PK {0} to type {1}", pk, pkType));
                }

                if (string.IsNullOrEmpty(attr.Eager)) {
                    // simple load
                    instance = attr.Required
                                ? AR.Find(type, convertedPk)
                                : AR.Peek(type, convertedPk);
                } else {
                    // load using eager fetching of lazy collections
                    var criteria = DetachedCriteria.For(type);
                    criteria.Add(Expression.Eq(pkModel.Key, convertedPk));
                    foreach (var associationToEagerFetch in attr.Eager.Split(',')) {
                        var clean = associationToEagerFetch.Trim();
                        if (clean.Length == 0)
                        {
                            continue;
                        }

                        criteria.SetFetchMode(clean, FetchMode.Eager);
                    }

                    var result = AR.Execute(type, s => criteria.GetExecutableCriteria(s).List());
                    if (result.Count > 0)
                        instance = result[0];
                }
            }

            if (instance == null && attr.Create)
            {
                instance = Activator.CreateInstance(type);
            }

            return instance;
        }

        private static KeyValuePair<string, IType> ObtainPrimaryKey(Castle.ActiveRecord.Model model) {
            if (!model.Metadata.HasIdentifierProperty && model.Metadata.IsInherited) {
                var parentmodel = AR.Holder.GetModel(model.Type.BaseType);
                return ObtainPrimaryKey(parentmodel);
            }

            return model.PrimaryKey;
        }
    }
}
