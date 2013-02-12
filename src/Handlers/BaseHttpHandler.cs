#region using

using System;
using System.Threading.Tasks;
using System.Web;

#endregion

namespace Dry.Common.Handlers {
    public abstract class BaseHttpHandler : IHttpAsyncHandler {
        protected abstract Task ProcessRequestAsync(HttpContext context);

        Task ProcessRequestAsync(HttpContext context, AsyncCallback cb) {
            return ProcessRequestAsync(context).ContinueWith(task => cb(task));
        }

        public void ProcessRequest(HttpContext context) {
            ProcessRequestAsync(context).Wait();
        }

        public bool IsReusable {
            get { return true; }
        }

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extradata) {
            return ProcessRequestAsync(context, cb);
        }

        public void EndProcessRequest(IAsyncResult result) {
            if (result == null) return;

            ((Task)result).Dispose();
        }
    }
}
