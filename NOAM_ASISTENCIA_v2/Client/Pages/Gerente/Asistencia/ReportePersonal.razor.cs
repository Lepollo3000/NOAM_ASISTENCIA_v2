using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Client.Utils.Features;
using NOAM_ASISTENCIA_V2.Shared.Models;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures.Asistencia;
using System.Text.Json;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Gerente.Asistencia;

partial class ReportePersonal
{
    [CascadingParameter] public MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Parameter] public string Username { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly string _allItemsText = "Mostrando {first_item} de {last_item}. Total: {all_items}";
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    private readonly int[] _pageSizeOption = { 5, 10, 15, 20 };

    private int _pageSize = 10;
    private AsistenciaPersonalDTO _model = new();
    private SearchParameters _searchParameters = new();
    private AsistenciaFilterParameters _filters = new();
    private MudTable<AsistenciaPersonalDTO> _table = new();

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();

        SetFilterParameters();
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new()
        {
            new BreadcrumbItem("Inicio", href: ""),
            new BreadcrumbItem("Reportes", href: "asistencia/reportes"),
            new BreadcrumbItem("Detalle Reporte", href: $"asistencia/reporte/{Username}")
        };

        await Layout.SetBreadcrumb(breadcrumb);
    }

    private void SetFilterParameters()
    {
        _filters = new()
        {
            FechaInicial = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            FechaFinal = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
            TimeZoneId = TimeZoneInfo.Local.Id,
            Username = Username
        };
    }

    private async Task<TableData<AsistenciaPersonalDTO>> GetServerData(TableState state)
    {
        _searchParameters.PageSize = state.PageSize;
        _searchParameters.PageNumber = state.Page + 1;
        _searchParameters.OrderBy = state.SortLabel == null ? state.SortLabel
            : $"{state.SortLabel} {state.SortDirection.ToDescriptionString()}";

        PagingResponse<AsistenciaPersonalDTO> response = await FetchSucursales();

        return new TableData<AsistenciaPersonalDTO>
        {
            Items = response.Items,
            TotalItems = response.MetaData.TotalCount
        };
    }

    private async Task<PagingResponse<AsistenciaPersonalDTO>> FetchSucursales()
    {
        var queryStringParam = new Dictionary<string, string>
        {
            ["pageNumber"] = _searchParameters.PageNumber.ToString(),
            ["pageSize"] = _searchParameters.PageSize.ToString(),
            ["searchTerm"] = _searchParameters.SearchTerm ?? "",
            ["orderBy"] = _searchParameters.OrderBy ?? "",

            ["username"] = _filters.Username ?? "",
            ["timeZoneId"] = _filters.TimeZoneId ?? "",
            ["fechaInicial"] = _filters.FechaInicial!.Value.ToString("yyyy-MM-dd") ?? "",
            ["fechaFinal"] = _filters.FechaFinal!.Value.ToString("yyyy-MM-dd") ?? ""
        };

        using var response = await HttpClient.GetAsync(
            QueryHelpers.AddQueryString("asistencias", queryStringParam)
        );

        if (response.IsSuccessStatusCode)
        {
            MetaData? metaData = JsonSerializer
                .Deserialize<MetaData>(response.Headers.GetValues("X-Pagination").First(), _options);

            Stream stream = await response.Content.ReadAsStreamAsync();

            var pagingResponse = new PagingResponse<AsistenciaPersonalDTO>()
            {
                Items = await JsonSerializer.DeserializeAsync<List<AsistenciaPersonalDTO>>(stream, _options) ?? null!,
                MetaData = metaData!
            };

            return pagingResponse;
        }

        var nullPagingResponse = new PagingResponse<AsistenciaPersonalDTO>
        {
            Items = null!,
            MetaData = null!
        };

        return nullPagingResponse;
    }

    private async Task FirstDateHasChanged()
    {
        await ValidateDates();
    }

    private async Task LastDateHasChanged()
    {
        await ValidateDates();
    }

    private async Task ValidateDates()
    {
        if (_filters.FechaInicial > _filters.FechaFinal)
        {
            DateTime? fechaInicial = _filters.FechaFinal;
            DateTime? fechaFinal = _filters.FechaInicial;

            _filters.FechaInicial = fechaInicial;
            _filters.FechaFinal = fechaFinal;

            StateHasChanged();
        }

        await _table.ReloadServerData();
    }
}
