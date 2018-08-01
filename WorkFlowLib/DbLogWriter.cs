using System;
using System.Threading.Tasks;
using Dreamlab.Db;

namespace WorkFlowLib
{
    /**
    * Created by jeremy on 5/24/2017 9:03:09 PM.
    */
    public class DbLogWriter : ILogWritter
    {
        private IDBRepository _dbRepository;

        public DbLogWriter()
        {

        }

        public DbLogWriter(IDBRepository dbRepository)
        {
            _dbRepository = dbRepository;
        }

        public void WriteLog(string title, string message, string parameter = null)
        {
            SaveLog(new LogItem { Title = title, Message = message, Params = parameter });
        }

        public void WriteExceptionLog(string title, Exception exception, string parameter = null)
        {
            SaveLog(new LogItem
            {
                Title = title,
                Message = exception.Message,
                Params = parameter
            });
        }

        private void SaveLog(LogItem item)
        {
            if (_dbRepository == null)
            {
                using (_dbRepository = new DBRepository(Codehelper.ConnectionStr))
                {
                    InsertLogToDb(item);
                }
            }
            else
            {
                InsertLogToDb(item);
            }
        }

        private void InsertLogToDb(LogItem item)
        {
            _dbRepository.Execute(@"INSERT INTO dbo.SystemLogs
(
    Description,
    Created,
    StatusID,
    LogTypeId,
    LogTitle,
    Parameter
)
VALUES(@0,GETDATE(), 1, 1, @1, @2)",
                 item.Desc + item.Message, item.Title, item.Params);
        }

        public async Task<bool> WriteLogAsync(string title, string message, string parameter = null)
        {
            return await Task.Run(() =>
            {
                WriteLog(title, message, parameter);
                return true;
            });
        }

        public async Task<bool> WriteExceptionLogAsync(string title, Exception exception, string parameter = null)
        {
            await Task.Run(() =>
            {
                WriteExceptionLog(title, exception, parameter);
                return true;
            });
            return true;
        }
    }
}