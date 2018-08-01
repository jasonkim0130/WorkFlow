namespace WorkFlow.Models
{
    public class WarningConfig
    {
        public string ConfigMode { get; set; }
        public string ConfirmMode { get; set; }
        public int? MaxQuantity { get; set; }
        public BuyerWarningBehavior WarningBehavior { get; set; }

        public static WarningConfig Default()
        {
            return new WarningConfig()
            {
                MaxQuantity = 10,
                WarningBehavior = new BuyerWarningBehavior() { Orders = 10, WithinHours = 10 }
            };
        }
    }

    public class BuyerWarningBehavior
    {
        public int WithinHours { get; set; }
        public int Orders { get; set; }
    }
}