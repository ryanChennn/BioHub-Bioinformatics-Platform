using BIOHub.Data;
using BIOHub.Models;
using BIOHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace BIOHub.Controllers
{
    [Authorize]
    public class SamplesController : Controller
    {
        private BIOHubDbContext _context;
        public SamplesController(BIOHubDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int subjectId)
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

            //查詢此受試者的所有檢體
            var samples =await  _context.Samples
                .AsNoTracking()
                .Where(s=>s.SubjectsID == subjectId)
                .OrderByDescending(s=>s.CollectionDate)
                .ThenByDescending(s=>s.SamplesID)
                .ToListAsync();


            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;
            ViewBag.ResearchProjectId = subject.ResearchProjectsID;



            return View(samples);
        }

        [HttpGet]
        public async Task<IActionResult>Create(int subjectId)
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

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            return View(new SamplesCreateViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Create(int subjectId, SamplesCreateViewModel model)
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

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;


            if (!ModelState.IsValid) {
                return View("Create",model);
            }

            string sampleCode = model.SampleCode.Trim();
            if (string.IsNullOrWhiteSpace(sampleCode))
            {
                ModelState.AddModelError(
                    nameof(model.SampleCode),
                    "檢體編號不可為空白。");
                return View("Create", model);
            }


            bool exists = await _context.Samples
                .AnyAsync(s=>s.SubjectsID == subject.SubjectsID && s.SampleCode == sampleCode);


            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.SampleCode),
                    "此受試者已存在相同的檢體編號。"
                    );
                return View("Create", model);
            }

            var sample = new Samples
            {
                //SubjectsID 由後端取得。
                SubjectsID = subject.SubjectsID,
                SampleCode = sampleCode,
                CollectionDate = model.CollectionDate!.Value,
                StorageTemperature = model.StorageTemperature!.Value,
                SampleStatus = model.SampleStatus?.Trim(),
                Remarks = model.Remarks?.Trim(),
                CreateAt = DateTime.Now
                 //僅在表單驗證成功後使用.Value。因為 CollectionDate、StorageTemperature 在 ViewModel 中是 Nullable，必須先通過[Required] 才能安全轉為 Entity 的非 Nullable 型別。
            };

            _context.Samples.Add(sample);
            try
            {
               await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    ex.GetBaseException().Message
                    );
                ModelState.AddModelError(
                    string.Empty,
                    "新增失敗，請檢查檢體編號是否重複或資料是否正確。"
                    );
                return View("Create", model);
            }
            return RedirectToAction(nameof(Index),new { subjectId = subject.SubjectsID });
        }

        public async Task<IActionResult>Details(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue,out var userId))
            {
                return Challenge();
            }

            var sample = await _context.Samples
                .AsNoTracking()
                .FirstOrDefaultAsync(sa=>sa.SamplesID == id && _context.Subjects  //每筆檢體都有獨立的 SamplesID 主鍵。
                .Any(s=>s.SubjectsID == sa.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if (sample == null) {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == sample.SubjectsID);
            if (subject == null) {
                return NotFound();
            }

            ViewBag.SubjectID = subject.SubjectsID;
            ViewBag.SubjectCode = subject.SubjectCode;

            return View(sample);
        }

        [HttpGet]

        public async Task<IActionResult>Edit(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue,out var userId))
            {
                return Challenge();
            }


            var sample = await  _context.Samples
                .AsNoTracking()
                .FirstOrDefaultAsync(sa=>sa.SamplesID == id && _context.Subjects
                .Any(s=>s.SubjectsID == sa.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if(sample == null) { 
                return NotFound();
            }

            var subject =await  _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == sample.SubjectsID);

            if(subject == null) {
                return NotFound();
            }


            ViewBag.SubjectsID = subject.SubjectsID;
            ViewBag.SubjectCode = subject.SubjectCode;

            var model = new SamplesEditViewModel
            {
                SamplesID = sample.SamplesID,
                SampleCode = sample.SampleCode,
                CollectionDate = sample.CollectionDate,
                StorageTemperature = sample.StorageTemperature,
                SampleStatus = sample.SampleStatus,
                Remarks = sample.Remarks,
            };


            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Edit(int id ,SamplesEditViewModel model)
        {
            //  比對網址和表單 ID
            if (id != model.SamplesID)
            {
                return BadRequest();
            }

            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }

            // 不使用 AsNoTracking，因為後續要更新 Entity
            var sample = await _context.Samples
                .FirstOrDefaultAsync(sa=>sa.SamplesID == id && _context.Subjects
                .Any(s=>s.SubjectsID == sa.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if (sample == null) {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == sample.SubjectsID);

            if (subject == null) {
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            string sampleCode = sample.SampleCode.Trim();

            if (string.IsNullOrWhiteSpace(sampleCode))
            {
                ModelState.AddModelError(
                    nameof(model.SampleCode),
                    "檢體編號不可為空白。"
                    );
                return View("Edit", model);

            }

            bool exists = await _context.Samples
                .AnyAsync(s=>s.SubjectsID == sample.SubjectsID &&
                s.SampleCode == sample.SampleCode &&
                s.SamplesID != sample.SamplesID);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.SampleCode),
                    "此受試者已有其他檢體使用相同編號。"
                    );
                return View("Edit", model);
            }

            sample.SampleCode = sampleCode;
            sample.CollectionDate = model.CollectionDate!.Value;
            sample.StorageTemperature = model.StorageTemperature!.Value;
            sample.SampleStatus = model.SampleStatus?.Trim();
            sample.Remarks = model.Remarks?.Trim();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "此檢體資料可能已被其他操作刪除或修改，請重新確認。");
                return View("Edit", model);
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    ex.GetBaseException().Message
                    );

                ModelState.AddModelError(
                    string.Empty,
                    "更新失敗，請檢查檢體編號是否重複或資料是否正確。"
                    );

                return View("Edit", model);
            }


            return RedirectToAction(nameof(Details),new { id = sample.SamplesID });
        }


        [HttpGet]
        public async Task<IActionResult>Delete(int id)
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }

            var sample = await _context.Samples
                .AsNoTracking()
                .FirstOrDefaultAsync(sa=>sa.SamplesID == id && _context.Subjects
                .Any(s=>s.SubjectsID == sa.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));

            if (sample == null) {
                return NotFound();
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == sample.SubjectsID);

            if (subject == null)
            {
                return NotFound();
            }



            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;




            return View(sample);
        }

        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletComfirmed(int id) {

            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue,out var userId))
            {
                return Challenge();
            }


            var sample = await _context.Samples
                .AsNoTracking()
                .FirstOrDefaultAsync(sa=>sa.SamplesID == id && _context.Subjects
                .Any(s=>s.SubjectsID == sa.SubjectsID && _context.ResearchProjects
                .Any(p=>p.ResearchProjectsID == s.ResearchProjectsID && p.UsersID == userId)));
            if(sample == null) {
                return NotFound(); 
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s=>s.SubjectsID == sample.SubjectsID);

            if(subject == null) {
                return NotFound();
            }

            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectId = subject.SubjectsID;

            try
            {
                 _context.Samples.Remove(sample);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }
            catch (DbUpdateException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    ex.GetBaseException().Message
                    );
                ModelState.AddModelError(
                    string.Empty,
                    "刪除失敗，請確認檢體資料是否仍有其他關聯。");
                return View("Delete", sample);
            }

            return RedirectToAction(nameof(Index), new {subjectId = sample.SubjectsID});
        }
    }
}


//刪除 Samples 不會連帶刪除 Subjects