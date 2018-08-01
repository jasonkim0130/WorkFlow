using System;
using System.Linq;
using System.Web.Http;
using Newtonsoft.Json;
using Dreamlab.Core;
using WorkFlowLib;
using WorkFlowLib.Data;
using WorkFlowLib.DTO;
using WorkFlowLib.Results;
using Omnibackend.Api.Workflow.Data;

namespace Omnibackend.Api.Controllers.Workflow
{
    [RoutePrefix("workflow/flowcase")]
    [Authorize]
    public class FlowCaseController : BaseWFApiController
    {

        [Route("set_read_status")]
        [HttpGet]
        [HttpPost]
        public object set_read_status(int flowCaseId, int status)
        {
            ApplicationUser manager = new ApplicationUser(Entities, User.Identity.Name);
            bool result = false;
            try
            {
                if (status == 0)
                    result = manager.SetCaseAsUnread(flowCaseId, User.Identity.Name);
                if (status == 1)
                    result = manager.SetCaseAsViewed(flowCaseId, User.Identity.Name);
                return new { ret_code = RetCode.Success, ret_msg = string.Empty };
            }
            catch (Exception ex)
            {
                return new { ret_code = RetCode.Error, ret_msg = ex.Message };
            }
        }

        [Route("set_flag_status")]
        [HttpGet]
        [HttpPost]
        public object set_flag_status(int flowCaseId, int flag)
        {
            WF_FlowCases flow = Entities.WF_FlowCases.FirstOrDefault(p => p.StatusId > 0 && p.FlowCaseId == flowCaseId);
            if (flow != null)
            {
                if (flag == 1)
                    flow.IsFlagged = 1;
                if (flag == 0)
                    flow.IsFlagged = 0;
                Entities.SaveChanges();
                return new { ret_code = RetCode.Success, ret_msg = string.Empty };
            }
            return new { ret_code = RetCode.Failure, ret_msg = "Unable set to read" }; ;
        }

        [Route("comment")]
        [HttpPost]
        public object comment_flowcase(int flowCaseId, string comments, bool ccToAll = false)
        {
            if (flowCaseId < 1)
                return new { ret_code = RetCode.Error, ret_msg = "Invalid flowCaseId" };
            if (string.IsNullOrWhiteSpace(comments))
                return new { ret_code = RetCode.Error, ret_msg = "Comments required" };
            Approver approver = new Approver(Entities, User.Identity.Name);
            CommentStepResult result = approver.CommentStep(flowCaseId, comments, ccToAll);
            if (result == CommentStepResult.Success)
            {
                approver.NotificationSender.Send();
                return new { ret_code = RetCode.Success, ret_msg = string.Empty };
            }
            return new { ret_code = RetCode.Failure, ret_msg = result.ToString() };
        }

        [Route("reject")]
        [HttpPost]
        public object reject_flowcase(UnApproveModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-reject_flowcase", JsonConvert.SerializeObject(model));
            if (model == null)
                return new { ret_code = RetCode.Error, ret_msg = "Invalid api parameter" };
            if (model.flowCaseId < 1)
                return new { ret_code = RetCode.Error, ret_msg = "Invalid flowCaseId" };
            if (string.IsNullOrWhiteSpace(model.comments))
                return new { ret_code = RetCode.Error, ret_msg = "Comments required" };
            Approver approver = new Approver(Entities, User.Identity.Name);
            var result = approver.Reject(model.flowCaseId, model.comments);
            if (result == ApproveResult.Rejected || result == ApproveResult.FlowRejected)
            {
                approver.NotificationSender.Send();
                return new { ret_code = RetCode.Success, ret_msg = string.Empty };
            }
            return new { ret_code = RetCode.Failure, ret_msg = result.ToString() };
        }

        [Route("sendback")]
        [HttpPost]
        public object sendback_flowcase(UnApproveModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-sendback_flowcase", JsonConvert.SerializeObject(model));
            if (model == null)
                return new { ret_code = RetCode.Error, ret_msg = "Invalid api parameter" };
            if (model.flowCaseId < 1)
                return new { ret_code = RetCode.Error, ret_msg = "Invalid flowCaseId" };
            if (string.IsNullOrWhiteSpace(model.comments))
                return new { ret_code = RetCode.Error, ret_msg = "Comments required" };
            Approver approver = new Approver(Entities, User.Identity.Name);
            var result = approver.Abort(model.flowCaseId, model.comments);
            if (result == ApproveResult.Aborted)
            {
                approver.NotificationSender.Send();
                return new { ret_code = RetCode.Success, ret_msg = string.Empty };
            }
            return new { ret_code = RetCode.Failure, ret_msg = result.ToString() };
        }

        [Route("approve")]
        [HttpPost]
        public object approve_flowcase(ApproveModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-approve_flowcase",JsonConvert.SerializeObject(model));
            Approver approver = new Approver(Entities, User.Identity.Name);
            try
            {
                ReturnApproveResult result = approver.Approve(model.flowCaseId, model.nextApprovers);
                if (result.Result == ApproveResult.Approved || result.Result == ApproveResult.FlowApproved)
                {
                    approver.NotificationSender.Send();
                    return new {ret_code = RetCode.Success, ret_msg = string.Empty};
                }
                return new { ret_code = RetCode.Failure, ret_msg = result.ToString() };
            }
            catch (Exception ex)
            {
                return new { ret_code = RetCode.Failure, ret_msg = ex.Message };
            }
        }

        [Route("get_next_approvers")]
        [HttpGet]
        [HttpPost]
        public object get_next_approvers(int flowCaseId)
        {
            Approver approver = new Approver(Entities, User.Identity.Name);
            int curGroupId = approver.GetCurrentStepGroupId(flowCaseId);
            NextStepData nsd = approver.GetNextStepApprovers(flowCaseId,curGroupId);
            if (nsd.EmployeeList == null)
                return null;
            return new {data = nsd.EmployeeList.Select(p => p.Select(q => q))};
        }

        [Route("cancel")]
        [HttpPost]
        public object cancel_flowcase(int flowCaseId)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-cancel_flowcase", JsonConvert.SerializeObject(flowCaseId));
            Applicant manager = new Applicant(Entities, User.Identity.Name);
            var result = manager.Cancel(flowCaseId);
            if (result == CancelFlowResult.Canceled)
            {
                manager.NotificationSender.Send();
                return new { ret_code = RetCode.Success, ret_msg = string.Empty };
            }
            return new { ret_code = RetCode.Failure, ret_msg = result.ToString() };
        }
    }
}
