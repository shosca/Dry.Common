#region using

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Scopes;
using Castle.Components.Binder;
using Castle.Components.Validator;
using Castle.MonoRail.Framework;
using Dry.Common.Monorail.Helpers;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using Dry.Common.ActiveRecord;
using Dry.Common.Binder;
using Dry.Common.Monorail;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    public abstract class BaseAction<T> : IContextAware, IDynamicActionProvider, IDynamicAction where T : class {
        public virtual string Action { get { return string.Empty; } }
        protected Type Type;
        protected Dry.Common.ActiveRecord.ARDataBinder Binder { get; private set; }
        protected IEngineContext Context { get; private set; }
        protected CustomTreeBuilder TreeBuilder = new CustomTreeBuilder();
        ValidatorRunner _validationRunner;
        protected ISet<QueryFilter> Queryfilters = new HashSet<QueryFilter>();
        protected ISet<OrderBy> Orderbys = new HashSet<OrderBy>();
        protected ISet<string> Fetches = new HashSet<string>();
        protected bool DontDoManyFetch = false;

        public string CreateAllowedProperties;
        public string CreateExcludedProperties;
        public string IdParameter { get; set; }
        public string TemplateObjectName { get; set; }
        public int PageSize { get; set; }

        public ValidatorRunner Validator {
            get { return _validationRunner ?? (_validationRunner = new ValidatorRunner(CommonExtensions.ValidatorRegistry)); }
        }

        protected BaseAction() {
            Type = typeof (T);
            TemplateObjectName = Type.Name.ToLower();
            Binder = new ARDataBinder();
            PageSize = MRHelper.DefaultPageSize;
        }

        public BaseAction<T> AddQueryFilter(QueryFilter qf) {
            Queryfilters.Add(qf);
            return this;
        }

        protected CompositeNode BuildCompositeNode(NameValueCollection dict, IEnumerable<QueryFilter> filters, bool applytemplatename) {
            CompositeNode rootnode;

            if (Queryfilters.HasItems()) {
                var nm = new NameValueCollection();
                foreach (var key in dict.AllKeys) {
                    nm[key] = dict[key];
                }
                foreach (var qf in filters.Where(qf => qf.Value != null)) {
                    var key = applytemplatename
                              ? TemplateObjectName + "." + qf.PropertyName
                              : qf.PropertyName;

                    if (qf.AlwaysOverwrite) {
                        nm[key] = qf.Value;
                    } else if (nm[key] == null) {
                        nm[key] = qf.Value;
                    }
                }
                rootnode = TreeBuilder.BuildSourceNode(nm);
            } else {
                rootnode = TreeBuilder.BuildSourceNode(dict);
            }
            return rootnode;
        }


        public BaseAction<T> SetTemplateObjectName(string objname) {
            TemplateObjectName = objname;
            return this;
        }

        public void SetContext(IEngineContext context) {
            Context = context;
            var attr = context.CurrentController.GetType().GetAttr<TemplateObjectNameAttribute>();
            if (attr != null) {
                TemplateObjectName = attr.Name;
            }
        }

        public virtual void IncludeActions(IEngineContext engineContext, IController controller, IControllerContext controllerContext) {
            controllerContext.DynamicActions[Action] = this;
        }


        public virtual object Execute(IEngineContext context, IController controller, IControllerContext controllerContext) {
            SetContext(context);
            controllerContext.PropertyBag["templateobjectname"] = TemplateObjectName;
            Orderbys.AddRange(
                context.CurrentController.GetType().GetAttrs<QueryOrderByAttribute>().Select(qo => qo.OrderBy)
            );
            Fetches.AddRange(
                context.CurrentController.GetType().GetAttrs<FetchAttribute>()
                    .SelectMany(a => a.FetchProperties )
            );
            Queryfilters.AddRange(
                context.CurrentController.GetType().GetAttrs<QueryFilterAttribute>().Select(attr => {
                    var cw = attr as IContextAware;
                    if (cw != null) cw.SetContext(context);

                    return attr.QueryFilter;
                })
            );
            DontDoManyFetch = controller.GetType().GetAttr<DontDoManyFetch>() == null;
            CreateAllowedProperties = controller.GetType().GetAttrs<CreateExcludeAttribute>().Select(a => a.Properties).FirstOrDefault();
            CreateExcludedProperties = controller.GetType().GetAttrs<CreateIncludeAttribute>().Select(a => a.Properties).FirstOrDefault();
            var idattr = controller.GetType().GetAttr<IdAttribute>();
            IdParameter = idattr != null ? idattr.IdParam : "id";

            var pgattr = controller.GetType().GetAttr<PageSizeAttribute>();
            if (pgattr != null)
                PageSize = pgattr.PageSize;

            return null;
        }

        static void InvokeInAll<TK>(IEnumerable<TK> items, Type parenttype, Action<TK> action) {
            if (items == null) return;
            var nested = parenttype.GetNestedTypes().ToArray();
            items.Where(p => nested.Contains(p.GetType())).ForAll(action);
        }

        IPreDelete<T>[] _predelete;
        public IPreDelete<T>[] PreDeletes {
            get { return _predelete ?? (_predelete = new IPreDelete<T>[0]); }
            set { _predelete = value; }
        }

        protected void OnPreDelete(IController controller, T instance) {
            InvokeInAll(PreDeletes, controller.GetType(), a => a.PreDelete(Context, instance));
        }

        IPostDelete<T>[] _postdelete;
        public IPostDelete<T>[] PostDeletes {
            get { return _postdelete ?? (_postdelete = new IPostDelete<T>[0]); }
            set { _postdelete = value; }
        }

        protected void OnPostDelete(IController controller, T instance) {
            InvokeInAll(PostDeletes, controller.GetType(), a => a.PostDelete(Context, instance));
        }

        IPreUpdate<T>[] _preupdate;
        public IPreUpdate<T>[] PreUpdates {
            get { return _preupdate ?? (_preupdate = new IPreUpdate<T>[0]); }
            set { _preupdate = value; }
        }
        protected void OnPreUpdate(IController controller, T instance) {
            InvokeInAll(PreUpdates, controller.GetType(), a => a.PreUpdate(Context, instance));
        }

        IPostUpdate<T>[] _postupdate;
        public IPostUpdate<T>[] PostUpdates {
            get { return _postupdate ?? (_postupdate = new IPostUpdate<T>[0]); }
            set { _postupdate = value; }
        }
        protected void OnPostUpdate(IController controller, T instance) {
            InvokeInAll(PostUpdates, controller.GetType(), a => a.PostUpdate(Context, instance));
        }

        IPreCreate<T>[] _precreate;
        public IPreCreate<T>[] PreCreates {
            get { return _precreate ?? (_precreate = new IPreCreate<T>[0]); }
            set { _precreate = value; }
        }
        protected void OnPreCreate(IController controller, T instance) {
            InvokeInAll(PreCreates, controller.GetType(), a => a.PreCreate(Context, instance));
        }

        IPostCreate<T>[] _postcreate;
        public IPostCreate<T>[] PostCreates {
            get { return _postcreate ?? (_postcreate = new IPostCreate<T>[0]); }
            set { _postcreate = value; }
        }
        protected void OnPostCreate(IController controller, T instance) {
            InvokeInAll(PostCreates, controller.GetType(), a => a.PostCreate(Context, instance));
        }

        IPreBind<T>[] _prebind;
        public IPreBind<T>[] PreBinds {
            get { return _prebind ?? (_prebind = new IPreBind<T>[0]); }
            set { _prebind = value; }
        }
        protected void OnPreBind(IController controller, T instance) {
            InvokeInAll(PreBinds, controller.GetType(), a => a.PreBind(Context, instance));
        }

        IPostBind<T>[] _postbind;
        public IPostBind<T>[] PostBinds {
            get { return _postbind ?? (_postbind = new IPostBind<T>[0]); }
            set { _postbind = value; }
        }
        protected void OnPostBind(IController controller, T instance) {
            InvokeInAll(PostBinds, controller.GetType(), a => a.PostBind(Context, instance));
        }

        IPreSave<T>[] _presave;
        public IPreSave<T>[] PreSaves {
            get { return _presave ?? (_presave = new IPreSave<T>[0]); }
            set { _presave = value; }
        }
        protected void OnPreSave(IController controller, T instance) {
            InvokeInAll(PreSaves, controller.GetType(), a => a.PreSave(Context, instance));
        }

        IPostSave<T>[] _postsave;
        public IPostSave<T>[] PostSaves {
            get { return _postsave ?? (_postsave = new IPostSave<T>[0]); }
            set { _postsave = value; }
        }
        protected void OnPostSave(IController controller, T instance) {
            InvokeInAll(PostSaves, controller.GetType(), a => a.PostSave(Context, instance));
        }

        IPreList[] _prelists;
        public IPreList[] PreLists {
            get { return _prelists ?? (_prelists = new IPreList[0]); }
            set { _prelists = value; }
        }
        protected void OnPreList(IController controller) {
            InvokeInAll(PreLists, controller.GetType(), a => a.PreList(Context));
        }

        IPostList<T>[] _postlists;
        public IPostList<T>[] PostLists {
            get { return _postlists ?? (_postlists = new IPostList<T>[0]); }
            set { _postlists = value; }
        }

        protected void OnPostList(IController controller, IEnumerable<T> items) {
            InvokeInAll(PostLists, controller.GetType(), a => a.PostList(Context, items));
        }

        public IPreView[] PreViews { get; set; }
        protected void OnPreView(IController controller) {
            InvokeInAll(PreViews, controller.GetType(), a => a.PreView(Context));
        }

        IPostView<T>[] _postviews;
        public IPostView<T>[] PostViews {
            get { return _postviews ?? (_postviews = new IPostView<T>[0]); }
            set { _postviews = value; }
        }
        protected void OnPostView(IController controller, T instance) {
            InvokeInAll(PostViews, controller.GetType(), a => a.PostView(Context, instance));
        }

        public object ExecuteView(IEngineContext context, IController controller, IControllerContext controllerContext) {
            OnPreView(controller);

            try {
                var instance = AR.Find<T>(context.GetParameter(IdParameter));
                if (instance == null) {
                    context.Handle404();
                    return null;
                }

                var id = AR.Holder.GetModel(Type).Metadata.GetIdentifier(instance, EntityMode.Poco);

                var loader = DetachedCriteria.For<T>()
                    .Add(Restrictions.Eq(Projections.Id(), id));

                foreach (var b in AR.Holder.GetModel(typeof(T)).BelongsTos) {
                    loader.CreateCriteria(b.Key, JoinType.LeftOuterJoin);
                }

                loader.SetResultTransformer(Transformers.DistinctRootEntity);
                if (!DontDoManyFetch) {
                    foreach (var hasmany in AR.Holder.GetModel(typeof(T)).HasManys) {
                        DetachedCriteria.For<T>()
                            .Add(Restrictions.Eq(Projections.Id(), id))
                            .CreateCriteria(hasmany.Value.Type.Role.Replace(typeof(T).FullName + ".", ""), JoinType.LeftOuterJoin)
                            .Future<T>();
                    }
                    foreach (var hasmany in AR.Holder.GetModel(typeof(T)).HasAndBelongsToManys) {
                        DetachedCriteria.For<T>()
                            .Add(Restrictions.Eq(Projections.Id(), id))
                            .CreateCriteria(hasmany.Value.Type.Role.Replace(typeof(T).FullName + ".", ""), JoinType.LeftOuterJoin)
                            .Future<T>();
                    }
                }

                controllerContext.PropertyBag[TemplateObjectName] = instance;

                OnPostView(controller, instance);
            }
            catch (Exception e) {
                context.ErrorMessages(e);
            }
            return null;
        }

        public object ExecuteCreate(IEngineContext context, IController controller, IControllerContext controllerContext) {
            T instance = null;
            string pk = null;
            var form = BuildCompositeNode(context.Request.Form, Queryfilters.Where(qf => qf.ApplyOnSave), true);

            using(var transaction = new TransactionScope()) {
                try {
                    Binder.AutoLoad = AutoLoadBehavior.OnlyNested;
                    instance = (T) Binder.BindObject(typeof (T), TemplateObjectName, CreateExcludedProperties, CreateAllowedProperties, form);
                    OnPostBind(controller, instance);

                    OnPreSave(controller, instance);
                    OnPreCreate(controller, instance);
                    if (Validator.IsValid(instance)) {
                        AR.Save<T>(instance);

                        OnPostCreate(controller, instance);
                        OnPostSave(controller, instance);

                        transaction.Flush();
                        transaction.VoteCommit();

                        pk = AR.Holder.GetClassMetadata(Type).GetIdentifier(instance, EntityMode.Poco).ToString();
                        context.SuccessMessage(instance + " has been created");
                    } else {
                        transaction.VoteRollBack();
                        context.ErrorMessages(400, Validator.GetErrorSummary(instance).ErrorMessages);
                        context.Flash[TemplateObjectName + "_form"] = form;
                    }
                } catch (Exception ex) {
                    transaction.VoteRollBack();
                    context.ErrorMessages(ex);
                    context.Flash[TemplateObjectName + "_form"] = form;
                } finally {
                    controllerContext.PropertyBag[TemplateObjectName] = instance;
                    controllerContext.PropertyBag["objectid"] = pk;
                }
            }

            return null;
        }

        public object ExecuteUpdate(IEngineContext context, IController controller, IControllerContext controllerContext) {
            controllerContext.Action = "update";
            using (var transaction = new TransactionScope()) {
                var instance = AR.Find<T>(context.GetParameter(IdParameter));
                if (instance == null) {
                    context.Handle404();
                    return null;
                }

                var pk = AR.Holder.GetModel(Type).Metadata.GetIdentifier(instance, EntityMode.Poco).ToString();

                try {
                    var excludeattr = controller.GetType().GetAttr<UpdateExcludeAttribute>();
                    var includeattr = controller.GetType().GetAttr<UpdateIncludeAttribute>();
                    var includeprops = includeattr == null ? null : includeattr.Properties;
                    var excludeprops = excludeattr == null ? null : excludeattr.Properties;

                    var form = BuildCompositeNode(context.Request.Form, Queryfilters.Where(qf => qf.ApplyOnSave), true);

                    OnPreBind(controller, instance);
                    Binder.AutoLoad = AutoLoadBehavior.NullIfInvalidKey;
                    Binder.BindObjectInstance(instance, TemplateObjectName, excludeprops, includeprops, form);
                    OnPostBind(controller, instance);

                    OnPreSave(controller, instance);
                    OnPreUpdate(controller, instance);
                    if (Validator.IsValid(instance)) {
                        AR.Save<T>(instance);
                        OnPostUpdate(controller, instance);
                        OnPostSave(controller, instance);

                        transaction.Flush();
                        transaction.VoteCommit();

                        pk = AR.Holder.GetClassMetadata(Type).GetIdentifier(instance, EntityMode.Poco).ToString();
                        controllerContext.PropertyBag[TemplateObjectName] = instance;
                        context.SuccessMessage(instance + " has been updated");

                    } else {
                        transaction.VoteRollBack();
                        context.ErrorMessages(Validator.GetErrorSummary(instance).ErrorMessages);
                    }
                } catch (Exception ex) {
                    transaction.VoteRollBack();
                    context.ErrorMessages(ex);
                } finally {
                    context.Flash[TemplateObjectName] = instance;
                    controllerContext.PropertyBag["objectid"] = pk;
                    context.CurrentControllerContext.PropertyBag[TemplateObjectName] = instance;
                }
            }

            return null;
        }

        public object ExecuteDelete(IEngineContext context, IController controller, IControllerContext controllerContext) {
            using (var transaction = new TransactionScope()) {
                var instance = AR.Find<T>(context.GetParameter("id"));
                try {
                    OnPreDelete(controller, instance);
                    AR.Delete<T>(instance);
                    OnPostDelete(controller, instance);

                    transaction.Flush();
                    transaction.VoteCommit();

                    context.SuccessMessage(instance + " has been deleted");
                } catch (Exception ex) {
                    transaction.VoteRollBack();
                    context.ErrorMessages(ex);
                } finally {
                    controllerContext.PropertyBag[TemplateObjectName] = instance;
                    context.Flash[TemplateObjectName] = instance;
                }
            }

            return null;
        }

        public object ExecuteNew(IEngineContext context, IController controller, IControllerContext controllerContext) {
            OnPreList(controller);

            if (context.Flash[TemplateObjectName + "_form"] != null) {
                var form = context.Flash[TemplateObjectName + "_form"] as CompositeNode;
                if (form != null) {
                    Binder.AutoLoad = AutoLoadBehavior.OnlyNested;
                    controllerContext.PropertyBag[TemplateObjectName] =
                        Binder.BindObject(typeof (T), TemplateObjectName, CreateExcludedProperties, CreateAllowedProperties, form);

                    return null;
                }
            }

            foreach (var info in Type.GetConstructors()) {
                var pars = info.GetParameters();
                if (pars.Length == 0) {
                    controllerContext.PropertyBag[TemplateObjectName] = info.Invoke(null);
                    return null;
                }
            }

            return null;
        }
    }
}
