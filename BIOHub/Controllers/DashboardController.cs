using BIOHub.Data;
using BIOHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BIOHub.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private BIOHubDbContext _context;

        public DashboardController(BIOHubDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task< IActionResult> Index()
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(UserIdValue, out var userId))
            {
                return Challenge();
            }



            //使用者所擁有的研究計畫, 使用 IQueryable 組合查詢
            var projects = _context.ResearchProjects
                 .AsNoTracking()
                 .Where(p => p.UsersID == userId);


            //上述研究計畫中的受試者
            var subjects = _context.Subjects
                .AsNoTracking()
                .Where(s => projects.Any(p=>p.ResearchProjectsID == s.ResearchProjectsID));


            var model = new DashboardViewModel();

            model.TotalProjects = await projects.CountAsync();
            
            model.TotalSubjects = await subjects.CountAsync();

            model.TotalClinicalData = await _context.ClinicalData
                .AsNoTracking()
                .CountAsync(c=> subjects.Any(s=>s.SubjectsID == c.SubjectsID));
            
            model.TotalSurvivalData = await _context.SurvivalData
                .AsNoTracking()
                .CountAsync(sd=>subjects.Any(s=>s.SubjectsID == sd.SubjectsID));

            model.TotalSamples = await _context.Samples
                .AsNoTracking()
                .CountAsync(sa=>subjects.Any(s=>s.SubjectsID == sa.SubjectsID));

            //最近建立的 5 個研究計畫
            model.RecentProjects = await projects
                .OrderByDescending(p => p.CreateAt)
                .ThenByDescending(p => p.ResearchProjectsID)
                .Take(5)
                .Select(p => new RecentProjectsViewModel
                {
                    ResearchProjectsID = p.ResearchProjectsID,
                    ProjectCode = p.ProjectCode,
                    ProjectName = p.ProjectName,
                    ProjectStatus = p.ProjectStatus,
                    CreateAt = p.CreateAt,
                }).ToListAsync();

            var analysisJobs = _context.AnalysisJob
                .AsNoTracking()
                .Where(j => j.UsersID == userId && projects
                .Any(p => p.ResearchProjectsID == j.ResearchProjectsID));

            model.TotalAnalysisJobs = await analysisJobs.CountAsync();
            model.PendingJobs = await analysisJobs.CountAsync(j=>j.JobStatus== "Pending");
            model.RunningJobs = await analysisJobs.CountAsync(j => j.JobStatus == "Running");
            model.CompletedJobs = await analysisJobs.CountAsync(j => j.JobStatus == "Completed");
            model.FailedJobs = await analysisJobs.CountAsync(j => j.JobStatus == "Failed");
            model.RecentAnalysisJobs = await (
                from job in analysisJobs
                join project in projects
                on job.ResearchProjectsID equals project.ResearchProjectsID
                orderby job.CreateAt descending,
                job.AnalysisJobID descending
                select new RecentAnalysisJobViewModel
                {
                    AnalysisJobID = job.AnalysisJobID,
                    JobName = job.JobName,
                    ProjectName = project.ProjectName,
                    Species = job.Species,
                    JobStatus = job.JobStatus,
                    CreateAt = job.CreateAt
                }
                ).Take(5).ToListAsync();


            return View(model);

            

            
            //ViewBag.ResearchProjectsCount = 0;
            //ViewBag.SubjectsCount = 0;
            //ViewBag.SamplesCount = 0;

            //// RNA-seq Dataset / Analysis Jobs are planned modules.
            //ViewBag.RnaSeqDatasetsCount = 0;
            //ViewBag.RunningAnalysisCount = 0;

            //return View();
        }
    }
}
