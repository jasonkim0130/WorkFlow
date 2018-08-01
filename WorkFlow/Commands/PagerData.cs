namespace WorkFlow.Commands
{
    public class PagerData<T>
    {
        public int Total { get; set; }
        public T Data { get; set; }
    }
}
