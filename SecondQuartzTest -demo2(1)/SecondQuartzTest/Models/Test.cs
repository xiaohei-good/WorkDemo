using System.ComponentModel.DataAnnotations;

namespace SecondQuartzTest.Models
{
    public class Test
    {
        [Required][Key] public Guid Id { get; set; }
    }
}
