using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using NOAM_ASISTENCIA_v2.Client.Shared;
using NOAM_ASISTENCIA_v2.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace NOAM_ASISTENCIA_v2.Client.Pages.Administrador.Servicios
{
    partial class Edit
    {
        [CascadingParameter] public MainLayout Layout { get; set; } = null!;

        [Parameter] public int ServicioId { get; set; }

        [Inject] private HttpClient _client { get; set; } = null!;
        [Inject] private NavigationManager _navManager { get; set; } = null!;

        private readonly string _title = "Edición Exitosa";
        private readonly string _message = "El servicio ha sido actualizado exitosamente.";
        private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

        private SucursalServicio _model = null!;
        private bool _isSuccessVisible = false;

        protected override async Task OnInitializedAsync()
        {
            await InitializeBreadcrumb();

            using var response = await _client.GetAsync($"sucursalesservicio/{ServicioId}");

            if (response.IsSuccessStatusCode)
            {
                Stream stream = await response.Content.ReadAsStreamAsync();

                _model = await JsonSerializer.DeserializeAsync<SucursalServicio>(stream, _options) ?? null!;
            }
        }

        private async Task InitializeBreadcrumb()
        {
            List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
            {
                new BreadcrumbItem("Inicio", href: ""),
                new BreadcrumbItem("Servicios", href: "/servicios"),
                new BreadcrumbItem("Editar", href: $"/servicios/edit/{ServicioId}")
            };

            await Layout.SetBreadcrumb(breadcrumb);
        }

        private async void OnValidSubmit(EditContext context)
        {
            using var response = await _client.PutAsJsonAsync($"sucursalesservicio/{ServicioId}", _model);

            if (response.IsSuccessStatusCode)
            {
                OpenDialog();
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
