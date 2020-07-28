using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Neo4j.AspNetCore.Identity.Sample.Models.ManageViewModels
{
    public class ConfigureTwoFactorViewModel
    {
        public string SelectedProvider { get; set; }

        public ICollection<SelectListItem> Providers { get; set; }
    }
}