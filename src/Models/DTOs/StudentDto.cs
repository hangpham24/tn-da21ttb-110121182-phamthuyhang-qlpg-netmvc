namespace GymManagement.Web.Models.DTOs
{
    public class StudentDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Status { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? ClassName { get; set; }
        public string? Avatar { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public DateTime? JoinDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public List<ClassRegistrationDto>? Registrations { get; set; }
    }

    public class ClassRegistrationDto
    {
        public string? ClassName { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Status { get; set; }
    }
}
