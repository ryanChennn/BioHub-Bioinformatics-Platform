using BIOHub.Data;
using BIOHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.NativeInterop;
using System.Data.Common;
using System.Security.Claims;

namespace BIOHub.Controllers
{
    public class ResearchProjectsController : Controller
    {
        private BIOHubDbContext _context;

        public ResearchProjectsController(BIOHubDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(userIdValue, out var userId))
            {
                return Challenge();
            }
            var project = await _context.ResearchProjects.
                AsNoTracking().
                Where(p=>p.UsersID == userId).
                OrderByDescending(p => p.CreateAt).
                ToListAsync();

            return View(project);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectCode",
            "ProjectName", 
            "Description", 
            "PrincipalInvestigator",
            "Institution",
            "StartDate","EndDate","ProjectStatus")] ResearchProjects project)
        {

            // 1. 取得目前登入者的 ID
            var userValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(userValue, out var userId))
            {
                return Challenge();
            }
            // 2. 確認登入者仍然存在，而且帳號仍啟用
            var currentUser = await _context.Users
                .SingleOrDefaultAsync(u=>u.UsersID == userId && u.IsActive);
            if (currentUser == null) {
                return Forbid();
            }
            // 3. 由伺服器設定建立者資料
            project.UsersID = userId;
            project.CreatedBy = currentUser.UserName;
            project.CreateAt = DateTime.Now;



            // 4. 移除伺服器負責填寫欄位的模型繫結驗證紀錄
            ModelState.Remove(nameof(ResearchProjects.UsersID));
            ModelState.Remove(nameof(ResearchProjects.CreatedBy));
            ModelState.Remove(nameof(ResearchProjects.CreateAt));

            // 5. 驗證使用者輸入的欄位
            if (!ModelState.IsValid)
            {
                return View(project);
            }
            project.CreateAt = DateTime.Now;

            // 6. 新增研究計畫
            _context.ResearchProjects.Add(project);
            await _context.SaveChangesAsync();

            // 7. 儲存成功後返回列表
            return RedirectToAction(nameof(Index));

        }

        public async Task<IActionResult> Details(int?ID) {
            if (ID == null) {
                return NotFound();
            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(userIdValue, out var userId))
            {
                return Challenge();
            }



            //[Authorize]：要求使用者先登入。
            //User.FindFirstValue(...)：從目前已驗證的身分資訊中取得 UsersID。
            //AsNoTracking()：告訴 EF Core 這次只讀取資料，不需要追蹤實體的修改。
            //OrderByDescending(...)：讓較新的研究計畫排在前面。
            var project =await _context.ResearchProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ResearchProjectsID == ID.Value && p.UsersID == userId);

            if (project == null) {
                return NotFound();
            }
            return View(project);

        }

        [HttpGet]
        public async Task<IActionResult>  Edit(int?ID) {
            if (ID == null) {
                return NotFound();

            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(userIdValue, out var userId))
            {
                return Challenge();
            }
            var project = await _context.ResearchProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(p=>p.ResearchProjectsID == ID.Value && p.UsersID == userId);

            if (project == null) {
                return NotFound();
            }
            return View(project);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int ID, ResearchProjects project)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId)) { 
                return Challenge();
            }

            // 重新從資料庫查詢，並確認研究計畫的所有權
            var projectdata = await _context.ResearchProjects
                .FirstOrDefaultAsync(p => p.ResearchProjectsID == ID && p.UsersID == userId);
            if (projectdata == null) {
                return NotFound();
            }

            // 只允許修改以下指定欄位
            var isValid = await TryUpdateModelAsync<ResearchProjects>(projectdata,
                "",
                p=>p.ProjectCode,
                p=>p.ProjectName,
                p=>p.Description,
                p=>p.PrincipalInvestigator,
                p=>p.Institution,
                p=>p.StartDate,
                p => p.EndDate);

            if (isValid) { 
                try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch(DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "儲存失敗，請確認專案編號是否重複，或稍後再試。");
            }}
            return View(project);


            //if (ID!= project.ResearchProjectsID)
            //{
            //    return NotFound();
            //}
            //if (!ModelState.IsValid)
            //{
            //    return View(project);
            //}


            //var existingProject = await _context.ResearchProjects.FindAsync(ID);
            //if (existingProject == null) {
            //    return NotFound();
            //};


            //existingProject.ProjectCode = project.ProjectCode;
            //existingProject.ProjectName = project.ProjectName;
            //existingProject.Description = project.Description;
            //existingProject.PrincipalInvestigator = project.PrincipalInvestigator;
            //existingProject.Institution = project.Institution;
            //existingProject.StartDate = project.StartDate;
            //existingProject.EndDate = project.EndDate;
            //existingProject.ProjectStatus = project.ProjectStatus;
            //existingProject.CreatedBy = project.CreatedBy;

            ////上面一大段可改成此寫法
            ////_context.Update(project);



            //await _context.SaveChangesAsync();
            //return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int? ID)
        {
            if (ID == null) {
                return NotFound();
            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId)) {
                return Challenge();
            }
            var project = await _context.ResearchProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(p=>p.ResearchProjectsID==ID.Value && p.UsersID == userId);

            if (project == null) {
                return NotFound();
            }
            return View (project);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        //[ValidateAntiForgeryToken] 能協助防止跨站請求偽造攻擊
        public async Task<IActionResult> DeleteComfirmed(int ID)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId)){
                return Challenge();
            }
            var project = await _context.ResearchProjects
                .FirstOrDefaultAsync(p => p.ResearchProjectsID == ID && p.UsersID == userId);

            if (project == null) {
                return NotFound();
            }

            // 已有研究對象時，先阻止刪除
            var hashSubjects = await _context.Subjects.AnyAsync(s => s.ResearchProjectsID == ID);
            if (hashSubjects) {
                ModelState.AddModelError(string.Empty, "此研究計畫已有研究對象，為保護研究資料，暫時不可刪除。");
                return View("Delete", project);
            }
            try
            {
                _context.ResearchProjects.Remove(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException){
                ModelState.AddModelError(string.Empty, "無法刪除此研究計畫，可能仍有相關資料正在使用。");
                return View("Delete",project);
            
            }
            //if (project != null) { 
            //    _context.ResearchProjects.Remove(project);
            //    await _context.SaveChangesAsync();
            //}
            //return RedirectToAction(nameof(Index));
        }

    }
}
