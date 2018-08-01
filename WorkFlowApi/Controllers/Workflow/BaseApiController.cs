using System.Web.Http;
using WorkFlowLib;
using WorkFlowLib.Data;

namespace Omnibackend.Api.Controllers.Workflow
{
    public class BaseWFApiController : ApiController
    {
        public WorkFlowEntities Entities { get; private set; }
        public readonly string Dir = Codehelper.IsUat ? "c:\\inetpub\\wf-files-uat\\" : "c:\\inetpub\\wf-files\\";

        public BaseWFApiController()
        {
            Entities = new WorkFlowEntities();
        }

        protected override void Dispose(bool disposing)
        {
            Entities.Dispose();
            base.Dispose(disposing);
        }
    }
}