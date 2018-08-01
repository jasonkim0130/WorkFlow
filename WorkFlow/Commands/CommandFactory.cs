using System;

namespace WorkFlow.Commands
{
    public class CommandFactory
    {
        public static CommandTask StartNew(ICommand cmd)
        {
            return new CommandTask { Command = () => cmd.Execute() };
        }

        public static CommandTask StartNew(CmdResult result)
        {
            return new CommandTask { Command = () => result };
        }

        public static CommandTask StartNew(Func<CmdResult> func)
        {
            return new CommandTask { Command = func };
        }

        public static CommandTask<TResult> StartNew<TResult>(ICommand<TResult> cmd)
        {
            return new CommandTask<TResult> { Command = () => cmd.Execute() };
        }

        public static CommandTask<TResult> StartNew<TResult>(Func<CmdResult<TResult>> func)
        {
            return new CommandTask<TResult> { Command = func };
        }
    }
}