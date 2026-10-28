using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyApp.Models
{
    public class CategoryFormViewModel
    {
        [Required(ErrorMessage = "Nazwa kategorii jest wymagana")]
        [StringLength(100)]
        [Display(Name = "Nazwa")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        [Display(Name = "Kategoria nadrzędna (opcjonalnie)")]
        public int? ParentCategoryId { get; set; }

        public List<SelectListItem> ParentCategories { get; set; } = new();
    }
}
