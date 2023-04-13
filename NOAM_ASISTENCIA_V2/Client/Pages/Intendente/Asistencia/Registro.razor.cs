using BlazorBarcodeScanner.ZXing.JS;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
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

    private BarcodeReader _scanner = new();

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
    private void InitLectorQR()
    {
        _scanner.StartDecoding();

        StateHasChanged();
    }

    private void ReceiveQRCode(BarcodeReceivedEventArgs args)
    {
        _scanner.StopDecoding();

        // SE GUARDA EL CODIGO RECIBIDO PARA ENVIARLO
        bool succeeded = int.TryParse(args.BarcodeText, out int codigoQR);

        StateHasChanged();
    }
}
