#region using

using System;

#endregion

namespace Dry.Common {
    public class FileSizeFormatProvider : IFormatProvider, ICustomFormatter {
        public object GetFormat(Type formatType) {
            if (formatType == typeof (ICustomFormatter)) return this;
            return null;
        }

        const string FileSizeFormat = "fs";
        const Decimal OneKiloByte = 1024M;
        const Decimal OneMegaByte = OneKiloByte * 1024M;
        const Decimal OneGigaByte = OneMegaByte * 1024M;

        public string Format(string format, object arg, IFormatProvider formatProvider) {
            if (format == null || !format.StartsWith(FileSizeFormat)) {
                return DefaultFormat(format, arg, formatProvider);
            }

            if (arg is string) {
                return DefaultFormat(format, arg, formatProvider);
            }

            Decimal size;

            try {
                size = Convert.ToDecimal(arg);
            }
            catch (InvalidCastException) {
                return DefaultFormat(format, arg, formatProvider);
            }

            string suffix;
            if (size > OneGigaByte) {
                size /= OneGigaByte;
                suffix = "GB";
            }
            else if (size > OneMegaByte) {
                size /= OneMegaByte;
                suffix = "MB";
            }
            else if (size > OneKiloByte) {
                size /= OneKiloByte;
                suffix = "kB";
            }
            else {
                suffix = " B";
            }

            string precision = format.Substring(2);
            if (String.IsNullOrEmpty(precision)) precision = "2";
            return String.Format("{0:N" + precision + "}{1}", size, suffix);
        }

        static string DefaultFormat(string format, object arg, IFormatProvider formatProvider) {
            var formattableArg = arg as IFormattable;
            return formattableArg != null ? formattableArg.ToString(format, formatProvider) : arg.ToString();
        }
    }
}
