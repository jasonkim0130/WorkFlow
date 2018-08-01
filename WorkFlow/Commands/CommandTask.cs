using System;
using System.Threading.Tasks;

namespace WorkFlow.Commands
{
    public class CommandTask
    {
        public CommandTask ContinueWith(ICommand cmd)
        {
            return ContinueWith(cmd.Execute);
        }

        public CommandTask ContinueWith(Func<CmdResult> func)
        {
            CommandTask newCmd = new CommandTask
            {
                Command = () =>
                {
                    CmdResult res = GetResult();
                    if (res.Success)
                        return func();
                    return new CmdResult { ErrorMessage = res.ErrorMessage, Success = false };
                }
            };
            return newCmd;
        }

        public CommandTask<TResult2> ContinueWith<TResult2>(ICommand<TResult2> cmd)
        {
            return ContinueWith(cmd.Execute);
        }

        public CommandTask<TResult2> ContinueWith<TResult2>(Func<CmdResult<TResult2>> func)
        {
            CommandTask<TResult2> newCmd = new CommandTask<TResult2>
            {
                Command = () =>
                {
                    CmdResult res = GetResult();
                    if (res.Success)
                        return func();
                    return new CmdResult<TResult2> { ErrorMessage = res.ErrorMessage, Success = false };
                }
            };
            return newCmd;
        }

        public Func<CmdResult> Command { get; set; }

        public CmdResult GetResult()
        {
            return Command();
        }

        public Task<CmdResult> GetResultAsync()
        {
            return Task.Run(Command);
        }
    }

    public class CommandTask<TResult>
    {
        public CommandTask<TResult2> ContinueWith<TResult2>(Func<TResult, ICommand<TResult2>> cmdFunc)
        {
            return ContinueWith(p => cmdFunc(p).Execute());
        }

        public CommandTask<TResult2> ContinueWith<TResult2>(Func<ICommand<TResult2>> cmdFunc)
        {
            return ContinueWith(p => cmdFunc().Execute());
        }

        public CommandTask<TResult2> ContinueWith<TResult2>(ICommand<TResult2> cmd)
        {
            return ContinueWith(p => cmd.Execute());
        }

        public CommandTask<TResult2> ContinueWith<TResult2>(Func<CmdResult<TResult2>> func)
        {
            return ContinueWith(p => func());
        }

        public CommandTask<TResult2> ContinueWith<TResult2>(Func<TResult, CmdResult<TResult2>> func)
        {
            CommandTask<TResult2> newCmd = new CommandTask<TResult2>
            {
                Command = () =>
                {
                    CmdResult<TResult> res = GetResult();
                    if (res.Success)
                        return func(res.Result);
                    return new CmdResult<TResult2> { ErrorMessage = res.ErrorMessage, Success = false };
                }
            };
            return newCmd;
        }

        public CommandTask ContinueWith(ICommand cmd)
        {
            return ContinueWith(p => cmd.Execute());
        }

        public CommandTask ContinueWith(Func<TResult, CmdResult> func)
        {
            CommandTask newCmd = new CommandTask
            {
                Command = () =>
                {
                    CmdResult<TResult> res = GetResult();
                    if (res.Success)
                        return func(res.Result);
                    return new CmdResult { ErrorMessage = res.ErrorMessage, Success = false };
                }
            };
            return newCmd;
        }

        public CommandTask ContinueWith(Func<TResult, ICommand> func)
        {
            return ContinueWith(p => func(p).Execute());
        }

        public Func<CmdResult<TResult>> Command { get; set; }

        public CmdResult<TResult> GetResult()
        {
            return Command();
        }

        public Task<CmdResult<TResult>> GetResultAsync()
        {
            return Task.Run(Command);
        }

        public CommandTask<TResult> KeepValue(KeptValue<TResult> keptValue)
        {
            return this.ContinueWith(res =>
            {
                keptValue.Value = res;
                return new CmdResult<TResult> { Success = true, Result = res };
            });
        }
    }

    public class KeptValue<T>
    {
        public T Value { get; set; }
    }
}
