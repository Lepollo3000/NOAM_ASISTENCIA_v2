using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Client.Utils.Features;
using NOAM_ASISTENCIA_V2.Shared.Models;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures.Asistencia;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures;
using System.Text.Json;
using Microsoft.JSInterop;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Gerente.Asistencia;

partial class ReporteGeneral
{
    [CascadingParameter] public MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly string _allItemsText = "Mostrando {first_item} de {last_item}. Total: {all_items}";
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    private readonly int[] _pageSizeOption = { 5, 10, 15, 20 };

    private int _pageSize = 10;
    private AsistenciaGeneralDTO _model = new();
    private IEnumerable<ServicioDTO> _servicios = new List<ServicioDTO>() { new() { Id = 0, Descripcion = "Ninguno" } };
    private SearchParameters _searchParameters = new();
    private AsistenciaFilterParameters _filters = new();
    private MudTable<AsistenciaGeneralDTO> _table = new();

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();

        await SetFilterParameters();
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new()
        {
            new BreadcrumbItem("Inicio", href: ""),
            new BreadcrumbItem("Reportes", href: "asistencia/reportes")
        };

        await Layout.SetBreadcrumb(breadcrumb);
    }

    private async Task SetFilterParameters()
    {
        _filters = new()
        {
            FechaInicial = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            FechaFinal = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
            TimeZoneId = TimeZoneInfo.Local.Id
        };

        await GetServicios();
    }

    private async Task GetServicios()
    {
        var showAllParam = new Dictionary<string, string> { ["showAll"] = true.ToString() };

        using var response = await HttpClient.GetAsync(
            QueryHelpers.AddQueryString("servicios", showAllParam)
        );

        if (response.IsSuccessStatusCode)
        {
            try
            {
                Stream stream = await response.Content.ReadAsStreamAsync();

                _servicios = _servicios.Concat(await JsonSerializer.DeserializeAsync<IEnumerable<ServicioDTO>>(stream, _options) ?? null!);

                _filters.ServicioId = _servicios.First().Id;
            }
            catch (Exception)
            {
                await UnhandledErrorAlert("Ocurrió un error inesperado.");
            }
        }
        else
        {
            await UnhandledErrorAlert("Error al tratar de obtener el listado de servicios.");
        }
    }

    private async Task<TableData<AsistenciaGeneralDTO>> GetServerData(TableState state)
    {
        _searchParameters.PageSize = state.PageSize;
        _searchParameters.PageNumber = state.Page + 1;
        _searchParameters.OrderBy = state.SortLabel == null ? state.SortLabel
            : $"{state.SortLabel} {state.SortDirection.ToDescriptionString()}";

        PagingResponse<AsistenciaGeneralDTO> response = await FetchSucursales();

        return new TableData<AsistenciaGeneralDTO>
        {
            Items = response.Items,
            TotalItems = response.MetaData.TotalCount
        };
    }

    private async Task<PagingResponse<AsistenciaGeneralDTO>> FetchSucursales()
    {
        var queryStringParam = new Dictionary<string, string>
        {
            ["pageNumber"] = _searchParameters.PageNumber.ToString(),
            ["pageSize"] = _searchParameters.PageSize.ToString(),
            ["searchTerm"] = _searchParameters.SearchTerm ?? string.Empty,
            ["orderBy"] = _searchParameters.OrderBy ?? string.Empty,

            ["username"] = _filters.Username ?? string.Empty,
            ["servicioId"] = _filters.ServicioId.ToString() ?? string.Empty,
            ["timeZoneId"] = _filters.TimeZoneId ?? string.Empty,
            ["fechaInicial"] = _filters.FechaInicial!.Value.ToString("yyyy-MM-dd") ?? string.Empty,
            ["fechaFinal"] = _filters.FechaFinal!.Value.ToString("yyyy-MM-dd") ?? string.Empty,

            ["esReporteGeneral"] = true.ToString()
        };

        using var response = await HttpClient.GetAsync(
            QueryHelpers.AddQueryString("asistencias", queryStringParam)
        );

        if (response.IsSuccessStatusCode)
        {
            MetaData? metaData = JsonSerializer
                .Deserialize<MetaData>(response.Headers.GetValues("X-Pagination").First(), _options);

            Stream stream = await response.Content.ReadAsStreamAsync();

            var pagingResponse = new PagingResponse<AsistenciaGeneralDTO>()
            {
                Items = await JsonSerializer.DeserializeAsync<List<AsistenciaGeneralDTO>>(stream, _options) ?? null!,
                MetaData = metaData!
            };

            return pagingResponse;
        }

        var nullPagingResponse = new PagingResponse<AsistenciaGeneralDTO>
        {
            Items = null!,
            MetaData = new MetaData()
        };

        return nullPagingResponse;
    }

    private void GetReporte()
    {
        var queryStringParam = new Dictionary<string, string>
        {
            ["pageNumber"] = _searchParameters.PageNumber.ToString(),
            ["pageSize"] = _searchParameters.PageSize.ToString(),
            ["searchTerm"] = _searchParameters.SearchTerm ?? string.Empty,
            ["orderBy"] = _searchParameters.OrderBy ?? string.Empty,

            ["username"] = _filters.Username ?? string.Empty,
            ["servicioId"] = _filters.ServicioId.ToString() ?? string.Empty,
            ["timeZoneId"] = _filters.TimeZoneId ?? string.Empty,
            ["fechaInicial"] = _filters.FechaInicial!.Value.ToString("yyyy-MM-dd") ?? string.Empty,
            ["fechaFinal"] = _filters.FechaFinal!.Value.ToString("yyyy-MM-dd") ?? string.Empty,

            ["esReporteGeneral"] = true.ToString()
        };

        NavManager.NavigateTo($"api/{QueryHelpers.AddQueryString("asistencias/reporteasistencia", queryStringParam)}", true);
        //await JSRuntime.InvokeVoidAsync("open", new object[] { QueryHelpers.AddQueryString("asistencias/reporteasistencia", queryStringParam), "_blank" });
    }

    private async Task ServicioIdChanged(int? value)
    {
        _filters.ServicioId = value;

        StateHasChanged();

        await _table.ReloadServerData();
    }

    private async Task FechaInicialChanged(DateTime? value)
    {
        _filters.FechaInicial = value;

        StateHasChanged();

        await ValidateDates();
    }

    private async Task FechaFinalChanged(DateTime? value)
    {
        _filters.FechaFinal = value;

        StateHasChanged();

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
        }

        await _table.ReloadServerData();
    }

    private async Task UnhandledErrorAlert(string message)
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Error,
            Title = "Ups, algo salió mal",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    {message} Intente de nuevo más tarde o consulte a un administrador.
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entendido"
        });
    }
}
