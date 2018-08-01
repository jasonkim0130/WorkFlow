namespace WorkFlowLib.Results
{
    public enum ApproveResult
    {
        InvalidFlowCase,
        UnableToSaveUserAction,
        InvalidFlowStep,
        FlowCaseTerminate,
        Approved,
        InvalidUserAction,
        AlreadyHandlered,
        CurrentStepInvalid,
        Rejected,
        FlowApproved,
        FlowRejected,
        Aborted,
        Expired,
        InvalidApprover,
        Error
    }

    public class ReturnApproveResult
    {
        public ApproveResult Result { get; set; }
        public string[] NextApprovers { get; set; }
    }
}