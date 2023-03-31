using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using NOAM_ASISTENCIA_v2.Client.Shared;
using NOAM_ASISTENCIA_v2.Shared.Models;
using System.Net.Http.Json;

namespace NOAM_ASISTENCIA_v2.Client.Pages.Administrador.Servicios
{
    partial class Create
    {
        [CascadingParameter] private MainLayout _layout { get; set; } = null!;

        [Inject] private HttpClient _client { get; set; } = null!;
        [Inject] private NavigationManager _navManager { get; set; } = null!;

        private readonly string _title = "Alta Exitosa";
        private readonly string _message = "El servicio ha sido dado de alta exitosamente.";

        private SucursalServicio _model = new();
        private bool _isSuccessVisible = false;

        protected override async Task OnInitializedAsync()
        {
            List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
            {
                new BreadcrumbItem("Home", href: ""),
                new BreadcrumbItem("Servicios", href: "/servicios"),
                new BreadcrumbItem("Crear", href: $"/servicios/create")
            };

            await _layout.SetBreadcrumb(breadcrumb);
        }

        private async void OnValidSubmit(EditContext context)
        {
            using (var response = await _client.PostAsJsonAsync("sucursalesservicio", _model))
            {
                if (response.IsSuccessStatusCode)
                {
                    OpenDialog();
                }
            }
        }

        private void OpenDialog()
        {
            _isSuccessVisible = true;
            StateHasChanged();
        }

        private void CloseDialog()
        {
            _isSuccessVisible = false;
            StateHasChanged();
            _navManager.NavigateTo("servicios");
        }
    }
}
