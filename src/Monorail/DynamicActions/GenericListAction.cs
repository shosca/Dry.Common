#region using

using Dry.Common.Queries;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    public class GenericListAction<T> : ListAction<T, GenericNhQuery<T>> where T : class {
        public GenericListAction(GenericNhQuery<T> nhQuery) : base(nhQuery) {}
    }

    public class GenericNhListAction<T> : ListAction<T, GenericNhQuery<T>> where T : class {
        public GenericNhListAction(GenericNhQuery<T> nhQuery) : base(nhQuery) {}
    }
}
