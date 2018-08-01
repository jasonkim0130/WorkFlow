using System;
using System.Threading.Tasks;

namespace WorkFlowLib
{
    /**
    * Created by jeremy on 5/24/2017 9:00:59 PM.
    */
    public interface ILogWritter
    {
        void WriteLog(string title, string message, string parameter = null);
        void WriteExceptionLog(string title, Exception exception, string parameter = null);
        Task<bool> WriteLogAsync(string title, string message, string parameter = null);
        Task<bool> WriteExceptionLogAsync(string title, Exception exception, string parameter = null);
    }

    public interface ILogSaver : IDisposable
    {
        bool SaveLog(LogItem item);
    }

    public class LogItem
    {
        public string Title { get; set; }
        public string Desc { get; set; }
        public string Message { get; set; }
        public object Params { get; set; }
    }
}