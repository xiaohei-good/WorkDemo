namespace SecondQuartzTest;

public class JobInfo
{
    public string Name { get; set; } = "";
    public bool IsMono { get; set; }
    public int Priority { get; set; }
    public long FireTime { get; set; }
}