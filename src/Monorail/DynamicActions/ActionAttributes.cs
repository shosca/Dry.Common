#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.MonoRail.Framework;
using Dry.Common.Monorail.Helpers;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class DefaultValueAttribute : Attribute, IParameterBinder {
        readonly object _value;

        public DefaultValueAttribute(object value) {
            _value = value;
        }

        public int CalculateParamPoints(IEngineContext context, IController controller, IControllerContext controllerContext,
                                        ParameterInfo parameterInfo) {
            var token = context.Request[parameterInfo.Name];
            if (CanConvert(parameterInfo.ParameterType, token))
                return 10;
            return 0;
        }

        static bool CanConvert(Type targetType, string token) {
            if (token == null)
                return false;

            try {
                Convert.ChangeType(token, targetType);
                return true;
            }
            catch (FormatException) {
                return false;
            }
        }

        public object Bind(IEngineContext context, IController controller, IControllerContext controllerContext,
                           ParameterInfo parameterInfo) {
            var token = context.Request[parameterInfo.Name];
            var type = parameterInfo.ParameterType;
            if (CanConvert(type, token))
                return Convert.ChangeType(token, type);
            return _value;
        }
    }

    public class QueryFilter {
        public QueryFilter() {}
        public QueryFilter(string propname, string val) {
            PropertyName = propname;
            Value = val;
        }
        public string PropertyName { get; set; }
        public string Value { get; set; }
        public bool ApplyOnSave { get; set; }
        public bool AlwaysOverwrite { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class QueryFilterAttribute : Attribute {
        public QueryFilter QueryFilter { get; set; }

        public QueryFilterAttribute(string property, string value, bool applyonsave, bool alwaysoverwrite) {
            QueryFilter = new QueryFilter {
                PropertyName = property,
                Value = value,
                ApplyOnSave = applyonsave,
                AlwaysOverwrite = alwaysoverwrite
            };
        }

        public QueryFilterAttribute(string property, object value, bool applyonsave, bool alwaysoverwrite) : this(property, value.ToString(), applyonsave, alwaysoverwrite) { }
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class QueryFilterFromRouteAttribute : QueryFilterAttribute, IContextAware {
        public QueryFilterFromRouteAttribute(string propertyname, string parameterkey, bool alwaysoverwrite) : base(propertyname, parameterkey, true, true) {
            ParameterKey = parameterkey;
        }

        public string ParameterKey { get; private set; }

        public void SetContext(IEngineContext context) {
            QueryFilter.Value = context.GetRouteParameter(ParameterKey);
        }
    }

    public class OrderBy {
        public OrderBy() {}
        public OrderBy(string prop, bool asc) {
            Prop = prop;
            Ascending = asc;
        }
        public string Prop { get; set; }

        public bool Ascending { get; set; }

        public int Order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class QueryOrderByAttribute : Attribute {
        public OrderBy OrderBy { get; private set; }

        public QueryOrderByAttribute() {}

        public QueryOrderByAttribute(string propertyName, bool ascending) : this(propertyName, ascending, 1) {}

        public QueryOrderByAttribute(string propertyName, bool ascending, int order) {
            OrderBy = new OrderBy {
                Prop = propertyName,
                Ascending = ascending,
                Order = order,
            };
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PageSizeAttribute : Attribute {
        public PageSizeAttribute() {}

        public PageSizeAttribute(int pagesize) {
            PageSize = pagesize;
        }

        public int PageSize { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DontDoManyFetch : Attribute {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class FetchAttribute : Attribute {
        public FetchAttribute() {}
        public FetchAttribute(params string[] propertyNames) {
            _properties.AddRange(propertyNames.SelectMany(s => s.SplitAndTrim(",")));
        }

        public FetchAttribute(string propertyName) {
            _properties.AddRange(propertyName.SplitAndTrim(","));
        }

        ISet<string> _properties = new HashSet<string>();
        public ISet<string> FetchProperties {
            get { return _properties; }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class IdAttribute : Attribute {
        public string IdParam { get; private set; }

        public IdAttribute(string idparam) {
            IdParam = idparam;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TemplateObjectNameAttribute : Attribute {
        public string Name { get; private set; }

        public TemplateObjectNameAttribute(string name) {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class UpdateExcludeAttribute : ExcludePropertiesAttribute {
        public UpdateExcludeAttribute(params string[] props) : base(props) {}
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CreateExcludeAttribute : ExcludePropertiesAttribute {
        public CreateExcludeAttribute(params string[] props) : base(props) {}
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class UpdateIncludeAttribute : ExcludePropertiesAttribute {
        public UpdateIncludeAttribute(params string[] props) : base(props) {}
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CreateIncludeAttribute : ExcludePropertiesAttribute {
        public CreateIncludeAttribute(params string[] props) : base(props) {}
    }

    public class ExcludePropertiesAttribute : Attribute {
        public string Properties { get; set; }

        public ExcludePropertiesAttribute(params string[] props) {
            Properties = props.Join(",");
        }
    }
}
