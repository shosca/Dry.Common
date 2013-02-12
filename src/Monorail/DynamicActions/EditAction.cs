#region using

using System;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Routing;
using Dry.Common.Monorail.Helpers;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using NHibernate.Transform;

#endregion

namespace Dry.Common.Monorail.DynamicActions {
    public class EditAction<T> : BaseAction<T> where T : class {
        public override string Action { get { return "edit"; } }

        public override object Execute(IEngineContext context, IController controller, IControllerContext controllerContext) {
            base.Execute(context, controller, controllerContext);
            OnPreList(controller);
            OnPreView(controller);

            try {
                var match = context.Items[RouteMatch.RouteMatchKey] as RouteMatch;
                if (match == null)
                    throw new MonoRailException("Cannot find 'id' from the url.");

                var instance = AR.Find<T>(match.Parameters["id"]);
                if (instance == null) {
                    context.Handle404();
                    return null;
                }

                controllerContext.PropertyBag[TemplateObjectName] = instance;
                var id = AR.Holder.GetModel(Type).Metadata.GetIdentifier(instance, EntityMode.Poco);
                controllerContext.PropertyBag["objectid"] = id;

                var loader = DetachedCriteria.For<T>()
                    .Add(Restrictions.Eq(Projections.Id(), id));
                foreach (var b in AR.Holder.GetModel(typeof(T)).BelongsTos) {
                    loader.CreateCriteria(b.Key, JoinType.LeftOuterJoin);
                }
                loader.SetResultTransformer(Transformers.DistinctRootEntity).Future<T>();

                if (controller.GetType().GetAttr<DontDoManyFetch>() == null) {
                    foreach (var hasmany in AR.Holder.GetModel(typeof(T)).HasManys) {
                        DetachedCriteria.For<T>()
                            .CreateCriteria(hasmany.Value.Type.Role.Replace(typeof(T) + ".", ""), JoinType.LeftOuterJoin)
                            .Future<T>();
                    }
                    foreach (var hasmany in AR.Holder.GetModel(typeof (T)).HasAndBelongsToManys) {
                        DetachedCriteria.For<T>()
                            .CreateCriteria(hasmany.Value.Type.Role.Replace(typeof(T) + ".", ""), JoinType.LeftOuterJoin)
                            .Future<T>();
                    }
                }
                OnPostView(controller, instance);

                return null;
            } catch (Exception ex) {
                context.ErrorMessages(ex);
            }
            context.Handle404();
            return null;
        }
    }
}
