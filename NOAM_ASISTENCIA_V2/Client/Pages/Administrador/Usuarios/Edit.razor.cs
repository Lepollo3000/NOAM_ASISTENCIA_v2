using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Shared.Models;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Administrador.Usuarios;

partial class Edit
{
    [CascadingParameter] public MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Parameter] public string Username { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private bool _isBusy = false;
    private UserDTO _model = null!;
    private UserDTO _newModel = null!;
    private IEnumerable<TurnoDTO> _turnos = null!;

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();

        _isBusy = true;

        await GetUsuario();
        await GetTurnos();

        _isBusy = false;
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
            {
                new BreadcrumbItem("Inicio", href: ""),
                new BreadcrumbItem("Usuarios", href: "usuarios"),
                new BreadcrumbItem("Editar", href: $"usuarios/edit/{Username}")
            };

        await Layout.SetBreadcrumb(breadcrumb);
    }

    private async Task GetUsuario()
    {
        using var response = await HttpClient.GetAsync($"users/{Username}");

        if (response.IsSuccessStatusCode)
        {
            try
            {
                Stream stream = await response.Content.ReadAsStreamAsync();

                _model = await JsonSerializer.DeserializeAsync<UserDTO>(stream, _options) ?? null!;
                _newModel = new UserDTO
                {
                    Id = _model.Id,
                    Username = _model.Username,
                    Nombre = _model.Nombre,
                    Apellido = _model.Apellido,
                    IdTurno = _model.IdTurno,
                    NombreTurno = _model.NombreTurno,
                    Lockout = _model.Lockout
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
    }

    private async Task GetTurnos()
    {
        var showAllParam = new Dictionary<string, string> { ["showAll"] = true.ToString() };

        using var response = await HttpClient.GetAsync(
            QueryHelpers.AddQueryString("turnos", showAllParam)
        );

        if (response.IsSuccessStatusCode)
        {
            try
            {
                Stream stream = await response.Content.ReadAsStreamAsync();

                _turnos = await JsonSerializer.DeserializeAsync<IEnumerable<TurnoDTO>>(stream, _options) ?? null!;
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
    }

    // CAMBIA EL NOMBRE DEL TURNO QUE SE SELECCIONÓ PARA PODER MOSTRARLO EN LOS ALERTS
    private void ChangeNombreTurno(int value)
    {
        TurnoDTO? turnoSeleccionado = _turnos.SingleOrDefault(t => t.Id == value);

        if (turnoSeleccionado != null)
        {
            _newModel.NombreTurno = turnoSeleccionado.Descripcion;
        }

        StateHasChanged();
    }

    private async void OnValidSubmit(EditContext context)
    {
        await ConfirmAlert();
    }

    private async Task ConfirmAlert()
    {
        string confirmButtonColor = Theme.Palette.Error.Value;
        string cancelButtonColor = Theme.Palette.Secondary.Value;

        bool cambioEnNombre = _newModel.Nombre != _model.Nombre;
        bool cambioEnApellido = _newModel.Apellido != _model.Apellido;
        bool cambioEnTurno = _newModel.IdTurno != _model.IdTurno;
        bool cambioEnEstatus = _newModel.Lockout != _model.Lockout;
        string cambios = "";

        string nombreLabel = DisplayName.GetDisplayName(_model, m => m.Nombre);
        string apellidoLabel = DisplayName.GetDisplayName(_model, m => m.Apellido);
        string turnoLabel = DisplayName.GetDisplayName(_model, m => m.IdTurno);
        string estatusLabel = DisplayName.GetDisplayName(_model, m => m.Lockout);

        if (!cambioEnNombre && !cambioEnApellido && !cambioEnTurno && !cambioEnEstatus)
        {
            await NoChangesAlert();
        }
        else
        {
            if (cambioEnNombre)
            {
                cambios += $"<br /><b>{nombreLabel}:</b> De '{_model.Nombre}' a '{_newModel.Nombre}'.";
            }

            if (cambioEnApellido)
            {
                cambios += $"<br /><b>{apellidoLabel}:</b> De '{_model.Apellido}' a '{_newModel.Apellido}'.";
            }

            if (cambioEnTurno)
            {
                ChangeNombreTurno(_newModel.IdTurno);

                cambios += $"<br /><b>{turnoLabel}:</b> De '{_model.NombreTurno}' a '{_newModel.NombreTurno}'.";
            }

            if (cambioEnEstatus)
            {
                string estadoOriginal = !_model.Lockout ? "Habilitado" : "Deshabilitado";
                string estadoNuevo = !_newModel.Lockout ? "Habilitado" : "Deshabilitado";

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
                                .PutAsJsonAsync($"users/{Username}", _newModel);

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
                    Se ha modificado el usuario '{_newModel.Username}' exitosamente.
                </div>",
            ShowConfirmButton = true,
            ConfirmButtonColor = confirmButtonColor,
            ConfirmButtonText = "Entendido",
            DidClose = new SweetAlertCallback(() =>
            {
                NavManager.NavigateTo("usuarios");
            })
        });
    }

    private async Task NoChangesAlert()
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
