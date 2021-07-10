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

using System;
using Xunit;

namespace PhoneNumbers.Test
{


/**
 * Unit tests for ShortNumberInfo.java
 *
 * @author Shaopeng Jia
 */
    public class ShortNumberInfoTest : TestMetadataTestCase
    {
        private static readonly ShortNumberInfo ShortInfo = ShortNumberInfo.GetInstance();

        [Fact]
        public void TestIsPossibleShortNumber()
        {
            var possibleNumber = new PhoneNumber.Builder().SetCountryCode(33).SetNationalNumber(123456L).Build();
            Assert.True(ShortInfo.IsPossibleShortNumber(possibleNumber));
            Assert.True(
                ShortInfo.IsPossibleShortNumberForRegion(Parse("123456", RegionCode.FR), RegionCode.FR));

            var impossibleNumber = new PhoneNumber.Builder().SetCountryCode(33).SetNationalNumber(9L).Build();
            Assert.False(ShortInfo.IsPossibleShortNumber(impossibleNumber));

            // Note that GB and GG share the country calling code 44, and that this number is possible but
            // not valid.
            Assert.True(ShortInfo.IsPossibleShortNumber(
                new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(11001L).Build()));
        }

        [Fact]
        public void TestIsValidShortNumber()
        {
            Assert.True(ShortInfo.IsValidShortNumber(
                new PhoneNumber.Builder().SetCountryCode(33).SetNationalNumber(1010L).Build()));
            Assert.True(ShortInfo.IsValidShortNumberForRegion(Parse("1010", RegionCode.FR), RegionCode.FR));
            Assert.False(ShortInfo.IsValidShortNumber(
                new PhoneNumber.Builder().SetCountryCode(33).SetNationalNumber(123456L).Build()));
            Assert.False(
                ShortInfo.IsValidShortNumberForRegion(Parse("123456", RegionCode.FR), RegionCode.FR));

            // Note that GB and GG share the country calling code 44.
            Assert.True(ShortInfo.IsValidShortNumber(
                new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(18001L).Build()));
        }

        [Fact]
        public void TestIsCarrierSpecific()
        {
            var carrierSpecificNumber = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(33669L).Build();
            Assert.True(ShortInfo.IsCarrierSpecific(carrierSpecificNumber));
            Assert.True(
                ShortInfo.IsCarrierSpecificForRegion(Parse("33669", RegionCode.US), RegionCode.US));

            var notCarrierSpecificNumber = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(911L).Build();
            Assert.False(ShortInfo.IsCarrierSpecific(notCarrierSpecificNumber));
            Assert.False(
                ShortInfo.IsCarrierSpecificForRegion(Parse("911", RegionCode.US), RegionCode.US));

            var carrierSpecificNumberForSomeRegion =
                new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(211L).Build();
            Assert.True(ShortInfo.IsCarrierSpecific(carrierSpecificNumberForSomeRegion));
            Assert.True(
                ShortInfo.IsCarrierSpecificForRegion(carrierSpecificNumberForSomeRegion, RegionCode.US));
            Assert.False(
                ShortInfo.IsCarrierSpecificForRegion(carrierSpecificNumberForSomeRegion, RegionCode.BB));
        }

        [Fact]
        public void TestIsSmsService()
        {
            var smsServiceNumberForSomeRegion =
                new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(21234L).Build();
            Assert.True(ShortInfo.IsSmsServiceForRegion(smsServiceNumberForSomeRegion, RegionCode.US));
            Assert.False(ShortInfo.IsSmsServiceForRegion(smsServiceNumberForSomeRegion, RegionCode.BB));
        }

        [Fact]
        public void TestGetExpectedCost()
        {
            var premiumRateExample = ShortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.PREMIUM_RATE);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE, ShortInfo.GetExpectedCostForRegion(
                Parse(premiumRateExample, RegionCode.FR), RegionCode.FR));
            var premiumRateNumber = new PhoneNumber.Builder().SetCountryCode(33)
                .SetNationalNumber(ulong.Parse(premiumRateExample)).Build();
            Assert.Equal(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE,
                ShortInfo.GetExpectedCost(premiumRateNumber));

            var standardRateExample = ShortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.STANDARD_RATE);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.STANDARD_RATE, ShortInfo.GetExpectedCostForRegion(
                Parse(standardRateExample, RegionCode.FR), RegionCode.FR));
            var standardRateNumber = new PhoneNumber.Builder().SetCountryCode(33)
                .SetNationalNumber(ulong.Parse(standardRateExample)).Build();
            Assert.Equal(ShortNumberInfo.ShortNumberCost.STANDARD_RATE,
                ShortInfo.GetExpectedCost(standardRateNumber));

            var tollFreeExample = ShortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.TOLL_FREE);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                ShortInfo.GetExpectedCostForRegion(Parse(tollFreeExample, RegionCode.FR), RegionCode.FR));
            var tollFreeNumber = new PhoneNumber.Builder().SetCountryCode(33)
                .SetNationalNumber(ulong.Parse(tollFreeExample)).Build();
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                ShortInfo.GetExpectedCost(tollFreeNumber));

            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                ShortInfo.GetExpectedCostForRegion(Parse("12345", RegionCode.FR), RegionCode.FR));
            var unknownCostNumber = new PhoneNumber.Builder().SetCountryCode(33).SetNationalNumber(12345L);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                ShortInfo.GetExpectedCost(unknownCostNumber.Build()));

            // Test that an invalid number may nevertheless have a cost other than UNKNOWN_COST.
            Assert.False(
                ShortInfo.IsValidShortNumberForRegion(Parse("116123", RegionCode.FR), RegionCode.FR));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                ShortInfo.GetExpectedCostForRegion(Parse("116123", RegionCode.FR), RegionCode.FR));
            var invalidNumber = new PhoneNumber.Builder().SetCountryCode(33).SetNationalNumber(116123L).Build();
            Assert.False(ShortInfo.IsValidShortNumber(invalidNumber));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                ShortInfo.GetExpectedCost(invalidNumber));

            // Test a nonexistent country code.
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                ShortInfo.GetExpectedCostForRegion(Parse("911", RegionCode.US), RegionCode.ZZ));
            unknownCostNumber.Clear();
            unknownCostNumber.SetCountryCode(123).SetNationalNumber(911L);
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                ShortInfo.GetExpectedCost(unknownCostNumber.Build()));
        }

        [Fact]
        public void TestGetExpectedCostForSharedCountryCallingCode()
        {
            // Test some numbers which have different costs in countries sharing the same country calling
            // code. In Australia, 1234 is premium-rate, 1194 is standard-rate, and 733 is toll-free. These
            // are not known to be valid numbers in the Christmas Islands.
            var ambiguousPremiumRateString = "1234";
            var ambiguousPremiumRateNumber =
                new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(1234L).Build();
            var ambiguousStandardRateString = "1194";
            var ambiguousStandardRateNumber =
                new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(1194L).Build();
            var ambiguousTollFreeString = "733";
            var ambiguousTollFreeNumber =
                new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(733L).Build();

            Assert.True(ShortInfo.IsValidShortNumber(ambiguousPremiumRateNumber));
            Assert.True(ShortInfo.IsValidShortNumber(ambiguousStandardRateNumber));
            Assert.True(ShortInfo.IsValidShortNumber(ambiguousTollFreeNumber));

            Assert.True(ShortInfo.IsValidShortNumberForRegion(
                Parse(ambiguousPremiumRateString, RegionCode.AU), RegionCode.AU));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE, ShortInfo.GetExpectedCostForRegion(
                Parse(ambiguousPremiumRateString, RegionCode.AU), RegionCode.AU));
            Assert.False(ShortInfo.IsValidShortNumberForRegion(
                Parse(ambiguousPremiumRateString, RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST, ShortInfo.GetExpectedCostForRegion(
                Parse(ambiguousPremiumRateString, RegionCode.CX), RegionCode.CX));
            // PREMIUM_RATE takes precedence over UNKNOWN_COST.
            Assert.Equal(ShortNumberInfo.ShortNumberCost.PREMIUM_RATE,
                ShortInfo.GetExpectedCost(ambiguousPremiumRateNumber));

            Assert.True(ShortInfo.IsValidShortNumberForRegion(
                Parse(ambiguousStandardRateString, RegionCode.AU), RegionCode.AU));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.STANDARD_RATE, ShortInfo.GetExpectedCostForRegion(
                Parse(ambiguousStandardRateString, RegionCode.AU), RegionCode.AU));
            Assert.False(ShortInfo.IsValidShortNumberForRegion(
                Parse(ambiguousStandardRateString, RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST, ShortInfo.GetExpectedCostForRegion(
                Parse(ambiguousStandardRateString, RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                ShortInfo.GetExpectedCost(ambiguousStandardRateNumber));

            Assert.True(ShortInfo.IsValidShortNumberForRegion(Parse(ambiguousTollFreeString, RegionCode.AU),
                RegionCode.AU));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE, ShortInfo.GetExpectedCostForRegion(
                Parse(ambiguousTollFreeString, RegionCode.AU), RegionCode.AU));
            Assert.False(ShortInfo.IsValidShortNumberForRegion(Parse(ambiguousTollFreeString, RegionCode.CX),
                RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST, ShortInfo.GetExpectedCostForRegion(
                Parse(ambiguousTollFreeString, RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                ShortInfo.GetExpectedCost(ambiguousTollFreeNumber));
        }

        [Fact]
        public void TestGetExampleShortNumber()
        {
            Assert.Equal("110", ShortInfo.GetExampleShortNumber(RegionCode.AD));
            Assert.Equal("15", ShortInfo.GetExampleShortNumber(RegionCode.FR));
            Assert.Equal("", ShortInfo.GetExampleShortNumber(RegionCode.UN001));
            Assert.Equal("", ShortInfo.GetExampleShortNumber(null));
        }

        [Fact]
        public void TestGetExampleShortNumberForCost()
        {
            Assert.Equal("15", ShortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.TOLL_FREE));
            Assert.Equal("611", ShortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.STANDARD_RATE));
            Assert.Equal("36665", ShortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.PREMIUM_RATE));
            Assert.Equal("", ShortInfo.GetExampleShortNumberForCost(RegionCode.FR,
                ShortNumberInfo.ShortNumberCost.UNKNOWN_COST));
        }

        [Fact]
        public void testConnectsToEmergencyNumber_US()
        {
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("911", RegionCode.US));
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("112", RegionCode.US));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("999", RegionCode.US));
        }

        [Fact]
        public void testConnectsToEmergencyNumberLongNumber_US()
        {
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("9116666666", RegionCode.US));
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("1126666666", RegionCode.US));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("9996666666", RegionCode.US));
        }

        [Fact]
        public void testConnectsToEmergencyNumberWithFormatting_US()
        {
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("9-1-1", RegionCode.US));
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("1-1-2", RegionCode.US));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("9-9-9", RegionCode.US));
        }

        [Fact]
        public void testConnectsToEmergencyNumberWithPlusSign_US()
        {
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("+911", RegionCode.US));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("\uFF0B911", RegionCode.US));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber(" +911", RegionCode.US));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("+112", RegionCode.US));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("+999", RegionCode.US));
        }

        [Fact]
        public void testConnectsToEmergencyNumber_BR()
        {
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("911", RegionCode.BR));
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("190", RegionCode.BR));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("999", RegionCode.BR));
        }

        [Fact]
        public void testConnectsToEmergencyNumberLongNumber_BR()
        {
            // Brazilian emergency numbers don't work when additional digits are appended.
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("9111", RegionCode.BR));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("1900", RegionCode.BR));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("9996", RegionCode.BR));
        }

        [Fact]
        public void testConnectsToEmergencyNumber_CL()
        {
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("131", RegionCode.CL));
            Assert.True(ShortInfo.ConnectsToEmergencyNumber("133", RegionCode.CL));
        }

        [Fact]
        public void testConnectsToEmergencyNumberLongNumber_CL()
        {
            // Chilean emergency numbers don't work when additional digits are appended.
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("1313", RegionCode.CL));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("1330", RegionCode.CL));
        }

        [Fact]
        public void testConnectsToEmergencyNumber_AO()
        {
            // Angola doesn't have any metadata for emergency numbers in the test metadata.
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("911", RegionCode.AO));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("222123456", RegionCode.AO));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("923123456", RegionCode.AO));
        }

        [Fact]
        public void testConnectsToEmergencyNumber_ZW()
        {
            // Zimbabwe doesn't have any metadata in the test metadata.
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("911", RegionCode.ZW));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("01312345", RegionCode.ZW));
            Assert.False(ShortInfo.ConnectsToEmergencyNumber("0711234567", RegionCode.ZW));
        }

        [Fact]
        public void testIsEmergencyNumber_US()
        {
            Assert.True(ShortInfo.IsEmergencyNumber("911", RegionCode.US));
            Assert.True(ShortInfo.IsEmergencyNumber("112", RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber("999", RegionCode.US));
        }

        [Fact]
        public void testIsEmergencyNumberLongNumber_US()
        {
            Assert.False(ShortInfo.IsEmergencyNumber("9116666666", RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber("1126666666", RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber("9996666666", RegionCode.US));
        }

        [Fact]
        public void testIsEmergencyNumberWithFormatting_US()
        {
            Assert.True(ShortInfo.IsEmergencyNumber("9-1-1", RegionCode.US));
            Assert.True(ShortInfo.IsEmergencyNumber("*911", RegionCode.US));
            Assert.True(ShortInfo.IsEmergencyNumber("1-1-2", RegionCode.US));
            Assert.True(ShortInfo.IsEmergencyNumber("*112", RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber("9-9-9", RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber("*999", RegionCode.US));
        }

        [Fact]
        public void testIsEmergencyNumberWithPlusSign_US()
        {
            Assert.False(ShortInfo.IsEmergencyNumber("+911", RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber("\uFF0B911", RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber(" +911", RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber("+112", RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber("+999", RegionCode.US));
        }

        [Fact]
        public void testIsEmergencyNumber_BR()
        {
            Assert.True(ShortInfo.IsEmergencyNumber("911", RegionCode.BR));
            Assert.True(ShortInfo.IsEmergencyNumber("190", RegionCode.BR));
            Assert.False(ShortInfo.IsEmergencyNumber("999", RegionCode.BR));
        }

        [Fact]
        public void testIsEmergencyNumberLongNumber_BR()
        {
            Assert.False(ShortInfo.IsEmergencyNumber("9111", RegionCode.BR));
            Assert.False(ShortInfo.IsEmergencyNumber("1900", RegionCode.BR));
            Assert.False(ShortInfo.IsEmergencyNumber("9996", RegionCode.BR));
        }

        [Fact]
        public void testIsEmergencyNumber_AO()
        {
            // Angola doesn't have any metadata for emergency numbers in the test metadata.
            Assert.False(ShortInfo.IsEmergencyNumber("911", RegionCode.AO));
            Assert.False(ShortInfo.IsEmergencyNumber("222123456", RegionCode.AO));
            Assert.False(ShortInfo.IsEmergencyNumber("923123456", RegionCode.AO));
        }

        [Fact]
        public void testIsEmergencyNumber_ZW()
        {
            // Zimbabwe doesn't have any metadata in the test metadata.
            Assert.False(ShortInfo.IsEmergencyNumber("911", RegionCode.ZW));
            Assert.False(ShortInfo.IsEmergencyNumber("01312345", RegionCode.ZW));
            Assert.False(ShortInfo.IsEmergencyNumber("0711234567", RegionCode.ZW));
        }

        [Fact]
        public void TestEmergencyNumberForSharedCountryCallingCode()
        {
            // Test the emergency number 112, which is valid in both Australia and the Christmas Islands.
            Assert.True(ShortInfo.IsEmergencyNumber("112", RegionCode.AU));
            Assert.True(ShortInfo.IsValidShortNumberForRegion(Parse("112", RegionCode.AU), RegionCode.AU));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                ShortInfo.GetExpectedCostForRegion(Parse("112", RegionCode.AU), RegionCode.AU));
            Assert.True(ShortInfo.IsEmergencyNumber("112", RegionCode.CX));
            Assert.True(ShortInfo.IsValidShortNumberForRegion(Parse("112", RegionCode.CX), RegionCode.CX));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                ShortInfo.GetExpectedCostForRegion(Parse("112", RegionCode.CX), RegionCode.CX));
            var sharedEmergencyNumber =
                new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(112L).Build();
            Assert.True(ShortInfo.IsValidShortNumber(sharedEmergencyNumber));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                ShortInfo.GetExpectedCost(sharedEmergencyNumber));
        }

        [Fact]
        public void TestOverlappingNANPANumber()
        {
            // 211 is an emergency number in Barbados, while it is a toll-free information line in Canada
            // and the USA.
            Assert.True(ShortInfo.IsEmergencyNumber("211", RegionCode.BB));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                ShortInfo.GetExpectedCostForRegion(Parse("211", RegionCode.BB), RegionCode.BB));
            Assert.False(ShortInfo.IsEmergencyNumber("211", RegionCode.US));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                ShortInfo.GetExpectedCostForRegion(Parse("211", RegionCode.US), RegionCode.US));
            Assert.False(ShortInfo.IsEmergencyNumber("211", RegionCode.CA));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.TOLL_FREE,
                ShortInfo.GetExpectedCostForRegion(Parse("211", RegionCode.CA), RegionCode.CA));
        }

        [Fact]
        public void TestCountryCallingCodeIsNotIgnored()
        {
            // +46 is the country calling code for Sweden (SE), and 40404 is a valid short number in the US.
            Assert.False(ShortInfo.IsPossibleShortNumberForRegion(
                Parse("+4640404", RegionCode.SE), RegionCode.US));
            Assert.False(ShortInfo.IsValidShortNumberForRegion(
                Parse("+4640404", RegionCode.SE), RegionCode.US));
            Assert.Equal(ShortNumberInfo.ShortNumberCost.UNKNOWN_COST,
                ShortInfo.GetExpectedCostForRegion(
                    Parse("+4640404", RegionCode.SE), RegionCode.US));
        }

        private PhoneNumber Parse(string number, string regionCode)
        {
            try
            {
                return PhoneUtil.Parse(number, regionCode);
            }
            catch (NumberParseException e)
            {
                throw new Exception(
                    "Test input data should always parse correctly: " + number + " (" + regionCode + ")", e);
            }
        }
    }
}
