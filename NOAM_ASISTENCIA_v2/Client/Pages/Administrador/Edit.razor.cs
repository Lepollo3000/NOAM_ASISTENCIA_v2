using Microsoft.AspNetCore.Components;
using MudBlazor;
using NOAM_ASISTENCIA_v2.Client.Shared;
using NOAM_ASISTENCIA_v2.Shared.Models;

namespace NOAM_ASISTENCIA_v2.Client.Pages.Administrador
{
    partial class Edit
    {
        [CascadingParameter] public MainLayout Layout { get; set; } = null!;
        [Parameter] public int ServicioId { get; set; }

        private SucursalServicio _model = null!;

        protected override async Task OnInitializedAsync()
        {
            List<BreadcrumbItem> breadcrumb = new List<BreadcrumbItem>()
            {
                new BreadcrumbItem("Home", href: ""),
                new BreadcrumbItem("Servicios", href: "/servicios"),
                new BreadcrumbItem("Editar", href: $"/servicios/edit/{ServicioId}")
            };

            await Layout.SetBreadcrumb(breadcrumb);
        }
    }
}
