#region using

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Castle.Components.Binder;
using Dry.Common;

#endregion

namespace Dry.Common.Binder {
    public class CustomTreeBuilder {
        public CompositeNode BuildSourceNode(NameValueCollection nameValueCollection) {
            var root = new CompositeNode("root");

            PopulateTree(root, nameValueCollection);

            return root;
        }

        public void PopulateTree(CompositeNode root, NameValueCollection nameValueCollection) {
            foreach (var key in nameValueCollection.AllKeys.OrderBy(a => a)) {
                if (key == null) continue;
                var vals = nameValueCollection.GetValues(key);

                if (vals == null) continue;

                var names = new Stack<string>(key.SplitAndTrim(".").Reverse());

                RecursiveProcessNode(root, names, vals);
            }
        }

        protected void RecursiveProcessNode(CompositeNode node, Stack<string> names, string[] value) {
            var name = names.Pop();
            var indexed = name.SplitAndTrim("[", "]").ToArray();

            if (names.IsEmpty()) {
                // leaf or indexed node
                if (indexed.Length > 1) {
                    AddLeafNode(node, typeof(string), name, value.FirstOrDefault());
                } else if (name.EndsWith("[]")) {
                    value = value
                        .SelectMany(a => a.SplitAndTrim(","))
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToArray();
                    if (value.Length < 1 || string.IsNullOrEmpty(value.Join(","))) {
                        GetOrCreateIndexedNode(node, name);
                    } else {
                        AddLeafNode(node, typeof(string[]), name, value);
                    }
                } else {
                    AddLeafNode(node, typeof(string), name, value.FirstOrDefault());
                }
            } else {
                // composite or indexed node
                if (indexed.Length <= 1) {
                    var newnode = GetOrCreateCompositeNode(node, name);
                    RecursiveProcessNode(newnode, names, value);
                } else if (name.EndsWith("[]")) {
                    // this is weird.
                } else {
                    var newnode = GetOrCreateIndexedNode(node, indexed.FirstOrDefault());
                    newnode = GetOrCreateCompositeNode(newnode, indexed.LastOrDefault());
                    RecursiveProcessNode(newnode, names, value);
                }
            }
        }

        string NormalizeKey(string key) {
            return key.EndsWith("[]") ? key.Substring(0, key.Length - 2) : key;
        }

        void AddLeafNode(CompositeNode parent, Type type, string nodeName, object value) {
            parent.AddChildNode(new LeafNode(type, NormalizeKey(nodeName), value));
        }

        CompositeNode GetOrCreateCompositeNode(CompositeNode parent, string nodeName) {
            nodeName = NormalizeKey(nodeName);
            var node = parent.GetChildNode(nodeName);

            if (node != null && node.NodeType != NodeType.Composite) {
                throw new BindingException("Attempt to create or obtain a composite node " +
                                           "named {0}, but a node with the same exists with the type {1}", nodeName, node.NodeType);
            }

            if (node == null) {
                node = new CompositeNode(nodeName);
                parent.AddChildNode(node);
            }

            return (CompositeNode) node;
        }

        CompositeNode GetOrCreateIndexedNode(CompositeNode parent, string nodeName) {
            nodeName = NormalizeKey(nodeName);
            var node = parent.GetChildNode(nodeName);

            if (node != null && node.NodeType != NodeType.Indexed) {
                throw new BindingException("Attempt to create or obtain an indexed node " +
                                           "named {0}, but a node with the same exists with the type {1}", nodeName, node.NodeType);
            }

            if (node == null) {
                node = new IndexedNode(nodeName);
                parent.AddChildNode(node);
            }

            return (IndexedNode) node;
        }
    }
}
