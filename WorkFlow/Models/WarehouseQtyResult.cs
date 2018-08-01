namespace WorkFlow.Models
{
    public class WarehouseQtyResult
    {
        public int ViewAllQty
        {
            get { return PreOrdered + AssignedNum + PickingNum + PickedNum + PackingNum + PackedNum + ShippingNum; }
        }
        public int PackDisplayQty
        {
            get { return PickedNum + PackingNum; }
        }
        public int ProcessingQty
        {
            get { return PickingNum + PickedNum + PackingNum + PackedNum + ShippingNum; }
        }
        public string WarehouseCode { get; set; }
        public int PickingNum { get; set; }
        public int PackedNum { get; set; }
        public int ShippedNum { get; set; }
        public int AssignedNum { get; set; }
        public int PickedNum { get; set; }
        public int PackingNum { get; set; }
        public int ShippingNum { get; set; }
        public int RfAssignedNum { get; set; }
        public int PreOrdered { get; set; }
        public int ShippedOutNum { get; set; }
        public int ProblemNum { get; set; }
    }
}