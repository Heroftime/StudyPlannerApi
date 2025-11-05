using System.ComponentModel.DataAnnotations;

namespace StudyPlannerApi.Models;

public class Subject
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int AverageTimeInMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
