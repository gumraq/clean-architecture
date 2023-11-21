﻿using Cargo.Infrastructure.Data.Model.Dictionary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cargo.Infrastructure.Data.Configurations
{
    class AircraftTypeConfigutaion : IEntityTypeConfiguration<AircraftType>
    {
        public void Configure(EntityTypeBuilder<AircraftType> builder)
        {
            builder.ToTable("AircraftTypes");

            builder.HasKey(a => a.Id);

            builder.Property(p => p.Id).ValueGeneratedOnAdd();
            builder.Property(p => p.IataCode).HasColumnName("Iata").HasMaxLength(3).IsRequired();
            builder.Property(p => p.IcaoCode).HasColumnName("Icao").HasMaxLength(5);
            builder.Property(p => p.InternationalName).HasColumnName("InternationalName").HasMaxLength(70).IsRequired();
            builder.Property(p => p.RussianName).HasColumnName("RussianName").HasMaxLength(100);
            builder.Property(p => p.MaxGrossCapacity).HasColumnName("MaxGrossCapacity");
            builder.Property(p => p.MaxGrossPayload).HasColumnName("MaxGrossPayload");
            builder.Property(p => p.MaxTakeOffWeight).HasColumnName("MaxTakeOffWeight");
            builder.Property(p => p.Oew).HasColumnName("Oew");
            builder.Property(p => p.IsCargo).HasColumnName("IsCargo");
            builder.Property(p => p.BaseType).HasColumnName("BaseType").HasMaxLength(5);
        }
    }
}
