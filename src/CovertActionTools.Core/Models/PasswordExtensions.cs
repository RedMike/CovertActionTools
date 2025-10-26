using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CovertActionTools.Core.Models
{
    public static class PasswordExtensions
    {
        private static readonly Regex InvalidCharRegex = new Regex("[^A-Za-z ]+");
        
        /// <summary>
        /// All words from Texts entries that are the correct character count, and only contain valid characters (A-Za-z)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="minCharCount"></param>
        /// <param name="maxCharCount"></param>
        /// <returns></returns>
        public static HashSet<string> GetLegacyPotentialPasswords(this PackageModel model, int minCharCount, int maxCharCount)
        {
            return new HashSet<string>(model.Texts
                .Values.SelectMany(x =>
                    InvalidCharRegex.Replace(x.Message, " ")
                        .Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                        .Distinct()
                        .Where(word => word.Length >= minCharCount && word.Length <= maxCharCount)
                )
            );
        }
    }
}