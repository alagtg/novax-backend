namespace YourProject.API.DTOs.Common;

public class PagedQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int SafePage => Page <= 0 ? 1 : Page;

    public int SafePageSize(int max = 50)
    {
        var ps = PageSize <= 0 ? 20 : PageSize;
        return ps > max ? max : ps;
    }
}
