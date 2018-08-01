using System.Collections.Generic;

namespace WorkFlowLib.Parameters
{
    public class SetNotifyMsgPara
    {
        public string Content { get; set; }
        public IEnumerable<string> TargetShops { get; set; }
    }
}
