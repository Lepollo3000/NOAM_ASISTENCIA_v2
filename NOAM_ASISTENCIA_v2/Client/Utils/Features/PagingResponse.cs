using NOAM_ASISTENCIA_v2.Shared.RequestFeatures;

namespace NOAM_ASISTENCIA_v2.Client.Utils.Features
{
    public class PagingResponse<T> where T : class
    {
        public List<T> Items { get; set; } = null!;
        public MetaData MetaData { get; set; } = null!;
    }
}
