using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.ViewModels.Student;

public class StudentChallengeDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Professor { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public string StatusKey { get; set; } = "pending";
    public string StatusLabel { get; set; } = "Pendente";
    public string? GradeDisplay { get; set; }
    public string? Feedback { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool HasSubmission { get; set; }
    public bool CanEditSubmission { get; set; }

    [Required(ErrorMessage = "Escreve a tua resposta antes de submeteres.")]
    [StringLength(8000, MinimumLength = 20, ErrorMessage = "A resposta deve ter entre 20 e 8000 caracteres.")]
    [Display(Name = "Resposta / entrega")]
    public string ResponseText { get; set; } = string.Empty;
}
