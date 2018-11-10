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
        public static readonly Locale English = new Locale("en", "GB");
        public static readonly Locale French = new Locale("fr", "FR");
        public static readonly Locale German = new Locale("de", "DE");
        public static readonly Locale Italian = new Locale("it", "IT");
        public static readonly Locale Korean = new Locale("ko", "KR");
        public static readonly Locale SimplifiedChinese = new Locale("zh", "CN");

        public readonly string Language;
        public readonly string Country;

        public Locale(string language, string countryCode)
        {
            Language = language;
            Country = countryCode;
        }

        public string GetDisplayCountry(Locale local)
        {
            if (string.IsNullOrEmpty(Country))
                return "";
            var name = GetCountryName(Country, local.Language);
            if (name != null)
                return name;
            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            if (lang != local.Language)
            {
                name = GetCountryName(Country, lang);
                if (name != null)
                    return name;
            }
            if (local.Language != "en" && lang != "en")
            {
                name = GetCountryName(Country, "en");
                if (name != null)
                    return name;
            }
            name = GetCountryName(Country, Language);
            return name ?? "";
        }

        private static string GetCountryName(string country, string language)
        {
            var names = LocaleData.Data[country];
            if (!names.TryGetValue(language, out var name))
                return null;
            if (name.Length > 0 && name[0] == '*')
                return names[name.Substring(1)];
            return name;
        }
    }

    public class PhoneNumberOfflineGeocoder
    {
        private static PhoneNumberOfflineGeocoder instance;
        private const string phonePrefixDataDirectory = "resources.geocoding";
        private readonly Assembly assembly;
        private readonly PrefixFileReader prefixFileReader;
        private static readonly object ThisLock = new object();

        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();

        public PhoneNumberOfflineGeocoder(Assembly assembly)
        {
            prefixFileReader = new PrefixFileReader(assembly, phonePrefixDataDirectory);
        }

        /**
         * Gets a {@link PhoneNumberOfflineGeocoder} instance to carry out international phone number
         * geocoding.
         *
         * <p> The {@link PhoneNumberOfflineGeocoder} is implemented as a singleton. Therefore, calling
         * this method multiple times will only result in one instance being created.
         *
         * @return  a {@link PhoneNumberOfflineGeocoder} instance
         */
        public static PhoneNumberOfflineGeocoder GetInstance()
        {
            lock (ThisLock)
            {
                return instance ?? (instance = new PhoneNumberOfflineGeocoder(
#if (NET35 || NET40)
                    Assembly.GetExecutingAssembly()));
#else
                    typeof(PhoneNumberOfflineGeocoder).GetTypeInfo().Assembly));
#endif

            }
        }

        /**
         * Returns the customary display name in the given language for the given territory the phone
         * number is from. If it could be from many territories, nothing is returned.
         */
        private string GetCountryNameForNumber(PhoneNumber number, Locale language)
        {
            var regionCodes =
                phoneUtil.GetRegionCodesForCountryCode(number.CountryCode);
            if (regionCodes.Count == 1)
            {
                return GetRegionDisplayName(regionCodes[0], language);
            }
            var regionWhereNumberIsValid = "ZZ";
            foreach (var regionCode in regionCodes)
            {
                if (phoneUtil.IsValidNumberForRegion(number, regionCode))
                {
                    // If the number has already been found valid for one region, then we don't know which
                    // region it belongs to so we return nothing.
                    if (!regionWhereNumberIsValid.Equals("ZZ"))
                    {
                        return "";
                    }
                    regionWhereNumberIsValid = regionCode;
                }
            }
            return GetRegionDisplayName(regionWhereNumberIsValid, language);
        }

        private string GetRegionDisplayName(string regionCode, Locale language)
        {
            return regionCode == null || regionCode.Equals("ZZ") ||
                   regionCode.Equals(PhoneNumberUtil.RegionCodeForNonGeoEntity)
                ? "" : new Locale("", regionCode).GetDisplayCountry(language);
        }

        /**
         * Returns a text description for the given phone number, in the language provided. The
         * description might consist of the name of the country where the phone number is from, or the
         * name of the geographical area the phone number is from if more detailed information is
         * available.
         *
         * <p>This method assumes the validity of the number passed in has already been checked, and that
         * the number is suitable for geocoding. We consider fixed-line and mobile numbers possible
         * candidates for geocoding.
         *
         * @param number  a valid phone number for which we want to get a text description
         * @param languageCode  the language code for which the description should be written
         * @return  a text description for the given language code for the given phone number, or an
         *     empty string if the number could come from multiple countries, or the country code is
         *     in fact invalid
         */
        public string GetDescriptionForValidNumber(PhoneNumber number, Locale languageCode)
        {
            var langStr = languageCode.Language;
            var scriptStr = "";  // No script is specified
            var regionStr = languageCode.Country;

            string areaDescription;
            var mobileToken = PhoneNumberUtil.GetCountryMobileToken(number.CountryCode);
            var nationalNumber = phoneUtil.GetNationalSignificantNumber(number);
            if (!mobileToken.Equals("") && nationalNumber.StartsWith(mobileToken))
            {
                // In some countries, eg. Argentina, mobile numbers have a mobile token before the national
                // destination code, this should be removed before geocoding.
                nationalNumber = nationalNumber.Substring(mobileToken.Length);
                var region = phoneUtil.GetRegionCodeForCountryCode(number.CountryCode);
                PhoneNumber copiedNumber;
                try
                {
                    copiedNumber = phoneUtil.Parse(nationalNumber, region);
                }
                catch (NumberParseException)
                {
                    // If this happens, just reuse what we had.
                    copiedNumber = number;
                }
                areaDescription = prefixFileReader.GetDescriptionForNumber(copiedNumber, langStr, scriptStr,
                    regionStr);
            }
            else
            {
                areaDescription = prefixFileReader.GetDescriptionForNumber(number, langStr, scriptStr,
                    regionStr);
            }
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
         * States" or even just "United States".
         *
         * <p>This method assumes the validity of the number passed in has already been checked.
         *
         * @param number  the phone number for which we want to get a text description
         * @param languageCode  the language code for which the description should be written
         * @param userRegion  the region code for a given user. This region will be omitted from the
         *     description if the phone number comes from this region. It should be a two-letter
         *     upper-case CLDR region code.
         * @return  a text description for the given language code for the given phone number, or an
         *     empty string if the number could come from multiple countries, or the country code is
         *     in fact invalid
         */
        public string GetDescriptionForValidNumber(PhoneNumber number, Locale languageCode, string userRegion)
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
        *     string if the number passed in is invalid or could belong to multiple countries
        */
        public string GetDescriptionForNumber(PhoneNumber number, Locale languageCode)
        {
            var numberType = phoneUtil.GetNumberType(number);
            if (numberType == PhoneNumberType.UNKNOWN)
            {
                return "";
            }
            if (!phoneUtil.IsNumberGeographical(numberType, number.CountryCode))
            {
                return GetCountryNameForNumber(number, languageCode);
            }
            return GetDescriptionForValidNumber(number, languageCode);
        }

        /**
        * As per {@link #getDescriptionForValidNumber(PhoneNumber, Locale, String)} but
        * explicitly checks the validity of the number passed in.
        *
        * @param number  the phone number for which we want to get a text description
        * @param languageCode  the language code for which the description should be written
        * @param userRegion  the region code for a given user. This region will be omitted from the
        *     description if the phone number comes from this region. It should be a two-letter
        *     upper-case CLDR region code.
        *     country code as defined by ISO 3166-1.
        * @return  a text description for the given language code for the given phone number, or empty
        *     string if the number passed in is invalid or could belong to multiple countries
        */
        public string GetDescriptionForNumber(PhoneNumber number, Locale languageCode, string userRegion)
        {
            var numberType = phoneUtil.GetNumberType(number);
            if (numberType == PhoneNumberType.UNKNOWN)
            {
                return "";
            }
            if (!phoneUtil.IsNumberGeographical(numberType, number.CountryCode))
            {
                return GetCountryNameForNumber(number, languageCode);
            }
            return GetDescriptionForNumber(number, languageCode, userRegion);
        }
    }
}