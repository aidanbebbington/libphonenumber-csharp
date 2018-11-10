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

/**
 * A utility that maps phone number prefixes to a list of strings describing the time zones to
 * which each prefix belongs.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhoneNumbers
{
    public class PrefixTimeZonesMap
    {
        private readonly PhonePrefixMap phonePrefixMap = new PhonePrefixMap();
        private const char RAW_STRING_TIMEZONES_SEPARATOR = '&';

        /**
        * Creates a {@link PrefixTimeZonesMap} initialized with {@code sortedPrefixTimeZoneMap}.  Note
        * that the underlying implementation of this method is expensive thus should not be called by
        * time-critical applications.
        *
        * @param sortedPrefixTimeZoneMap  a map from phone number prefixes to their corresponding time
        * zones, sorted in ascending order of the phone number prefixes as integers.
        */
        public void ReadPrefixTimeZonesMap(SortedDictionary<int, string> sortedPrefixTimeZoneMap)
        {
            phonePrefixMap.ReadPhonePrefixMap(sortedPrefixTimeZoneMap);
        }

        /**
         * Supports Java Serialization.
         */
        public void WriteExternal(BinaryWriter objectOutput)
        {
            phonePrefixMap.WriteExternal(objectOutput);
        }

        public void ReadExternal(BinaryReader objectInput)
        {
            phonePrefixMap.ReadExternal(objectInput);
        }

        /**
         * Returns the list of time zones {@code key} corresponds to.
         *
         * <p>{@code key} could be the calling country code and the full significant number of a
         * certain number, or it could be just a phone-number prefix.
         * For example, the full number 16502530000 (from the phone-number +1 650 253 0000) is a valid
         * input. Also, any of its prefixes, such as 16502, is also valid.
         *
         * @param key  the key to look up
         * @return  the list of corresponding time zones
         */
        private List<string> LookupTimeZonesForNumber(long key)
        {
            // Lookup in the map data. The returned string may consist of several time zones, so it must be split.
            var timezonesString = phonePrefixMap.Lookup(key);
            return timezonesString != null ? TokenizeRawOutputString(timezonesString) : new List<string>();
        }

        /**
         * As per {@link #lookupTimeZonesForNumber(long)}, but receives the number as a PhoneNumber
         * instead of a long.
         *
         * @param number  the phone number to look up
         * @return  the list of corresponding time zones
         */
        public List<string> LookupTimeZonesForNumber(PhoneNumber number)
        {
            var phonePrefix = long.Parse(number.CountryCode
                                         + PhoneNumberUtil.GetInstance().GetNationalSignificantNumber(number));
            return LookupTimeZonesForNumber(phonePrefix);
        }

        /**
         * Returns the list of time zones {@code number}'s calling country code corresponds to.
         *
         * @param number  the phone number to look up
         * @return  the list of corresponding time zones
         */
        public List<string> LookupCountryLevelTimeZonesForNumber(PhoneNumber number)
        {
            return LookupTimeZonesForNumber(number.CountryCode);
        }

        /**
         * Split {@code timezonesString} into all the time zones that are part of it.
         */
        private List<string> TokenizeRawOutputString(string timezonesString)
        {
            return timezonesString.Split(RAW_STRING_TIMEZONES_SEPARATOR).ToList();
        }

        /**
         * Dumps the mappings contained in the phone prefix map.
         */
        public override string ToString()
        {
            return phonePrefixMap.ToString();
        }
    }
}
