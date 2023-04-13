using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Client.Utils;
using NOAM_ASISTENCIA_V2.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Administrador.Servicios;

partial class Edit
{
    [CascadingParameter] public MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Parameter] public int ServicioId { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private bool _isBusy = false;
    private ServicioDTO _model = null!;
    private ServicioDTO _newModel = null!;

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();

        _isBusy = true;

        using var response = await HttpClient.GetAsync($"servicios/{ServicioId}");

        if (response.IsSuccessStatusCode)
        {
            try
            {
                Stream stream = await response.Content.ReadAsStreamAsync();

                _model = await JsonSerializer.DeserializeAsync<ServicioDTO>(stream, _options) ?? null!;
                _newModel = new ServicioDTO
                {
                    Id = _model.Id,
                    CodigoId = _model.CodigoId,
                    Descripcion = _model.Descripcion,
                    Habilitado = _model.Habilitado
                };
            }
            catch (Exception)
            {
                await UnhandledErrorAlert();
            }
        }
        else
        {
            await UnhandledErrorAlert();
        }

        _isBusy = false;
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
            {
                new BreadcrumbItem("Inicio", href: ""),
                new BreadcrumbItem("Servicios", href: "servicios"),
                new BreadcrumbItem("Editar", href: $"servicios/edit/{ServicioId}")
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

        bool cambioEnDescripcion = _newModel.Descripcion != _model.Descripcion;
        bool cambioEnEstatus = _newModel.Habilitado != _model.Habilitado;
        string cambios = "";

        string estatusLabel = DisplayName.GetDisplayName(_model, m => m.Habilitado);
        string descripcionLabel = DisplayName.GetDisplayName(_model, m => m.Descripcion);

        if (cambioEnDescripcion && !cambioEnEstatus)
        {
            await SinCambiosAlert();
        }
        else
        {
            if (cambioEnDescripcion)
            {
                cambios += $"<br /><b>{descripcionLabel}:</b> De '{_model.Descripcion}' a '{_newModel.Descripcion}'.";
            }

            if (cambioEnEstatus)
            {
                string estadoOriginal = _model.Habilitado ? "Habilitado" : "Deshabilitado";
                string estadoNuevo = _newModel.Habilitado ? "Habilitado" : "Deshabilitado";

                cambios += $"<br /><b>{estatusLabel}:</b> De {estadoOriginal} a {estadoNuevo}.";
            }

            await SwalService.FireAsync(new SweetAlertOptions
            {
                Icon = SweetAlertIcon.Warning,
                Title = "¿Desea realizar esta acción?",
                Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                        Está a punto de modificar las siguientes propiedades:
                        <br />{cambios}
                    </div>",
                ShowConfirmButton = true,
                ConfirmButtonColor = confirmButtonColor,
                ConfirmButtonText = "Aceptar",
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
                            // ACTUALIZAR REGISTRO EN EL SERVIDOR
                            using var response = await HttpClient
                                .PutAsJsonAsync($"servicios/{ServicioId}", _newModel);

                            if (response.IsSuccessStatusCode)
                            {
                                await SuccessfulConfirmAlert();
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
    }

    private async Task SuccessfulConfirmAlert()
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Success,
            Title = "Modificación exitosa",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    Se ha modificado el servicio '{_newModel.CodigoId}' exitosamente.
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entendido",
            DidClose = new SweetAlertCallback(() =>
            {
                NavManager.NavigateTo("servicios");
            })
        });
    }

    private async Task SinCambiosAlert()
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Error,
            Title = "No hay cambios",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    No se detectaron cambios para actualizar. Verifique los cambios que está realizando o contacte a un administrador.
                </div>",
            ShowConfirmButton = false,
            ShowCancelButton = true,
            CancelButtonColor = confirmButtonColor,
            CancelButtonText = "Entendido"
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
