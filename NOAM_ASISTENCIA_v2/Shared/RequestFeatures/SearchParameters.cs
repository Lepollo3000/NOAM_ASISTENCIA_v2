
namespace NOAM_ASISTENCIA_v2.Shared.RequestFeatures
{
    public class SearchParameters
    {
        const int maxPageSize = 50;
        private int _pageSize = 10;
        public int PageNumber { get; set; } = 1;
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = (value > maxPageSize) ? maxPageSize : value; }
        }
        public string? SearchTerm { get; set; }
        public string? OrderBy { get; set; }
    }
}
