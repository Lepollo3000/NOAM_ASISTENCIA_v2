using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using NOAM_ASISTENCIA_v2.Client.Shared;
using NOAM_ASISTENCIA_v2.Client.Utils.Features;
using NOAM_ASISTENCIA_v2.Shared.Models;
using NOAM_ASISTENCIA_v2.Shared.RequestFeatures;
using System.Text.Json;

namespace NOAM_ASISTENCIA_v2.Client.Pages.Administrador.Servicios
{
    partial class Index
    {
        [CascadingParameter] public MainLayout Layout { get; set; } = null!;

        [Inject] private HttpClient _client { get; set; } = null!;

        private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
        private readonly int[] _pageSizeOption = { 5, 10, 15, 20 };

        private SearchParameters _searchParameters = new();
        private MudTable<SucursalServicio> _table = new();

        protected override async Task OnInitializedAsync()
        {
            List<BreadcrumbItem> breadcrumb = new()
            {
                new BreadcrumbItem("Home", href: ""),
                new BreadcrumbItem("Servicios", href: "/servicios")
            };

            await Layout.SetBreadcrumb(breadcrumb);
        }

        private async Task<TableData<SucursalServicio>> GetServerData(TableState state)
        {
            _searchParameters.PageSize = state.PageSize;
            _searchParameters.PageNumber = state.Page + 1;
            _searchParameters.OrderBy = state.SortLabel == null ? state.SortLabel
                : $"{state.SortLabel} {state.SortDirection.ToDescriptionString()}";

            PagingResponse<SucursalServicio> response = await GetSucursales(_searchParameters);

            return new TableData<SucursalServicio>
            {
                Items = response.Items,
                TotalItems = response.MetaData.TotalCount
            };
        }

        private void OnSearch(string searchTerm)
        {
            _searchParameters.SearchTerm = searchTerm;
            _table.ReloadServerData();
        }

        private async Task<PagingResponse<SucursalServicio>> GetSucursales(SearchParameters productParameters)
        {
            var queryStringParam = new Dictionary<string, string>
            {
                ["pageNumber"] = productParameters.PageNumber.ToString(),
                ["pageSize"] = productParameters.PageSize.ToString(),
                ["searchTerm"] = productParameters.SearchTerm ?? "",
                ["orderBy"] = productParameters.OrderBy ?? ""
            };

            using (var response = await _client.GetAsync(QueryHelpers.AddQueryString("sucursalesservicio", queryStringParam)))
            {
                if (response.IsSuccessStatusCode)
                {
                    MetaData? metaData = JsonSerializer
                        .Deserialize<MetaData>(response.Headers.GetValues("X-Pagination").First(), _options);

                    Stream stream = await response.Content.ReadAsStreamAsync();

                    var pagingResponse = new PagingResponse<SucursalServicio>()
                    {
                        Items = await JsonSerializer.DeserializeAsync<List<SucursalServicio>>(stream, _options) ?? null!,
                        MetaData = metaData!
                    };

                    return pagingResponse;
                }

                var nullPagingResponse = new PagingResponse<SucursalServicio>
                {
                    Items = null!,
                    MetaData = null!
                };

                return nullPagingResponse;
            }
        }
    }
}
