using System.Collections.Generic;
using System.Linq;

namespace WorkFlow.Models
{
    public class QtySummaryResult
    {
        public int OrderedNum { get; set; }
        public int ApprovedNum { get; set; }
        public int AllProcessingNum { get; set; }
        public int ShippedRefundNum { get; set; }
        public int UnshippedRefundNum { get; set; }
        public int NewCommingCount { get; set; }
        public int ProblemCount { get; set; }
        public int CancelledCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int EditableCount { get; set; }
        public Dictionary<string, WarehouseQtyResult> WarehouseResults { get; set; }
        public int LogisticCommonNum { get; set; }
        public int LogisticProblemNum { get; set; }
        public int LogisticCancelledNum { get; set; }
        public int LogisticOutOfStockNum { get; set; }

        public int GetAssignedQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].AssignedNum : 0);
        }

        public int GetPreOrderQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].PreOrdered : 0);
        }

        public int GetViewAllQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].ViewAllQty : 0);
        }

        public int GetProcessingQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].ProcessingQty : 0);
        }

        public int GetPickingQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].PickingNum : 0);
        }

        public int GetPackQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].PackDisplayQty : 0);
        }

        public int GetPackedQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].PackedNum : 0);
        }

        public int GetPickedQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].PickedNum : 0);
        }

        public int GetShippedQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].ShippedNum : 0);
        }

        public int GetShippedOutQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].ShippedOutNum : 0);
        }

        public int? GetRFAssignedQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].RfAssignedNum : 0);
        }

        public int? GetProblemQty(params string[] codes)
        {
            return codes.Sum(code => WarehouseResults.ContainsKey(code) ? WarehouseResults[code].ProblemNum : 0);
        }

        public object GetJSON(List<WarehousesGroupModel> group = null)
        {
            return new
            {
                Page = (object) new[]
                {
                    new {status = "Ordered", value = OrderedNum, expression = ".ReviewOrders"},
                    new {status = "EditOrders", value = EditableCount, expression = ".EditOrders"},
                    new {status = "Approved", value = ApprovedNum, expression = ".LogisticsConfig"},
                },
                Warehouses =
                group != null
                    ? WarehouseResults.Where(p => group.Any(q => q.Hdr.WarehouseCode == p.Key)).Select(p =>
                    {
                        var whs =
                            group.First(g => g.Hdr.WarehouseCode == p.Key)
                                .Detl.Select(d => d.WarehouseCode)
                                .Concat(new[] {p.Key}).ToArray();
                        return new
                        {
                            code = p.Key,
                            values = new
                            {
                                total = GetViewAllQty(whs),
                                num = GetAssignedQty(whs) + GetPreOrderQty(whs),
                                processingNum = GetProcessingQty(whs),
                                assignednum = GetAssignedQty(whs),
                                pickingnum = GetPickingQty(whs),
                                pickednum = GetPickedQty(whs),
                                packnum = GetPackQty(whs),
                                packednum = GetPackedQty(whs),
                                shippednum = GetShippedQty(whs)
                            }
                        };
                    })
                    : WarehouseResults.Select(p => new
                    {
                        code = p.Key,
                        values = new
                        {
                            total = p.Value.ViewAllQty,
                            num = p.Value.AssignedNum + p.Value.PreOrdered,
                            processingNum = p.Value.ProcessingQty,
                            assignednum = p.Value.AssignedNum,
                            pickingnum = p.Value.PickingNum, 
                            pickednum = p.Value.PickedNum,
                            packnum = p.Value.PickedNum + p.Value.PackingNum,
                            packednum = p.Value.PackedNum + p.Value.ShippingNum,
                            shippednum = p.Value.ShippedNum
                        }
                    })
            };
        }

        public WarehouseQtyResult GetWarehouseResult(string warehouseCode)
        {
            return WarehouseResults.ContainsKey(warehouseCode) ? WarehouseResults[warehouseCode] : null;
        }
    }
}