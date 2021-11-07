using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Http;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger,IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                var count = _unitOfWork.ShoppingCart
                   .GetAll(c => c.ApplicationUserId == claim.Value)
                   .ToList().Count();

                // HttpContext.Session.SetObject(SD.ssShoppingCart, cart);
                HttpContext.Session.SetInt32(SD.ssShoppingCart, count);
            }
            return View(productList);
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Details(int id)
        {
            var product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id, includeProperties: "Category,CoverType");
            ShoppingCart shoppingCart = new ShoppingCart()
            {
                Product = product,
                ProductId = product.Id
            };
            return View(shoppingCart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
            cart.Id = 0;
            if (ModelState.IsValid)
            {
                //add to cart
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                cart.ApplicationUserId = claim.Value;

                ShoppingCart cartFromDB = _unitOfWork.ShoppingCart.GetFirstOrDefault(
                    u => u.ApplicationUserId == cart.ApplicationUserId && u.ProductId == cart.ProductId,
                    includeProperties: "Product");

                if (cartFromDB == null)
                {
                    //no records found
                    _unitOfWork.ShoppingCart.Add(cart);
                }
                else
                {
                    cartFromDB.Count += cart.Count;
                    _unitOfWork.ShoppingCart.Update(cartFromDB);
                }
                _unitOfWork.Save();
                var count = _unitOfWork.ShoppingCart
                    .GetAll(c => c.ApplicationUserId == cart.ApplicationUserId)
                    .ToList().Count();

               // HttpContext.Session.SetObject(SD.ssShoppingCart, cart);
                HttpContext.Session.SetInt32(SD.ssShoppingCart, count);


                return RedirectToAction(nameof(Index));

            }
            else
            {
                var product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == cart.ProductId, includeProperties: "Category,CoverType");
                ShoppingCart shoppingCart = new ShoppingCart()
                {
                    Product = product,
                    ProductId = product.Id
                };
                return View(shoppingCart);
            }

            
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
