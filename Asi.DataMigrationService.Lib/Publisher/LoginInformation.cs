using System;
using Asi.Core.Interfaces;
using Newtonsoft.Json;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public class LoginInformation
    {
        public LoginInformation()
        {
            Uri = new Uri("http://localhost", UriKind.Absolute);
            UserCredentials = new UserCredentials(string.Empty, string.Empty);
        }
        public LoginInformation(Uri uri, UserCredentials userCredentials)
        {
            Uri = uri;
            UserCredentials = userCredentials;
        }
        public LoginInformation(Uri uri, string userName, string password) : this(uri, new UserCredentials(userName, password)) { }
        public LoginInformation(string url, string userName, string password) : this(new Uri(url), new UserCredentials(userName, password)) { }

        public Uri Uri { get; set; }
        public UserCredentials UserCredentials { get; set; }
        [JsonIgnore]
        public bool IsComplete
        {
            get
            {
                return Uri != null && !string.IsNullOrEmpty(UserCredentials?.UserName) && !string.IsNullOrEmpty(UserCredentials?.Password);
            }
        }
        public bool IsValidated { get; set; }
    }
}
