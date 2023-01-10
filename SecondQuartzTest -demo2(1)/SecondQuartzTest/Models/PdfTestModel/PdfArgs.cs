namespace SecondQuartzTest;

public class PdfArg 
{
    public string? Version { get; set; } 
    public string? OutputName { get; set; } 
    public AllowArgs? Allowed { get; set; }
    public List<FileData>? Files { get; set; }
}

