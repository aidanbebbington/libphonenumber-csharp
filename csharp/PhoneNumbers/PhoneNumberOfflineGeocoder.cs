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
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace PhoneNumbers
{

    public class Locale
    {
        public static readonly Locale English = new("en", "GB");
        public static readonly Locale French = new("fr", "FR");
        public static readonly Locale German = new("de", "DE");
        public static readonly Locale Italian = new("it", "IT");
        public static readonly Locale Korean = new("ko", "KR");
        public static readonly Locale SimplifiedChinese = new("zh", "CN");

        public readonly string Language;
        public readonly string Country;

        public Locale(string language, string countryCode)
        {
            Language = language;
            Country = countryCode;
        }

        public string GetDisplayCountry(string language)
        {
            if(string.IsNullOrEmpty(Country))
                return "";
            var name = GetCountryName(Country, language);
            if(name != null)
                return name;
            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            if(lang != language)
            {
                name = GetCountryName(Country, lang);
                if(name != null)
                    return name;
            }
            if(language != "en" && lang != "en")
            {
                name = GetCountryName(Country, "en");
                if(name != null)
                    return name;
            }
            name = GetCountryName(Country, Language);
            return name ?? "";
        }

        private static string GetCountryName(string country, string language)
        {
            var names = LocaleData.Data[country];
            if (!names.TryGetValue(language, out string name))
                return null;
            if (name.Length > 0 && name[0] == '*')
                return names[name.Substring(1)];
            return name;
        }
    }
    /**
     * An offline geocoder which provides geographical information related to a phone number.
     *
     * @author Shaopeng Jia
     */
    public class PhoneNumberOfflineGeocoder
    {
        private static PhoneNumberOfflineGeocoder instance;
        private const string MAPPING_DATA_DIRECTORY = "geocoding.";
        private static readonly object ThisLock = new();

        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
        private readonly string phonePrefixDataDirectory;
        private readonly Assembly assembly;

        // The mappingFileProvider knows for which combination of countryCallingCode and language a phone
        // prefix mapping file is available in the file system, so that a file can be loaded when needed.
        private readonly MappingFileProvider mappingFileProvider;

        // A mapping from countryCallingCode_lang to the corresponding phone prefix map that has been
        // loaded.
        private readonly Dictionary<string, AreaCodeMap> availablePhonePrefixMaps = new();

        // @VisibleForTesting
        public PhoneNumberOfflineGeocoder(string phonePrefixDataDirectory, Assembly asm = null)
        {
            var files = new SortedDictionary<int, HashSet<string>>();
#if NETSTANDARD1_3 || PORTABLE
            asm ??= typeof(PhoneNumberOfflineGeocoder).GetTypeInfo().Assembly;
#else
            asm ??= typeof(PhoneNumberOfflineGeocoder).Assembly;
#endif
            var allNames = asm.GetManifestResourceNames();
            var prefix = asm.GetName().Name + "." + phonePrefixDataDirectory;
            var names = allNames.Where(n => n.StartsWith(prefix, StringComparison.Ordinal));
            foreach (var n in names)
            {
                var name = n.Substring(prefix.Length).Split('.');
                int country;
                try
                {
                    country = int.Parse(name[1], CultureInfo.InvariantCulture);
                }
                catch(FormatException)
                {
                    throw new Exception("Failed to parse geocoding file name: " + name);
                }
                var language = name[0];
                if (!files.TryGetValue(country, out var languages))
                    files[country] = languages = new HashSet<string>();
                languages.Add(language);
            }
            mappingFileProvider = new();
            mappingFileProvider.ReadFileConfigs(files);
            this.assembly = asm;
            this.phonePrefixDataDirectory = prefix;
        }

        private AreaCodeMap GetPhonePrefixDescriptions(
            int prefixMapKey, string language, string script, string region)
        {
            var fileName = mappingFileProvider.GetFileName(prefixMapKey, language, script, region);
            if (fileName.Length == 0)
            {
                return null;
            }
            lock (availablePhonePrefixMaps)
            {
                if (!availablePhonePrefixMaps.TryGetValue(fileName, out var map))
                    map = LoadAreaCodeMapFromFile(fileName);
                return map;
            }
        }

        private AreaCodeMap LoadAreaCodeMapFromFile(string fileName)
        {
            var resName = phonePrefixDataDirectory + fileName;
            using (var fp = assembly.GetManifestResourceStream(resName))
            {
                var areaCodeMap = AreaCodeParser.ParseAreaCodeMap(fp);
                return availablePhonePrefixMaps[fileName] = areaCodeMap;
            }
        }

        /**
         * Gets a {@link PhoneNumberOfflineGeocoder} instance to carry out international phone number
         * geocoding.
         *
         * <p /> The {@link PhoneNumberOfflineGeocoder} is implemented as a singleton. Therefore, calling
         * this method multiple times will only result in one instance being created.
         *
         * @return  a {@link PhoneNumberOfflineGeocoder} instance
         */
        public static PhoneNumberOfflineGeocoder GetInstance()
        {
            lock (ThisLock)
            {
                return instance ?? (instance = new PhoneNumberOfflineGeocoder(MAPPING_DATA_DIRECTORY));
            }
        }

        /**
         * Preload the data file for the given language and country calling code, so that a future lookup
         * for this language and country calling code will not incur any file loading.
         *
         * @param locale  specifies the language of the data file to load
         * @param countryCallingCode   specifies the country calling code of phone numbers that are
         *     contained by the file to be loaded
         */
        public void LoadDataFile(Locale locale, int countryCallingCode)
        {
            instance.GetPhonePrefixDescriptions(countryCallingCode, locale.Language, "",
                locale.Country);
        }

        /**
         * Returns the customary display name in the given language for the given territory the phone
         * number is from.
         */
        private string GetCountryNameForNumber(PhoneNumber number, Locale language)
        {
            var regionCode = phoneUtil.GetRegionCodeForNumber(number);
            return GetRegionDisplayName(regionCode, language);
        }

        /**
        * Returns the customary display name in the given language for the given region.
        */
        private static string GetRegionDisplayName(string regionCode, Locale language)
        {
            return regionCode == null || regionCode.Equals("ZZ") ||
                   regionCode.Equals(PhoneNumberUtil.REGION_CODE_FOR_NON_GEO_ENTITY)
                ? "" : new Locale("", regionCode).GetDisplayCountry(language.Language);
        }

        /**
        * Returns a text description for the given phone number, in the language provided. The
        * description might consist of the name of the country where the phone number is from, or the
        * name of the geographical area the phone number is from if more detailed information is
        * available.
        *
        * <p />This method assumes the validity of the number passed in has already been checked.
        *
        * @param number  a valid phone number for which we want to get a text description
        * @param languageCode  the language code for which the description should be written
        * @return  a text description for the given language code for the given phone number
        */
        public string GetDescriptionForValidNumber(PhoneNumber number, Locale languageCode)
        {
            var langStr = languageCode.Language;
            var scriptStr = "";  // No script is specified
            var regionStr = languageCode.Country;

            var areaDescription =
                GetAreaDescriptionForNumber(number, langStr, scriptStr, regionStr);
            return (areaDescription.Length > 0)
                ? areaDescription : GetCountryNameForNumber(number, languageCode);
        }

        /**
        * As per {@link #getDescriptionForValidNumber(PhoneNumber, Locale)} but also considers the
        * region of the user. If the phone number is from the same region as the user, only a lower-level
        * description will be returned, if one exists. Otherwise, the phone number's region will be
        * returned, with optionally some more detailed information.
        *
        * <p>For example, for a user from the region "US" (United States), we would show "Mountain View,
        * CA" for a particular number, omitting the United States from the description. For a user from
        * the United Kingdom (region "GB"), for the same number we may show "Mountain View, CA, United
        * States" or even just "United States".</p>
        *
        * This method assumes the validity of the number passed in has already been checked.
        *
        * @param number  the phone number for which we want to get a text description
        * @param languageCode  the language code for which the description should be written
        * @param userRegion  the region code for a given user. This region will be omitted from the
        *     description if the phone number comes from this region. It is a two-letter uppercase ISO
        *     country code as defined by ISO 3166-1.
        * @return  a text description for the given language code for the given phone number, or empty
        *     string if the number passed in is invalid
        */
        public string GetDescriptionForValidNumber(PhoneNumber number, Locale languageCode,
            string userRegion)
        {
            // If the user region matches the number's region, then we just show the lower-level
            // description, if one exists - if no description exists, we will show the region(country) name
            // for the number.
            var regionCode = phoneUtil.GetRegionCodeForNumber(number);
            if (userRegion.Equals(regionCode))
            {
                return GetDescriptionForValidNumber(number, languageCode);
            }
            // Otherwise, we just show the region(country) name for now.
            return GetRegionDisplayName(regionCode, languageCode);
            // TODO: Concatenate the lower-level and country-name information in an appropriate
            // way for each language.
        }

        /**
        * As per {@link #getDescriptionForValidNumber(PhoneNumber, Locale)} but explicitly checks
        * the validity of the number passed in.
        *
        * @param number  the phone number for which we want to get a text description
        * @param languageCode  the language code for which the description should be written
        * @return  a text description for the given language code for the given phone number, or empty
        *     string if the number passed in is invalid
        */
        public string GetDescriptionForNumber(PhoneNumber number, Locale languageCode)
        {
            return !phoneUtil.IsValidNumber(number) ? "" : GetDescriptionForValidNumber(number, languageCode);
        }

        /**
        * As per {@link #getDescriptionForValidNumber(PhoneNumber, Locale, String)} but
        * explicitly checks the validity of the number passed in.
        *
        * @param number  the phone number for which we want to get a text description
        * @param languageCode  the language code for which the description should be written
        * @param userRegion  the region code for a given user. This region will be omitted from the
        *     description if the phone number comes from this region. It is a two-letter uppercase ISO
        *     country code as defined by ISO 3166-1.
        * @return  a text description for the given language code for the given phone number, or empty
        *     string if the number passed in is invalid
        */
        public string GetDescriptionForNumber(PhoneNumber number, Locale languageCode,
            string userRegion)
        {
            return !phoneUtil.IsValidNumber(number)
                ? ""
                : GetDescriptionForValidNumber(number, languageCode, userRegion);
        }

        private string GetAreaDescriptionForNumber(
            PhoneNumber number, string lang, string script, string region)
        {
            var countryCallingCode = number.CountryCode;
            // As the NANPA data is split into multiple files covering 3-digit areas, use a phone number
            // prefix of 4 digits for NANPA instead, e.g. 1650.
            //int phonePrefix = (countryCallingCode != 1) ?
            //    countryCallingCode : (1000 + (int) (number.NationalNumber / 10000000));
            var phonePrefix = countryCallingCode;

            var phonePrefixDescriptions =
                GetPhonePrefixDescriptions(phonePrefix, lang, script, region);
            var description = phonePrefixDescriptions?.Lookup(number);
            // When a location is not available in the requested language, fall back to English.
            if (string.IsNullOrEmpty(description) && MayFallBackToEnglish(lang))
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
