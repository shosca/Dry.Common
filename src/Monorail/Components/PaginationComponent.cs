#region using

using System;
using System.IO;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Framework.ViewComponents;

#endregion

namespace Dry.Common.Monorail.Components {
    [ViewComponentDetails("Pagination", Cache = ViewComponentCache.Always, Sections="startblock,endblock,next,prev,link" )]
    public class PaginationComponent : AbstractPaginationViewComponent {
        const string PrevSection = "prev";
        const string NextSection = "next";
        const string LinkSection = "link";

        int _adjacents = 2;
        [ViewComponentParam]
        public int Adjacents { get { return _adjacents; } set { _adjacents = value; } }

        [ViewComponentParam]
        public string Area { get; set; }
        [ViewComponentParam]
        public string Controller { get; set; }
        [ViewComponentParam]
        public string Action { get; set; }

        DictHelper.MonoRailDictionary _qs = DictHelper.Create();
        [ViewComponentParam]
        public DictHelper.MonoRailDictionary Querystring { get { return _qs; } set { _qs = value; } }

        protected override void StartBlock(StringWriter writer) {
            writer.Write("<div class='pagination'>");
        }

        public override void Render() {
            if (Page.TotalPages <= 1) { return; }

            var writer = new StringWriter();

            StartBlock(writer);
            WriteStart(writer);
            WritePrev(writer);
            if (Page.TotalPages < (4 + (Adjacents * 2))) {
                WriteNumberedLinks(writer, 1, Page.TotalPages);
            } else {
                if ((Page.TotalPages - (Adjacents * 2) > Page.CurrentPageIndex) && (Page.CurrentPageIndex > (Adjacents * 2))) {
                    var lower = Page.CurrentPageIndex - Adjacents;
                    var upper = Page.CurrentPageIndex + Adjacents;
                    // elipsis at begin is only required if there is more than one page between lower adjacent and ever displayed begin pages
                    var requireElipsisBetweenBeginAndLowerAdjacentIndex = (lower - 2) > 1;
                    // elipsis at end is only required if there is more than one page between upper adjacent and ever displayed end pages
                    var requireElipsisBetweenUpperAdjacentIndexAndEnd = (Page.TotalPages - upper) > 1;

                    WriteNumberedLinks(writer, 1, 2);
                    if (requireElipsisBetweenBeginAndLowerAdjacentIndex)
                        WriteElipsis(writer);
                    WriteNumberedLinks(writer, lower, upper);
                    if (requireElipsisBetweenUpperAdjacentIndexAndEnd)
                        WriteElipsis(writer);
                    WriteNumberedLinks(writer, Page.TotalPages - 1, Page.TotalPages);
                } else if (Page.CurrentPageIndex < (Page.TotalPages / 2)) {
                    WriteNumberedLinks(writer, 1, 2 + (Adjacents * 2));
                    WriteElipsis(writer);
                    WriteNumberedLinks(writer, Page.TotalPages - 1, Page.TotalPages);
                } else {
                    WriteNumberedLinks(writer, 1, 2);
                    WriteElipsis(writer);
                    WriteNumberedLinks(writer, Page.TotalPages - (2 + (Adjacents * 2)), Page.TotalPages);
                }
            }
            WriteNext(writer);
            WriteEnd(writer);
            EndBlock(writer);
            RenderText(writer.ToString());
        }

        protected override string CreateUrlForPage(int pageIndex) {
            var old = Querystring[PageParamName];
            Querystring[PageParamName] = pageIndex;
            var dict = DictHelper.Create();
            dict.Add("area", string.IsNullOrEmpty(Area) ? EngineContext.UrlInfo.Area : Area);
            dict.Add("controller", string.IsNullOrEmpty(Controller) ? EngineContext.UrlInfo.Controller : Controller);
            dict.Add("action", string.IsNullOrEmpty(Action) ? EngineContext.UrlInfo.Action : Action);
            dict.Add("querystring", Querystring);
            var urlparams = UrlBuilderParameters.From(dict).SetRouteMatch(EngineContext.CurrentControllerContext.RouteMatch);
            urlparams.EncodeForLink = true;
            var url = EngineContext.Services.UrlBuilder.BuildUrl(EngineContext.UrlInfo, urlparams);
            Querystring[PageParamName] = old;
            return url;
        }

        void WriteNext(StringWriter writer) {
            var text = "Next &rarr;";
            if (Context.HasSection(NextSection)) {
                TextWriter capwriter = new StringWriter();
                Context.RenderSection(NextSection, capwriter);
                text = capwriter.ToString().Trim();
            }
            if (!Page.HasNextPage) {
                writer.Write(string.Format("<li class='next disabled'>"));
            } else {
                writer.Write(string.Format("<li class='next'>"));
            }
            WritePageLink(writer, Page.NextPageIndex, text);
            writer.Write("</li>");
        }

        void WritePrev(StringWriter writer) {
            var text = "&larr; Prev";
            if (Context.HasSection(PrevSection)) {
                TextWriter capwriter = new StringWriter();
                Context.RenderSection(PrevSection, capwriter);
                text = capwriter.ToString().Trim();
            }
            if (!Page.HasPreviousPage) {
                writer.Write(string.Format("<li class='prev disabled'>"));
            } else {
                writer.Write(string.Format("<li class='prev'>"));
            }
            WritePageLink(writer, Page.PreviousPageIndex, text);
            writer.Write("</li>");
        }

        void WritePageLink(StringWriter writer, int index, string text) {
            var url = CreateUrlForPage(index);
            if (Context.HasSection(LinkSection)) {
                PropertyBag["pageIndex"] = index;
                PropertyBag["text"] = text;
                PropertyBag["url"] = url;
            } else {
                writer.Write(String.Format("<a data-pageindex='{0}' href=\"{1}\">{2}</a>\r\n", index, url, text));
            }
        }


        void WriteElipsis(StringWriter writer) {
            writer.Write("<li class='disabled'><a href='#'>&#8230;</a></li>");
        }

        void WriteNumberedLinks(StringWriter writer, int index, int totalPages) {
            for (var i = index; i <= totalPages; i++) {
                writer.Write(i == Page.CurrentPageIndex ? "<li class='active'>" : "<li>");
                WritePageLink(writer, i, i.ToString());
                writer.Write("</li>");
            }
        }


        void WriteEnd(StringWriter writer) {
            writer.Write("</ul>");
        }

        void WriteStart(StringWriter writer) {
            writer.Write("<ul>");
        }
    }
}
