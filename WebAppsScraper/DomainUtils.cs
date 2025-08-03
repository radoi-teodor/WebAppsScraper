using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;

namespace WebAppsScraper
{
    internal class DomainUtils
    {
        // IP - DC IP
        // DOMAIN - THE DOMAIN URL (lab.domain.com)
        // it will use current LDAP session
        public static List<string> GetDomainComputers(string ip, string domain)
        {
            string[] domainParts = domain.Split('.');
            string dn = string.Join(",", Array.ConvertAll(domainParts, part => $"DC={part}"));

            string ldapPath = $"LDAP://{ip}/{dn}";

            List<string> computerNames = new List<string>();

            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(ldapPath))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(entry))
                    {
                        searcher.Filter = "(objectCategory=computer)";
                        searcher.PropertiesToLoad.Add("name");

                        foreach (SearchResult result in searcher.FindAll())
                        {
                            if (result.Properties.Contains("name"))
                            {
                                computerNames.Add(result.Properties["name"][0].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LDAP Exception: {ex.Message}");
            }

            return computerNames;
        }
    }
}
