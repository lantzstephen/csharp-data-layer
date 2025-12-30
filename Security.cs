using System;
using System.DirectoryServices;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;

namespace Portfolio.Common
{
    /// <summary>
    /// Security utilities for Active Directory integration.
    /// Demonstrates: AD/LDAP queries, role-based authorization, enum usage.
    /// </summary>
    public static class Security
    {
        /// <summary>
        /// Authorization role levels.
        /// </summary>
        public enum Role
        {
            RoleAny,
            RoleRead,
            RoleWrite,
            RoleOverride,
            RoleProcess,
            RoleConfig,
            RoleAdmin
        }

        /// <summary>
        /// Get all AD groups the user belongs to that match configured roles.
        /// </summary>
        public static string ADGroupsGranted(
            IConfiguration configuration,
            string applicationName,
            string adUser,
            bool firstOnly = false)
        {
            bool inGroup = false;
            string userADGroups = "";
            string allADGroups = GetConfiguredADGroups(configuration, applicationName);
            string environment = configuration.GetSection("AppSettings")["Environment"];

            if (!string.IsNullOrEmpty(allADGroups) && !string.IsNullOrEmpty(environment))
            {
                foreach (string adGroup in allADGroups.Replace("{ENV}", environment).Split('|'))
                {
                    inGroup = UserIsInADGroup(adGroup, adUser);
                    if (inGroup)
                    {
                        userADGroups += $" {adGroup},";
                        if (firstOnly) break;
                    }
                }
            }

            return userADGroups.Trim().TrimEnd(',');
        }

        /// <summary>
        /// Check if a user belongs to a specific Active Directory group.
        /// Uses tokenGroups attribute for efficient nested group membership check.
        /// </summary>
        public static bool UserIsInADGroup(string adGroup, string adUser)
        {
            try
            {
                string groupName = "";
                DirectorySearcher ds = new DirectorySearcher();
                ds.Filter = String.Format("(&(objectClass=user)(sAMAccountName={0}))", adUser);

                SearchResult sr = ds.FindOne();
                DirectoryEntry user = sr.GetDirectoryEntry();
                user.RefreshCache(new string[] { "tokenGroups" });

                for (int i = 0; i < user.Properties["tokenGroups"].Count; i++)
                {
                    SecurityIdentifier sid = new SecurityIdentifier(
                        (byte[])user.Properties["tokenGroups"][i], 0);
                    NTAccount nt = (NTAccount)sid.Translate(typeof(NTAccount));

                    groupName = System.IO.Path.GetFileName(nt.Value);

                    if (groupName == adGroup)
                        return true;
                }
            }
            catch (Exception) { }

            return false;
        }

        /// <summary>
        /// Get configured AD groups for an application (placeholder for actual config lookup).
        /// </summary>
        private static string GetConfiguredADGroups(IConfiguration configuration, string applicationName)
        {
            // In production, this would query a config table
            return configuration.GetSection("Security")[$"{applicationName}:ADGroups"] ?? "";
        }
    }
}
