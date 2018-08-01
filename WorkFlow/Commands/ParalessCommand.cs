using System.Text;
using System.Threading.Tasks;

namespace WorkFlow.Commands
{
    public abstract class Command<TResult> : ICommand<TResult>
    {
        public StringBuilder Logs { get; set; }
        public abstract CmdResult<TResult> Execute();
        public Task<CmdResult<TResult>> ExecuteAsync()
        {
            return Task.Run(() => Execute());
        }
    }

    public abstract class Command : ICommand
    {
        public StringBuilder Logs { get; set; }
        public abstract CmdResult Execute();
        public Task<CmdResult> ExecuteAsync()
        {
            return Task.Run(() => Execute());
        }
    }
}