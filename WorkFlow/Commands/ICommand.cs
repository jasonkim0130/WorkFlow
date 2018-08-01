using System.Threading.Tasks;

namespace WorkFlow.Commands
{
    public interface ICommand
    {
        CmdResult Execute();
        Task<CmdResult> ExecuteAsync();
    }

    public interface ICommand<TResult>
    {
        CmdResult<TResult> Execute();
        Task<CmdResult<TResult>> ExecuteAsync();
    }

    public class CmdResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CmdResult<TResult> : CmdResult
    {
        public TResult Result { get; set; }
    }
}