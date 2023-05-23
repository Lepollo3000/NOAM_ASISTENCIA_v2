using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Client.Utils.Features;
using NOAM_ASISTENCIA_V2.Shared.Models;
using NOAM_ASISTENCIA_V2.Shared.RequestFeatures;
using QRCoder;
using System.Net.Http.Json;
using System.Text.Json;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Administrador.Servicios;

partial class Index
{
    [CascadingParameter] public MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    private readonly string _allItemsText = "Mostrando {first_item} de {last_item}. Total: {all_items}";
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    private readonly int[] _pageSizeOption = { 5, 10, 15, 20 };

    private int _pageSize = 10;
    private ServicioDTO _model = new();
    private SearchParameters _searchParameters = new();
    private MudTable<ServicioDTO> _table = new();

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new()
        {
            new BreadcrumbItem("Inicio", href: ""),
            new BreadcrumbItem("Servicios", href: "servicios")
        };

        await Layout.SetBreadcrumb(breadcrumb);
    }

    private async Task<TableData<ServicioDTO>> GetServerData(TableState state)
    {
        _searchParameters.PageSize = state.PageSize;
        _searchParameters.PageNumber = state.Page + 1;
        _searchParameters.OrderBy = state.SortLabel == null ? state.SortLabel
            : $"{state.SortLabel} {state.SortDirection.ToDescriptionString()}";

        PagingResponse<ServicioDTO> response = await FetchSucursales(_searchParameters);

        return new TableData<ServicioDTO>
        {
            Items = response.Items,
            TotalItems = response.MetaData.TotalCount
        };
    }

    private async Task<PagingResponse<ServicioDTO>> FetchSucursales(SearchParameters productParameters)
    {
        var queryStringParam = new Dictionary<string, string>
        {
            ["pageNumber"] = productParameters.PageNumber.ToString(),
            ["pageSize"] = productParameters.PageSize.ToString(),
            ["searchTerm"] = productParameters.SearchTerm ?? "",
            ["orderBy"] = productParameters.OrderBy ?? ""
        };

        using var response = await HttpClient.GetAsync(QueryHelpers
            .AddQueryString("servicios", queryStringParam));

        if (response.IsSuccessStatusCode)
        {
            MetaData? metaData = JsonSerializer
                .Deserialize<MetaData>(response.Headers.GetValues("X-Pagination").First(), _options);

            Stream stream = await response.Content.ReadAsStreamAsync();

            var pagingResponse = new PagingResponse<ServicioDTO>()
            {
                Items = await JsonSerializer.DeserializeAsync<List<ServicioDTO>>(stream, _options) ?? null!,
                MetaData = metaData!
            };

            return pagingResponse;
        }

        var nullPagingResponse = new PagingResponse<ServicioDTO>
        {
            Items = null!,
            MetaData = null!
        };

        return nullPagingResponse;
    }

    private async Task ModificarEstatusRegistro(ServicioDTO registro)
    {
        await ConfirmAlert(registro);
    }

    private async Task DownloadQRCode(int servicioId, string codigoId)
    {
        await DownloadQRCodeAlert(servicioId, codigoId);
    }

    private async Task DownloadQRCodeAlert(int servicioId, string codigoId)
    {
        await SwalService.FireAsync(new SweetAlertOptions
        {
            Title = "Generando código QR... Espere",
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
                // POR ALGUNA PENDEJA RAZÓN SI NO SE PONE UN DELAY NO SE MUESTRA LA ALERTA
                await Task.Delay(250);

                bool successful = await GenerateQRCode(servicioId, codigoId);

                if (successful)
                {
                    await SuccessfulDownloadAlert();
                }
            })
        });
    }

    private async Task<bool> GenerateQRCode(int servicioId, string codigoId)
    {
        try
        {
            QRCodeGenerator qrCodeGenerate = new();

            // SE OBTIENEN LOS DATOS PARA CODIFICAR EL CÓDIGO QR
            QRCodeData qrCodeData = qrCodeGenerate.CreateQrCode(
                plainText: servicioId.ToString(),
                eccLevel: QRCodeGenerator.ECCLevel.Q
            );

            BitmapByteQRCode qrCode = new(qrCodeData);

            // SE OBTIENE EL GRÁFICO DEL CÓDIGO QR
            byte[] qrBitMap = qrCode.GetGraphic(20);

            // SE TRANSFORMA EL ARREGLO DE BYTES EN TEXTO PLANO CODIFICADO EN BASE 64
            string base64 = Convert.ToBase64String(qrBitMap);

            // INVOCA UNA FUNCION DE JS PARA DESCARGAR LA IMAGEN GENERADA DEL CODIGO QR
            await JsRuntime.InvokeAsync<object>("descargarImagenQR", $"QR_{codigoId}", base64);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task SuccessfulDownloadAlert()
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Success,
            Title = "Descarga exitosa",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: center"">
                    Código QR generado exitosamente.
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

    private async Task ConfirmAlert(ServicioDTO registro)
    {
        string confirmButtonColor = Theme.Palette.Error.Value;
        string cancelButtonColor = Theme.Palette.Secondary.Value;
        string estatusObjetivo = registro.Habilitado ? "Deshabilitado" : "Habilitado";

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Warning,
            Title = "¿Desea realizar esta acción?",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    Está a punto de cambiar el estado del servicio '{registro.CodigoId}' a {estatusObjetivo}.
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
                            .PutAsJsonAsync($"servicios/{registro.Id}", registro);

                        if (response.IsSuccessStatusCode)
                        {
                            await SuccessfulAlert(registro);
                        }
                        else
                        {
                            await UnhandledErrorAlert(registro);
                        }
                    })
                });
            }
        });
    }

    private async Task SuccessfulAlert(ServicioDTO registro)
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Success,
            Title = "Modificación exitosa",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    Se ha modificado el estado del servicio '{registro.CodigoId}' exitosamente.
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

    private async Task UnhandledErrorAlert(ServicioDTO registro)
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
            ConfirmButtonText = "Entendido",
            DidOpen = new SweetAlertCallback(() =>
            {
                registro.Habilitado = !registro.Habilitado;

                StateHasChanged();
            })
        });
    }
}
