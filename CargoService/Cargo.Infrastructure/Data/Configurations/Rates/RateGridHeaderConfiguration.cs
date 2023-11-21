using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cargo.Infrastructure.Data.Model.Rates;

namespace Cargo.Infrastructure.Data.Configurations.Rates
{
	internal class RateGridHeaderConfiguration: IEntityTypeConfiguration<RateGridHeader>
	{
		public void Configure (EntityTypeBuilder<RateGridHeader> builder)
		{
			builder.ToTable("RateGridHeaders").HasComment("Contains all read grid headers.");

			builder.HasKey(a => a.Id);

			builder.Property(p => p.Id).ValueGeneratedOnAdd().HasComment("ID and primary key of a rate grid.");
			builder.Property(p => p.Code).HasColumnName("Code").HasMaxLength(10).IsRequired().HasComment("Rate grid's code/name. Must be an English(10) unique string.");

			builder.HasIndex(p => p.Code, "IX_RateGridHeaders1").IsUnique(true);
		}
	}
}
