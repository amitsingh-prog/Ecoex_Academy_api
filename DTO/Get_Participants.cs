namespace Ecoex_Academy_Api.DTO
{
    public class Get_Participants
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
