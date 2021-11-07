using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            CoverType CoverType = new CoverType();
            if (id == null)
            {
                //this is for create
                return View(CoverType);
            }
            //this is for edit
            // CoverType = _unitOfWork.CoverType.Get(id.GetValueOrDefault()); using net frame
            var parameter = new DynamicParameters();
            parameter.Add("@Id", id);
            CoverType = _unitOfWork.SP_Call.OneRecord<CoverType>(SD.Proc_CoverType_Get, parameter);

            if (CoverType == null)
            {
                return NotFound();
            }
            return View(CoverType);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(CoverType CoverType)
        {
            if (ModelState.IsValid)
            {
                var parameter = new DynamicParameters();
                parameter.Add("@Name", CoverType.Name);

                if (CoverType.Id == 0)
                {
                    //_unitOfWork.CoverType.Add(CoverType); using net framework
                    _unitOfWork.SP_Call.Execute(SD.Proc_CoverType_Create, parameter);
                }
                else
                {
                    //  _unitOfWork.CoverType.Update(CoverType); using net frame
                    parameter.Add("@Id", CoverType.Id);
                    _unitOfWork.SP_Call.Execute(SD.Proc_CoverType_Update, parameter);

                }
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            return View(CoverType);
        }


        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            // var allObj = _unitOfWork.CoverType.GetAll(); using net frame
            var allObj = _unitOfWork.SP_Call.List<CoverType>(SD.Proc_CoverType_GetAll, null);//useing db procedures
            return Json(new { data = allObj });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            // var objFromDb = _unitOfWork.CoverType.Get(id);using net frame
            var parameter = new DynamicParameters();
            parameter.Add("@Id", id);
            var objFromDb = _unitOfWork.SP_Call.OneRecord<CoverType>(SD.Proc_CoverType_Get, parameter);

            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            //_unitOfWork.CoverType.Remove(objFromDb);using net frame
            _unitOfWork.SP_Call.Execute(SD.Proc_CoverType_Delete, parameter);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Successful" });

        }

        #endregion
    }
}