/*
 * Copyright (C) 2011 The Libphonenumber Authors
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

/**
 * A helper class doing file handling and lookup of phone number prefix mappings.
 *
 * @author Shaopeng Jia
 */

using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PhoneNumbers
{
    public class PrefixFileReader
    {
        private readonly Assembly assembly;
        private readonly string phonePrefixDataDirectory;
        // The mappingFileProvider knows for which combination of countryCallingCode and language a phone
        // prefix mapping file is available in the file system, so that a file can be loaded when needed.
        private readonly MappingFileProvider mappingFileProvider = new MappingFileProvider();
        // A mapping from countryCallingCode_lang to the corresponding phone prefix map that has been
        // loaded.
        private readonly Dictionary<string, PhonePrefixMap> availablePhonePrefixMaps =
            new Dictionary<string, PhonePrefixMap>();

        public PrefixFileReader(Assembly assembly, string phonePrefixDataDirectory)
        {
            this.assembly = assembly;
            this.phonePrefixDataDirectory = phonePrefixDataDirectory;
            LoadMappingFileProvider();
        }

        private void LoadMappingFileProvider()
        {
            var source = assembly.GetManifestResourceStream(phonePrefixDataDirectory + "config");
            using (var input = new BinaryReader(source))
            {
                mappingFileProvider.ReadExternal(input);

            }
        }

        private PhonePrefixMap GetPhonePrefixDescriptions(
            int prefixMapKey, string language, string script, string region)
        {
            var fileName = mappingFileProvider.GetFileName(prefixMapKey, language, script, region);
            if (fileName.Length == 0)
            {
                return null;
            }
            if (!availablePhonePrefixMaps.ContainsKey(fileName))
            {
                LoadPhonePrefixMapFromFile(fileName);
            }
            return availablePhonePrefixMaps[fileName];
        }

        private void LoadPhonePrefixMapFromFile(string fileName)
        {
            var source = assembly.GetManifestResourceStream(phonePrefixDataDirectory + fileName);
            using (var input = new BinaryReader(source))
            {
                var map = new PhonePrefixMap();
                map.ReadExternal(input);
                availablePhonePrefixMaps.Add(fileName, map);
            }
        }

        /**
        * Returns a text description in the given language for the given phone number.
        *
        * @param number  the phone number for which we want to get a text description
        * @param language  two or three-letter lowercase ISO language codes as defined by ISO 639. Note
        *     that where two different language codes exist (e.g. 'he' and 'iw' for Hebrew) we use the
        *     one that Java/Android canonicalized on ('iw' in this case).
        * @param script  four-letter titlecase (the first letter is uppercase and the rest of the letters
        *     are lowercase) ISO script code as defined in ISO 15924
        * @param region  two-letter uppercase ISO country code as defined by ISO 3166-1
        * @return  a text description in the given language for the given phone number, or an empty
        *     string if a description is not available
        */
        public string GetDescriptionForNumber(
            PhoneNumber number, string language, string script, string region)
        {
            var countryCallingCode = number.CountryCode;
            // As the NANPA data is split into multiple files covering 3-digit areas, use a phone number
            // prefix of 4 digits for NANPA instead, e.g. 1650.
            var phonePrefix = countryCallingCode != 1
                ? countryCallingCode : 1000 + (int)(number.NationalNumber / 10000000);
            var phonePrefixDescriptions =
                GetPhonePrefixDescriptions(phonePrefix, language, script, region);
            var description = phonePrefixDescriptions?.Lookup(number);
            // When a location is not available in the requested language, fall back to English.
            if (string.IsNullOrEmpty(description) && MayFallBackToEnglish(language))
            {
                var defaultMap = GetPhonePrefixDescriptions(phonePrefix, "en", "", "");
                if (defaultMap == null)
                {
                    return "";
                }
                description = defaultMap.Lookup(number);
            }
            return description ?? "";
        }

        private static bool MayFallBackToEnglish(string lang)
        {
            // Don't fall back to English if the requested language is among the following:
            // - Chinese
            // - Japanese
            // - Korean
            return !lang.Equals("zh") && !lang.Equals("ja") && !lang.Equals("ko");
        }
    }
}
