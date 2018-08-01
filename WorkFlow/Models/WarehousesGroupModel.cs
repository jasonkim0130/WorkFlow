using System;

namespace WorkFlow.Models
{
    public class WarehousesGroupModel
    {
        public MainWarehouses Hdr { get; set; }
        public GroupWarehouses[] Detl { get; set; }
    }
    public class MainWarehouses
    {
        public int HDR_Id { get; set; }
        public string WarehouseCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    public class GroupWarehouses
    {
        public string WarehouseCode { get; set; }
        public DateTime OperationStart { get; set; }
        public DateTime OperationEnd { get; set; }
    }
}