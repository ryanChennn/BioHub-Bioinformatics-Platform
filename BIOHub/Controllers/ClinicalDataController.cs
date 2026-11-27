using Azure;
using BIOHub.Data;
using BIOHub.Models;
using BIOHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace BIOHub.Controllers
{
    public class ClinicalDataController : Controller
    {
        private BIOHubDbContext _context;
        public ClinicalDataController(BIOHubDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task< IActionResult> Index(int subjectId)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == subjectId && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId));

            if (subject == null) {
                return NotFound();
            }

            var clinicalData = await _context.ClinicalData
                .AsNoTracking().Where(c=>c.SubjectsID == subjectId)
                .OrderByDescending(c=>c.CreateAt)
                .ToListAsync();

            ViewBag.SubjectId = subject.SubjectsID;
            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.ResearchProjectId = subject.ResearchProjectsID;


            return View(clinicalData);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int subjectId)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue,out var userId))
            {
                return Challenge();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == subjectId && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId));

            if(subject == null) { return NotFound(); }

            ViewBag.SubjectId = subject.SubjectsID;
            ViewBag.SubjectCode = subject.SubjectCode;


            return View(new ClinicalDataCreateViewModel ());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Create(int subjectId ,ClinicalDataCreateViewModel model)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue,out var userId))
            {
                return Challenge();
            }

            var subject =await  _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == subjectId && _context.ResearchProjects.Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId));
            if (subject == null) {
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;


            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userName = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Challenge();
            }


            var cliniclaDate = new ClinicalData
            {
                SubjectsID = subject.SubjectsID,
                Diagnosis = model.Diagnosis.Trim(),
                Stage = model.Stage,
                Treatments = model.Treatments?.Trim()??string.Empty,
                Responses = model.Responses?.Trim()??string.Empty,
                Note = model.Note?.Trim(),
                CreateBy = userName,
                CreateAt = DateTime.Now
            };

            _context.ClinicalData.Add(cliniclaDate);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.GetBaseException().Message);
                ModelState.AddModelError(string.Empty, "新增臨床資料失敗，請確認資料是否正確。");
                return View(model);
            }
            return RedirectToAction(nameof(Index), new {subjectId});
        }


        [HttpGet]
        public async Task<IActionResult>Details(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue, out var userId)) {
                return Challenge();
            }

            var clinicalData = await _context.ClinicalData
                .AsNoTracking()
                .FirstOrDefaultAsync(c=>c.ClinicalDataID == id && _context.Subjects
                .Any(s=>s.SubjectsID == c.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if(clinicalData == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == clinicalData.SubjectsID);

            if (subject == null) { 
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectID = subject.SubjectsID;

            return View(clinicalData);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id) {

            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue, out var userId)) {
                return Challenge();
            }

            var clinicalData = await _context.ClinicalData
                .AsNoTracking()
                .FirstOrDefaultAsync(c=>c.ClinicalDataID == id && _context.Subjects
                .Any(s=>s.SubjectsID == c.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if(clinicalData == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstAsync(s=>s.SubjectsID == clinicalData.SubjectsID);

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            //Entity 轉換成 EditViewModel
            var model = new ClinicalDataEditViewModel
            {
                ClinicalDataID = clinicalData.ClinicalDataID,
                Diagnosis = clinicalData.Diagnosis,
                Stage = clinicalData.Stage,
                Treatments = clinicalData.Treatments,
                Responses = clinicalData.Responses,
                Note = clinicalData.Note
            };
            
            return View(model);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClinicalDataEditViewModel model)
        {
            if( id!= model.ClinicalDataID)
            {
                return BadRequest();
            }
            

            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue,out var userId))
            {
                return Challenge();
            }

            var clinicalData = await _context.ClinicalData
                .FirstOrDefaultAsync(c=>c.ClinicalDataID == id && _context.Subjects
                .Any(s=>s.SubjectsID == c.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if (clinicalData == null) {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == clinicalData.SubjectsID );

            if (subject == null) {
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }
            //// 6. 確保必填字串不是只有空白
            //var diagnosis = model.Diagnosis.Trim();
            //var stage = model.Stage.Trim();

            //if (string.IsNullOrWhiteSpace(diagnosis))
            //{
            //    ModelState.AddModelError(
            //        nameof(model.Diagnosis),
            //        "請輸入有效的臨床診斷");
            //}

            //if (string.IsNullOrWhiteSpace(stage))
            //{
            //    ModelState.AddModelError(
            //        nameof(model.Stage),
            //        "請輸入有效的疾病分期");
            //}



            clinicalData.Diagnosis = model.Diagnosis;
            clinicalData.Stage = model.Stage;
            clinicalData.Treatments = model.Treatments?.Trim()??string.Empty;
            clinicalData.Responses = model.Responses?.Trim()??string.Empty;
            clinicalData.Note = model.Note?.Trim();

            try
            {
               await _context.SaveChangesAsync();
            }
            catch(DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.GetBaseException().Message);
                ModelState.AddModelError(string.Empty, "儲存失敗，請檢查資料是否正確。");
                return View("Edit", model);
            }

            //更新成功後，返回同一筆資料的 Details
            return RedirectToAction(nameof(Details), new {id = clinicalData.ClinicalDataID});
        }



        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue ,out var userId ))
            {
                return Challenge();
            }


            var clinicalData = await _context.ClinicalData
                .AsNoTracking()
                .FirstOrDefaultAsync(c=>c.ClinicalDataID == id && _context.Subjects
                .Any(s=>s.SubjectsID == c.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if (clinicalData == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == clinicalData.SubjectsID);

            if (subject == null) {
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            return View(clinicalData);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComfirmed(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue,out var userId))
            {
                return Challenge();
            }

            var clinicalData = await _context.ClinicalData
                .AsNoTracking()
                .FirstOrDefaultAsync(c=>c.ClinicalDataID == id && _context.Subjects
                .Any(s=>s.SubjectsID == c.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));
            if (clinicalData == null) {
                return NotFound();
            }


            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == clinicalData.SubjectsID);

            if (subject == null) {
                return NotFound();
            }

            ViewBag.SubgectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            try
            {
                _context.ClinicalData.Remove(clinicalData);
               await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                   "刪除失敗，可能仍有其他關聯資料。");

                return View("Delete", clinicalData);
            }



            return RedirectToAction(nameof(Index), new {subjectId = subject.SubjectsID});
        }

    }
}
