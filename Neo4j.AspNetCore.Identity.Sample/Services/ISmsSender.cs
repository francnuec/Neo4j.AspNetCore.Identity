﻿using System.Threading.Tasks;

namespace Neo4j.AspNetCore.Identity.Sample.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}