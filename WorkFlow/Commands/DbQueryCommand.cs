using Dreamlab.Db;

namespace WorkFlow.Commands
{
    public abstract class DbQueryCommand<TResult> : DbCommand<TResult[]>
    {
        public override CmdResult<TResult[]> Execute()
        {
            ExecutedResult<TResult[]> res = Repository.GetDataObjects<TResult>(GetSql());
            return new CmdResult<TResult[]>
            {
                ErrorMessage = res.ErrorMessage,
                Success = res.Success,
                Result = res.ReturnValue
            };
        }
    }

    public abstract class DbSingleQueryCommand<TResult> : DbCommand<TResult>
    {
        public override CmdResult<TResult> Execute()
        {
            ExecutedResult<TResult> res = Repository.GetDataObject<TResult>(GetSql());
            return new CmdResult<TResult>
            {
                ErrorMessage = res.ErrorMessage,
                Success = res.Success,
                Result = res.ReturnValue
            };
        }
    }

    public abstract class DbSingleQueryCommand<TParameter, TResult> : DbCommand<TResult>
    {
        public TParameter Parameter { get; set; }
        public override CmdResult<TResult> Execute()
        {
            ExecutedResult<TResult> res = Repository.GetDataObject<TResult>(GetSql(), Parameter);
            return new CmdResult<TResult>
            {
                ErrorMessage = res.ErrorMessage,
                Success = res.Success,
                Result = res.ReturnValue
            };
        }
    }

    public abstract class DbQueryCommand<TParameter, TResult> : DbCommand<TResult[]>
    {
        public TParameter Parameter { get; set; }
        public override CmdResult<TResult[]> Execute()
        {
            ExecutedResult<TResult[]> res = Repository.GetDataObjects<TResult>(GetSql(), Parameter);
            return new CmdResult<TResult[]>
            {
                ErrorMessage = res.ErrorMessage,
                Success = res.Success,
                Result = res.ReturnValue
            };
        }
    }

    public abstract class PagerDbQueryCommand<TParameter, TResult> : DbCommand<PagerData<TResult[]>> where TResult : new()
    {
        public TParameter Parameter { get; set; }
        public override CmdResult<PagerData<TResult[]>> Execute()
        {
            ExecutedResult<TResult[]> res = Repository.GetDataObjects<TResult>(GetSql(), Parameter);
            ExecutedResult<int> total = Repository.GetScalar<int>(GetCountSql(), Parameter);
            return new CmdResult<PagerData<TResult[]>>
            {
                ErrorMessage = res.ErrorMessage,
                Success = res.Success,
                Result = new PagerData<TResult[]>
                {
                    Data = res.ReturnValue,
                    Total = total.ReturnValue
                }
            };
        }
        public virtual string GetCountSql()
        {
            return null;
        }
    }
}