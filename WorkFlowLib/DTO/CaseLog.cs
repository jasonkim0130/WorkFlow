using System;

namespace WorkFlowLib.DTO
{
    public class CaseLog
    {
        public DateTime Created { get; set; }
        public string ReceiverUser { get; set; }
        public string SenderUser { get; set; }
        public int ActionId { get; set; }
        public string LogType { get; set; }
        public int MessageId { get; set; }
        public int? MessageTypeId { get; set; }
        public string Comments { get; set; }
    }
}