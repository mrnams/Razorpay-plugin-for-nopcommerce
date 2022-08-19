using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Razorpay.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.Razorpay.MerchantId")]
        public string MerchantId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Razorpay.Key")] //Encryption Key
        public string Key { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Razorpay.MerchantParam")]
        public string MerchantParam { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Razorpay.PayUri")] //Payment URI
        public string PayUri { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Razorpay.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Razorpay.AccessCode")] //Access Code
        public string AccessCode { get; set; }
    }
}