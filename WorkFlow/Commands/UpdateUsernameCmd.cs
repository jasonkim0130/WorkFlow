using Dreamlab.Db;

namespace WorkFlow.Commands
{
    public class UpdateUsernameCmd: DbNonQueryCommand<UserPara>
    {
        public UpdateUsernameCmd(IDBRepository dbRepository)
        {
            this.Repository = dbRepository;
        }

        public override string GetSql()
        {
            return @"
IF EXISTS (SELECT 1 FROM dbo.Users WHERE UserNo=@UserNo)
UPDATE dbo.Users SET Username=@Username WHERE UserNo=@UserNo
ELSE
INSERT INTO dbo.Users
(
    UserNo,
    Username,
    Created,
    StatusId,
    LastDeviceLang,
    RegistrationToken
)
VALUES
(   @UserNo,        
    @Username,      
    GETDATE(),
    1,       
    NULL,    
    NULL     
    )";
        }

        public override CmdResult Execute()
        {
            ExecutedResult<int?> res = Repository.GetScalar<int?>(GetSql(), new
            {
                UserNo = Parameter.UserNo,
                Username = Parameter.Username
            });
            return new CmdResult<int>
            {
                Success = res.Success,
                ErrorMessage = res.ErrorMessage
            };
        }
    }
    
    public class UserPara
    {
        public string UserNo { get; set; }
        public string Username { get; set; }
    }
}
