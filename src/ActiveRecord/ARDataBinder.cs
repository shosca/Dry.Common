using Dry.Common.Monorail;
using NHibernate.Type;
using System;
using System.Collections;
using System.Reflection;
using Castle.ActiveRecord;
using Castle.Components.Binder;
using Iesi.Collections;
using Iesi.Collections.Generic;
using System.Collections.Generic;

namespace Dry.Common.ActiveRecord {
    public class ARDataBinder : DataBinder {
        protected internal static readonly object[] EmptyArg = new object[0];

        private string[] expectCollPropertiesList;
        private Stack<Castle.ActiveRecord.Model> modelStack = new Stack<Castle.ActiveRecord.Model>();

        public ARDataBinder() : base() {
            TreatEmptyGuidAsNull = true;
        }

        public bool PersistChanges { get; set; }

        public AutoLoadBehavior AutoLoad { get; set; }

        public bool TreatEmptyGuidAsNull { get; set; }

        protected override void PushInstance(object instance, string prefix) {
            var model = AR.Holder.GetModel(instance.GetType());

            if (model == null && modelStack.Count != 0) {
                foreach(var nestedModel in CurrentARModel.Components) {
                    if (string.Compare(nestedModel.Key, prefix, true) == 0) {
                        model = nestedModel.Value;
                        break;
                    }
                }
            }

            if (model != null) {
                modelStack.Push(model);
            }

            base.PushInstance(instance, prefix);
        }

        protected override void PopInstance(object instance, string prefix) {
            var model = AR.Holder.GetModel(instance.GetType());

            if (model != null && modelStack.Count != 0) {
                var actualModel = modelStack.Pop();

                if (actualModel != model) {
                    throw new BindingException("Unexpected ARModel on the stack: found {0}, expecting {1}",
                        actualModel.ToString(), model.ToString());
                }
            }

            base.PopInstance(instance, prefix);
        }

        protected Castle.ActiveRecord.Model CurrentARModel {
            get { return modelStack.Count == 0 ? null : modelStack.Peek(); }
        }

        public object BindObject(Type targetType, string prefix, string exclude, string allow, string expect,
                                 CompositeNode treeRoot) {
            expectCollPropertiesList = CreateNormalizedList(expect);

            return BindObject(targetType, prefix, exclude, allow, treeRoot);
        }

        protected override object CreateInstance(Type instanceType, String paramPrefix, Node node) {
            if (node == null) {
                throw new BindingException(
                    "Nothing found for the given prefix. Are you sure the form fields are using the prefix " +
                    paramPrefix + "?");
            }

            if (node.NodeType != NodeType.Composite) {
                throw new BindingException("Unexpected node type. Expecting Composite, found " + node.NodeType);
            }

            var cNode = (CompositeNode) node;

            object instance;

            var shouldLoad = AutoLoad != AutoLoadBehavior.Never;

            if (AutoLoad == AutoLoadBehavior.OnlyNested) {
                shouldLoad = StackDepth != 0;
            }

            var model = AR.Holder.GetModel(instanceType);

            if (shouldLoad && model == null) {
                // Nested type or unregistered type
                shouldLoad = false;
            }

            if (shouldLoad) {
                if (instanceType.IsArray) {
                    throw new BindingException("ARDataBinder AutoLoad does not support arrays");
                }

                KeyValuePair<string, IType> pkModel;

                var id = ObtainPrimaryKeyValue(model, cNode, paramPrefix, out pkModel);

                if (IsValidKey(id)) {
                    instance = FindByPrimaryKey(instanceType, id);
                } else {
                    if (AutoLoad == AutoLoadBehavior.NewInstanceIfInvalidKey ||
                        (AutoLoad == AutoLoadBehavior.NewRootInstanceIfInvalidKey && StackDepth == 0)) {
                        instance = base.CreateInstance(instanceType, paramPrefix, node);
                    } else if (AutoLoad == AutoLoadBehavior.NullIfInvalidKey ||
                             AutoLoad == AutoLoadBehavior.OnlyNested ||
                             (AutoLoad == AutoLoadBehavior.NewRootInstanceIfInvalidKey && StackDepth != 0)) {
                        instance = null;
                    } else {
                        throw new BindingException(string.Format(
                                                       "Could not find primary key '{0}' for '{1}'",
                                                       pkModel.Key, instanceType.FullName));
                    }
                }
            } else {
                instance = base.CreateInstance(instanceType, paramPrefix, node);
            }

            return instance;
        }

        protected bool FindPropertyInHasAndBelongsToMany(Castle.ActiveRecord.Model model, string propertyName,
                                                         ref Type foundType, ref Castle.ActiveRecord.Model foundModel) {
            foreach(var hasMany2ManyModel in model.HasAndBelongsToManys) {
                // Inverse=true relations will be ignored
                if (hasMany2ManyModel.Key == propertyName && !hasMany2ManyModel.Value.Persister.IsInverse) {
                    foundType = hasMany2ManyModel.Value.Persister.ElementType.ReturnedClass;
                    foundModel = AR.Holder.GetModel(foundType);
                    return true;
                }
            }

            if (model.Metadata.IsInherited) {
                var basemodel = AR.Holder.GetModel(model.Type.BaseType);
                return FindPropertyInHasAndBelongsToMany(basemodel, propertyName, ref foundType, ref foundModel);
            }

            return false;
        }

        protected bool FindPropertyInHasMany(Castle.ActiveRecord.Model model, string propertyName,
                                             ref Type foundType, ref Castle.ActiveRecord.Model foundModel) {
            foreach(var hasManyModel in model.HasManys) {
                // Inverse=true relations will be ignored
                if (hasManyModel.Key == propertyName && !hasManyModel.Value.Persister.IsInverse) {
                    foundType = hasManyModel.Value.Persister.ElementType.ReturnedClass;
                    foundModel = AR.Holder.GetModel(foundType);
                    return true;
                }
            }

            if (model.Metadata.IsInherited) {
                var basemodel = AR.Holder.GetModel(model.Type.BaseType);
                return FindPropertyInHasMany(basemodel, propertyName, ref foundType, ref foundModel);
            }

            return false;
        }

        protected override object BindSpecialObjectInstance(Type instanceType, string prefix, Node node,
                                                            out bool succeeded) {
            succeeded = false;

            var model = CurrentARModel;

            if (model == null) {
                return null;
            }

            var container = CreateContainer(instanceType);

            Type targetType = null;
            Castle.ActiveRecord.Model targetModel = null;

            var found = FindPropertyInHasAndBelongsToMany(model, prefix, ref targetType, ref targetModel);

            if (!found) {
                found = FindPropertyInHasMany(model, prefix, ref targetType, ref targetModel);
            }

            if (found) {
                succeeded = true;

                ClearContainer(container);

                if (node.NodeType == NodeType.Indexed) {
                    var indexNode = (IndexedNode) node;

                    var collArray = Array.CreateInstance(targetType, indexNode.ChildrenCount);

                    collArray = (Array) InternalBindObject(collArray.GetType(), prefix, node);

                    foreach(var item in collArray) {
                        AddToContainer(container, item);
                    }

                } else if (node.NodeType == NodeType.Leaf) {
                    var pkModel = targetModel.PrimaryKey;
                    var pkType = pkModel.Value.ReturnedClass;

                    var leafNode = (LeafNode) node;

                    bool convSucceeded;

                    if (leafNode.IsArray) {
                        // Multiples values found
                        foreach(var element in (Array) leafNode.Value) {
                            var keyConverted = Converter.Convert(pkType, leafNode.ValueType.GetElementType(), element, out convSucceeded);

                            if (convSucceeded) {
                                var item = FindByPrimaryKey(targetType, keyConverted);
                                if (item != null) AddToContainer(container, item);
                            }
                        }
                    }
                    else {
                        // Single value found
                        var keyConverted = Converter.Convert(pkType, leafNode.ValueType.GetElementType(), leafNode.Value, out convSucceeded);

                        if (convSucceeded) {
                            var item = FindByPrimaryKey(targetType, keyConverted);
                            AddToContainer(container, item);
                        }
                    }
                }
            }

            return container;
        }

        protected override void SetPropertyValue(object instance, PropertyInfo prop, object value) {
            if (prop.CanRead && !prop.CanWrite && IsContainerType(prop.PropertyType)) {
                var container = prop.GetValue(instance, null);
                var enumerable = value as IEnumerable;
                if (container != null && enumerable != null) {
                    ClearContainer(container);
                    foreach (var o in enumerable) {
                        AddToContainer(container, o);
                    }
                }
                return;
            }

            base.SetPropertyValue(instance, prop, value);
        }

        protected virtual object FindByPrimaryKey(Type targetType, object id) {
            return AR.Find(targetType, id);
        }

        protected override bool IsSpecialType(Type instanceType) {
            return IsContainerType(instanceType);
        }

        protected bool IsBelongsToRef(Castle.ActiveRecord.Model arModel, string prefix) {
            if (arModel.BelongsTos.ContainsKey(prefix))
                return true;

            if (arModel.Metadata.IsInherited) {
                var parentmodel = AR.Holder.GetModel(arModel.Type.BaseType);
                return IsBelongsToRef(parentmodel, prefix);
            }

            return false;
        }

        protected override bool ShouldRecreateInstance(object value, Type type, string prefix, Node node) {
            if (IsContainerType(type)) {
                return true;
            }

            if (node != null && CurrentARModel != null) {
                // If it's a belongsTo ref, we need to recreate it
                // instead of overwrite its properties, otherwise NHibernate will complain
                if (IsBelongsToRef(CurrentARModel, prefix)) {
                    return true;
                }
            }

            return base.ShouldRecreateInstance(value, type, prefix, node);
        }

        protected override void BeforeBindingProperty(object instance, PropertyInfo prop, string prefix, CompositeNode node) {
            base.BeforeBindingProperty(instance, prop, prefix, node);

            if (IsPropertyExpected(prop, node)) {
                ClearExpectedCollectionProperties(instance, prop);
            }
        }

        private bool IsPropertyExpected(PropertyInfo prop, CompositeNode node) {
            var propId = string.Format("{0}.{1}", node.FullName, prop.Name);

            if (expectCollPropertiesList != null) {
                return Array.BinarySearch(expectCollPropertiesList, propId, CaseInsensitiveComparer.Default) >= 0;
            }

            return false;
        }

        private void ClearExpectedCollectionProperties(object instance, PropertyInfo prop) {
            var value = prop.GetValue(instance, null);

            ClearContainer(value);
        }

        private object ObtainPrimaryKeyValue(Castle.ActiveRecord.Model model, CompositeNode node, String prefix, out KeyValuePair<string, IType> pkModel) {
            pkModel = ObtainPrimaryKey(model);

            var pkPropName = pkModel.Key;

            var idNode = node.GetChildNode(pkPropName);

            if (idNode == null) return null;

            if (idNode != null && idNode.NodeType != NodeType.Leaf) {
                throw new BindingException("Expecting leaf node to contain id for ActiveRecord class. " +
                                           "Prefix: {0} PK Property Name: {1}", prefix, pkPropName);
            }

            var lNode = (LeafNode) idNode;

            if (lNode == null) {
                throw new BindingException("ARDataBinder autoload failed as element {0} " +
                                           "doesn't have a primary key {1} value", prefix, pkPropName);
            }

            bool conversionSuc;

            return Converter.Convert(pkModel.Value.ReturnedClass, lNode.ValueType, lNode.Value, out conversionSuc);
        }

        private static KeyValuePair<string, IType> ObtainPrimaryKey(Castle.ActiveRecord.Model model) {
            if (model.Metadata.IsInherited) {
                var parent = AR.Holder.GetModel(model.Type.BaseType);
                return ObtainPrimaryKey(parent);
            }

            return model.PrimaryKey;
        }

        private bool IsValidKey(object id) {
            if (id != null) {
                if (id is string) {
                    return id.ToString() != String.Empty;
                } else if (id is Guid) {
                    if (TreatEmptyGuidAsNull) {
                        return Guid.Empty != ((Guid) id);
                    } else {
                        return true;
                    }
                } else {
                    return Convert.ToInt64(id) != 0;
                }
            }

            return false;
        }

        private static bool IsContainerType(Type type) {
            var isContainerType = type == typeof(IList) || type == typeof(ISet);

            if (!isContainerType && type.IsGenericType) {
                var genericArgs = type.GetGenericArguments();

                var genType = typeof(ICollection<>).MakeGenericType(genericArgs);

                isContainerType = genType.IsAssignableFrom(type);
            }

            return isContainerType;
        }

        private static object CreateContainer(Type type) {
            if (type.IsGenericType) {
                if (type.GetGenericTypeDefinition() == typeof(Iesi.Collections.Generic.ISet<>)) {
                    var genericArgs = type.GetGenericArguments();
                    var genericType = typeof(HashedSet<>).MakeGenericType(genericArgs);
                    return Activator.CreateInstance(genericType);
                } else if (type.GetGenericTypeDefinition() == typeof(IList<>)) {
                    var genericArgs = type.GetGenericArguments();
                    var genericType = typeof(List<>).MakeGenericType(genericArgs);
                    return Activator.CreateInstance(genericType);
                }
            } else {
                if (type == typeof(IList)) {
                    return new ArrayList();
                } else if (type == typeof(ISet)) {
                    return new HashedSet();
                }
            }

            return null;
        }

        private static void ClearContainer(object instance) {
            if (instance is IList) {
                (instance as IList).Clear();
            } else if (instance is ISet) {
                (instance as ISet).Clear();
            }
        }

        private static void AddToContainer(object container, object item) {
            if (container is IList) {
                (container as IList).Add(item);
            } else if (container is ISet) {
                (container as ISet).Add(item);
            } else if (container != null) {
                var itemType = item.GetType();

                var collectionType = typeof(ICollection<>).MakeGenericType(itemType);

                if (collectionType.IsAssignableFrom(container.GetType())) {
                    var addMethod = container.GetType().GetMethod("Add");

                    addMethod.Invoke(container, new [] {item});
                }
            }
        }
    }
}
