#region using

using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Components.Binder;

#endregion

namespace Dry.Common.Queries {
    public static class NodeExtensions {
        public static string GetParameter(this CompositeNode node, string param) {
            var childnode = node.GetChildNode(param) as LeafNode;
            return childnode == null ? string.Empty : childnode.Value.ToString();
        }

        public static IEnumerable<CompositeNode> Descendents(this CompositeNode node) {
            if (node == null) throw new ArgumentNullException("node");
            foreach (var child in node.ChildNodes.OfType<CompositeNode>()) {
                yield return child;
                foreach (var desc in child.Descendents()) {
                    yield return desc;
                }
            }
        }
    }
}
