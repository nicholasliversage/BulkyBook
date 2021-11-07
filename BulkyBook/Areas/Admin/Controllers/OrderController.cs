using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderDetailsVM orderDetailsVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            orderDetailsVM = new OrderDetailsVM()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id,
                includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetails.GetAll(o => o.OrderId == id,
                includeProperties: "Product")
            };
            return View(orderDetailsVM);
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing(int id)
        {
            OrderHeader OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);
            OrderHeader.OrderStatus = SD.StatusInProcess;
            _unitOfWork.Save();
            return RedirectToAction("Index");

        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            OrderHeader OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderDetailsVM.OrderHeader.Id);
            OrderHeader.OrderStatus = SD.StatusShipped;
            OrderHeader.TrackingNumber = orderDetailsVM.OrderHeader.TrackingNumber;
            OrderHeader.Carrier = orderDetailsVM.OrderHeader.TrackingNumber;
            OrderHeader.ShippingDate = DateTime.Now;

            _unitOfWork.Save();
            return RedirectToAction("Index");

        }


        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(int id)
        {
            OrderHeader OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);
            if (OrderHeader.PaymentStatus == SD.StatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Amount = Convert.ToInt32(OrderHeader.OrderTotal * 100),
                    Reason = RefundReasons.RequestedByCustomer,
                    Charge = OrderHeader.TransactionId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                OrderHeader.OrderStatus = SD.StatusRefunded;
                OrderHeader.PaymentStatus = SD.StatusRefunded;
            }
            else
            {
                OrderHeader.OrderStatus = SD.StatusCancelled;
                OrderHeader.PaymentStatus = SD.StatusCancelled;
            }
            

            _unitOfWork.Save();
            return RedirectToAction("Index");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Details")]
        public IActionResult Details(string stripeToken)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderDetailsVM.OrderHeader.Id,
                  includeProperties: "ApplicationUser");
            if (stripeToken != null)
            {
              
                //process the payment
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(orderHeader.OrderTotal * 100),
                    Currency = "usd",
                    Description = "Order ID : " + orderHeader.Id,
                    Source = stripeToken
                };

                var service = new ChargeService();
                Charge charge = service.Create(options);

                if (charge.BalanceTransactionId == null)
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    orderHeader.TransactionId = charge.BalanceTransactionId;
                }
                if (charge.Status.ToLower() == "succeeded")
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    orderHeader.PaymentDate = DateTime.Now;
                }

                _unitOfWork.Save();
            }
            return RedirectToAction("Details", "Order", new { id = orderHeader.Id });

        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetOrderList(string status)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
             IEnumerable<OrderHeader> OrderHeaders;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                OrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                OrderHeaders = _unitOfWork.OrderHeader.GetAll(
                    u=>u.ApplicationUserId==claim.Value,
                    includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    OrderHeaders = OrderHeaders.Where(o => o.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    OrderHeaders = OrderHeaders.Where(o => o.OrderStatus == SD.StatusApproved ||
                                       o.OrderStatus == SD.StatusInProcess ||
                                       o.OrderStatus == SD.StatusPending); break;
                case "completed":
                    OrderHeaders = OrderHeaders.Where(o => o.OrderStatus == SD.StatusShipped);
                    break;
                case "rejected":
                    OrderHeaders = OrderHeaders.Where(o => o.OrderStatus == SD.StatusCancelled ||
                   o.OrderStatus == SD.StatusRefunded ||
                   o.OrderStatus == SD.PaymentStatusRejected);
                    break;

                default:
                    break;
            }
            return Json(new { data = OrderHeaders });
        }


        #endregion
    }
}