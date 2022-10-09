using System.ComponentModel.DataAnnotations;

namespace wordslab.manager.config
{
    public class CloudAccountConfig : BaseConfig
    {
        private CloudAccountConfig() : this(null, null) { }

        public CloudAccountConfig(string accountName, string credentials)
        {
            AccountName = accountName;
            Credentials = credentials;
        }

        // Connexion

        [Key]
        public string AccountName { get; set; }
        
        public string Credentials { get; set; }

        // Quotas and limlits

        public CloudAccountQuotas Quotas { get; set; }

        public string BillingCurrency { get; set; }

        public int MonthlyFixedBillLimit { get; set; }

        public int MonthlyUsageBillLimit { get; set; }
    }
}
