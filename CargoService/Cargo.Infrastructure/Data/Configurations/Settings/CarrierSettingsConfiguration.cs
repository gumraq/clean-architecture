using Cargo.Infrastructure.Data.Model.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cargo.Infrastructure.Data.Configurations.Settings
{
    public class CarrierSettingsConfiguration : IEntityTypeConfiguration<CarrierSettings>
    {
        public void Configure(EntityTypeBuilder<CarrierSettings> builder) 
        {
            builder.ToTable("CarrierSettings");

            builder.HasKey(a => new { a.CarrierId, a.ParametersSettingsId });

            builder.Property(p => p.Value).HasColumnName("Value").HasMaxLength(100).IsRequired();

            builder.HasOne(p => p.Carrier)
                .WithMany(d => d.CarrierSettings)
                .HasForeignKey(il => il.CarrierId);

            builder.HasOne(p => p.ParametersSettings)
                .WithMany(d => d.CarrierSettings)
                .HasForeignKey(il => il.ParametersSettingsId);
        }
    }
}
