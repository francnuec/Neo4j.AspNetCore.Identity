using System.Threading.Tasks;

namespace Neo4j.AspNetCore.Identity.Sample.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}