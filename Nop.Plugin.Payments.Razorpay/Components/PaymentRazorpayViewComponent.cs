using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Razorpay.Components
{
    [ViewComponent(Name = RazorpayDefaults.VIEW_COMPONENT_NAME)]
    public class PaymentrazorpayViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.Razorpay/Views/PaymentInfo.cshtml");
        }
    }
}
