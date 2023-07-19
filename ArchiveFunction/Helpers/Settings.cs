using System;

namespace groveale
{
    public class Settings
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
        public string? StorageConnectionString {get;set;}
        public string? LinkToKB {get;set;}
        public string ArchiveHubUrl {get;set;}
        public string ArchiveHubListName {get;set;} 

        public static Settings LoadSettings()
        {
            return new Settings 
            {
                ClientId = Environment.GetEnvironmentVariable("clientId"),
                ClientSecret = Environment.GetEnvironmentVariable("clientSecret"),
                TenantId = Environment.GetEnvironmentVariable("tenantId"),
                StorageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString"),
                LinkToKB = Environment.GetEnvironmentVariable("linkToKB"),
                ArchiveHubUrl = Environment.GetEnvironmentVariable("archiveHubUrl"),
                ArchiveHubListName = Environment.GetEnvironmentVariable("archiveHubListName")
            };
        }
    }
}