/*
 * Copyright (C) 2012 The Libphonenumber Authors
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
#if (NET35 || NET40)
using System.Reflection;
#endif

namespace PhoneNumbers
{

    /**
     * An offline mapper from phone numbers to time zones.
     */
    public class PhoneNumberToTimeZonesMapper
    {
        private const string MAPPING_DATA_DIRECTORY =
            "/com/google/i18n/phonenumbers/timezones/data/";

        private const string MAPPING_DATA_FILE_NAME = "map_data";

        // This is defined by ICU as the unknown time zone.
        private const string UNKNOWN_TIMEZONE = "Etc/Unknown";

        // A list with the ICU unknown time zone as single element.
        // @VisibleForTesting
        internal static readonly List<string> UnknownTimeZoneList = new List<string>
        {
            UNKNOWN_TIMEZONE
        };

        private readonly PrefixTimeZonesMap prefixTimeZonesMap;

        public PhoneNumberToTimeZonesMapper(string prefixTimeZonesMapDataDirectory)
        {
            prefixTimeZonesMap = LoadPrefixTimeZonesMapFromFile(
                prefixTimeZonesMapDataDirectory + MAPPING_DATA_FILE_NAME);
        }

        private PhoneNumberToTimeZonesMapper(PrefixTimeZonesMap prefixTimeZonesMap)
        {
            this.prefixTimeZonesMap = prefixTimeZonesMap;
        }

        private static PrefixTimeZonesMap LoadPrefixTimeZonesMapFromFile(string path)
        {
#if (NET35 || NET40)
            var asm = Assembly.GetExecutingAssembly();
#else
            var asm = typeof(PhoneNumberUtil).GetTypeInfo().Assembly;
#endif
            var source = asm.GetManifestResourceStream(path);
            BinaryReader input = null;
            var map = new PrefixTimeZonesMap();
            try
            {
                input = new BinaryReader(source);
                map.ReadExternal(input);
            }
            catch (IOException)
            {
            }
            finally
            {
                Close(input);
            }

            return map;
        }

        private static void Close(BinaryReader input)
        {
            if (input != null)
            {
                try
                {
                    input.Close();
                }
                catch (IOException)
                {
                }
            }
        }

        /**
         * Helper class used for lazy instantiation of a PhoneNumberToTimeZonesMapper. This also loads the
         * map data in a thread-safe way.
         */
        private static class LazyHolder
        {
            internal static readonly PhoneNumberToTimeZonesMapper Instance =
                new PhoneNumberToTimeZonesMapper(
                    LoadPrefixTimeZonesMapFromFile(MAPPING_DATA_DIRECTORY + MAPPING_DATA_FILE_NAME));
        }


        /**
         * Gets a {@link PhoneNumberToTimeZonesMapper} instance.
         *
         * <p> The {@link PhoneNumberToTimeZonesMapper} is implemented as a singleton. Therefore, calling
         * this method multiple times will only result in one instance being created.
         *
         * @return  a {@link PhoneNumberToTimeZonesMapper} instance
         */
        public static PhoneNumberToTimeZonesMapper GetInstance()
        {
            lock (LazyHolder.Instance)
            {
                return LazyHolder.Instance;
            }
        }

        /**
         * Returns a list of time zones to which a phone number belongs.
         *
         * <p>This method assumes the validity of the number passed in has already been checked, and that
         * the number is geo-localizable. We consider fixed-line and mobile numbers possible candidates
         * for geo-localization.
         *
         * @param number  a valid phone number for which we want to get the time zones to which it belongs
         * @return  a list of the corresponding time zones or a single element list with the default
         *     unknown time zone if no other time zone was found or if the number was invalid
         */
        public List<string> GetTimeZonesForGeographicalNumber(PhoneNumber number)
        {
            return GetTimeZonesForGeocodableNumber(number);
        }

        /**
         * As per {@link #getTimeZonesForGeographicalNumber(PhoneNumber)} but explicitly checks
         * the validity of the number passed in.
         *
         * @param number  the phone number for which we want to get the time zones to which it belongs
         * @return  a list of the corresponding time zones or a single element list with the default
         *     unknown time zone if no other time zone was found or if the number was invalid
         */
        public List<string> GetTimeZonesForNumber(PhoneNumber number)
        {
            var numberType = PhoneNumberUtil.GetInstance().GetNumberType(number);
            if (numberType == PhoneNumberType.UNKNOWN)
            {
                return UnknownTimeZoneList;
            }
            if (!PhoneNumberUtil.GetInstance().IsNumberGeographical(
                numberType, number.CountryCode))
            {
                return GetCountryLevelTimeZonesforNumber(number);
            }

            return GetTimeZonesForGeographicalNumber(number);
        }

        /**
         * Returns a string with the ICU unknown time zone.
         */
        public static string GetUnknownTimeZone()
        {
            return UNKNOWN_TIMEZONE;
        }

        /**
         * Returns a list of time zones to which a geocodable phone number belongs.
         *
         * @param number  the phone number for which we want to get the time zones to which it belongs
         * @return  the list of corresponding  time zones or a single element list with the default
         *     unknown time zone if no other time zone was found or if the number was invalid
         */
        private List<string> GetTimeZonesForGeocodableNumber(PhoneNumber number)
        {
            var timezones = prefixTimeZonesMap.LookupTimeZonesForNumber(number);
            return !timezones.Any() ? UnknownTimeZoneList : timezones;
        }

        /**
         * Returns the list of time zones corresponding to the country calling code of {@code number}.
         *
         * @param number  the phone number to look up
         * @return  the list of corresponding time zones or a single element list with the default
         *     unknown time zone if no other time zone was found
         */
        private List<string> GetCountryLevelTimeZonesforNumber(PhoneNumber number)
        {
            var timezones = prefixTimeZonesMap.LookupCountryLevelTimeZonesForNumber(number);
            return !timezones.Any() ? UnknownTimeZoneList : timezones;
        }

    }
}
