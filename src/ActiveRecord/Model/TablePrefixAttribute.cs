#region using

using System;

#endregion

namespace Dry.Common.ActiveRecord.Model {
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false), Serializable]
    public class TablePrefixAttribute : Attribute {
        public string Prefix { get; private set; }

        public TablePrefixAttribute(string prefix) {
            Prefix = prefix;
        }
    }
}
