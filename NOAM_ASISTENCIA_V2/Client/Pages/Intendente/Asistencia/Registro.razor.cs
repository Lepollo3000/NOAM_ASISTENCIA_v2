using BlazorBarcodeScanner.ZXing.JS;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Client.Utils;
using NOAM_ASISTENCIA_V2.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Intendente.Asistencia;

partial class Registro
{
    [CascadingParameter] private MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private BarcodeReader? _scanner = new();
    private string _scannerVisibility = "d-none";

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
        {
            new BreadcrumbItem("Inicio", href: ""),
            new BreadcrumbItem("Asistencia", href: "asistencia"),
            new BreadcrumbItem("Registro", href: $"asistencia/registro")
        };

        await Layout.SetBreadcrumb(breadcrumb);
    }

    private async Task InitLectorQR()
    {
        await _scanner!.StartDecoding();
        _scannerVisibility = "d-block";

        StateHasChanged();
    }

    private async Task StopLectorQR()
    {
        await _scanner!.StopDecoding();
        _scannerVisibility = "d-none";

        StateHasChanged();
    }

    private async Task ReceiveQRCodeAsync(BarcodeReceivedEventArgs args)
    {
        await StopLectorQR();

        // SE GUARDA EL CODIGO RECIBIDO PARA ENVIARLO
        bool succeeded = int.TryParse(args.BarcodeText, out int codigoQR);

        if (succeeded)
        {
            await LoadingAlert(codigoQR);
        }
    }

    private async Task LoadingAlert(int codigoQR)
    {
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
                // CREA REGISTRO EN EL SERVIDOR
                using var response = await HttpClient.PostAsJsonAsync(
                    "asistencias",
                    new AsistenciaRegistroDTO
                    {
                        ServicioId = codigoQR,
                        TimeZoneId = TimeZoneInfo.Local.Id
                    }
                 );

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        Stream stream = await response.Content.ReadAsStreamAsync();

                        AsistenciaRegistroResultDTO result = await JsonSerializer
                            .DeserializeAsync<AsistenciaRegistroResultDTO>(stream, _options) ?? null!;

                        await SuccessfulAlert(result);
                    }
                    catch (Exception)
                    {
                        await UnhandledErrorAlert("Ocurrió un error inesperado. Intente de nuevo más tarde o consulte a un administrador.");
                    }
                }
                else
                {
                    await UnhandledErrorAlert(await response.Content.ReadAsStringAsync());
                }
            })
        });
    }

    private async Task SuccessfulAlert(AsistenciaRegistroResultDTO result)
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Success,
            Title = "Registro de asistencia exitoso",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    Se ha registrado la <b>{(result.EsEntrada ? "entrada" : "salida")}</b>
                    del usuario <b>{result.Username}</b> exitosamente
                    <br />
                    <div class=""text-end"">
                        <br />Fecha: <b>{result.Fecha.ToString("yyyy/MM/dd")}</b>
                        <br />Hora: <b>{result.Fecha.ToString("HH:mm")}</b>
                    <div>
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entendido"
        });
    }

    private async Task UnhandledErrorAlert(string mensaje)
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Error,
            Title = "Lo sentimos, algo salió mal",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    {mensaje}
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entendido"
        });
    }
}
