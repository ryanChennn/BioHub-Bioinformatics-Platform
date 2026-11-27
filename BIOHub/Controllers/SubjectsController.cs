using BIOHub.Data;
using BIOHub.Models;
using BIOHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.NativeInterop;
using System.Data.Common;
using System.Security.Claims;


namespace BIOHub.Controllers
{
    public class SubjectsController : Controller
    {
        private BIOHubDbContext _context;
        public SubjectsController(BIOHubDbContext context)
        {
            _context = context;
        }


        // GET: Subjects?researchProjectId=1
        [HttpGet]
        public async Task<IActionResult> Index(int researchProjectId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId)) {
                return Challenge();
            }

            var project = await _context.ResearchProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(p=>p.ResearchProjectsID == researchProjectId && p.UsersID == userId );
            if (project == null) {
                return NotFound();
            }

            // 只取得該研究計畫的 Subjects
            var subjects = await _context.Subjects
                .AsNoTracking()
                .Where(s=>s.ResearchProjectsID == researchProjectId)
                .OrderBy(s=>s.SubjectCode)
                .ToListAsync();

            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ResearchProjectId = researchProjectId;
            return View(subjects);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create(int researchProjectId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Challenge();
            }

            var project = await _context.ResearchProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ResearchProjectsID == researchProjectId && p.UsersID == userId);
            if(project == null)
            {
                return NotFound();
            }

            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ResearchProjectId = researchProjectId;
            return View(new SubjectsCreateViewModel());
        }



        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int researchProjectId, SubjectsCreateViewModel model)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId)) {
                return Challenge();
            
            }

            var project = await _context.ResearchProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ResearchProjectsID == researchProjectId && p.UsersID == userId);

            if(project == null)
            {
                return NotFound();
            }

            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ResearchProjectId = researchProjectId;

            if (!ModelState.IsValid) {
                return View(model);
            }

            var subjectCode = model.SubjectCode.Trim();
            bool exists = await _context.Subjects
                .AnyAsync(s=>s.ResearchProjectsID == researchProjectId && s.SubjectCode == subjectCode );
            //AnyAsync()：先檢查同一研究計畫中是否已有相同的 SubjectCode。

            if (exists)
            {
                ModelState.AddModelError(nameof(model.SubjectCode), "此研究計畫已存在相同的研究對象編號");

                return View(model);
            }

            var subject = new Subjects
            {
                ResearchProjectsID = researchProjectId,
                SubjectCode = subjectCode,
                Gender = model.Gender,
                Birth = model.Birth.Value,
                Age = model.Age,
                EnrollmentDate = model.EnrollmentDate.Value,
                Status = model.Status.Trim(),
                CreateAt = DateTime.Now
            };
            _context.Subjects.Add(subject);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "新增失敗，請確認研究對象編號是否重複或資料是否正確。");
                return View(model);
            }

            return RedirectToAction(nameof(Index),new { researchProjectId });


            }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult>Details(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId)) {

                return Challenge();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == id && s.ResearchProjects.UsersID == userId);
            if (subject == null) {
                return NotFound();
            }

            var project = await _context.ResearchProjects
                .AsNoTracking()
                .FirstAsync(p=>p.ResearchProjectsID == subject.ResearchProjectsID);
            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ResearchProjectId = subject.ResearchProjectsID;

            return View(subject);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue ,out var userId))
            {
                return Challenge();
            }

            var subject = await _context.Subjects.AsNoTracking().FirstOrDefaultAsync(s => s.SubjectsID == id && _context.ResearchProjects.Any(p => p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId));
            if (subject == null) { 
                return NotFound(); }

            var project = await _context.ResearchProjects.AsNoTracking().FirstAsync(p => p.ResearchProjectsID == subject.ResearchProjectsID && p.UsersID == userId);

            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ResearchProjectId = project.ResearchProjectsID;

            var model = new SubjectsEditViewModel
            {
                SubjectsID = subject.SubjectsID,
                SubjectCode = subject.SubjectCode,
                Gender = subject.Gender,
                Age = subject.Age,
                Birth = subject.Birth,
                EnrollmentDate = subject.EnrollmentDate,
                Status = subject.Status
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SubjectsEditViewModel model)
        {
            if (id != model.SubjectsID) { 
                return BadRequest();
            }

            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s=>s.SubjectsID ==id && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId));

            if (subject == null) { 
                return NotFound();
            }

            var project = await _context.ResearchProjects
                .AsNoTracking()
                .FirstAsync(p=>p.ResearchProjectsID == subject.ResearchProjectsID && p.UsersID == userId);

            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ResearchProjectId = project.ResearchProjectsID;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var subjectCode = model.SubjectCode.Trim();

            bool exists = await _context.Subjects
                .AnyAsync(s => s.ResearchProjectsID == subject.ResearchProjectsID && s.SubjectCode == subjectCode && s.SubjectsID != id);

            if (exists) {
                ModelState.AddModelError(nameof(model.SubjectCode), "此研究計畫已有相同受試者編號");

                return View(model);
            }
            
            subject.SubjectCode = subjectCode;
            subject.Gender = model.Gender;
            subject.Age = model.Age;
            subject.Birth = model.Birth;
            subject.EnrollmentDate = model.EnrollmentDate;
            subject.Status = model.Status.Trim();

            try{
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty, "更新失敗，請確認受試者編號是否重複或資料是否正確。");
                return View (model);
            }
            return RedirectToAction(nameof(Details),new {id = subject.SubjectsID});
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue, out var userId)) {
                return Challenge();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == id && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId));

            if (subject == null) {
                return NotFound();
            }


            var project = await _context.ResearchProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(p=>p.ResearchProjectsID == subject.ResearchProjectsID && p.UsersID == userId);

            if (project == null) {
                return NotFound();
            }

            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ResearchProjectId = project.ResearchProjectsID;
            return View(subject);
        }



        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        //這裡使用的[HttpPost, ActionName("Delete")] 是 ASP.NET Core MVC 常見的做法，讓 GET Delete 與 POST Delete 能使用同一個網址，但執行不同功能。
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue, out var userId)) {
                return Challenge();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubjectsID == id && _context.ResearchProjects.Any(p => p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId));
            if(subject == null) { 
                return NotFound(); 
            }

            var project = await _context.ResearchProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(p=>p.ResearchProjectsID == subject.ResearchProjectsID && p.UsersID == userId);
            if (project == null) {
                return NotFound();
            }

            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ResearchProjectId = project.ResearchProjectsID;

            bool hasClinicalData = await _context.ClinicalData.AnyAsync(c=>c.SubjectsID == subject.SubjectsID);

            bool hasSamples = await _context.Samples.AnyAsync(s=>s.SubjectsID == subject.SubjectsID);

            bool hasSurvivalData = await _context.SurvivalData.AnyAsync(sd=>sd.SubjectsID == subject.SubjectsID);

            if(hasClinicalData || hasSamples || hasSurvivalData)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "此受試者已有關聯的臨床、檢體或存活分析資料，無法刪除。");

                return View("Delete", subject);
            }

            try
            {
                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new {researchProjectId = subject.ResearchProjectsID});
            }
            catch(DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty, "刪除失敗，可能仍有其他關聯資料。");
                return View("Delete", subject);
            }

            
        }

    }
}

