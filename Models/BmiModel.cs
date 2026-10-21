using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class BmiModel
    {
        [Required(ErrorMessage = "Waga jest wymagana.")]
        [Range(20, 300, ErrorMessage = "Podaj wagę w przedziale od 20 do 300 kg.")]
        [Display(Name = "Waga (kg)")]
        public double? Weight { get; set; }

        [Required(ErrorMessage = "Wzrost jest wymagany.")]
        [Range(50, 250, ErrorMessage = "Podaj wzrost w przedziale od 50 do 250 cm.")]
        [Display(Name = "Wzrost (cm)")]
        public double? Height { get; set; }

        public double? BmiResult { get; set; }
        public string? Category { get; set; }
    }
}