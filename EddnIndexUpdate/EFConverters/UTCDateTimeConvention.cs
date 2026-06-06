using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EddnIndexUpdate.EFConverters;

public class UTCDateTimeConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (property.GetPrecision() is int precision && property.Builder is { } propertyBuilder)
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        propertyBuilder.HasConversion(new UTCDateTimeConverterNonNull(precision));
                    }
                    else if (property.ClrType == typeof(DateTimeOffset))
                    {
                        propertyBuilder.HasConversion(new UTCDateTimeConverter(precision));
                    }
                }
            }
        }
    }
}
