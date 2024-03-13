namespace DreamBot.Models
{
    public class UserTaskAddResult
    {
        public string ErrorMessage { get; set; } = string.Empty;

        public bool Success => string.IsNullOrEmpty(ErrorMessage);
    }
}