using System.ComponentModel.DataAnnotations;
using WorkFlowLib.DTO;

namespace WorkFlowLib
{
    public class UserSearchModel
    {
        public ApproverType approverType { get; set; }
        [Required]
        public int flowId { get; set; }
        public PredefinedRole predefinedrole { get; set; }
        public RoleCriteria rolecriteria { get; set; }
        public ManagerOption manageroption { get; set; }
        public string approver { get; set; }
        public NoApproverModel[] NoApproverModel { get; set; }
        public SecretaryRule[] secretaryRules { get; set; }

        public bool IsValid()
        {
            if (approverType == ApproverType.Person)
                return !string.IsNullOrWhiteSpace(approver);
            if (approverType == ApproverType.RoleCriteria)
                return !string.IsNullOrWhiteSpace(rolecriteria?.gradeoperator) && rolecriteria.grade > 0;
            return true;
        }
    }

    public class SecretaryRule
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string SecretaryId { get; set; }
        public string SecretaryName { get; set; }
    }
}