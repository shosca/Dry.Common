using System;
using Dry.Common.Model;
using NHibernate;
using NHibernate.Event;
using NHibernate.Persister.Entity;
using Dry.Common.ActiveRecord;

namespace Dry.Common.Model {
    public abstract class AuditEventListener<T> : IPreUpdateEventListener, IPreInsertEventListener, IPreCollectionUpdateEventListener, IPreCollectionRecreateEventListener, IPreCollectionRemoveEventListener where T : class {
        public bool OnPreUpdate(PreUpdateEvent e) {
            var u = e.Entity as IModifyAuditable<T>;
            if (u != null) {
                u.ModifiedBy = GetEntity(e.Session);
                SetModified(e.Persister, e.State, u);
            }
            return false;
        }

        public bool OnPreInsert(PreInsertEvent e) {
            var i = e.Entity as ICreateAuditable<T>;
            if (i != null) {
                if (i.CreatedBy != null)
                    i.CreatedBy = GetEntity(e.Session);
                SetCreated(e.Persister, e.State, i);
            }

            var u = e.Entity as IModifyAuditable<T>;
            if (u != null) {
                if (u.ModifiedBy != null)
                    u.ModifiedBy = GetEntity(e.Session);
                SetModified(e.Persister, e.State, u);
            }
            return false;
        }

        static void SetModified(IEntityPersister persister, object[] state, IModifyAuditable<T> u) {
            NHEventHelper.Set(persister, state, "ModifiedAt", DateTime.Now);
            NHEventHelper.Set(persister, state, "ModifiedBy", u.ModifiedBy);
        }

        static void SetCreated(IEntityPersister persister, object[] state, ICreateAuditable<T> u) {
            NHEventHelper.Set(persister, state, "CreatedAt", DateTime.Now);
            NHEventHelper.Set(persister, state, "CreatedBy", u.CreatedBy);
        }


        public abstract T GetEntity(ISession session);

        public void OnPreUpdateCollection(PreCollectionUpdateEvent e) {
            OnCollection(e);
        }

        public void OnPreRecreateCollection(PreCollectionRecreateEvent e) {
            OnCollection(e);
        }

        public void OnPreRemoveCollection(PreCollectionRemoveEvent e) {
            OnCollection(e);
        }

        void OnCollection(AbstractCollectionEvent e) {
            var affected = e.AffectedOwnerOrNull as IModifyAuditable<T>;
            if (affected == null) return;

            affected.ModifiedBy = GetEntity(e.Session);
            affected.ModifiedAt = DateTime.Now;
        }
    }
}
