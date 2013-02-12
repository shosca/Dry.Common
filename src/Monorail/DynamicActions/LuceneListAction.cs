#region using

using Dry.Common.Queries;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    public class LuceneListAction<T> : ListAction<T, LuceneNhQuery<T>> where T : class {
        public LuceneListAction(LuceneNhQuery<T> nhQuery) : base(nhQuery) {}
    }
}
