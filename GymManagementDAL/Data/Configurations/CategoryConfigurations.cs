using GymManagementDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementDAL.Data.Configurations
{
    internal class CategoryConfigurations : IEntityTypeConfiguration<CategoryEntity>
    {
        public void Configure(EntityTypeBuilder<CategoryEntity> builder)
        {
            builder.Property(X => X.CategoryName)
                .HasColumnType("varchar")
                .HasMaxLength(20);

            builder.HasData(
                              new CategoryEntity { Id = 1, CategoryName = "Cardio" },
                              new CategoryEntity { Id = 2, CategoryName = "Strength" },
                              new CategoryEntity { Id = 3, CategoryName = "Yoga" },
                              new CategoryEntity { Id = 4, CategoryName = "Boxing" },
                              new CategoryEntity { Id = 5, CategoryName = "CrossFit" }
                          );
        }
    }
}
