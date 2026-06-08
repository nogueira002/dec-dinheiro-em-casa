using System.ComponentModel.DataAnnotations;

namespace dotnet_mvc.ViewModels.Professor;

public class ReviewSubmissionViewModel
{
    public int SubmissionId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ChallengeTitle { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsAlreadyGraded { get; set; }

    [Required(ErrorMessage = "A nota é obrigatória.")]
    [Range(0.0, 20.0, ErrorMessage = "A nota deve ser um valor entre 0 e 20.")]
    public decimal? Grade { get; set; }

    [MaxLength(4000, ErrorMessage = "O feedback não pode exceder 4000 caracteres.")]
    public string? Feedback { get; set; }
}
