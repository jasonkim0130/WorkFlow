using Dreamlab.Db;

namespace WorkFlow.Commands
{
    public abstract class DbNonQueryCommand<TParameter> : DbCommand
    {
        public TParameter Parameter { get; set; }
        public override CmdResult Execute()
        {
            ExecutedResult<int?> res = Repository.GetScalar<int?>(GetSql(), GetCmdParameter());
            return new CmdResult
            {
                ErrorMessage = res.ErrorMessage,
                Success = res.ReturnValue.HasValue && res.ReturnValue.Value > 0
            };
        }

        public virtual object GetCmdParameter()
        {
            return Parameter;
        }
    }

    public abstract class DbParalessNonQueryCommand : DbCommand
    {
        public override CmdResult Execute()
        {
            ExecutedResult<int?> res = Repository.GetScalar<int?>(GetSql());
            return new CmdResult
            {
                ErrorMessage = res.ErrorMessage,
                Success = res.ReturnValue.HasValue && res.ReturnValue.Value > 0
            };
        }
    }
}
