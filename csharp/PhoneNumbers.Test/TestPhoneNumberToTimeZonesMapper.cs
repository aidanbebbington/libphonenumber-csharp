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
using System.Linq;
using Xunit;

namespace PhoneNumbers.Test
{

/**
 * Unit tests for PhoneNumberToTimeZonesMapper.java
 *
 * @author Walter Erquinigo
 */
    public class PhoneNumberToTimeZonesMapperTest
    {
        private readonly PhoneNumberToTimeZonesMapper prefixTimeZonesMapper =
            new PhoneNumberToTimeZonesMapper(TEST_MAPPING_DATA_DIRECTORY);

        private const string TEST_MAPPING_DATA_DIRECTORY =
            "/com/google/i18n/phonenumbers/timezones/testing_data/";

        // Set up some test numbers to re-use.
        private static readonly PhoneNumber AUNumber =
            new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(236618300L).Build();

        private static readonly PhoneNumber CaNumber =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6048406565L).Build();

        private static readonly PhoneNumber KoNumber =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(22123456L).Build();

        private static readonly PhoneNumber KoInvalidNumber =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(1234L).Build();

        private static readonly PhoneNumber USNumber1 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6509600000L).Build();

        private static readonly PhoneNumber USNumber2 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2128120000L).Build();

        private static readonly PhoneNumber USNumber3 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6174240000L).Build();

        private static readonly PhoneNumber USInvalidNumber =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(123456789L).Build();

        private static readonly PhoneNumber NumberWithInvalidCountryCode =
            new PhoneNumber.Builder().SetCountryCode(999).SetNationalNumber(2423651234L).Build();

        private static readonly PhoneNumber InternationalTollFree =
            new PhoneNumber.Builder().SetCountryCode(800).SetNationalNumber(12345678L).Build();

// NANPA time zones.
        private static readonly string ChicagoTz = "America/Chicago";
        private static readonly string LosAngelesTz = "America/Los_Angeles";
        private static readonly string NewYorkTz = "America/New_York";

        private static readonly string WinnipegTz = "America/Winnipeg";

        // Non NANPA time zones.
        private static readonly string SeoulTz = "Asia/Seoul";
        private static readonly string SydneyTz = "Australia/Sydney";

        static List<string> BuildListOfTimeZones(params string[] timezones)
        {
            return timezones.ToList();
        }

        private static List<string> GetNanpaTimeZonesList()
        {
            return BuildListOfTimeZones(NewYorkTz, ChicagoTz, WinnipegTz, LosAngelesTz);
        }

        [Fact]
        public void TestGetTimeZonesForNumber()
        {
            // Test with invalid numbers even when their country code prefixes exist in the mapper.
            Assert.Equal(PhoneNumberToTimeZonesMapper.UnknownTimeZoneList,
                prefixTimeZonesMapper.GetTimeZonesForNumber(USInvalidNumber));
            Assert.Equal(PhoneNumberToTimeZonesMapper.UnknownTimeZoneList,
                prefixTimeZonesMapper.GetTimeZonesForNumber(KoInvalidNumber));
            // Test with valid prefixes.
            Assert.Equal(BuildListOfTimeZones(SydneyTz),
                prefixTimeZonesMapper.GetTimeZonesForNumber(AUNumber));
            Assert.Equal(BuildListOfTimeZones(SeoulTz),
                prefixTimeZonesMapper.GetTimeZonesForNumber(KoNumber));
            Assert.Equal(BuildListOfTimeZones(WinnipegTz),
                prefixTimeZonesMapper.GetTimeZonesForNumber(CaNumber));
            Assert.Equal(BuildListOfTimeZones(LosAngelesTz),
                prefixTimeZonesMapper.GetTimeZonesForNumber(USNumber1));
            Assert.Equal(BuildListOfTimeZones(NewYorkTz),
                prefixTimeZonesMapper.GetTimeZonesForNumber(USNumber2));
            // Test with an invalid country code.
            Assert.Equal(PhoneNumberToTimeZonesMapper.UnknownTimeZoneList,
                prefixTimeZonesMapper.GetTimeZonesForNumber(NumberWithInvalidCountryCode));
            // Test with a non geographical phone number.
            Assert.Equal(PhoneNumberToTimeZonesMapper.UnknownTimeZoneList,
                prefixTimeZonesMapper.GetTimeZonesForNumber(InternationalTollFree));
        }

        [Fact]
        public void TestGetTimeZonesForValidNumber()
        {
            // Test with invalid numbers even when their country code prefixes exist in the mapper.
            Assert.Equal(GetNanpaTimeZonesList(),
                prefixTimeZonesMapper.GetTimeZonesForGeographicalNumber(USInvalidNumber));
            Assert.Equal(BuildListOfTimeZones(SeoulTz),
                prefixTimeZonesMapper.GetTimeZonesForGeographicalNumber(KoInvalidNumber));
            // Test with valid prefixes.
            Assert.Equal(BuildListOfTimeZones(SydneyTz),
                prefixTimeZonesMapper.GetTimeZonesForGeographicalNumber(AUNumber));
            Assert.Equal(BuildListOfTimeZones(SeoulTz),
                prefixTimeZonesMapper.GetTimeZonesForGeographicalNumber(KoNumber));
            Assert.Equal(BuildListOfTimeZones(WinnipegTz),
                prefixTimeZonesMapper.GetTimeZonesForGeographicalNumber(CaNumber));
            Assert.Equal(BuildListOfTimeZones(LosAngelesTz),
                prefixTimeZonesMapper.GetTimeZonesForGeographicalNumber(USNumber1));
            Assert.Equal(BuildListOfTimeZones(NewYorkTz),
                prefixTimeZonesMapper.GetTimeZonesForGeographicalNumber(USNumber2));
            // Test with an invalid country code.
            Assert.Equal(PhoneNumberToTimeZonesMapper.UnknownTimeZoneList,
                prefixTimeZonesMapper.GetTimeZonesForGeographicalNumber(
                    NumberWithInvalidCountryCode));
            // Test with a non geographical phone number.
            Assert.Equal(PhoneNumberToTimeZonesMapper.UnknownTimeZoneList,
                prefixTimeZonesMapper.GetTimeZonesForGeographicalNumber(
                    InternationalTollFree));
        }

        [Fact]
        public void TestGetTimeZonesForValidNumberSearchingAtCountryCodeLevel()
        {
            // Test that the country level time zones are returned when the number passed in is valid but
            // not covered by any non-country level prefixes in the mapper.
            Assert.Equal(prefixTimeZonesMapper.GetTimeZonesForNumber(USNumber3),
                GetNanpaTimeZonesList());
        }
    }
}
