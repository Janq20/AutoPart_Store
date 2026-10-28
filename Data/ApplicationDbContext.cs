using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace MyApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(user => user.Email)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasIndex(category => category.Name)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .HasOne(category => category.ParentCategory)
                .WithMany(category => category.Subcategories)
                .HasForeignKey(category => category.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .Property(product => product.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Product>()
                .HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(order => order.User)
                .WithMany()
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(order => order.DeliveryAddress)
                .WithMany()
                .HasForeignKey(order => order.DeliveryAddressId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderItem>()
                .HasOne(item => item.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(item => item.Product)
                .WithMany(product => product.OrderItems)
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}