/*
 * Copyright (C) 2011 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PhoneNumbers
{
    /**
    * A utility which knows the data files that are available for the geocoder to use. The data files
    * contain mappings from phone number prefixes to text descriptions, and are organized by country
    * calling code and language that the text descriptions are in.
    *
    * @author Shaopeng Jia
    */
    public class MappingFileProvider
    {
        private int numOfEntries;
        private int[] countryCallingCodes;
        private List<HashSet<string>> availableLanguages;
        private static readonly Dictionary<string, string> LocaleNormalizationMap;

        static MappingFileProvider()
        {
            var normalizationMap = new Dictionary<string, string>
            {
                ["zh_TW"] = "zh_Hant",
                ["zh_HK"] = "zh_Hant",
                ["zh_MO"] = "zh_Hant"
            };
            LocaleNormalizationMap = normalizationMap;
        }

        /**
        * Creates an empty {@link MappingFileProvider}. The default constructor is necessary for
        * implementing {@link Externalizable}. The empty provider could later be populated by
        * {@link #readFileConfigs(java.util.SortedMap)} or {@link #readExternal(java.io.ObjectInput)}.
        */

        /**
         * Initializes an {@link MappingFileProvider} with {@code availableDataFiles}.
         *
         * @param availableDataFiles  a map from country calling codes to sets of languages in which data
         *     files are available for the specific country calling code. The map is sorted in ascending
         *     order of the country calling codes as integers.
         */
        public void ReadFileConfigs(SortedDictionary<int, HashSet<string>> availableDataFiles)
        {
            numOfEntries = availableDataFiles.Count;
            countryCallingCodes = new int[numOfEntries];
            availableLanguages = new List<HashSet<string>>(numOfEntries);
            var index = 0;
            foreach (var countryCallingCode in availableDataFiles.Keys)
            {
                countryCallingCodes[index++] = countryCallingCode;
                availableLanguages.Add(new HashSet<string>(availableDataFiles[countryCallingCode]));
            }
        }

        public void ReadExternal(BinaryReader objectInput)
        {
            numOfEntries = objectInput.ReadInt32();
            if (countryCallingCodes == null || countryCallingCodes.Length < numOfEntries)
            {
                countryCallingCodes = new int[numOfEntries];
            }
            if (availableLanguages == null)
            {
                availableLanguages = new List<HashSet<string>>();
            }
            for (var i = 0; i < numOfEntries; i++)
            {
                countryCallingCodes[i] = objectInput.ReadInt32();
                var numOfLangs = objectInput.ReadInt32();
                var setOfLangs = new HashSet<string>();
                for (var j = 0; j < numOfLangs; j++)
                {
                    setOfLangs.Add(objectInput.ReadString());
                }
                availableLanguages.Add(setOfLangs);
            }
        }

        public void WriteExternal(BinaryWriter objectOutput)
        {
            objectOutput.Write(numOfEntries);
            for (var i = 0; i < numOfEntries; i++)
            {
                objectOutput.Write(countryCallingCodes[i]);
                var setOfLangs = availableLanguages[i];
                var numOfLangs = setOfLangs.Count;
                objectOutput.Write(numOfLangs);
                foreach (var lang in setOfLangs)
                {
                    objectOutput.Write(lang);
                }
            }
        }


        /**
         * Returns a string representing the data in this class. The string contains one line for each
         * country calling code. The country calling code is followed by a '|' and then a list of
         * comma-separated languages sorted in ascending order.
         */
        public override string ToString()
        {
            var output = new StringBuilder();
            for (var i = 0; i < numOfEntries; i++)
            {
                output.Append(countryCallingCodes[i]);
                output.Append('|');
                foreach (var lang in availableLanguages[i].OrderBy(a => a))
                {
                    output.Append(lang);
                    output.Append(',');
                }
                output.Append('\n');
            }
            return output.ToString();
        }

        /**
         * Gets the name of the file that contains the mapping data for the {@code countryCallingCode} in
         * the language specified.
         *
         * @param countryCallingCode  the country calling code of phone numbers which the data file
         *     contains
         * @param language  two-letter lowercase ISO language codes as defined by ISO 639-1
         * @param script  four-letter titlecase (the first letter is uppercase and the rest of the letters
         *     are lowercase) ISO script codes as defined in ISO 15924
         * @param region  two-letter uppercase ISO country codes as defined by ISO 3166-1
         * @return  the name of the file, or empty string if no such file can be found
         */
        public string GetFileName(int countryCallingCode, string language, string script, string region)
        {
            if (language.Length == 0)
            {
                return "";
            }
            var index = Array.BinarySearch(countryCallingCodes, countryCallingCode);
            if (index < 0)
            {
                return "";
            }
            var setOfLangs = availableLanguages[index];
            if (setOfLangs.Count > 0)
            {
                var languageCode = FindBestMatchingLanguageCode(setOfLangs, language, script, region);
                if (languageCode.Length > 0)
                {
                    var fileName = new StringBuilder();
                    fileName.Append(countryCallingCode).Append('_').Append(languageCode);
                    return fileName.ToString();
                }
            }
            return "";
        }

        private string FindBestMatchingLanguageCode(
            ICollection<string> setOfLangs, string language, string script, string region)
        {
            var fullLocale = ConstructFullLocale(language, script, region);
            var fullLocaleStr = fullLocale.ToString();
            if (LocaleNormalizationMap.TryGetValue(fullLocaleStr, out var normalizedLocale))
            {
                if (setOfLangs.Contains(normalizedLocale))
                {
                    return normalizedLocale;
                }
            }
            if (setOfLangs.Contains(fullLocaleStr))
            {
                return fullLocaleStr;
            }

            if (OnlyOneOfScriptOrRegionIsEmpty(script, region))
            {
                if (setOfLangs.Contains(language))
                {
                    return language;
                }
            }
            else if (script.Length > 0 && region.Length > 0)
            {
                var langWithScript = new StringBuilder(language).Append('_').Append(script);
                var langWithScriptStr = langWithScript.ToString();
                if (setOfLangs.Contains(langWithScriptStr))
                {
                    return langWithScriptStr;
                }

                var langWithRegion = new StringBuilder(language).Append('_').Append(region);
                var langWithRegionStr = langWithRegion.ToString();
                if (setOfLangs.Contains(langWithRegionStr))
                {
                    return langWithRegionStr;
                }

                if (setOfLangs.Contains(language))
                {
                    return language;
                }
            }
            return "";
        }

        private static bool OnlyOneOfScriptOrRegionIsEmpty(string script, string region)
        {
            return script.Length == 0 && region.Length > 0 || region.Length == 0 && script.Length > 0;
        }

        private StringBuilder ConstructFullLocale(string language, string script, string region)
        {
            var fullLocale = new StringBuilder(language);
            AppendSubsequentLocalePart(script, fullLocale);
            AppendSubsequentLocalePart(region, fullLocale);
            return fullLocale;
        }

        private static void AppendSubsequentLocalePart(string subsequentLocalePart, StringBuilder fullLocale)
        {
            if (subsequentLocalePart.Length > 0)
            {
                fullLocale.Append('_').Append(subsequentLocalePart);
            }
        }
    }
}