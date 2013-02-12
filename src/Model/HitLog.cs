#region using

using System;
using Dry.Common.ActiveRecord.Model;

#endregion

namespace Dry.Common.Model {
    public class HitLog : BaseHiLoModel<HitLog> {
        public virtual string ClientHost { get; set; }
        public virtual DateTime LogTime { get; set; }
        public virtual string Username { get; set; }
        public virtual string Method { get; set; }
        public virtual string Target { get; set; }
        public virtual string Parameters { get; set; }
        public virtual int ServiceStatus { get; set; }
        public virtual string UserAgent { get; set; }
        public virtual TimeSpan ResponseTime { get; set;  }
    }
}
