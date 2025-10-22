using System.ComponentModel.DataAnnotations;

namespace StudyPlannerApi.Models
{
    public class School
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Address { get; set; } = string.Empty;

    }
}