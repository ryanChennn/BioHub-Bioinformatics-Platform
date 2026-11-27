using BIOHub.Data;
using BIOHub.Models;
using BIOHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BIOHub.Controllers
{
    [Authorize]
    public class SurvivalDataController : Controller
    {
        private BIOHubDbContext _context;
        public SurvivalDataController(BIOHubDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task< IActionResult> Index(int subjectId)
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

            if (subject == null) {
                return NotFound();
            }

            var survivalData = await _context.SurvivalData
                .AsNoTracking()
                .FirstOrDefaultAsync(sd => sd.SubjectsID == subject.SubjectsID);

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;
            ViewBag.ResearchProjectId = subject.ResearchProjectsID;

            return View(survivalData);
        }

        [HttpGet]
        [Authorize]
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

            if(subject == null) { 
                return NotFound();
            }

            bool exists = await _context.SurvivalData.AnyAsync(sd => sd.SubjectsID == subjectId);

            if (exists) { 
                RedirectToAction(nameof(Index), new {subjectId});
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;
            return View(new SurvivalDataCreateViewModel());
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Create(int  subjectId, SurvivalDataCreateViewModel model)
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

            if( subject == null) { 
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;


            if (!ModelState.IsValid) { 
                return View("Create",model);
            }

            if(model.OS_Status.HasValue != model.OS_Month.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "OS 存活時間與事件狀態必須同時填寫。");
            }

            if (model.PFS_Status.HasValue != model.PFS_Month.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "PFS 存活時間與事件狀態必須同時填寫。");
            }

            if(!model.OS_Month.HasValue && !model.PFS_Month.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "請至少填寫一組 OS 或 PFS 存活資料。");
            }

            if (!ModelState.IsValid) { 
                return View ("Create", model);
            }

            var exists = await _context.SurvivalData.AnyAsync(sd => sd.SubjectsID == subjectId);

            if (exists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "此受試者已建立存活分析資料，請使用編輯功能。");

                return View("Create", model);

              }

            var survivalData = new SurvivalData
            {
                SubjectsID = subject.SubjectsID,
                OS_Month = model.OS_Month,
                OS_Status = model.OS_Status,
                PFS_Month = model.PFS_Month,
                PFS_Status = model.PFS_Status,
                LastFollowUpDate = DateTime.UtcNow,
                UpdatedAt = null
             };

            _context.SurvivalData.Add(survivalData);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "新增失敗，請確認存活資料是否重複或資料格式是否正確。"
                    );

                return View("Create", model);
            }

            return RedirectToAction(nameof(Index), new {subjectId});
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult>Edit(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }

            var survivalData = await _context.SurvivalData
                .AsNoTracking()
                .FirstOrDefaultAsync(sd=>sd.SurvivalDataID == id && _context.Subjects
                .Any(s=>s.SubjectsID == sd.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if (survivalData == null)
            {
                return NotFound();
            }


            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == survivalData.SubjectsID);

            if(subject == null)
            {
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            var model = new SurvivalDataEditViewModel
            {
                SurvivalDataID = survivalData.SurvivalDataID,
                OS_Month = survivalData.OS_Month,
                OS_Status = survivalData.OS_Status,
                PFS_Month = survivalData.PFS_Month,
                PFS_Status = survivalData.PFS_Status,
                LastFollowUpDate = survivalData.LastFollowUpDate
            };



            return View(model);

           
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        async public Task<IActionResult>Edit(int id, SurvivalDataEditViewModel model)
        {
            if(id != model.SurvivalDataID)
            {
                return BadRequest();
            }

            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }

            // 3. 重新查詢資料並驗證所有權
            // 不使用 AsNoTracking，因為等一下要更新 Entity
            var survivalData = await _context.SurvivalData
                .FirstOrDefaultAsync(sd=>sd.SurvivalDataID == id && _context.Subjects
                .Any(s=>s.SubjectsID == sd.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID &&p.UsersID == userId)));

            if (survivalData == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == survivalData.SubjectsID);

            if (subject == null)
            {
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;



            //檢查 ViewModel 的基本驗證
            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            if(model.OS_Month.HasValue != model.OS_Status.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "OS 存活時間與事件狀態必須同時填寫。"
                    );
            }

            if (model.PFS_Month.HasValue != model.PFS_Status.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "PFS 存活時間與事件狀態必須同時填寫。"
                    );
            }

            if (!model.OS_Month.HasValue &&  !model.PFS_Month.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "請至少保留一組 OS 或 PFS 存活資料。"
                    );
            }


            if (!ModelState.IsValid) {
                return View("Edit", model);
            }

            survivalData.OS_Month = model.OS_Month;
            survivalData.OS_Status = model.OS_Status;
            survivalData.PFS_Month = model.PFS_Month;
            survivalData.PFS_Status = model.PFS_Status;
            survivalData.LastFollowUpDate = model.LastFollowUpDate;


            // 更新最後修改時間
            survivalData.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    ex.GetBaseException().Message);

                ModelState.AddModelError(
                    string.Empty,
                     "更新失敗，請確認資料是否正確。");
                return View("Edit", model);

            }

            // 11. 修改成功後返回該受試者的 SurvivalData Index
            return RedirectToAction(nameof(Index), new {subjectId = subject.SubjectsID});
        }

        [HttpGet]
        public async Task<IActionResult>Delete(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue, out var userId)) {
                return Challenge();
            }


            var survivalData = await _context.SurvivalData
                .AsNoTracking()
                .FirstOrDefaultAsync(sd=>sd.SurvivalDataID == id && _context.Subjects
                .Any(s=>s.SubjectsID == sd.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if(survivalData == null)
            {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == survivalData.SubjectsID);

            if (subject == null) {
                return NotFound();
            }


            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            return View(survivalData);
        }


        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>DeleteComfirmed(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }

            var survivalData = await _context.SurvivalData
                .AsNoTracking()
                .FirstOrDefaultAsync(sd=>sd.SurvivalDataID == id && _context.Subjects
                .Any(s=>s.SubjectsID == sd.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if (survivalData == null) {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == survivalData.SubjectsID);

            if(subject == null)
            {
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            try
            {
                _context.SurvivalData.Remove(survivalData);
               await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }
            catch(DbUpdateException ex )
            {
                System.Diagnostics.Debug.WriteLine(
                    ex.GetBaseException().Message);

                ModelState.AddModelError(
                    string.Empty,
                    "刪除失敗，請確認資料庫關聯及狀態。");
            }




            return RedirectToAction(nameof(Index), new {subjectId = subject.SubjectsID});
        }


    }
}
