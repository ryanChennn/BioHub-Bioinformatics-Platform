using BIOHub.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BIOHub.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardApiController : Controller
    {
        private BIOHubDbContext _context;
        public DashboardApiController(BIOHubDbContext context)
        {
            _context = context;
        }


        // GET: /api/dashboard/stats
        [HttpGet("stats")]
        public async Task< IActionResult> GetStats()
        {
            var UserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(UserIdValue, out var userId)) {
                return Unauthorized();
            }

            var projects =  _context.ResearchProjects
                .AsNoTracking()
                .Where(p => p.UsersID == userId);

            var subjects = _context.Subjects
                .AsNoTracking()
                .Where(s => projects.Any(p => p.ResearchProjectsID == s.ResearchProjectsID));


            var TotalProjects = await projects.CountAsync();

            var TotalSubjects = await subjects.CountAsync();

            var TotalClinicalData = await _context.ClinicalData
                .AsNoTracking()
                .CountAsync(c=>subjects.Any(s=>s.SubjectsID == c.SubjectsID));
            var TotalSurvivalData = await _context.SurvivalData
                .AsNoTracking()
                .CountAsync(sd =>subjects.Any(s=>s.SubjectsID == sd.SubjectsID));
            var TotalSamples = await _context.Samples
                .AsNoTracking()
                .CountAsync(sa=>subjects.Any(s=>s.SubjectsID == sa.SubjectsID));




            return Ok(new
            {
                TotalProjects,
                TotalSubjects,
                TotalClinicalData,
                TotalSurvivalData,
                TotalSamples,
                updatedAt = DateTime.UtcNow
            });
        }
    }
}
