using Microsoft.EntityFrameworkCore;
using StudyPlannerApi.Models;



namespace StudyPlannerApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }

        public DbSet<School> Schools { get; set; }
    }
}                                                                                                   
