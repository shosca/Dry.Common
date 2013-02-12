using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using NHibernate.Cfg;
using NHibernate.Event;
using NHibernate.Search.Event;

namespace Dry.Common.ActiveRecord {
    public class NHibernateSearchContributor : INHContributor {
        public void Contribute(Configuration cfg) {
            if (!cfg.Properties.ContainsKey(NHibernate.Search.Environment.AnalyzerClass)) return;

            var eventlistener = new FullTextIndexEventListener();
            var collectioneventlistener = new FullTextIndexCollectionEventListener();

            cfg.AppendListeners(ListenerType.PostDelete, new IPostDeleteEventListener[] {eventlistener});
            cfg.AppendListeners(ListenerType.PostInsert, new IPostInsertEventListener[] {eventlistener});
            cfg.AppendListeners(ListenerType.PostUpdate, new IPostUpdateEventListener[] {eventlistener});
            cfg.AppendListeners(ListenerType.PostCollectionRecreate, new IPostCollectionRecreateEventListener[] {collectioneventlistener});
            cfg.AppendListeners(ListenerType.PostCollectionRemove, new IPostCollectionRemoveEventListener[] {collectioneventlistener});
            cfg.AppendListeners(ListenerType.PostCollectionUpdate, new IPostCollectionUpdateEventListener[] {collectioneventlistener});
        }
    }
}
