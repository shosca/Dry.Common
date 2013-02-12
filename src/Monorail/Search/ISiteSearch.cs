#region using

using System;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Helpers;

#endregion

namespace Dry.Common.Monorail.Search {
    public interface ISiteSearch {
        Type Type { get; }
        string ViewComponent { get; }
        DictHelper.MonoRailDictionary Querystring { get; }
        bool HasResults { get; }
        void Search(IEngineContext context);
    }
}
