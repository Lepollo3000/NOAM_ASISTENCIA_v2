using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using NOAM_ASISTENCIA_v2.Client.Shared;
using NOAM_ASISTENCIA_v2.Client.Utils.Features;
using NOAM_ASISTENCIA_v2.Shared.Models;
using NOAM_ASISTENCIA_v2.Shared.RequestFeatures;
using System.Text.Json;

namespace NOAM_ASISTENCIA_v2.Client.Pages.Administrador
{
    partial class Index
    {
        [CascadingParameter] public MainLayout Layout { get; set; } = null!;

        [Inject] private HttpClient _client { get; set; } = null!;

        private readonly JsonSerializerOptions _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private readonly int[] _pageSizeOption = { 5, 10, 15, 20 };

        private ProductParameters _productParameters = new ProductParameters();
        private MudTable<SucursalServicio> table = new MudTable<SucursalServicio>();

        protected override async Task OnInitializedAsync()
        {
            List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
            {
                new BreadcrumbItem("Home", href: ""),
                new BreadcrumbItem("Servicios", href: "/servicios")
            };

            await Layout.SetBreadcrumb(breadcrumb);
        }

        private async Task<TableData<SucursalServicio>> GetServerData(TableState state)
        {
            _productParameters.PageSize = state.PageSize;
            _productParameters.PageNumber = state.Page + 1;
            _productParameters.OrderBy = state.SortLabel + state.SortDirection.ToDescriptionString();

            PagingResponse<SucursalServicio> response = await GetSucursales(_productParameters);

            return new TableData<SucursalServicio>
            {
                Items = response.Items,
                TotalItems = response.MetaData.TotalCount
            };
        }

        private void OnSearch(string searchTerm)
        {
            _productParameters.SearchTerm = searchTerm;
            table.ReloadServerData();
        }

        private async Task<PagingResponse<SucursalServicio>> GetSucursales(ProductParameters productParameters)
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
