using System;
using Castle.ActiveRecord;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Dry.Common.ActiveRecord.Mapping;
using Dry.Common.ActiveRecord.Model;
using Dry.Common.Model;

[assembly: TablePrefix("common")]

namespace Dry.Common.Model {
    public class Mapping : DefaultMappingContributor {
        public class MapFileInfo : ClassMapping<FileInfo> {
            public MapFileInfo() {
                this.IdGuidComb();
                this.Table();

                Property(x => x.MimeType);
                Property(x => x.FileName);
                Property(x => x.Etag);
                Property(x => x.Size);

                ManyToOne(
                    x => x.Data,
                    m => {
                        m.Insert(false);
                        m.Update(false);
                        m.Column("id");
                        m.Lazy(LazyRelation.Proxy);
                    }
                );
            }
        }

        public class MapFileData : ClassMapping<FileData> {
            public MapFileData() {
                Id(x => x.Id, m => m.Generator(Generators.Foreign<FileData>(x => x.FileInfo)));
                Table(ActiveRecord.Mapping.Conventions.TableName<FileInfo>());
                OneToOne(x => x.FileInfo, m => { });
                Property(x => x.Data, m => m.Length(Int32.MaxValue));
            }
        }

        public class HitLogMap : ClassMapping<HitLog> {
            public HitLogMap() {
                this.IdHilo(1000);
                this.Table();

                Property(x => x.ClientHost, m => m.NotNullable(true));
                Property(x => x.LogTime, m => m.NotNullable(true));
                Property(x => x.Username, m => m.NotNullable(true));
                Property(x => x.Method, m => m.NotNullable(true));
                Property(x => x.Target, m => m.NotNullable(true));
                Property(x => x.Parameters, m => m.Length(Int16.MaxValue));
                Property(x => x.ServiceStatus, m => m.NotNullable(true));
                Property(x => x.UserAgent, m => m.Length(2000));
                Property(x => x.ResponseTime);
            }
        }
    }
}
