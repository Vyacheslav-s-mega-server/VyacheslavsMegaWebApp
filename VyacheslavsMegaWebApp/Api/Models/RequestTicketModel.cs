namespace VyacheslavsMegaWebApp.Api.Models
{
    public class RequestTicketModel
    {
        public string Username { get; set; } = string.Empty;
        public string SessionKey { get; set; } = string.Empty;
        public string Hwid { get; set; } = string.Empty;
    }
}
