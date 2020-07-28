using System.ComponentModel.DataAnnotations;

namespace Neo4j.AspNetCore.Identity.Sample.Models.AccountViewModels
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required] [EmailAddress] public string Email { get; set; }
    }
}