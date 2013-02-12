#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Routing;

#endregion

namespace Dry.Common.Monorail {
    public static class Routing {
        public static string GetAreaName(this Type type) {
            var area = string.Empty;
            var detailsattribute = type.GetAttr<ControllerDetailsAttribute>(true);
            if (detailsattribute != null) {
                area = detailsattribute.Area;
            }
            return area;
        }

        public static string GetControllerName(this Type type) {
            var name = type.Name.ToLower().Replace("controller", "");
            var detailsattribute = type.GetAttr<ControllerDetailsAttribute>(true);
            if (detailsattribute != null && !string.IsNullOrEmpty(detailsattribute.Name))
                name = detailsattribute.Name;

            return name;
        }

        public static string[] GetActions(this Type type) {
            var actions = new HashSet<string>(new [] { "index", "new", "edit", "create", "update", "delete" });
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            actions.AddRange(methods.Select(m => m.Name.ToLower()));
            actions.AddRange(type.GetAttrs<DynamicActionProviderAttribute>().Select(d => d.ProviderType.Name.ToLower().Replace("action", "").Replace("`1", "")));
            return actions.ToArray();
        }

        public static Dictionary<string, List<Type>> Areas = new Dictionary<string, List<Type>>();

        public static void Register(Assembly assembly) {
            var controllers = assembly.GetTypes().Where(t => typeof (Controller).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var controller in controllers) {
                var area = controller.GetAreaName();    
                if (!Areas.ContainsKey(area))
                    Areas.Add(area, new List<Type>());

                Areas[area].Add(controller);
            }
        }

        public static void SetupRoutes(IRoutingRuleContainer rules) {
            foreach (var area in Areas) {
                var areaname = area.Key;
                var controllernames = area.Value.Select(GetControllerName).ToArray();
                if (string.IsNullOrEmpty(areaname)) {
                    rules.Add(new PatternRoute(areaname + "/", "[controller]")
                                .Restrict("controller").AnyOf(controllernames)
                                .DefaultForArea().IsEmpty
                                .DefaultForController().Is("home")
                                .DefaultForAction().Is("index"));

                    rules.Add(new PatternRoute(areaname + "+/action", "<controller>/[action]")
                                .Restrict("controller").AnyOf(controllernames)
                                .DefaultForArea().IsEmpty
                                .DefaultForController().Is("home")
                                .DefaultForAction().Is("index"));

                    rules.Add(new PatternRoute(areaname + "+/id/action", "<controller>/<id>/[action]")
                                .Restrict("controller").AnyOf(controllernames)
                                .Restrict("id").ValidInteger
                                .DefaultForArea().IsEmpty
                                .DefaultForAction().Is("view"));

                    rules.Add(new PatternRoute(areaname + "+/guid/action", "<controller>/<id>/[action]")
                                .Restrict("controller").AnyOf(controllernames)
                                .Restrict("id").ValidGuid
                                .DefaultForArea().IsEmpty
                                .DefaultForController().Is("home")
                                .DefaultForAction().Is("view"));
                } else {
                    var actionnames = area.Value.SelectMany(GetActions).Distinct().ToArray();
                    rules.Add(new PatternRoute(areaname + "+/action", "<area>/<controller>/[action]")
                                .Restrict("area").AnyOf(new [] { areaname })
                                .Restrict("controller").AnyOf(controllernames)
                                .Restrict("action").AnyOf(actionnames)
                                .DefaultForAction().Is("index"));

                    rules.Add(new PatternRoute(areaname + "+/id/action", "<area>/<controller>/<id>/[action]")
                                .Restrict("area").AnyOf(new [] { areaname })
                                .Restrict("controller").AnyOf(controllernames)
                                .Restrict("action").AnyOf(actionnames)
                                .Restrict("id").ValidInteger
                                .DefaultForAction().Is("view"));

                    rules.Add(new PatternRoute(areaname + "+/guid/action", "<area>/<controller>/<id>/[action]")
                                .Restrict("area").AnyOf(new [] { areaname })
                                .Restrict("controller").AnyOf(controllernames)
                                .Restrict("action").AnyOf(actionnames)
                                .Restrict("id").ValidGuid
                                .DefaultForAction().Is("view"));

                    rules.Add(new PatternRoute(areaname + "+/areaid/action", "<area>/<areaid>/<controller>/[action]")
                                .DefaultForArea().Is(areaname)
                                .Restrict("area").AnyOf(new [] { areaname })
                                .Restrict("controller").AnyOf(controllernames)
                                .Restrict("areaid").ValidInteger
                                .Restrict("action").AnyOf(actionnames)
                                .DefaultForAction().Is("index"));

                    rules.Add(new PatternRoute(areaname + "+/areaid/id/action", "<area>/<areaid>/<controller>/<id>/[action]")
                                .DefaultForArea().Is(areaname)
                                .Restrict("area").AnyOf(new [] { areaname })
                                .Restrict("controller").AnyOf(controllernames)
                                .Restrict("areaid").ValidInteger
                                .Restrict("action").AnyOf(actionnames)
                                .Restrict("id").ValidInteger
                                .DefaultForAction().Is("view"));

                    rules.Add(new PatternRoute(areaname + "+/areaid/guid/action",  "<area>/<areaid>/<controller>/<id>/[action]")
                                .DefaultForArea().Is(areaname)
                                .Restrict("area").AnyOf(new [] { areaname })
                                .Restrict("controller").AnyOf(controllernames)
                                .Restrict("areaid").ValidInteger
                                .Restrict("action").AnyOf(actionnames)
                                .Restrict("id").ValidGuid
                                .DefaultForAction().Is("view"));
                }
            }
        }
    }
}
