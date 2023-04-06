using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Shared.Models;
using System.Net.Http.Json;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Administrador.Turnos;

partial class Create
{
    [CascadingParameter] private MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private TurnoDTO _model = new() { Habilitado = true };

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
            {
                new BreadcrumbItem("Inicio", href: ""),
                new BreadcrumbItem("Turnos", href: "turnos"),
                new BreadcrumbItem("Crear", href: $"turnos/create")
            };

        await Layout.SetBreadcrumb(breadcrumb);
    }

    private async void OnValidSubmit(EditContext context)
    {
        await ConfirmAlert();
    }

    private async Task ConfirmAlert()
    {
        string confirmButtonColor = Theme.Palette.Error.Value;
        string cancelButtonColor = Theme.Palette.Secondary.Value;

        string estatus = _model.Habilitado ? "Habilitado" : "Deshabilitado";
        string descripcion = _model.Descripcion;

        string estatusLabel = DisplayName.GetDisplayName(_model, m => m.Habilitado);
        string descripcionLabel = DisplayName.GetDisplayName(_model, m => m.Descripcion);

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Warning,
            Title = "¿Desea realizar esta acción?",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    El turno será dado de alta con las siguientes propiedades:
                        <br />
                        <br /><b>{descripcionLabel}:</b> {descripcion}.
                        <br /><b>{estatusLabel}:</b> {estatus}.
                        <br />
                    <br />Una vez realizada, esta acción no podrá revertirse.
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
                        using var response = await HttpClient
                            .PostAsJsonAsync("turnos", _model);

                        if (response.IsSuccessStatusCode)
                        {
                            await SuccessfulAlert();
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

    private async Task SuccessfulAlert()
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        string estatus = _model.Habilitado ? "Habilitado" : "Deshabilitado";
        string descripcion = _model.Descripcion;

        string estatusLabel = DisplayName.GetDisplayName(_model, m => m.Habilitado);
        string descripcionLabel = DisplayName.GetDisplayName(_model, m => m.Descripcion);

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Success,
            Title = "Alta exitosa",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    El turno ha sido dado de alta exitosamente con las siguientes propiedades:
                        <br />
                        <br /><b>{descripcionLabel}:</b> {descripcion}.
                        <br /><b>{estatusLabel}:</b> {estatus}.
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entendido",
            DidClose = new SweetAlertCallback(() =>
            {
                NavManager.NavigateTo("turnos");
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
