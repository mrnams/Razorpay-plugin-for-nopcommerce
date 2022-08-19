using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using CCA.Util;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Razorpay.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Razorpay.Controllers
{
    public class PaymentRazorpayController : BasePaymentController
    {
        private readonly RazorpayPaymentSettings _razorpayPaymentSettings;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;

        public PaymentRazorpayController(RazorpayPaymentSettings razorpayPaymentSettings,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IPaymentPluginManager paymentPluginManager,
            IPermissionService permissionService,
            ISettingService settingService)
        {
            _razorpayPaymentSettings = razorpayPaymentSettings;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _paymentPluginManager = paymentPluginManager;
            _permissionService = permissionService;
            _settingService = settingService;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var model = new ConfigurationModel
            {
                MerchantId = _razorpayPaymentSettings.MerchantId,
                Key = _razorpayPaymentSettings.Key,
                MerchantParam = _razorpayPaymentSettings.MerchantParam,
                PayUri = _razorpayPaymentSettings.PayUri,
                AdditionalFee = _razorpayPaymentSettings.AdditionalFee,
                AccessCode = _razorpayPaymentSettings.AccessCode
            };

            return View("~/Plugins/Payments.Razorpay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //save settings
            _razorpayPaymentSettings.MerchantId = model.MerchantId;
            _razorpayPaymentSettings.Key = model.Key;
            _razorpayPaymentSettings.MerchantParam = model.MerchantParam;
            _razorpayPaymentSettings.PayUri = model.PayUri;
            _razorpayPaymentSettings.AdditionalFee = model.AdditionalFee;
            _razorpayPaymentSettings.AccessCode = model.AccessCode;
            await _settingService.SaveSettingAsync(_razorpayPaymentSettings);

            return await Configure();
        }

        public async Task<ActionResult> Return()
        {
            if (!(await _paymentPluginManager.LoadPluginBySystemNameAsync("Payments.Razorpay") is RazorpayPaymentProcessor processor) ||
                !_paymentPluginManager.IsPluginActive(processor) ||
                !processor.PluginDescriptor.Installed)
                throw new NopException("Razorpay module cannot be loaded");

            //assign following values to send it to verifychecksum function.
            if (string.IsNullOrWhiteSpace(_razorpayPaymentSettings.Key))
                throw new NopException("Razorpay key is not set");

            var workingKey = _razorpayPaymentSettings.Key;
            var ccaCrypto = new CCACrypto();
            var encResponse = ccaCrypto.Decrypt(Request.Form["encResp"], workingKey);
            var paramList = new NameValueCollection();
            foreach (var seg in encResponse.Split('&'))
            {
                var parts = seg.Split('=');

                if (parts.Length <= 0)
                    continue;

                paramList.Add(parts[0].Trim(), parts[1].Trim());
            }

            var sb = new StringBuilder();
            sb.AppendLine("Razorpay:");
            for (var i = 0; i < paramList.Count; i++)
            {
                sb.AppendLine(paramList.Keys[i] + " = " + paramList[i]);
            }

            var orderId = paramList["Order_Id"];
            var authDesc = paramList["order_status"];

            var order = await _orderService.GetOrderByIdAsync(Convert.ToInt32(orderId));

            if (order == null)
                return RedirectToAction("Index", "Home", new { area = string.Empty });

            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = sb.ToString(),
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });

            //var merchantId = Params["Merchant_Id"];
            //var Amount = Params["Amount"];
            //var myUtility = new RazorpayHelper();
            //var checksum = myUtility.verifychecksum(merchantId, orderId, Amount, AuthDesc, _razorpayPaymentSettings.Key, checksum);

            if (!authDesc.Equals("Success", StringComparison.InvariantCultureIgnoreCase))
            {
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });
            }

            //here you need to put in the routines for a successful transaction such as sending an email to customer,
            //setting database status, informing logistics etc etc

            if (_orderProcessingService.CanMarkOrderAsPaid(order))
            {
                await _orderProcessingService.MarkOrderAsPaidAsync(order);
            }

            //thank you for shopping with us. Your credit card has been charged and your transaction is successful
            return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
        }
    }
}