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
                    ["SectionInfo"] = SectionInfoUniversal,
                    ["AssignmentName"] = AssignmentNameUniversal,
                    ["ClassName"] = GetAzStUniCN,
                    ["SectionName"] = GetAzStUniSN
                },
                ["Ohio State University"] = new Dictionary<string, Func<string, string>>
                {
                    ["SectionInfo"] = SectionInfoUniversal,
                    ["AssignmentName"] = AssignmentNameUniversal,
                    ["ClassName"] = GetOhioStUniCN,
                    ["SectionName"] = GetOhioStUniSN
                },
                ["University of Washington"] = new Dictionary<string, Func<string, string>>
                {
                    ["SectionInfo"] = SectionInfoUniversal,
                    ["AssignmentName"] = AssignmentNameUniversal,
                    ["ClassName"] = GetUWStUniCN,
                    ["SectionName"] = GetUWStUniSN
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
         * Certain universities may have edge cases for section naming. We will keep the definition
         * to be lambda functions; full on functions can be added if more logic is needed down the line.
         */

        // Universal
        /* TODO: 
        If an instructor decides to do something and add a bracket in the assignment name, this will break.
            possible improvement for a SectionInfoUniversal regex. I haven't seen such an edge case yet, but this should
            addressed before V1 release. 

        Another improvement idea is to use named capture groups; this would make running regex
        multiple times unnessecary.  
        */
        private string SectionInfoUniversal(string summary) => Regex.Match(summary, @"(?<=\[).*?(?=\])").Value.Trim();
        private string AssignmentNameUniversal(string summary) => Regex.Match(summary, @"^([^\[]*)").Value.Trim();

        // Generalized Regex; deals with 3-4
        private string GetGeneralCN(string sectionInfo) => Regex.Match(sectionInfo, @"[A-Z]{3, 5} ?[0-9]{3}[a-zA-Z]{0, 3}").Value.Trim();

        // Arizona State University
        private string GetAzStUniCN(string sectionInfo) => Regex.Match(sectionInfo, @"[A-Z]{3}[0-9]{3}?").Value.Trim();
        private string GetAzStUniSN(string sectionInfo) => Regex.Match(sectionInfo, @"[0-9]{4}(.*?)[ABC]").Value.Trim();

        // Ohio State University
        private string GetOhioStUniCN(string sectionInfo) => Regex.Match(sectionInfo, @"[A-Z]{3,} [0-9]{4}(?:\.[0-9]{2})?(?:[A-Z])?").Value.Trim();
        private string GetOhioStUniSN(string sectionInfo) => Regex.Match(sectionInfo, @"(?<=[.* .* ])[A-Z]{2}[0-9]{4}").Value.Trim();

        // University of Washington
        private string GetUWStUniCN(string sectionInfo) => Regex.Match(sectionInfo, @"^[A-Z][A-Z& ]*?\s+\d{3}(?=\s+[A-Z][A-Z0-9]?$)").Value.Trim();
        private string GetUWStUniSN(string sectionInfo) => Regex.Match(sectionInfo, @"(?<=\s)[A-Z][A-Z0-9]?$").Value.Trim();
    }
}
