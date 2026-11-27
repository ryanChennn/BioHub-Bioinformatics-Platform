using Microsoft.EntityFrameworkCore;
using BIOHub.Models;

namespace BIOHub.Data
{
    public class BIOHubDbContext:DbContext
    {
        public BIOHubDbContext(DbContextOptions<BIOHubDbContext> options) : base(options)
        {

        }

        public DbSet<Users>Users { get; set; }

        public DbSet<ResearchProjects> ResearchProjects { get; set; }

        public DbSet<Subjects> Subjects { get; set; }

        public DbSet<ClinicalData> ClinicalData { get; set; }

        public DbSet<Samples> Samples { get; set; }
        public DbSet<SurvivalData>SurvivalData { get; set; }
        public DbSet<AnalysisJob> AnalysisJob { get; set; }
        public DbSet<AnalysisFiles> AnalysisFiles { get; set; }
        public DbSet<AnalysisResults> AnalysisResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>().HasIndex(u => u.UserName).IsUnique();

            modelBuilder.Entity<Users>().HasIndex(u=>u.Email).IsUnique();

            modelBuilder.Entity<ResearchProjects>().HasIndex(r=>r.ProjectCode).IsUnique();

            modelBuilder.Entity<Subjects>().HasIndex(s=> new
            {
                s.ResearchProjectsID,
                s.SubjectCode
            }).IsUnique();

            modelBuilder.Entity<Samples>().HasIndex(s => new
            {
                s.SubjectsID,
                s.SampleCode
            }).IsUnique();

            modelBuilder.Entity<Subjects>()
                .HasOne(s=>s.ResearchProjects)
                .WithMany(p=>p.Subjects)
                .HasForeignKey(s=>s.ResearchProjectsID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClinicalData>()
                .HasOne(c => c.Subjects)
                .WithMany(s => s.ClinicalData)
                .HasForeignKey(c => c.SubjectsID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Samples>()
                .HasOne(s => s.Subjects)
                .WithMany(s => s.Samples)
                .HasForeignKey(s => s.SubjectsID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ResearchProjects>()
                .HasOne(r=>r.User)
                .WithMany(p=>p.ResearchProjects)
                .HasForeignKey(r=>r.UsersID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SurvivalData>()
                .HasOne(sd=>sd.Subjects)   //SurvivalData 對應一位受試者
                .WithOne()                             //Subjects 最多對應一筆 SurvivalData
                .HasForeignKey<SurvivalData>(sd=>sd.SubjectsID) //指定 SubjectsID 為外鍵
                .OnDelete(DeleteBehavior.Restrict);  //避免刪除 Subjects 時自動刪除 SurvivalData
            
            // AnalysisJobs -> ResearchProjects
            modelBuilder.Entity<AnalysisJob>()
                .HasOne<ResearchProjects>()
                .WithMany()
                .HasForeignKey(j => j.ResearchProjectsID)
                .OnDelete(DeleteBehavior.Restrict);

            // AnalysisJobs -> Users
            modelBuilder.Entity<AnalysisJob>()
                .HasOne<Users>()
                .WithMany()
                .HasForeignKey(j => j.UsersID)
                .OnDelete(DeleteBehavior.Restrict);

            // AnalysisFiles -> AnalysisJobs
            modelBuilder.Entity<AnalysisFiles>()
                .HasOne<AnalysisJob>()
                .WithMany()
                .HasForeignKey(f => f.AnalysisJobID)
                .OnDelete(DeleteBehavior.Restrict);



            //使 AnalysisResults.AnalysisJobID 建立唯一索引，限制每個分析工作最多對應一筆結果摘要。
            // AnalysisResults -> AnalysisJobs
            modelBuilder.Entity<AnalysisResults>()
                .HasOne<AnalysisJob>()
                .WithOne()
                .HasForeignKey<AnalysisResults>(
                r => r.AnalysisJobID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
