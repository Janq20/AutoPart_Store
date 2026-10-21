using Microsoft.AspNetCore.Mvc;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class BmiController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new BmiModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(BmiModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Weight.HasValue && model.Height.HasValue)
            {
                // Wzrost z cm na m: BMI = kg / (m * m)
                double heightInMeters = model.Height.Value / 100.0;
                double bmi = model.Weight.Value / (heightInMeters * heightInMeters);
                model.BmiResult = Math.Round(bmi, 2);

                model.Category = model.BmiResult switch
                {
                    < 18.5 => "Niedowaga",
                    < 25.0 => "Waga prawidłowa",
                    < 30.0 => "Nadwaga",
                    _ => "Otyłość"
                };
            }

            return View(model);
        }
    }
}