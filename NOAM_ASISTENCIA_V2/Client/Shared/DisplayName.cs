using System.Linq.Expressions;
using System.Reflection;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NOAM_ASISTENCIA_V2.Client.Shared;

public static class DisplayName
{
    public static string GetDisplayName<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty>> expressionFor)
    {
        Type type = typeof(TModel);

        MemberExpression expression = (MemberExpression)expressionFor.Body;
        string propertyName = (expression.Member is PropertyInfo) ? expression.Member.Name : null!;

        // First look into attributes on a type and it's parents
        DisplayAttribute attr;
        attr = (DisplayAttribute)type.GetProperty(propertyName)!.GetCustomAttributes(typeof(DisplayAttribute), true).SingleOrDefault()!;

        // Look for [MetadataType] attribute in type hierarchy
        // http://stackoverflow.com/questions/1910532/attribute-isdefined-doesnt-see-attributes-applied-with-metadatatype-class
        if (attr == null)
        {
            MetadataTypeAttribute metadataType = (MetadataTypeAttribute)type.GetCustomAttributes(typeof(MetadataTypeAttribute), true).FirstOrDefault()!;

            if (metadataType != null)
            {
                var property = metadataType.MetadataClassType.GetProperty(propertyName);
                if (property != null)
                {
                    attr = (DisplayAttribute)property.GetCustomAttributes(typeof(DisplayNameAttribute), true).SingleOrDefault()!;
                }
            }
        }

        return (attr != null) ? attr.Name! : String.Empty;
    }
}
