#region using

using System.Globalization;
using System.IO;
using System.Linq;

#endregion

namespace Dry.Common.Monorail {
    public class TemplateLocation {
        static readonly string P = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);

        public string Area { private get; set; }
        public string Controller { private get; set; }
        public string Action { private get; set; }

        public override string  ToString() {
            return P + string.Join(P, new[] { Area, Controller, Action }.Where(x => !string.IsNullOrEmpty(x))).ToLower();
        }
    }
}