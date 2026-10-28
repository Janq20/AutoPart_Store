using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyApp.Models
{
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa produktu jest wymagana")]
        [StringLength(200)]
        [Display(Name = "Nazwa")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Cena musi być większa od zera")]
        [Display(Name = "Cena (zł)")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stan magazynowy nie może być ujemny")]
        [Display(Name = "Stan magazynowy")]
        public int StockQuantity { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Wybierz kategorię")]
        [Display(Name = "Kategoria")]
        public int MainCategoryId { get; set; }

        [Display(Name = "Podkategoria (opcjonalnie)")]
        public int? SubcategoryId { get; set; }

        [Display(Name = "Produkt dostępny w katalogu")]
        public bool IsActive { get; set; } = true;

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Subcategories { get; set; } = new();
    }
}
