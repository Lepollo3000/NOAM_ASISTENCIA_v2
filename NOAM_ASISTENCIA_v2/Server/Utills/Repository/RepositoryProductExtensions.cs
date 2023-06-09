﻿using System.Reflection;
using System.Text;
using System.Linq.Dynamic.Core;
using NOAM_ASISTENCIA_V2.Server.Models;

namespace NOAM_ASISTENCIA_V2.Server.Utils.Repository
{
    public static class RepositoryProductExtensions
    {
        public static IQueryable<Servicio> Search(this IQueryable<Servicio> products, string searchTearm)
        {
            if (string.IsNullOrWhiteSpace(searchTearm))
                return products;

            var lowerCaseSearchTerm = searchTearm.Trim().ToLower();

            return products.Where(p => p.Descripcion.ToLower().Contains(lowerCaseSearchTerm));
        }

        public static IQueryable<Servicio> Sort(this IQueryable<Servicio> products, string orderByQueryString)
        {
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return products.OrderBy(e => e.Id);

            var orderParams = orderByQueryString.Trim().Split(',');
            var propertyInfos = typeof(Servicio).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var orderQueryBuilder = new StringBuilder();

            foreach (var param in orderParams)
            {
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                var propertyFromQueryName = param.Split(" ")[0];
                var objectProperty = propertyInfos.FirstOrDefault(pi => pi.Name.Equals(propertyFromQueryName, StringComparison.InvariantCultureIgnoreCase));

                if (objectProperty == null)
                    continue;

                var direction = param.EndsWith(" desc") ? "descending" : "ascending";
                orderQueryBuilder.Append($"{objectProperty.Name} {direction}, ");
            }

            var orderQuery = orderQueryBuilder.ToString().TrimEnd(',', ' ');

            if (string.IsNullOrWhiteSpace(orderQuery))
                return products.OrderBy(e => e.Id);

            return products.OrderBy(orderQuery);
        }
    }
}
