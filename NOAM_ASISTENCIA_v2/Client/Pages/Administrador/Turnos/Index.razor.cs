using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Client.Utils.Features;
using NOAM_ASISTENCIA_V2.Shared.Models;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures;
using System.Net.Http.Json;
using System.Text.Json;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Administrador.Turnos;

partial class Index
{
    [CascadingParameter] public MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly string _allItemsText = "Mostrando {first_item} de {last_item}. Total: {all_items}";
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    private readonly int[] _pageSizeOption = { 5, 10, 15, 20 };

    private int _pageSize = 5;
    private TurnoDTO _model = new();
    private SearchParameters _searchParameters = new();
    private MudTable<TurnoDTO> _table = new();

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new()
            {
                new BreadcrumbItem("Inicio", href: ""),
                new BreadcrumbItem("Turnos", href: "turnos")
            };

        await Layout.SetBreadcrumb(breadcrumb);
    }

    private async Task<TableData<TurnoDTO>> GetServerData(TableState state)
    {
        _searchParameters.PageSize = state.PageSize;
        _searchParameters.PageNumber = state.Page + 1;
        _searchParameters.OrderBy = state.SortLabel == null ? state.SortLabel
            : $"{state.SortLabel} {state.SortDirection.ToDescriptionString()}";

        PagingResponse<TurnoDTO> response = await FetchSucursales(_searchParameters);

        return new TableData<TurnoDTO>
        {
            Items = response.Items,
            TotalItems = response.MetaData.TotalCount
        };
    }

    private async Task<PagingResponse<TurnoDTO>> FetchSucursales(SearchParameters productParameters)
    {
        var queryStringParam = new Dictionary<string, string>
        {
            ["pageNumber"] = productParameters.PageNumber.ToString(),
            ["pageSize"] = productParameters.PageSize.ToString(),
            ["searchTerm"] = productParameters.SearchTerm ?? "",
            ["orderBy"] = productParameters.OrderBy ?? ""
        };

        using var response = await HttpClient.GetAsync(QueryHelpers.AddQueryString("turnos", queryStringParam));

        if (response.IsSuccessStatusCode)
        {
            MetaData? metaData = JsonSerializer
                .Deserialize<MetaData>(response.Headers.GetValues("X-Pagination").First(), _options);

            Stream stream = await response.Content.ReadAsStreamAsync();

            var pagingResponse = new PagingResponse<TurnoDTO>()
            {
                Items = await JsonSerializer.DeserializeAsync<List<TurnoDTO>>(stream, _options) ?? null!,
                MetaData = metaData!
            };

            return pagingResponse;
        }

        var nullPagingResponse = new PagingResponse<TurnoDTO>
        {
            Items = null!,
            MetaData = null!
        };

        return nullPagingResponse;
    }

    private async Task ModificarEstatusRegistro(TurnoDTO registro)
    {
        await ConfirmAlert(registro);
    }

    private async Task ConfirmAlert(TurnoDTO registro)
    {
        string confirmButtonColor = Theme.Palette.Error.Value;
        string cancelButtonColor = Theme.Palette.Secondary.Value;
        string estatusObjetivo = registro.Habilitado ? "Deshabilitado" : "Habilitado";

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Warning,
            Title = "¿Desea realizar esta acción?",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    Está a punto de cambiar el estado del turno '{registro.Descripcion}' a {estatusObjetivo}.
                    <br />
                    <br />Si bien se puede revertir esta acción, los resultados de la misma no se 
                    pueden deshacer.
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entiendo los riesgos y acepto",
            ShowCancelButton = true,
            CancelButtonColor = cancelButtonColor,
            CancelButtonText = "Cancelar",
            FocusConfirm = false
        })
        .ContinueWith(async (swalTask) =>
        {
            SweetAlertResult swalResult = await swalTask;

            if (swalResult.IsConfirmed)
            {
                // DE HABILITADO A DESHABILITADO Y VICEVERSA
                registro.Habilitado = !registro.Habilitado;

                await SwalService.FireAsync(new SweetAlertOptions
                {
                    Title = "Cargando... Espere",
                    Html = $@"<div class=""mx-4 my-5"" style=""text-align: center"">
                            <i class=""text-info fa fa-sync-alt fa-4x fa-spin""></i>
                        </div>",
                    ShowConfirmButton = false,
                    ShowCancelButton = false,
                    AllowEscapeKey = false,
                    AllowEnterKey = false,
                    AllowOutsideClick = false,
                    DidOpen = new SweetAlertCallback(async () =>
                    {
                        // ACTUALIZAR REGISTRO EN EL SERVIDOR
                        using var response = await HttpClient
                            .PutAsJsonAsync($"turnos/{registro.Id}", registro);

                        if (response.IsSuccessStatusCode)
                        {
                            await SuccessfulAlert(registro);
                        }
                        else
                        {
                            await UnhandledErrorAlert();
                        }
                    })
                });
            }
        });
    }

    private async Task SuccessfulAlert(TurnoDTO registro)
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Success,
            Title = "Modificación exitosa",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    Se ha modificado el estado del turno '{registro.Descripcion}' exitosamente.
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entendido",
            DidClose = new SweetAlertCallback(async () =>
            {
                await _table.ReloadServerData();
            })
        });
    }

    private async Task UnhandledErrorAlert()
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Error,
            Title = "Ups, algo salió mal",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    Ocurrió un error inesperado. Intente de nuevo más tarde o consulte a un administrador.
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entendido"
        });
    }
}
