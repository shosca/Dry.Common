#region using

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Castle.MonoRail.Framework;

#endregion

namespace Dry.Common.Monorail.Filters {
    public class WhitespaceTransformFilter : TransformFilter {
        //private static readonly Regex _reg = new Regex(@"(?<=[^])\t{2,}|(?<=[>])\s{2,}(?=[<])|(?<=[>])\s{2,11}(?=[<])|(?=[\n])\s{2,}");

        ///New simplified Regex found at http://blog.madskristensen.dk/post/Remove-whitespace-from-your-pages.aspx
        static readonly Regex BetweenTags = new Regex(@">\s+<", RegexOptions.Compiled);

        static readonly Regex LineBreaks = new Regex(@"\n\s+", RegexOptions.Compiled);

        public WhitespaceTransformFilter(Stream baseStream)
            : base(baseStream) {}

        public override void Write(byte[] buffer, int offset, int count) {
            if (Closed) throw new ObjectDisposedException("WhitespaceTransformFilter");

            var content = Encoding.Default.GetString(buffer, offset, count);

            content = BetweenTags.Replace(content, "> <");
            content = LineBreaks.Replace(content, string.Empty);

            var output = Encoding.Default.GetBytes(content);
            BaseStream.Write(output, 0, output.Length);
        }
    }
}
