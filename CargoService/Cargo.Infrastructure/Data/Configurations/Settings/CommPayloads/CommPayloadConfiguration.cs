using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cargo.Infrastructure.Data.Model.Settings.CommPayloads;

namespace Cargo.Infrastructure.Data.Configurations
{
    class CommPayloadConfiguration : IEntityTypeConfiguration<CommPayload>
    {
        public void Configure(EntityTypeBuilder<CommPayload> builder)
        {
            builder.ToTable("CommPayloads");

            builder.HasKey(a => a.Id);
            builder.Property(p => p.Id).ValueGeneratedOnAdd();

            builder.Property(p => p.Weight).IsRequired();
            builder.Property(p => p.Volume).IsRequired();
        }
    }
}
