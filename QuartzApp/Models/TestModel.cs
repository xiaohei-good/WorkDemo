using System.ComponentModel.DataAnnotations;

namespace QuartzApp.Models
{
    public class TestModel
    {
        [Required] public Guid Id { get; set; }
        [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
    }
}
