namespace BanTayVang.API.DTOs.Common
{
    public class PaginationDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }
}