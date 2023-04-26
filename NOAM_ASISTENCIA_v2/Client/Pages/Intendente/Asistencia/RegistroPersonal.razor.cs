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

partial class RegistroPersonal
{
    [CascadingParameter] private MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private BarcodeReader? _scanner = new();
    private string _scannerVisibility = "d-none";
    private string _instructionsVisibility = "d-block";

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
        {
            new BreadcrumbItem("Inicio", href: ""),
            new BreadcrumbItem("Registro de Asistencia", href: "asistencia/registro")
        };

        await Layout.SetBreadcrumb(breadcrumb);
    }

    private async Task InitLectorQR()
    {
        await _scanner!.StartDecoding();
        _scannerVisibility = "d-block";
        _instructionsVisibility = "d-none";

        StateHasChanged();
    }

    private async Task StopLectorQR()
    {
        await _scanner!.StopDecoding();
        _scannerVisibility = "d-none";
        _instructionsVisibility = "d-block";

        StateHasChanged();
    }

    private async Task ReceiveQRCodeAsync(BarcodeReceivedEventArgs args)
    {
        await StopLectorQR();

        await Task.Delay(250); //SI NO SE HACE ESTO PUEDE LLEGARSE A TRIGGEREAR VARIAS VECES

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

                        string htmlMessage = $@"
                            Se ha registrado la <b>{(result.EsEntrada ? "entrada" : "salida")}</b>
                            del usuario <b>{result.Username}</b> exitosamente
                            <br />
                            <div class=""text-end"">
                                <br />Fecha: <b>{result.Fecha.ToString("yyyy/MM/dd")}</b>
                                <br />Hora: <b>{result.Fecha.ToString("HH:mm")}</b>
                            <div>
                        ";

                        await SuccessfulAlert(htmlMessage);
                    }
                    catch (Exception)
                    {
                        string error = "Ocurrió un error inesperado. Intente de nuevo más tarde o consulte a un administrador.";

                        await UnhandledErrorAlert(error);
                    }
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();

                    await UnhandledErrorAlert(error);
                }
            })
        });
    }

    private async Task SuccessfulAlert(string message)
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Success,
            Title = "Registro de asistencia exitoso",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    {message}
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
