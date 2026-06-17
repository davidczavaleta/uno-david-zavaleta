using System.ComponentModel.DataAnnotations;

namespace OrderOrchestration.Api.Bff.Models
{
    /// <summary>
    /// Petición REST para resolver manualmente una orden en revisión (panel admin).
    /// </summary>
    public class ReviewRequestDto
    {
        public bool Approved { get; set; }

        [Required(ErrorMessage = "Reviewer es requerido.")]
        public string Reviewer { get; set; } = string.Empty;
    }
}
