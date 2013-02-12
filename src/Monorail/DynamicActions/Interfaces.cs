#region using

using System.Collections.Generic;
using Castle.MonoRail.Framework;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    public interface IPreBind<in T> {
        void PreBind(IEngineContext context, T item);
    }

    public interface IPostBind<in T> {
        void PostBind(IEngineContext context, T item);
    }

    public interface IPreSave<in T> {
        void PreSave(IEngineContext context, T item);
    }

    public interface IPostSave<in T> {
        void PostSave(IEngineContext context, T item);
    }

    public interface IPreCreate<in T> {
        void PreCreate(IEngineContext context, T item);
    }

    public interface IPostCreate<in T> {
        void PostCreate(IEngineContext context, T item);
    }

    public interface IPreUpdate<in T> {
        void PreUpdate(IEngineContext context, T item);
    }

    public interface IPostUpdate<in T> {
        void PostUpdate(IEngineContext context, T item);
    }

    public interface IPreDelete<in T> {
        void PreDelete(IEngineContext context, T item);
    }

    public interface IPostDelete<in T> {
        void PostDelete(IEngineContext context, T item);
    }

    public interface IPreList {
        void PreList(IEngineContext context);
    }

    public interface IPostList<in T> {
        void PostList(IEngineContext context, IEnumerable<T> items);
    }

    public interface IPreView {
        void PreView(IEngineContext context);
    }

    public interface IPostView<in T> {
        void PostView(IEngineContext context, T item);
    }
}
