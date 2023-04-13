using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Shared.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using NOAM_ASISTENCIA_V2.Client.Utils;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Administrador.Usuarios;

partial class Create
{
    [CascadingParameter] private MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private UserRegisterDTO _model = new() { IdTurno = 1 };

    private UserPasswordChangeDTO _passwordChangeModel = new();
    private string _passwordInputIcon = "fa fa-eye-slash";
    private InputType _passwordInputType = InputType.Password;
    private bool _isPasswordIconShown;

    private string _passwordConfirmIcon = "fa fa-eye-slash";
    private InputType _passwordConfirmType = InputType.Password;
    private bool _isPasswordConfirmIconShown;

    private IEnumerable<TurnoDTO> _turnos = null!;
    private IEnumerable<string> _roles = null!;

    private void PasswordIconPressed()
    {
        if (_isPasswordIconShown)
        {
            _isPasswordIconShown = false;
            _passwordInputIcon = "fa fa-eye-slash";
            _passwordInputType = InputType.Password;
        }
        else
        {
            _isPasswordIconShown = true;
            _passwordInputIcon = "fa fa-eye";
            _passwordInputType = InputType.Text;
        }
    }

    private void ConfirmPasswordIconPressed()
    {
        if (_isPasswordConfirmIconShown)
        {
            _isPasswordConfirmIconShown = false;
            _passwordConfirmIcon = "fa fa-eye-slash";
            _passwordConfirmType = InputType.Password;
        }
        else
        {
            _isPasswordConfirmIconShown = true;
            _passwordConfirmIcon = "fa fa-eye";
            _passwordConfirmType = InputType.Text;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();

        await GetTurnos();
        await GetRoles();
    }

    private async Task InitializeBreadcrumb()
    {
        List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
        {
            new BreadcrumbItem("Inicio", href: ""),
            new BreadcrumbItem("Usuarios", href: "usuarios"),
            new BreadcrumbItem("Crear", href: $"usuarios/create")
        };

        await Layout.SetBreadcrumb(breadcrumb);
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

    private async Task GetRoles()
    {
        using var response = await HttpClient.GetAsync("users/getroles");

        if (response.IsSuccessStatusCode)
        {
            try
            {
                Stream stream = await response.Content.ReadAsStreamAsync();

                _roles = await JsonSerializer.DeserializeAsync<IEnumerable<string>>(stream, _options) ?? null!;
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
            _model.NombreTurno = turnoSeleccionado.Descripcion;
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

        string username = _model.Username;
        string nombre = _model.Nombre;
        string apellido = _model.Apellido;
        string password = _model.Password;
        string estatus = !_model.Lockout ? "Habilitado" : "Deshabilitado";

        string usernameLabel = DisplayName.GetDisplayName(_model, m => m.Username);
        string nombreLabel = DisplayName.GetDisplayName(_model, m => m.Nombre);
        string apellidoLabel = DisplayName.GetDisplayName(_model, m => m.Apellido);
        string passwordLabel = DisplayName.GetDisplayName(_model, m => m.Password);
        string estatusLabel = DisplayName.GetDisplayName(_model, m => m.Lockout);

        ChangeNombreTurno(_model.IdTurno);

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Warning,
            Title = "¿Desea realizar esta acción?",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    El usuario será dado de alta con las siguientes propiedades:
                        <br />
                        <br /><b>{usernameLabel}:</b> {username}.
                        <br /><b>{nombreLabel}:</b> {nombre}.
                        <br /><b>{apellidoLabel}:</b> {apellido}.
                        <br /><b>{passwordLabel}:</b> {password}.
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
                            .PostAsJsonAsync("users", _model);

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

        string username = _model.Username;
        string nombre = _model.Nombre;
        string apellido = _model.Apellido;
        string password = _model.Password;
        string estatus = !_model.Lockout ? "Habilitado" : "Deshabilitado";

        string usernameLabel = DisplayName.GetDisplayName(_model, m => m.Username);
        string nombreLabel = DisplayName.GetDisplayName(_model, m => m.Nombre);
        string apellidoLabel = DisplayName.GetDisplayName(_model, m => m.Apellido);
        string passwordLabel = DisplayName.GetDisplayName(_model, m => m.Password);
        string estatusLabel = DisplayName.GetDisplayName(_model, m => m.Lockout);

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Success,
            Title = "Alta exitosa",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    El usuario ha sido dado de alta exitosamente con las siguientes propiedades:
                        <br />
                        <br /><b>{usernameLabel}:</b> {username}.
                        <br /><b>{nombreLabel}:</b> {nombre}.
                        <br /><b>{apellidoLabel}:</b> {apellido}.
                        <br /><b>{passwordLabel}:</b> {password}.
                        <br /><b>{estatusLabel}:</b> {estatus}.
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
