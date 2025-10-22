using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using StudyPlannerApi.Data;

namespace StudyPlannerApi.Models;

public class Course
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Code { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Semester { get; set; }

    public int CreditHours { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

