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

partial class ReporteIndividual
{
    [CascadingParameter] public MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Parameter] public string Username { get; set; } = null!;
    [Parameter] public int ServicioId { get; set; }
    [Parameter] public DateTime FechaMes { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly string _allItemsText = "Mostrando {first_item} de {last_item}. Total: {all_items}";
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    private readonly int[] _pageSizeOption = { 5, 10, 15, 20 };

    private int _pageSize = 10;
    private bool allRendered = false;
    private AsistenciaPersonalDTO _model = new();
    private IEnumerable<ServicioDTO> _servicios = new List<ServicioDTO>() { new() { Id = 0, Descripcion = "Ninguno" } };
    private SearchParameters _searchParameters = new();
    private AsistenciaFilterParameters _filters = new();
    private MudTable<AsistenciaPersonalDTO> _table = new();

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
            new BreadcrumbItem("Reportes", href: "asistencia/reportes"),
            new BreadcrumbItem("Detalle Reporte", href: $"asistencia/reporte/{Username}")
        };

        await Layout.SetBreadcrumb(breadcrumb);
    }

    private async Task SetFilterParameters()
    {
        _filters = new()
        {
            FechaMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            ServicioId = ServicioId,
            TimeZoneId = TimeZoneInfo.Local.Id,
            Username = Username
        };

        await GetServicios();

        await _table.ReloadServerData();

        allRendered = true;
    }

    private async Task GetServicios()
    {
        var showAllParam = new Dictionary<string, string> { ["showAll"] = true.ToString() };

        using var response = await HttpClient.GetAsync(QueryHelpers.AddQueryString(
            "servicios", showAllParam));

        if (response.IsSuccessStatusCode)
        {
            try
            {
                Stream stream = await response.Content.ReadAsStreamAsync();

                _servicios = _servicios.Concat(await JsonSerializer.DeserializeAsync<IEnumerable<ServicioDTO>>(stream, _options) ?? null!);

                _filters.ServicioId = _servicios.Where(a => a.Id == ServicioId).First().Id;

                StateHasChanged();
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

    private async Task<TableData<AsistenciaPersonalDTO>> GetServerData(TableState state)
    {
        // ES PARA QUE NO SE MANDE A LLAMAR SI NO SE HA CAMBIADO OTRA COSA JIJIJI
        if (allRendered)
            NavManager.NavigateTo($"asistencia/reporte/{Username}/{_filters.ServicioId ?? 0}/{_filters.FechaMes!.Value.ToString("yyyy-MM-dd")}");

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
            ["searchTerm"] = _searchParameters.SearchTerm ?? string.Empty,
            ["orderBy"] = _searchParameters.OrderBy ?? string.Empty,

            ["username"] = _filters.Username ?? string.Empty,
            ["servicioId"] = _filters.ServicioId.ToString() ?? string.Empty,
            ["timeZoneId"] = _filters.TimeZoneId ?? string.Empty,
            ["fechaMes"] = _filters.FechaMes!.Value.ToString("yyyy-MM-dd") ?? string.Empty
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

    private async Task ServicioIdChanged(int? value)
    {
        _filters.ServicioId = value;

        StateHasChanged();

        await _table.ReloadServerData();
    }

    private async Task FechaInicialChanged(DateTime? value)
    {
        _filters.FechaMes = value;

        StateHasChanged();

        await _table.ReloadServerData();
    }

    private async Task SuccessfulAlert(string titulo, string message)
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Success,
            Title = $"{titulo}",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: center"">
                    {message}
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entendido",
            DidOpen = new SweetAlertCallback(() =>
            {
                StateHasChanged();
            })
        });
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
