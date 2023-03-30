using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using NOAM_ASISTENCIA_v2.Client.Shared;
using NOAM_ASISTENCIA_v2.Shared.Models;

namespace NOAM_ASISTENCIA_v2.Client.Pages.Administrador.Servicios
{
    partial class Create
    {
        [CascadingParameter] public MainLayout Layout { get; set; } = null!;
        [Inject] private HttpClient _client { get; set; } = null!;
        
        private SucursalServicio _model = new();

        protected override async Task OnInitializedAsync()
        {
            List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
            {
                new BreadcrumbItem("Home", href: ""),
                new BreadcrumbItem("Servicios", href: "/servicios"),
                new BreadcrumbItem("Crear", href: $"/servicios/create")
            };

            await Layout.SetBreadcrumb(breadcrumb);
        }

        private void OnValidSubmit(EditContext context)
        {


            StateHasChanged();
        }
    }
}
