using System.ComponentModel.DataAnnotations;

namespace PdfConversionTestDemo;

public class FileData
{
    [Required] public int SortNr { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    [Required] public string Type { get; set; } = string.Empty;
    public string? Background { get; set; } 

}

