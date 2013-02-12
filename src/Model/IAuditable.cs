#region using



#endregion

namespace Dry.Common.Model {
    public interface IAuditable<T> : ICreateAuditable<T>, IModifyAuditable<T> where T : class { }
}
