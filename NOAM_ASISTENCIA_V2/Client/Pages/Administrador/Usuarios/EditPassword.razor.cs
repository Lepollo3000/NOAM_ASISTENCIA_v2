using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using NOAM_ASISTENCIA_V2.Client.Shared;
using NOAM_ASISTENCIA_V2.Shared.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace NOAM_ASISTENCIA_V2.Client.Pages.Administrador.Usuarios;

partial class EditPassword
{
    [CascadingParameter] public MainLayout Layout { get; set; } = null!;
    [CascadingParameter] public MudTheme Theme { get; set; } = null!;

    [Parameter] public string Username { get; set; } = null!;

    [Inject] private HttpClient HttpClient { get; set; } = null!;
    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private SweetAlertService SwalService { get; set; } = null!;

    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private UserDTO _model = null!;

    private UserPasswordChangeDTO _passwordChangeModel = new();
    private string _passwordInputIcon = "fa fa-eye-slash";
    private InputType _passwordInputType = InputType.Password;
    private bool _isPasswordIconShown;

    private string _passwordConfirmIcon = "fa fa-eye-slash";
    private InputType _passwordConfirmType = InputType.Password;
    private bool _isPasswordConfirmIconShown;

    protected override async Task OnInitializedAsync()
    {
        await InitializeBreadcrumb();

        await GetUsuario();
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
            }
            catch (Exception)
            {
                await UnhandledErrorAlert();
            }
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
            await NotFoundAlert();
        }
        else
        {
            await UnhandledErrorAlert();
        }
    }


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

    private async void OnValidPasswordSubmit(EditContext context)
    {
        await ConfirmPasswordAlert();
    }

    private async Task ConfirmPasswordAlert()
    {
        if (_model.ForgotPassword)
        {
            string confirmButtonColor = Theme.Palette.Error.Value;
            string cancelButtonColor = Theme.Palette.Secondary.Value;

            string newPasswordLabel = DisplayName.GetDisplayName(_passwordChangeModel, m => m.NewPassword);
            string newPassword = _passwordChangeModel.NewPassword;

            await SwalService.FireAsync(new SweetAlertOptions
            {
                Icon = SweetAlertIcon.Warning,
                Title = "¿Desea realizar esta acción?",
                Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                        Está a punto de cambiar la contraseña del usuario: <b>{_model.Username}</b>.
                        <br />
                        <br /><b>{newPasswordLabel}:</b> {newPassword}
                        <br />
                        <br />Asegúrese de que la nueva contraseña que se asignará sea la correcta.
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
                                .PutAsJsonAsync($"users/forgotpassword/{Username}", _passwordChangeModel);

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
                    Se ha modificado el usuario '{_model.Username}' exitosamente.
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

    private async Task NotFoundAlert()
    {
        string confirmButtonColor = Theme.Palette.Primary.Value;

        await SwalService.FireAsync(new SweetAlertOptions
        {
            Icon = SweetAlertIcon.Error,
            Title = "No hay cambios",
            Html = $@"<div class=""mx-4 my-3"" style=""text-align: justify"">
                    El usuario que se desea editar no se encontró o no existe.
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
