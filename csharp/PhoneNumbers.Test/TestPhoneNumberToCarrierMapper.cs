/*
 * Copyright (C) 2013 The Libphonenumber Authors
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

using Xunit;

namespace PhoneNumbers.Test
{

/**
 * Unit tests for PhoneNumberToCarrierMapper.java
 *
 * @author Cecilia Roes
 */
    public class PhoneNumberToCarrierMapperTest
    {
        private readonly PhoneNumberToCarrierMapper carrierMapper =
            new PhoneNumberToCarrierMapper(TEST_MAPPING_DATA_DIRECTORY);

        private const string TEST_MAPPING_DATA_DIRECTORY =
            "/com/google/i18n/phonenumbers/carrier/testing_data/";

        // Set up some test numbers to re-use.
        private static readonly PhoneNumber AoMobile1 =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(917654321L).Build();

        private static readonly PhoneNumber AoMobile2 =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(927654321L).Build();

        private static readonly PhoneNumber AoFixed1 =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(22254321L).Build();

        private static readonly PhoneNumber AoFixed2 =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(26254321L).Build();

        private static readonly PhoneNumber AoInvalidNumber =
            new PhoneNumber.Builder().SetCountryCode(244).SetNationalNumber(101234L).Build();

        private static readonly PhoneNumber UkMobile1 =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7387654321L).Build();

        private static readonly PhoneNumber UkMobile2 =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7487654321L).Build();

        private static readonly PhoneNumber UkFixed1 =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(1123456789L).Build();

        private static readonly PhoneNumber UkFixed2 =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(2987654321L).Build();

        private static readonly PhoneNumber UkInvalidNumber =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7301234L).Build();

        private static readonly PhoneNumber UkPager =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7601234567L).Build();

        private static readonly PhoneNumber USFixedOrMobile =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502123456L).Build();

        private static readonly PhoneNumber NumberWithInvalidCountryCode =
            new PhoneNumber.Builder().SetCountryCode(999).SetNationalNumber(2423651234L).Build();

        private static readonly PhoneNumber InternationalTollFree =
            new PhoneNumber.Builder().SetCountryCode(800).SetNationalNumber(12345678L).Build();

        [Fact]
        public void TestGetNameForMobilePortableRegion()
        {
            Assert.Equal("British carrier",
                carrierMapper.GetNameForNumber(UkMobile1, Locale.English));
            Assert.Equal("Brittisk operat\u00F6r",
                carrierMapper.GetNameForNumber(UkMobile1, new Locale("sv", "SE")));
            Assert.Equal("British carrier",
                carrierMapper.GetNameForNumber(UkMobile1, Locale.French));
            // Returns an empty string because the UK implements mobile number portability.
            Assert.Equal("", carrierMapper.GetSafeDisplayName(UkMobile1, Locale.English));
        }

        [Fact]
        public void TestGetNameForNonMobilePortableRegion()
        {
            Assert.Equal("Angolan carrier",
                carrierMapper.GetNameForNumber(AoMobile1, Locale.English));
            Assert.Equal("Angolan carrier",
                carrierMapper.GetSafeDisplayName(AoMobile1, Locale.English));
        }

        [Fact]
        public void TestGetNameForFixedLineNumber()
        {
            Assert.Equal("", carrierMapper.GetNameForNumber(AoFixed1, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForNumber(UkFixed1, Locale.English));
            // If the carrier information is present in the files and the method that assumes a valid
            // number is used, a carrier is returned.
            Assert.Equal("Angolan fixed line carrier",
                carrierMapper.GetNameForValidNumber(AoFixed2, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForValidNumber(UkFixed2, Locale.English));
        }

        [Fact]
        public void TestGetNameForFixedOrMobileNumber()
        {
            Assert.Equal("US carrier", carrierMapper.GetNameForNumber(USFixedOrMobile,
                Locale.English));
        }

        [Fact]
        public void TestGetNameForPagerNumber()
        {
            Assert.Equal("British pager", carrierMapper.GetNameForNumber(UkPager, Locale.English));
        }

        [Fact]
        public void TestGetNameForNumberWithNoDataFile()
        {
            Assert.Equal("", carrierMapper.GetNameForNumber(NumberWithInvalidCountryCode,
                Locale.English));
            Assert.Equal("", carrierMapper.GetNameForNumber(InternationalTollFree,
                Locale.English));
            Assert.Equal("", carrierMapper.GetNameForValidNumber(NumberWithInvalidCountryCode,
                Locale.English));
            Assert.Equal("", carrierMapper.GetNameForValidNumber(InternationalTollFree,
                Locale.English));
        }

        [Fact]
        public void TestGetNameForNumberWithMissingPrefix()
        {
            Assert.Equal("", carrierMapper.GetNameForNumber(UkMobile2, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForNumber(AoMobile2, Locale.English));
        }

        [Fact]
        public void TestGetNameForInvalidNumber()
        {
            Assert.Equal("", carrierMapper.GetNameForNumber(UkInvalidNumber, Locale.English));
            Assert.Equal("", carrierMapper.GetNameForNumber(AoInvalidNumber, Locale.English));
        }
    }
}
