namespace WorkFlow.Commands
{
    public abstract class Command<TParameter, TResult> : Command<TResult>
    {
        public TParameter Paramter { get; set; }
    }
}