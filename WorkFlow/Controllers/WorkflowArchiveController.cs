using System.Linq;
using System.Web.Mvc;
using WorkFlowLib;
using WorkFlowLib.Data;
using WorkFlowLib.DTO.Query;

namespace WorkFlow.Controllers
{
    public class WorkflowArchiveController : WFController
    {
        // GET: Workflow/WorkflowArchive
        public ActionResult Index()
        {
            WF_FlowTypes[] types = WFEntities.WF_FlowTypes.Where(p => p.StatusId > 0).ToArray();
            return PartialView(types);
        }
               
        public ActionResult ListApplication()
        {
            ApplicationUser manager = new ApplicationUser(WFEntities, this.Username);
            var data = manager.DraftQuery.OrderByDescending(p => p.Created)
                .Select(p => new PendingQueryRow
                {
                    IsFlagged = p.IsFlagged,
                    FlowCaseId = p.FlowCaseId,
                    Subject = p.Subject,
                    Department = p.Department,
                    Rejected = p.Rejected,
                    Aborted = p.ReviseAbort,
                    Applicant = p.UserNo,
                    Type = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                    CompletedGroup = p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0 && q.OrderId <= p.WF_StepGroups.OrderId),
                    TotalGroup = p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0),
                    SubmitDate = p.Created,
                    LastApproverViewed = p.LastApproverViewed,
                    Ver = p.Ver,
                    IsDraft = p.StatusId == 0,
                    HasAttachment = p.WF_FlowCases_Attachments.Any(q => q.StatusId > 0)
                });
            ViewBag.currentUser = this.Username;
            ViewBag.IsDraft = true;
            return PartialView(data);
        }        
    }
}