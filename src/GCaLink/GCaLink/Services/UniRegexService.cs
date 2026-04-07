using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GCaLink.Services
{
    class UniRegexService
    {
        private readonly Dictionary<string, Dictionary<string, Func<string, string>>> _uniParseFunctions;
        private string setUniversity;
        public UniRegexService(string universitySetting) {

            setUniversity = universitySetting;

            // Try to keep these in alphabetical order for the sake of maintainability pls
            _uniParseFunctions = new Dictionary<string, Dictionary<string, Func<string, string>>>
            {
                ["Arizona State University"] = new Dictionary<string, Func<string, string>>
                {
                    ["SectionInfo"] = GetAzStUniSI,
                    ["AssignmentName"] = GetAzStUniAN,
                    ["ClassName"] = GetAzStUniCN,
                    ["SectionName"] = GetAzStUniSN
                },
                ["Ohio State University"] = new Dictionary<string, Func<string, string>>
                {
                    ["SectionInfo"] = GetOhioStUniSI,
                    ["AssignmentName"] = GetOhioStUniAN,
                    ["ClassName"] = GetOhioStUniCN,
                    ["SectionName"] = GetOhioStUniSN
                }
            };
        }

        public string GetSectionInfo(string summary)
        {
            return _uniParseFunctions[setUniversity]["SectionInfo"](summary);
        }

        public string GetAssignmentName(string summary)
        {
            return _uniParseFunctions[setUniversity]["AssignmentName"](summary);
        }

        public string GetClassName(string sectionInfo)
        {
            return _uniParseFunctions[setUniversity]["ClassName"](sectionInfo);
        }
        
        public string GetSectionName(string sectionInfo)
        {
            return _uniParseFunctions[setUniversity]["SectionName"](sectionInfo);
        }

        /* NOTE FOR CONTRIBUTORS:
         * Certain universities may have edge cases for section naming. We will keep Regex.Match in every definition for this reason.
         */

        // Arizona State University
        private string GetAzStUniSI(string summary) => Regex.Match(summary, @"(?<=\[).*?(?=\])").Value.Trim();
        private string GetAzStUniAN(string summary) => Regex.Match(summary, @"^([^\[]*)").Value.Trim();
        private string GetAzStUniCN(string sectionInfo) => Regex.Match(sectionInfo, @"[A-Z]{3}[0-9]{3}").Value.Trim();
        private string GetAzStUniSN(string sectionInfo) => Regex.Match(sectionInfo, @"[0-9]{4}(.*?)[ABC]").Value.Trim();

        // Ohio State University
        private string GetOhioStUniSI(string summary) => "";
        private string GetOhioStUniAN(string summary) => "";
        private string GetOhioStUniCN(string sectionInfo) => "";
        private string GetOhioStUniSN(string sectionInfo) => "";

    }
}
