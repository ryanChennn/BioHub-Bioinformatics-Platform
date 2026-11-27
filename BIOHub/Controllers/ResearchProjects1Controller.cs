
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BIOHub.Models;
using BIOHub.Data;

public class ResearchProjects1Controller : Controller
{
    private readonly BIOHubDbContext _context;

    public ResearchProjects1Controller(BIOHubDbContext context)
    {
        _context = context;
    }

    // GET: RESEARCHPROJECTSS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.ResearchProjects.ToListAsync());
    }

    // GET: RESEARCHPROJECTSS/Details/5
    public async Task<IActionResult> Details(int? researchprojectsid)
    {
        if (researchprojectsid == null)
        {
            return NotFound();
        }

        var researchprojects = await _context.ResearchProjects
            .FirstOrDefaultAsync(m => m.ResearchProjectsID == researchprojectsid);
        if (researchprojects == null)
        {
            return NotFound();
        }

        return View(researchprojects);
    }

    // GET: RESEARCHPROJECTSS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: RESEARCHPROJECTSS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ResearchProjectsID,ProjectCode,ProjectName,Description,PrincipalInvestigator,Institution,StartDate,EndDate,ProjectStatus,CreatedBy,CreateAt,Subjects")] ResearchProjects researchprojects)
    {
        if (ModelState.IsValid)
        {
            _context.Add(researchprojects);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(researchprojects);
    }

    // GET: RESEARCHPROJECTSS/Edit/5
    public async Task<IActionResult> Edit(int? researchprojectsid)
    {
        if (researchprojectsid == null)
        {
            return NotFound();
        }

        var researchprojects = await _context.ResearchProjects.FindAsync(researchprojectsid);
        if (researchprojects == null)
        {
            return NotFound();
        }
        return View(researchprojects);
    }

    // POST: RESEARCHPROJECTSS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? researchprojectsid, [Bind("ResearchProjectsID,ProjectCode,ProjectName,Description,PrincipalInvestigator,Institution,StartDate,EndDate,ProjectStatus,CreatedBy,CreateAt,Subjects")] ResearchProjects researchprojects)
    {
        if (researchprojectsid != researchprojects.ResearchProjectsID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(researchprojects);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResearchProjectsExists(researchprojects.ResearchProjectsID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(researchprojects);
    }

    // GET: RESEARCHPROJECTSS/Delete/5
    public async Task<IActionResult> Delete(int? researchprojectsid)
    {
        if (researchprojectsid == null)
        {
            return NotFound();
        }

        var researchprojects = await _context.ResearchProjects
            .FirstOrDefaultAsync(m => m.ResearchProjectsID == researchprojectsid);
        if (researchprojects == null)
        {
            return NotFound();
        }

        return View(researchprojects);
    }

    // POST: RESEARCHPROJECTSS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? researchprojectsid)
    {
        var researchprojects = await _context.ResearchProjects.FindAsync(researchprojectsid);
        if (researchprojects != null)
        {
            _context.ResearchProjects.Remove(researchprojects);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ResearchProjectsExists(int? researchprojectsid)
    {
        return _context.ResearchProjects.Any(e => e.ResearchProjectsID == researchprojectsid);
    }
}
