/*
 * Copyright (C) 2009 Google Inc.
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
using System.Text;
using Xunit;

namespace PhoneNumbers.Test
{
    /**
    * Unit tests for PhoneNumberUtil.java
    *
    * Note that these tests use the test metadata, not the normal metadata file, so should not be used
    * for regression test purposes - these tests are illustrative only and test functionality.
    *
    * @author Shaopeng Jia
    * @author Lara Rennie
    */
    [Collection("TestMetadataTestCase")]
    public class TestPhoneNumberUtil
    {
        private readonly PhoneNumberUtil phoneUtil;

        public TestPhoneNumberUtil()
        {
            phoneUtil = TestMetadataTestCase.PhoneUtil;
        }

        // Set up some test numbers to re-use.
        private static readonly PhoneNumber AlphaNumericNumber =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(80074935247L).Build();
        private static readonly PhoneNumber ARMobile =
            new PhoneNumber.Builder().SetCountryCode(54).SetNationalNumber(91187654321L).Build();
        private static readonly PhoneNumber ARNumber =
            new PhoneNumber.Builder().SetCountryCode(54).SetNationalNumber(1187654321).Build();
        private static readonly PhoneNumber AUNumber =
            new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(236618300L).Build();
        private static readonly PhoneNumber BSMobile =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2423570000L).Build();
        private static readonly PhoneNumber BSNumber =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2423651234L).Build();
        // Note that this is the same as the example number for DE in the metadata.
        private static readonly PhoneNumber DENumber =
            new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(30123456L).Build();
        private static readonly PhoneNumber DEMobile =
            new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(15123456789L).Build();
        private static readonly PhoneNumber DEShortNumber =
            new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(1234L).Build();
        private static readonly PhoneNumber GBMobile =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(7912345678L).Build();
        private static readonly PhoneNumber GBNumber =
            new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(2070313000L).Build();
        private static readonly PhoneNumber ITMobile =
            new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(345678901L).Build();
        private static readonly PhoneNumber ITNumber =
            new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(236618300L).
            SetItalianLeadingZero(true).Build();
        private static readonly PhoneNumber JPStarNumber =
            new PhoneNumber.Builder().SetCountryCode(81).SetNationalNumber(2345).Build();
        // Numbers to test the formatting rules from Mexico.
        private static readonly PhoneNumber MXMobile1 =
            new PhoneNumber.Builder().SetCountryCode(52).SetNationalNumber(12345678900L).Build();
        private static readonly PhoneNumber MXMobile2 =
            new PhoneNumber.Builder().SetCountryCode(52).SetNationalNumber(15512345678L).Build();
        private static readonly PhoneNumber MXNumber1 =
            new PhoneNumber.Builder().SetCountryCode(52).SetNationalNumber(3312345678L).Build();
        private static readonly PhoneNumber MXNumber2 =
            new PhoneNumber.Builder().SetCountryCode(52).SetNationalNumber(8211234567L).Build();
        private static readonly PhoneNumber NZNumber =
            new PhoneNumber.Builder().SetCountryCode(64).SetNationalNumber(33316005L).Build();
        private static readonly PhoneNumber SGNumber =
            new PhoneNumber.Builder().SetCountryCode(65).SetNationalNumber(65218000L).Build();
        // A too-long and hence invalid US number.
        private static readonly PhoneNumber USLongNumber =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(65025300001L).Build();
        private static readonly PhoneNumber USNumber =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();
        private static readonly PhoneNumber USPremium =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(9002530000L).Build();
        // Too short, but still possible US numbers.
        private static readonly PhoneNumber USLocalNumber =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2530000L).Build();
        private static readonly PhoneNumber USShortByOneNumber =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(650253000L).Build();
        private static readonly PhoneNumber USTollfree =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(8002530000L).Build();
        private static readonly PhoneNumber USSpoof =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(0L).Build();
        private static readonly PhoneNumber USSpoofWithRawInput =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(0L)
                .SetRawInput("000-000-0000").Build();
        private static readonly PhoneNumber InternationalTollFree =
            new PhoneNumber.Builder().SetCountryCode(800).SetNationalNumber(12345678L).Build();
        // We set this to be the same length as numbers for the other non-geographical country prefix that
        // we have in our test metadata. However, this is not considered valid because they differ in
        // their country calling code.
        private static readonly PhoneNumber InternationalTollFreeTooLong =
            new PhoneNumber.Builder().SetCountryCode(800).SetNationalNumber(1234567890L).Build();
        private static readonly PhoneNumber UniversalPremiumRate =
            new PhoneNumber.Builder().SetCountryCode(979).SetNationalNumber(123456789L).Build();


        private static PhoneNumber.Builder Update(PhoneNumber p)
        {
            return new PhoneNumber.Builder().MergeFrom(p);
        }

        private static NumberFormat.Builder Update(NumberFormat p)
        {
            return new NumberFormat.Builder().MergeFrom(p);
        }

        private static PhoneMetadata.Builder Update(PhoneMetadata p)
        {
            return new PhoneMetadata.Builder().MergeFrom(p);
        }

        private static void AreEqual(PhoneNumber.Builder p1, PhoneNumber.Builder p2)
        {
            Assert.Equal(p1.Clone().Build(), p2.Clone().Build());
        }

        [Fact]
        public void TestGetSupportedRegions()
        {
            Assert.True(phoneUtil.GetSupportedRegions().Count > 0);
        }

        [Fact]
        public void TestGetInstanceLoadUSMetadata()
        {
            var metadata = phoneUtil.GetMetadataForRegion(RegionCode.US);
            Assert.Equal("US", metadata.Id);
            Assert.Equal(1, metadata.CountryCode);
            Assert.Equal("011", metadata.InternationalPrefix);
            Assert.True(metadata.HasNationalPrefix);
            Assert.Equal(2, metadata.NumberFormatCount);
            Assert.Equal("(\\d{3})(\\d{3})(\\d{4})",
                metadata.NumberFormatList[1].Pattern);
            Assert.Equal("$1 $2 $3", metadata.NumberFormatList[1].Format);
            Assert.Equal("[13-689]\\d{9}|2[0-35-9]\\d{8}",
                metadata.GeneralDesc.NationalNumberPattern);
            Assert.Equal("[13-689]\\d{9}|2[0-35-9]\\d{8}",
                metadata.FixedLine.NationalNumberPattern);
            Assert.Equal(1, metadata.GeneralDesc.PossibleLengthCount);
            Assert.Equal(10, metadata.GeneralDesc.PossibleLengthList[0]);
            // Possible lengths are the same as the general description, so aren't stored separately in the
            // toll free element as well.
            Assert.Equal(0, metadata.TollFree.PossibleLengthCount);
            Assert.Equal("900\\d{7}", metadata.PremiumRate.NationalNumberPattern);
            // No shared-cost data is available, so its national number data should not be set.
            Assert.False(metadata.SharedCost.HasNationalNumberPattern);
        }

        [Fact]
        public void TestGetInstanceLoadDEMetadata()
        {
            var metadata = phoneUtil.GetMetadataForRegion(RegionCode.DE);
            Assert.Equal("DE", metadata.Id);
            Assert.Equal(49, metadata.CountryCode);
            Assert.Equal("00", metadata.InternationalPrefix);
            Assert.Equal("0", metadata.NationalPrefix);
            Assert.Equal(6, metadata.NumberFormatCount);
            Assert.Equal(1, metadata.NumberFormatList[5].LeadingDigitsPatternCount);
            Assert.Equal("900", metadata.NumberFormatList[5].LeadingDigitsPatternList[0]);
            Assert.Equal("(\\d{3})(\\d{3,4})(\\d{4})",
                metadata.NumberFormatList[5].Pattern);
            Assert.Equal("$1 $2 $3", metadata.NumberFormatList[5].Format);
            Assert.Equal(2, metadata.GeneralDesc.PossibleLengthLocalOnlyCount);
            Assert.Equal(8, metadata.GeneralDesc.PossibleLengthCount);
            // Nothing is present for fixed-line, since it is the same as the general desc, so for
            // efficiency reasons we don't store an extra value.
            Assert.Equal(0, metadata.FixedLine.PossibleLengthCount);
            Assert.Equal(2, metadata.Mobile.PossibleLengthCount);
            Assert.Equal("(?:[24-6]\\d{2}|3[03-9]\\d|[789](?:0[2-9]|[1-9]\\d))\\d{1,8}",
                metadata.FixedLine.NationalNumberPattern);
            Assert.Equal("30123456", metadata.FixedLine.ExampleNumber);
            Assert.Equal(10, metadata.TollFree.PossibleLengthList[0]);
            Assert.Equal("900([135]\\d{6}|9\\d{7})", metadata.PremiumRate.NationalNumberPattern);
        }

        [Fact]
        public void TestGetInstanceLoadARMetadata()
        {
            var metadata = phoneUtil.GetMetadataForRegion(RegionCode.AR);
            Assert.Equal("AR", metadata.Id);
            Assert.Equal(54, metadata.CountryCode);
            Assert.Equal("00", metadata.InternationalPrefix);
            Assert.Equal("0", metadata.NationalPrefix);
            Assert.Equal("0(?:(11|343|3715)15)?", metadata.NationalPrefixForParsing);
            Assert.Equal("9$1", metadata.NationalPrefixTransformRule);
            Assert.Equal("$2 15 $3-$4", metadata.NumberFormatList[2].Format);
            Assert.Equal("(\\d)(\\d{4})(\\d{2})(\\d{4})",
                     metadata.NumberFormatList[3].Pattern);
            Assert.Equal("(\\d)(\\d{4})(\\d{2})(\\d{4})",
                     metadata.IntlNumberFormatList[3].Pattern);
            Assert.Equal("$1 $2 $3 $4", metadata.IntlNumberFormatList[3].Format);
        }

        [Fact]
        public void TestGetInstanceLoadInternationalTollFreeMetadata()
        {
            var metadata = phoneUtil.GetMetadataForNonGeographicalRegion(800);
            Assert.Equal("001", metadata.Id);
            Assert.Equal(800, metadata.CountryCode);
            Assert.Equal("$1 $2", metadata.NumberFormatList[0].Format);
            Assert.Equal("(\\d{4})(\\d{4})", metadata.NumberFormatList[0].Pattern);
            Assert.Equal("12345678", metadata.TollFree.ExampleNumber);
        }

        [Fact]
        public void TestGetLengthOfGeographicalAreaCode()
        {
            // Google MTV, which has area code "650".
            Assert.Equal(3, phoneUtil.GetLengthOfGeographicalAreaCode(USNumber));

            // A North America toll-free number, which has no area code.
            Assert.Equal(0, phoneUtil.GetLengthOfGeographicalAreaCode(USTollfree));

            // Google London, which has area code "20".
            Assert.Equal(2, phoneUtil.GetLengthOfGeographicalAreaCode(GBNumber));

            // A UK mobile phone, which has no area code.
            Assert.Equal(0, phoneUtil.GetLengthOfGeographicalAreaCode(GBMobile));

            // Google Buenos Aires, which has area code "11".
            Assert.Equal(2, phoneUtil.GetLengthOfGeographicalAreaCode(ARNumber));

            // Google Sydney, which has area code "2".
            Assert.Equal(1, phoneUtil.GetLengthOfGeographicalAreaCode(AUNumber));

            // Italian numbers - there is no national prefix, but it still has an area code.
            Assert.Equal(2, phoneUtil.GetLengthOfGeographicalAreaCode(ITNumber));

            // Google Singapore. Singapore has no area code and no national prefix.
            Assert.Equal(0, phoneUtil.GetLengthOfGeographicalAreaCode(SGNumber));

            // An invalid US number (1 digit shorter), which has no area code.
            Assert.Equal(0, phoneUtil.GetLengthOfGeographicalAreaCode(USShortByOneNumber));

            // An international toll free number, which has no area code.
            Assert.Equal(0, phoneUtil.GetLengthOfGeographicalAreaCode(InternationalTollFree));
        }

        [Fact]
        public void TestGetLengthOfNationalDestinationCode()
        {
            // Google MTV, which has national destination code (NDC) "650".
            Assert.Equal(3, phoneUtil.GetLengthOfNationalDestinationCode(USNumber));

            // A North America toll-free number, which has NDC "800".
            Assert.Equal(3, phoneUtil.GetLengthOfNationalDestinationCode(USTollfree));

            // Google London, which has NDC "20".
            Assert.Equal(2, phoneUtil.GetLengthOfNationalDestinationCode(GBNumber));

            // A UK mobile phone, which has NDC "7912".
            Assert.Equal(4, phoneUtil.GetLengthOfNationalDestinationCode(GBMobile));

            // Google Buenos Aires, which has NDC "11".
            Assert.Equal(2, phoneUtil.GetLengthOfNationalDestinationCode(ARNumber));

            // An Argentinian mobile which has NDC "911".
            Assert.Equal(3, phoneUtil.GetLengthOfNationalDestinationCode(ARMobile));

            // Google Sydney, which has NDC "2".
            Assert.Equal(1, phoneUtil.GetLengthOfNationalDestinationCode(AUNumber));

            // Google Singapore, which has NDC "6521".
            Assert.Equal(4, phoneUtil.GetLengthOfNationalDestinationCode(SGNumber));

            // An invalid US number (1 digit shorter), which has no NDC.
            Assert.Equal(0, phoneUtil.GetLengthOfNationalDestinationCode(USShortByOneNumber));

            // A number containing an invalid country calling code, which shouldn't have any NDC.
            var number = new PhoneNumber.Builder().SetCountryCode(123).SetNationalNumber(6502530000L).Build();
            Assert.Equal(0, phoneUtil.GetLengthOfNationalDestinationCode(number));

            // An international toll free number, which has NDC "1234".
            Assert.Equal(4, phoneUtil.GetLengthOfNationalDestinationCode(InternationalTollFree));

            // A mobile number from China is geographical, but does not have an area code: however it still
            // can be considered to have a national destination code.
            var cnMobile = new PhoneNumber.Builder().SetCountryCode(86).SetNationalNumber(18912341234L).Build();
            Assert.Equal(3, phoneUtil.GetLengthOfNationalDestinationCode(cnMobile));
        }

        [Fact]
        public void TestGetCountryMobileToken()
        {
            Assert.Equal("9", PhoneNumberUtil.GetCountryMobileToken(phoneUtil.GetCountryCodeForRegion(
                RegionCode.AR)));

            // Country calling code for Sweden, which has no mobile token.
            Assert.Equal("", PhoneNumberUtil.GetCountryMobileToken(phoneUtil.GetCountryCodeForRegion(
                RegionCode.SE)));
        }

        [Fact]
        public void TestGetNationalSignificantNumber()
        {
            Assert.Equal("6502530000", phoneUtil.GetNationalSignificantNumber(USNumber));

            // An Italian mobile number.
            Assert.Equal("345678901", phoneUtil.GetNationalSignificantNumber(ITMobile));

            // An Italian fixed line number.
            Assert.Equal("0236618300", phoneUtil.GetNationalSignificantNumber(ITNumber));

            Assert.Equal("12345678", phoneUtil.GetNationalSignificantNumber(InternationalTollFree));
        }

        [Fact]
        public void TestGetNationalSignificantNumber_ManyLeadingZeros()
        {
            var number = new PhoneNumber.Builder()
                .SetCountryCode(1)
                .SetNationalNumber(650)
                .SetItalianLeadingZero(true)
                .SetNumberOfLeadingZeros(2)
                .Build();
            Assert.Equal("00650", phoneUtil.GetNationalSignificantNumber(number));
            // Set a bad value; we shouldn't crash, we shouldn't output any leading zeros at all.
            number = Update(number).SetNumberOfLeadingZeros(-3).Build();
            Assert.Equal("650", phoneUtil.GetNationalSignificantNumber(number));
        }

        [Fact]
        public void TestGetExampleNumber()
        {
            Assert.Equal(DENumber, phoneUtil.GetExampleNumber(RegionCode.DE));

            Assert.Equal(DENumber, phoneUtil.GetExampleNumberForType(RegionCode.DE,
                PhoneNumberType.FIXED_LINE));
            Assert.Equal(DEMobile, phoneUtil.GetExampleNumberForType(RegionCode.DE,
                PhoneNumberType.MOBILE));
            // For the US, the example number is placed under general description, and hence should be used
            // for both fixed line and mobile, so neither of these should return null.
            Assert.NotNull(phoneUtil.GetExampleNumberForType(RegionCode.US,
                PhoneNumberType.FIXED_LINE));
            Assert.NotNull(phoneUtil.GetExampleNumberForType(RegionCode.US,
                PhoneNumberType.MOBILE));
            // CS is an invalid region, so we have no data for it.
            Assert.Null(phoneUtil.GetExampleNumberForType(RegionCode.CS,
                PhoneNumberType.MOBILE));
            // RegionCode 001 is reserved for supporting non-geographical country calling code. We don't
            // support getting an example number for it with this method.
            Assert.Null(phoneUtil.GetExampleNumber(RegionCode.UN001));
        }

        [Fact]
        public void TestConvertAlphaCharactersInNumber()
        {
            var input = "1800-ABC-DEF";
            // Alpha chars are converted to digits; everything else is left untouched.
            var expectedOutput = "1800-222-333";
            Assert.Equal(expectedOutput, PhoneNumberUtil.ConvertAlphaCharactersInNumber(input));
        }


        [Fact]
        public void TestNormaliseRemovePunctuation()
        {
            var inputNumber = "034-56&+#2\u00AD34";
            var expectedOutput = "03456234";
            Assert.Equal(expectedOutput,
                PhoneNumberUtil.Normalize(inputNumber));
        }

        [Fact]
        public void TestNormaliseReplaceAlphaCharacters()
        {
            var inputNumber = "034-I-am-HUNGRY";
            var expectedOutput = "034426486479";
            Assert.Equal(expectedOutput,
                PhoneNumberUtil.Normalize(inputNumber));
        }

        [Fact]
        public void TestNormaliseOtherDigits()
        {
            var inputNumber = "\uFF125\u0665";
            var expectedOutput = "255";
            Assert.Equal(expectedOutput,
                PhoneNumberUtil.Normalize(inputNumber));
            // Eastern-Arabic digits.
            inputNumber = "\u06F52\u06F0";
            expectedOutput = "520";
            Assert.Equal(expectedOutput,
                PhoneNumberUtil.Normalize(inputNumber));
        }

        [Fact]
        public void TestNormaliseStripAlphaCharacters()
        {
            var inputNumber = "034-56&+a#234";
            var expectedOutput = "03456234";
            Assert.Equal(expectedOutput,
                PhoneNumberUtil.NormalizeDigitsOnly(inputNumber));
        }

        [Fact]
        public void TestFormatUSNumber()
        {
            Assert.Equal("650 253 0000", phoneUtil.Format(USNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+1 650 253 0000", phoneUtil.Format(USNumber, PhoneNumberFormat.INTERNATIONAL));

            Assert.Equal("800 253 0000", phoneUtil.Format(USTollfree, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+1 800 253 0000", phoneUtil.Format(USTollfree, PhoneNumberFormat.INTERNATIONAL));

            Assert.Equal("900 253 0000", phoneUtil.Format(USPremium, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+1 900 253 0000", phoneUtil.Format(USPremium, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("tel:+1-900-253-0000", phoneUtil.Format(USPremium, PhoneNumberFormat.RFC3966));

            // Numbers with all zeros in the national number part will be formatted by using the raw_input
            // if that is available no matter which format is specified.
            Assert.Equal("000-000-0000",
                phoneUtil.Format(USSpoofWithRawInput, PhoneNumberFormat.NATIONAL));
            Assert.Equal("0", phoneUtil.Format(USSpoof, PhoneNumberFormat.NATIONAL));
        }

        [Fact]
        public void TestFormatBSNumber()
        {
            Assert.Equal("242 365 1234", phoneUtil.Format(BSNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+1 242 365 1234", phoneUtil.Format(BSNumber, PhoneNumberFormat.INTERNATIONAL));
        }

        [Fact]
        public void TestFormatGBNumber()
        {
            Assert.Equal("(020) 7031 3000", phoneUtil.Format(GBNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+44 20 7031 3000", phoneUtil.Format(GBNumber, PhoneNumberFormat.INTERNATIONAL));

            Assert.Equal("(07912) 345 678", phoneUtil.Format(GBMobile, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+44 7912 345 678", phoneUtil.Format(GBMobile, PhoneNumberFormat.INTERNATIONAL));
        }

        [Fact]
        public void TestFormatDENumber()
        {
            var deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(301234L).Build();
            Assert.Equal("030/1234", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+49 30/1234", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("tel:+49-30-1234", phoneUtil.Format(deNumber, PhoneNumberFormat.RFC3966));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(291123L).Build();
            Assert.Equal("0291 123", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+49 291 123", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(29112345678L).Build();
            Assert.Equal("0291 12345678", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+49 291 12345678", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(912312345L).Build();
            Assert.Equal("09123 12345", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+49 9123 12345", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(80212345L).Build();
            Assert.Equal("08021 2345", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+49 8021 2345", phoneUtil.Format(deNumber, PhoneNumberFormat.INTERNATIONAL));
            // Note this number is correctly formatted without national prefix. Most of the numbers that
            // are treated as invalid numbers by the library are short numbers, and they are usually not
            // dialed with national prefix.
            Assert.Equal("1234", phoneUtil.Format(DEShortNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+49 1234", phoneUtil.Format(DEShortNumber, PhoneNumberFormat.INTERNATIONAL));

            deNumber = new PhoneNumber.Builder().SetCountryCode(49).SetNationalNumber(41341234).Build();
            Assert.Equal("04134 1234", phoneUtil.Format(deNumber, PhoneNumberFormat.NATIONAL));
        }

        [Fact]
        public void TestFormatITNumber()
        {
            Assert.Equal("02 3661 8300", phoneUtil.Format(ITNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+39 02 3661 8300", phoneUtil.Format(ITNumber, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+390236618300", phoneUtil.Format(ITNumber, PhoneNumberFormat.E164));

            Assert.Equal("345 678 901", phoneUtil.Format(ITMobile, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+39 345 678 901", phoneUtil.Format(ITMobile, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+39345678901", phoneUtil.Format(ITMobile, PhoneNumberFormat.E164));
        }

        [Fact]
        public void TestFormatAUNumber()
        {
            Assert.Equal("02 3661 8300", phoneUtil.Format(AUNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+61 2 3661 8300", phoneUtil.Format(AUNumber, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+61236618300", phoneUtil.Format(AUNumber, PhoneNumberFormat.E164));

            var auNumber = new PhoneNumber.Builder().SetCountryCode(61).SetNationalNumber(1800123456L).Build();
            Assert.Equal("1800 123 456", phoneUtil.Format(auNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+61 1800 123 456", phoneUtil.Format(auNumber, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+611800123456", phoneUtil.Format(auNumber, PhoneNumberFormat.E164));
        }

        [Fact]
        public void TestFormatARNumber()
        {
            Assert.Equal("011 8765-4321", phoneUtil.Format(ARNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+54 11 8765-4321", phoneUtil.Format(ARNumber, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+541187654321", phoneUtil.Format(ARNumber, PhoneNumberFormat.E164));

            Assert.Equal("011 15 8765-4321", phoneUtil.Format(ARMobile, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+54 9 11 8765 4321", phoneUtil.Format(ARMobile,
                                                            PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+5491187654321", phoneUtil.Format(ARMobile, PhoneNumberFormat.E164));
        }

        [Fact]
        public void TestFormatMXNumber()
        {
            Assert.Equal("045 234 567 8900", phoneUtil.Format(MXMobile1, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+52 1 234 567 8900", phoneUtil.Format(
                MXMobile1, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+5212345678900", phoneUtil.Format(MXMobile1, PhoneNumberFormat.E164));

            Assert.Equal("045 55 1234 5678", phoneUtil.Format(MXMobile2, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+52 1 55 1234 5678", phoneUtil.Format(
                MXMobile2, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+5215512345678", phoneUtil.Format(MXMobile2, PhoneNumberFormat.E164));

            Assert.Equal("01 33 1234 5678", phoneUtil.Format(MXNumber1, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+52 33 1234 5678", phoneUtil.Format(MXNumber1, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+523312345678", phoneUtil.Format(MXNumber1, PhoneNumberFormat.E164));

            Assert.Equal("01 821 123 4567", phoneUtil.Format(MXNumber2, PhoneNumberFormat.NATIONAL));
            Assert.Equal("+52 821 123 4567", phoneUtil.Format(MXNumber2, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+528211234567", phoneUtil.Format(MXNumber2, PhoneNumberFormat.E164));
        }

        [Fact]
        public void TestFormatOutOfCountryCallingNumber()
        {
            Assert.Equal("00 1 900 253 0000",
            phoneUtil.FormatOutOfCountryCallingNumber(USPremium, RegionCode.DE));

            Assert.Equal("1 650 253 0000",
            phoneUtil.FormatOutOfCountryCallingNumber(USNumber, RegionCode.BS));

            Assert.Equal("00 1 650 253 0000",
            phoneUtil.FormatOutOfCountryCallingNumber(USNumber, RegionCode.PL));

            Assert.Equal("011 44 7912 345 678",
            phoneUtil.FormatOutOfCountryCallingNumber(GBMobile, RegionCode.US));

            Assert.Equal("00 49 1234",
            phoneUtil.FormatOutOfCountryCallingNumber(DEShortNumber, RegionCode.GB));
            // Note this number is correctly formatted without national prefix. Most of the numbers that
            // are treated as invalid numbers by the library are short numbers, and they are usually not
            // dialed with national prefix.
            Assert.Equal("1234", phoneUtil.FormatOutOfCountryCallingNumber(DEShortNumber, RegionCode.DE));

            Assert.Equal("011 39 02 3661 8300",
            phoneUtil.FormatOutOfCountryCallingNumber(ITNumber, RegionCode.US));
            Assert.Equal("02 3661 8300",
            phoneUtil.FormatOutOfCountryCallingNumber(ITNumber, RegionCode.IT));
            Assert.Equal("+39 02 3661 8300",
            phoneUtil.FormatOutOfCountryCallingNumber(ITNumber, RegionCode.SG));

            Assert.Equal("6521 8000",
            phoneUtil.FormatOutOfCountryCallingNumber(SGNumber, RegionCode.SG));

            Assert.Equal("011 54 9 11 8765 4321",
            phoneUtil.FormatOutOfCountryCallingNumber(ARMobile, RegionCode.US));
            Assert.Equal("011 800 1234 5678",
                 phoneUtil.FormatOutOfCountryCallingNumber(InternationalTollFree, RegionCode.US));

            var arNumberWithExtn = new PhoneNumber.Builder().MergeFrom(ARMobile).SetExtension("1234").Build();
            Assert.Equal("011 54 9 11 8765 4321 ext. 1234",
            phoneUtil.FormatOutOfCountryCallingNumber(arNumberWithExtn, RegionCode.US));
            Assert.Equal("0011 54 9 11 8765 4321 ext. 1234",
            phoneUtil.FormatOutOfCountryCallingNumber(arNumberWithExtn, RegionCode.AU));
            Assert.Equal("011 15 8765-4321 ext. 1234",
            phoneUtil.FormatOutOfCountryCallingNumber(arNumberWithExtn, RegionCode.AR));
        }

        [Fact]
        public void TestFormatOutOfCountryWithInvalidRegion()
        {
            // AQ/Antarctica isn't a valid region code for phone number formatting,
            // so this falls back to intl formatting.
            Assert.Equal("+1 650 253 0000",
                phoneUtil.FormatOutOfCountryCallingNumber(USNumber, RegionCode.AQ));
            // For region code 001, the out-of-country format always turns into the international format.
            Assert.Equal("+1 650 253 0000",
                 phoneUtil.FormatOutOfCountryCallingNumber(USNumber, RegionCode.UN001));

        }

        [Fact]
        public void TestFormatOutOfCountryWithPreferredIntlPrefix()
        {
            // This should use 0011, since that is the preferred international prefix (both 0011 and 0012
            // are accepted as possible international prefixes in our test metadta.)
            Assert.Equal("0011 39 02 3661 8300",
                phoneUtil.FormatOutOfCountryCallingNumber(ITNumber, RegionCode.AU));

            // Testing preferred international prefixes with ~ are supported (designates waiting).
            Assert.Equal("8~10 39 02 3661 8300", phoneUtil.FormatOutOfCountryCallingNumber(ITNumber, RegionCode.UZ));
        }

        [Fact]
        public void TestFormatOutOfCountryKeepingAlphaChars()
        {
            var alphaNumericNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(8007493524L)
                .SetRawInput("1800 six-flag")
                .Build();
            Assert.Equal("0011 1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetRawInput("1-800-SIX-flag")
                .Build();
            Assert.Equal("0011 1 800-SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetRawInput("Call us from UK: 00 1 800 SIX-flag")
                .Build();
            Assert.Equal("0011 1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetRawInput("800 SIX-flag")
                .Build();
            Assert.Equal("0011 1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            // Formatting from within the NANPA region.
            Assert.Equal("1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.US));

            Assert.Equal("1 800 SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.BS));

            // Testing that if the raw input doesn't exist, it is formatted using
            // FormatOutOfCountryCallingNumber.
            alphaNumericNumber = Update(alphaNumericNumber)
                .ClearRawInput()
                .Build();
            Assert.Equal("00 1 800 749 3524",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.DE));

            // Testing AU alpha number formatted from Australia.
            alphaNumericNumber = Update(alphaNumericNumber)
                .SetCountryCode(61).SetNationalNumber(827493524L)
                .SetRawInput("+61 82749-FLAG").Build();
            alphaNumericNumber = Update(alphaNumericNumber).Build();
            // This number should have the national prefix fixed.
            Assert.Equal("082749-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetRawInput("082749-FLAG").Build();
            Assert.Equal("082749-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            alphaNumericNumber = Update(alphaNumericNumber)
                .SetNationalNumber(18007493524L).SetRawInput("1-800-SIX-flag").Build();
            // This number should not have the national prefix prefixed, in accordance with the override for
            // this specific formatting rule.
            Assert.Equal("1-800-SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AU));

            // The metadata should not be permanently changed, since we copied it before modifying patterns.
            // Here we check this.
            alphaNumericNumber = Update(alphaNumericNumber)
                .SetNationalNumber(1800749352L).Build();
            Assert.Equal("1800 749 352",
                phoneUtil.FormatOutOfCountryCallingNumber(alphaNumericNumber, RegionCode.AU));

            // Testing a region with multiple international prefixes.
            Assert.Equal("+61 1-800-SIX-FLAG",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.SG));
            // Testing the case of calling from a non-supported region.
            Assert.Equal("+61 1-800-SIX-FLAG",
                 phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AQ));

            // Testing the case with an invalid country calling code.
            alphaNumericNumber = Update(alphaNumericNumber)
                .SetCountryCode(0).SetNationalNumber(18007493524L).SetRawInput("1-800-SIX-flag").Build();
            // Uses the raw input only.
            Assert.Equal("1-800-SIX-flag",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.DE));

            // Testing the case of an invalid alpha number.
            alphaNumericNumber = Update(alphaNumericNumber)
                .SetCountryCode(1).SetNationalNumber(80749L).SetRawInput("180-SIX").Build();
            // No country-code stripping can be done.
            Assert.Equal("00 1 180-SIX",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.DE));

            // Testing the case of calling from a non-supported region.
            alphaNumericNumber = Update(alphaNumericNumber)
                .SetCountryCode(1).SetNationalNumber(80749L).SetRawInput("180-SIX").Build();
            // No country-code stripping can be done since the number is invalid.
            Assert.Equal("+1 180-SIX",
                phoneUtil.FormatOutOfCountryKeepingAlphaChars(alphaNumericNumber, RegionCode.AQ));
        }

        [Fact]
        public void TestFormatWithCarrierCode()
        {
            // We only support this for AR in our test metadata, and only for mobile numbers starting with
            // certain values.
            var arMobile = new PhoneNumber.Builder().SetCountryCode(54).SetNationalNumber(92234654321L).Build();
            Assert.Equal("02234 65-4321", phoneUtil.Format(arMobile, PhoneNumberFormat.NATIONAL));
            // Here we force 14 as the carrier code.
            Assert.Equal("02234 14 65-4321",
                phoneUtil.FormatNationalNumberWithCarrierCode(arMobile, "14"));
            // Here we force the number to be shown with no carrier code.
            Assert.Equal("02234 65-4321",
                phoneUtil.FormatNationalNumberWithCarrierCode(arMobile, ""));
            // Here the international rule is used, so no carrier code should be present.
            Assert.Equal("+5492234654321", phoneUtil.Format(arMobile, PhoneNumberFormat.E164));
            // We don't support this for the US so there should be no change.
            Assert.Equal("650 253 0000", phoneUtil.FormatNationalNumberWithCarrierCode(USNumber, "15"));
        }

        [Fact]
        public void TestFormatWithPreferredCarrierCode()
        {
            // We only support this for AR in our test metadata.
            var arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(91234125678L).Build();
            // Test formatting with no preferred carrier code stored in the number itself.
            Assert.Equal("01234 15 12-5678",
                phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, "15"));
            Assert.Equal("01234 12-5678",
            phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, ""));
            // Test formatting with preferred carrier code present.
            arNumber = Update(arNumber).SetPreferredDomesticCarrierCode("19").Build();
            Assert.Equal("01234 12-5678", phoneUtil.Format(arNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("01234 19 12-5678",
                phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, "15"));
            Assert.Equal("01234 19 12-5678",
                phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, ""));
            // When the preferred_domestic_carrier_code is present (even when it is just a space), use it
            // instead of the default carrier code passed in.
            arNumber = Update(arNumber).SetPreferredDomesticCarrierCode(" ").Build();
            Assert.Equal("01234   12-5678",
                phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, "15"));
            // When the preferred_domestic_carrier_code is present but empty, treat it as unset and use
            // instead the default carrier code passed in.
            arNumber = Update(arNumber).SetPreferredDomesticCarrierCode("").Build();
            Assert.Equal("01234 15 12-5678",
                phoneUtil.FormatNationalNumberWithPreferredCarrierCode(arNumber, "15"));
            // We don't support this for the US so there should be no change.
            var usNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(4241231234L).SetPreferredDomesticCarrierCode("99")
                .Build();
            Assert.Equal("424 123 1234", phoneUtil.Format(usNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal("424 123 1234",
            phoneUtil.FormatNationalNumberWithPreferredCarrierCode(usNumber, "15"));
        }

        [Fact]
        public void TestFormatNumberForMobileDialing()
        {
            // US toll free numbers are marked as noInternationalDialling in the test metadata for testing
            // purposes.
            Assert.Equal("800 253 0000",
                phoneUtil.FormatNumberForMobileDialing(USTollfree, RegionCode.US,
                    true /*  keep formatting */));
            Assert.Equal("", phoneUtil.FormatNumberForMobileDialing(USTollfree, RegionCode.CN, true));
            Assert.Equal("+1 650 253 0000",
                phoneUtil.FormatNumberForMobileDialing(USNumber, RegionCode.US, true));
            var usNumberWithExtn = new PhoneNumber.Builder().MergeFrom(USNumber).SetExtension("1234").Build();
            Assert.Equal("+1 650 253 0000",
                phoneUtil.FormatNumberForMobileDialing(usNumberWithExtn, RegionCode.US, true));

            Assert.Equal("8002530000",
                phoneUtil.FormatNumberForMobileDialing(USTollfree, RegionCode.US,
                    false /* remove formatting */));
            Assert.Equal("", phoneUtil.FormatNumberForMobileDialing(USTollfree, RegionCode.CN, false));
            Assert.Equal("+16502530000",
                phoneUtil.FormatNumberForMobileDialing(USNumber, RegionCode.US, false));
            Assert.Equal("+16502530000",
                phoneUtil.FormatNumberForMobileDialing(usNumberWithExtn, RegionCode.US, false));

            // An invalid US number, which is one digit too long.
            Assert.Equal("+165025300001",
                phoneUtil.FormatNumberForMobileDialing(USLongNumber, RegionCode.US, false));
            Assert.Equal("+1 65025300001",
                phoneUtil.FormatNumberForMobileDialing(USLongNumber, RegionCode.US, true));

            // Star numbers. In real life they appear in Israel, but we have them in JP in our test
            // metadata.
            Assert.Equal("*2345",
                phoneUtil.FormatNumberForMobileDialing(JPStarNumber, RegionCode.JP, false));
            Assert.Equal("*2345",
                phoneUtil.FormatNumberForMobileDialing(JPStarNumber, RegionCode.JP, true));

            Assert.Equal("+80012345678",
                phoneUtil.FormatNumberForMobileDialing(InternationalTollFree, RegionCode.JP, false));
            Assert.Equal("+800 1234 5678",
                phoneUtil.FormatNumberForMobileDialing(InternationalTollFree, RegionCode.JP, true));

            // Test that the Australian emergency number 000 is formatted correctly.
            var auNumber = new PhoneNumber.Builder()
                .SetCountryCode(61)
                .SetNationalNumber(0L)
                .SetItalianLeadingZero(true)
                .SetNumberOfLeadingZeros(2)
                .Build();
            Assert.Equal("000", phoneUtil.FormatNumberForMobileDialing(auNumber, RegionCode.AU, false));
            Assert.Equal("", phoneUtil.FormatNumberForMobileDialing(auNumber, RegionCode.NZ, false));
        }

        [Fact]
        public void TestFormatByPattern()
        {
            var newNumFormat = new NumberFormat.Builder()
                .SetPattern("(\\d{3})(\\d{3})(\\d{4})")
                .SetFormat("($1) $2-$3").Build();
            var newNumberFormats = new List<NumberFormat> {newNumFormat};

            Assert.Equal("(650) 253-0000", phoneUtil.FormatByPattern(USNumber, PhoneNumberFormat.NATIONAL,
                newNumberFormats));
            Assert.Equal("+1 (650) 253-0000", phoneUtil.FormatByPattern(USNumber,
                PhoneNumberFormat.INTERNATIONAL,
                newNumberFormats));
            Assert.Equal("tel:+1-650-253-0000", phoneUtil.FormatByPattern(USNumber,
                PhoneNumberFormat.RFC3966, newNumberFormats));


            // $NP is set to '1' for the US. Here we check that for other NANPA countries the US rules are
            // followed.
            newNumberFormats[0] = Update(newNumberFormats[0])
                .SetNationalPrefixFormattingRule("$NP ($FG)")
                .SetFormat("$1 $2-$3").Build();
            Assert.Equal("1 (242) 365-1234",
                phoneUtil.FormatByPattern(BSNumber, PhoneNumberFormat.NATIONAL,
                newNumberFormats));
            Assert.Equal("+1 242 365-1234",
                phoneUtil.FormatByPattern(BSNumber, PhoneNumberFormat.INTERNATIONAL,
                newNumberFormats));

            newNumberFormats[0] = Update(newNumberFormats[0])
                .SetPattern("(\\d{2})(\\d{5})(\\d{3})")
                .SetFormat("$1-$2 $3").Build();

            Assert.Equal("02-36618 300",
                phoneUtil.FormatByPattern(ITNumber, PhoneNumberFormat.NATIONAL,
                newNumberFormats));
            Assert.Equal("+39 02-36618 300",
                phoneUtil.FormatByPattern(ITNumber, PhoneNumberFormat.INTERNATIONAL,
                newNumberFormats));

            newNumberFormats[0] = Update(newNumberFormats[0]).SetNationalPrefixFormattingRule("$NP$FG")
                .SetPattern("(\\d{2})(\\d{4})(\\d{4})")
                .SetFormat("$1 $2 $3").Build();
            Assert.Equal("020 7031 3000",
                phoneUtil.FormatByPattern(GBNumber, PhoneNumberFormat.NATIONAL,
                newNumberFormats));

            newNumberFormats[0] = Update(newNumberFormats[0]).SetNationalPrefixFormattingRule("($NP$FG)").Build();
            Assert.Equal("(020) 7031 3000",
                phoneUtil.FormatByPattern(GBNumber, PhoneNumberFormat.NATIONAL,
                newNumberFormats));

            newNumberFormats[0] = Update(newNumberFormats[0]).SetNationalPrefixFormattingRule("").Build();
            Assert.Equal("20 7031 3000",
                phoneUtil.FormatByPattern(GBNumber, PhoneNumberFormat.NATIONAL,
                newNumberFormats));

            Assert.Equal("+44 20 7031 3000",
                phoneUtil.FormatByPattern(GBNumber, PhoneNumberFormat.INTERNATIONAL,
                newNumberFormats));
        }

        [Fact]
        public void TestFormatE164Number()
        {
            Assert.Equal("+16502530000", phoneUtil.Format(USNumber, PhoneNumberFormat.E164));
            Assert.Equal("+4930123456", phoneUtil.Format(DENumber, PhoneNumberFormat.E164));
            Assert.Equal("+80012345678", phoneUtil.Format(InternationalTollFree, PhoneNumberFormat.E164));
        }

        [Fact]
        public void TestFormatNumberWithExtension()
        {
            var nzNumber = new PhoneNumber.Builder().MergeFrom(NZNumber).SetExtension("1234").Build();
            // Uses default extension prefix:
            Assert.Equal("03-331 6005 ext. 1234", phoneUtil.Format(nzNumber, PhoneNumberFormat.NATIONAL));
            // Uses RFC 3966 syntax.
            Assert.Equal("tel:+64-3-331-6005;ext=1234", phoneUtil.Format(nzNumber, PhoneNumberFormat.RFC3966));
            // Extension prefix overridden in the territory information for the US:
            var usNumberWithExtension = new PhoneNumber.Builder().MergeFrom(USNumber)
                .SetExtension("4567").Build();
            Assert.Equal("650 253 0000 extn. 4567", phoneUtil.Format(usNumberWithExtension,
                PhoneNumberFormat.NATIONAL));
        }

        [Fact]
        public void TestFormatInOriginalFormat()
        {
            var number1 = phoneUtil.ParseAndKeepRawInput("+442087654321", RegionCode.GB);
            Assert.Equal("+44 20 8765 4321", phoneUtil.FormatInOriginalFormat(number1, RegionCode.GB));

            var number2 = phoneUtil.ParseAndKeepRawInput("02087654321", RegionCode.GB);
            Assert.Equal("(020) 8765 4321", phoneUtil.FormatInOriginalFormat(number2, RegionCode.GB));

            var number3 = phoneUtil.ParseAndKeepRawInput("011442087654321", RegionCode.US);
            Assert.Equal("011 44 20 8765 4321", phoneUtil.FormatInOriginalFormat(number3, RegionCode.US));

            var number4 = phoneUtil.ParseAndKeepRawInput("442087654321", RegionCode.GB);
            Assert.Equal("44 20 8765 4321", phoneUtil.FormatInOriginalFormat(number4, RegionCode.GB));

            var number5 = phoneUtil.Parse("+442087654321", RegionCode.GB);
            Assert.Equal("(020) 8765 4321", phoneUtil.FormatInOriginalFormat(number5, RegionCode.GB));

            // Invalid numbers that we have a formatting pattern for should be formatted properly. Note area
            // codes starting with 7 are intentionally excluded in the test metadata for testing purposes.
            var number6 = phoneUtil.ParseAndKeepRawInput("7345678901", RegionCode.US);
            Assert.Equal("734 567 8901", phoneUtil.FormatInOriginalFormat(number6, RegionCode.US));

            // US is not a leading zero country, and the presence of the leading zero leads us to format the
            // number using raw_input.
            var number7 = phoneUtil.ParseAndKeepRawInput("0734567 8901", RegionCode.US);
            Assert.Equal("0734567 8901", phoneUtil.FormatInOriginalFormat(number7, RegionCode.US));

            // This number is valid, but we don't have a formatting pattern for it. Fall back to the raw
            // input.
            var number8 = phoneUtil.ParseAndKeepRawInput("02-4567-8900", RegionCode.KR);
            Assert.Equal("02-4567-8900", phoneUtil.FormatInOriginalFormat(number8, RegionCode.KR));

            var number9 = phoneUtil.ParseAndKeepRawInput("01180012345678", RegionCode.US);
            Assert.Equal("011 800 1234 5678", phoneUtil.FormatInOriginalFormat(number9, RegionCode.US));

            var number10 = phoneUtil.ParseAndKeepRawInput("+80012345678", RegionCode.KR);
            Assert.Equal("+800 1234 5678", phoneUtil.FormatInOriginalFormat(number10, RegionCode.KR));

            // US local numbers are formatted correctly, as we have formatting patterns for them.
            var localNumberUS = phoneUtil.ParseAndKeepRawInput("2530000", RegionCode.US);
            Assert.Equal("253 0000", phoneUtil.FormatInOriginalFormat(localNumberUS, RegionCode.US));

            var numberWithNationalPrefixUS =
            phoneUtil.ParseAndKeepRawInput("18003456789", RegionCode.US);
            Assert.Equal("1 800 345 6789",
            phoneUtil.FormatInOriginalFormat(numberWithNationalPrefixUS, RegionCode.US));

            var numberWithoutNationalPrefixGB =
            phoneUtil.ParseAndKeepRawInput("2087654321", RegionCode.GB);
            Assert.Equal("20 8765 4321",
            phoneUtil.FormatInOriginalFormat(numberWithoutNationalPrefixGB, RegionCode.GB));
            // Make sure no metadata is modified as a result of the previous function call.
            Assert.Equal("(020) 8765 4321", phoneUtil.FormatInOriginalFormat(number5, RegionCode.GB));

            var numberWithNationalPrefixMX =
            phoneUtil.ParseAndKeepRawInput("013312345678", RegionCode.MX);
            Assert.Equal("01 33 1234 5678",
            phoneUtil.FormatInOriginalFormat(numberWithNationalPrefixMX, RegionCode.MX));

            var numberWithoutNationalPrefixMX =
            phoneUtil.ParseAndKeepRawInput("3312345678", RegionCode.MX);
            Assert.Equal("33 1234 5678",
            phoneUtil.FormatInOriginalFormat(numberWithoutNationalPrefixMX, RegionCode.MX));

            var numberWithoutNationalPrefixJP =
            phoneUtil.ParseAndKeepRawInput("0777012", RegionCode.JP);
            Assert.Equal("0777012",
            phoneUtil.FormatInOriginalFormat(numberWithoutNationalPrefixJP, RegionCode.JP));

            var numberWithCarrierCodeBR =
            phoneUtil.ParseAndKeepRawInput("012 3121286979", RegionCode.BR);
            Assert.Equal("012 3121286979",
            phoneUtil.FormatInOriginalFormat(numberWithCarrierCodeBR, RegionCode.BR));

            // The default national prefix used in this case is 045. When a number with national prefix 044
            // is entered, we return the raw input as we don't want to change the number entered.
            var numberWithNationalPrefixMX1 =
            phoneUtil.ParseAndKeepRawInput("044(33)1234-5678", RegionCode.MX);
            Assert.Equal("044(33)1234-5678",
            phoneUtil.FormatInOriginalFormat(numberWithNationalPrefixMX1, RegionCode.MX));

            var numberWithNationalPrefixMX2 =
            phoneUtil.ParseAndKeepRawInput("045(33)1234-5678", RegionCode.MX);
            Assert.Equal("045 33 1234 5678",
            phoneUtil.FormatInOriginalFormat(numberWithNationalPrefixMX2, RegionCode.MX));

            // The default international prefix used in this case is 0011. When a number with international
            // prefix 0012 is entered, we return the raw input as we don't want to change the number
            // entered.
            var outOfCountryNumberFromAU1 =
            phoneUtil.ParseAndKeepRawInput("0012 16502530000", RegionCode.AU);
            Assert.Equal("0012 16502530000",
            phoneUtil.FormatInOriginalFormat(outOfCountryNumberFromAU1, RegionCode.AU));

            var outOfCountryNumberFromAU2 =
            phoneUtil.ParseAndKeepRawInput("0011 16502530000", RegionCode.AU);
            Assert.Equal("0011 1 650 253 0000",
            phoneUtil.FormatInOriginalFormat(outOfCountryNumberFromAU2, RegionCode.AU));

            // Test the star sign is not removed from or added to the original input by this method.
            var starNumber = phoneUtil.ParseAndKeepRawInput("*1234", RegionCode.JP);
            Assert.Equal("*1234", phoneUtil.FormatInOriginalFormat(starNumber, RegionCode.JP));
            var numberWithoutStar = phoneUtil.ParseAndKeepRawInput("1234", RegionCode.JP);
            Assert.Equal("1234", phoneUtil.FormatInOriginalFormat(numberWithoutStar, RegionCode.JP));
        }

        [Fact]
        public void TestIsPremiumRate()
        {
            Assert.Equal(PhoneNumberType.PREMIUM_RATE, phoneUtil.GetNumberType(USPremium));

            var premiumRateNumber = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(892123L).Build();
            Assert.Equal(PhoneNumberType.PREMIUM_RATE,
                phoneUtil.GetNumberType(premiumRateNumber));

            premiumRateNumber = Update(premiumRateNumber).SetCountryCode(44).SetNationalNumber(9187654321L).Build();
            Assert.Equal(PhoneNumberType.PREMIUM_RATE,
                phoneUtil.GetNumberType(premiumRateNumber));

            premiumRateNumber = Update(premiumRateNumber).SetCountryCode(49).SetNationalNumber(9001654321L).Build();
            Assert.Equal(PhoneNumberType.PREMIUM_RATE,
                phoneUtil.GetNumberType(premiumRateNumber));

            premiumRateNumber = Update(premiumRateNumber).SetCountryCode(49).SetNationalNumber(90091234567L).Build();
            Assert.Equal(PhoneNumberType.PREMIUM_RATE,
                phoneUtil.GetNumberType(premiumRateNumber));

            Assert.Equal(PhoneNumberType.PREMIUM_RATE,
                 phoneUtil.GetNumberType(UniversalPremiumRate));
        }

        [Fact]
        public void TestIsTollFree()
        {
            var tollFreeNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(8881234567L).Build();
            Assert.Equal(PhoneNumberType.TOLL_FREE,
                phoneUtil.GetNumberType(tollFreeNumber));

            tollFreeNumber = Update(tollFreeNumber).SetCountryCode(39).SetNationalNumber(803123L).Build();
            Assert.Equal(PhoneNumberType.TOLL_FREE,
                phoneUtil.GetNumberType(tollFreeNumber));

            tollFreeNumber = Update(tollFreeNumber).SetCountryCode(44).SetNationalNumber(8012345678L).Build();
            Assert.Equal(PhoneNumberType.TOLL_FREE,
                phoneUtil.GetNumberType(tollFreeNumber));

            tollFreeNumber = Update(tollFreeNumber).SetCountryCode(49).SetNationalNumber(8001234567L).Build();
            Assert.Equal(PhoneNumberType.TOLL_FREE,
                phoneUtil.GetNumberType(tollFreeNumber));

            Assert.Equal(PhoneNumberType.TOLL_FREE,
                 phoneUtil.GetNumberType(InternationalTollFree));
        }

        [Fact]
        public void TestIsMobile()
        {
            Assert.Equal(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(BSMobile));
            Assert.Equal(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(GBMobile));
            Assert.Equal(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(ITMobile));
            Assert.Equal(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(ARMobile));

            var mobileNumber = new PhoneNumber.Builder()
                .SetCountryCode(49).SetNationalNumber(15123456789L).Build();
            Assert.Equal(PhoneNumberType.MOBILE, phoneUtil.GetNumberType(mobileNumber));
        }

        [Fact]
        public void TestIsFixedLine()
        {
            Assert.Equal(PhoneNumberType.FIXED_LINE, phoneUtil.GetNumberType(BSNumber));
            Assert.Equal(PhoneNumberType.FIXED_LINE, phoneUtil.GetNumberType(ITNumber));
            Assert.Equal(PhoneNumberType.FIXED_LINE, phoneUtil.GetNumberType(GBNumber));
            Assert.Equal(PhoneNumberType.FIXED_LINE, phoneUtil.GetNumberType(DENumber));
        }

        [Fact]
        public void TestIsFixedLineAndMobile()
        {
            Assert.Equal(PhoneNumberType.FIXED_LINE_OR_MOBILE,
                 phoneUtil.GetNumberType(USNumber));

            var fixedLineAndMobileNumber = new PhoneNumber.Builder().
                SetCountryCode(54).SetNationalNumber(1987654321L).Build();
            Assert.Equal(PhoneNumberType.FIXED_LINE_OR_MOBILE,
                 phoneUtil.GetNumberType(fixedLineAndMobileNumber));
        }

        [Fact]
        public void TestIsSharedCost()
        {
            var gbNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(8431231234L).Build();
            Assert.Equal(PhoneNumberType.SHARED_COST, phoneUtil.GetNumberType(gbNumber));
        }

        [Fact]
        public void TestIsVoip()
        {
            var gbNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(5631231234L).Build();
            Assert.Equal(PhoneNumberType.VOIP, phoneUtil.GetNumberType(gbNumber));
        }

        [Fact]
        public void TestIsPersonalNumber()
        {
            var gbNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(7031231234L).Build();
            Assert.Equal(PhoneNumberType.PERSONAL_NUMBER,
                phoneUtil.GetNumberType(gbNumber));
        }

        [Fact]
        public void TestIsUnknown()
        {
            // Invalid numbers should be of type UNKNOWN.
            Assert.Equal(PhoneNumberType.UNKNOWN, phoneUtil.GetNumberType(USLocalNumber));
        }

        [Fact]
        public void TestIsValidNumber()
        {
            Assert.True(phoneUtil.IsValidNumber(USNumber));
            Assert.True(phoneUtil.IsValidNumber(ITNumber));
            Assert.True(phoneUtil.IsValidNumber(GBMobile));
            Assert.True(phoneUtil.IsValidNumber(InternationalTollFree));
            Assert.True(phoneUtil.IsValidNumber(UniversalPremiumRate));

            var nzNumber = new PhoneNumber.Builder().SetCountryCode(64).SetNationalNumber(21387835L).Build();
            Assert.True(phoneUtil.IsValidNumber(nzNumber));
        }

        [Fact]
        public void TestIsValidForRegion()
        {
            // This number is valid for the Bahamas, but is not a valid US number.
            Assert.True(phoneUtil.IsValidNumber(BSNumber));
            Assert.True(phoneUtil.IsValidNumberForRegion(BSNumber, RegionCode.BS));
            Assert.False(phoneUtil.IsValidNumberForRegion(BSNumber, RegionCode.US));
            var bsInvalidNumber =
                new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2421232345L).Build();
            // This number is no longer valid.
            Assert.False(phoneUtil.IsValidNumber(bsInvalidNumber));

            // La Mayotte and Reunion use 'leadingDigits' to differentiate them.
            var reNumber = new PhoneNumber.Builder()
                .SetCountryCode(262).SetNationalNumber(262123456L).Build();
            Assert.True(phoneUtil.IsValidNumber(reNumber));
            Assert.True(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.RE));
            Assert.False(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.YT));
            // Now change the number to be a number for La Mayotte.
            reNumber = Update(reNumber).SetNationalNumber(269601234L).Build();
            Assert.True(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.YT));
            Assert.False(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.RE));
            // This number is no longer valid for La Reunion.
            reNumber = Update(reNumber).SetNationalNumber(269123456L).Build();
            Assert.False(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.YT));
            Assert.False(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.RE));
            Assert.False(phoneUtil.IsValidNumber(reNumber));
            // However, it should be recognised as from La Mayotte, since it is valid for this region.
            Assert.Equal(RegionCode.YT, phoneUtil.GetRegionCodeForNumber(reNumber));
            // This number is valid in both places.
            reNumber = Update(reNumber).SetNationalNumber(800123456L).Build();
            Assert.True(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.YT));
            Assert.True(phoneUtil.IsValidNumberForRegion(reNumber, RegionCode.RE));
            Assert.True(phoneUtil.IsValidNumberForRegion(InternationalTollFree, RegionCode.UN001));
            Assert.False(phoneUtil.IsValidNumberForRegion(InternationalTollFree, RegionCode.US));

            Assert.False(phoneUtil.IsValidNumberForRegion(InternationalTollFree, RegionCode.ZZ));

            // Invalid country calling codes.
            var invalidNumber = new PhoneNumber.Builder().SetCountryCode(3923).SetNationalNumber(2366L).Build();
            Assert.False(phoneUtil.IsValidNumberForRegion(invalidNumber, RegionCode.ZZ));
            Assert.False(phoneUtil.IsValidNumberForRegion(invalidNumber, RegionCode.UN001));
            invalidNumber = new PhoneNumber.Builder().SetCountryCode(0).Build();
            Assert.False(phoneUtil.IsValidNumberForRegion(invalidNumber, RegionCode.UN001));
            Assert.False(phoneUtil.IsValidNumberForRegion(invalidNumber, RegionCode.ZZ));
        }

        [Fact]
        public void TestIsNotValidNumber()
        {
            Assert.False(phoneUtil.IsValidNumber(USLocalNumber));

            var invalidNumber = new PhoneNumber.Builder()
                .SetCountryCode(49).SetNationalNumber(1234L).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));

            invalidNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(3316005L).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));

            // Invalid country calling codes.
            invalidNumber = new PhoneNumber.Builder().SetCountryCode(3923).SetNationalNumber(2366L).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));
            invalidNumber = new PhoneNumber.Builder().SetCountryCode(0).SetNationalNumber(2366L).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));

            Assert.False(phoneUtil.IsValidNumber(InternationalTollFreeTooLong));
        }

        [Fact]
        public void TestGetRegionCodeForCountryCode()
        {
            Assert.Equal(RegionCode.US, phoneUtil.GetRegionCodeForCountryCode(1));
            Assert.Equal(RegionCode.GB, phoneUtil.GetRegionCodeForCountryCode(44));
            Assert.Equal(RegionCode.DE, phoneUtil.GetRegionCodeForCountryCode(49));
            Assert.Equal(RegionCode.UN001, phoneUtil.GetRegionCodeForCountryCode(800));
            Assert.Equal(RegionCode.UN001, phoneUtil.GetRegionCodeForCountryCode(979));
        }

        [Fact]
        public void TestGetRegionCodeForNumber()
        {
            Assert.Equal(RegionCode.BS, phoneUtil.GetRegionCodeForNumber(BSNumber));
            Assert.Equal(RegionCode.US, phoneUtil.GetRegionCodeForNumber(USNumber));
            Assert.Equal(RegionCode.GB, phoneUtil.GetRegionCodeForNumber(GBMobile));
            Assert.Equal(RegionCode.UN001, phoneUtil.GetRegionCodeForNumber(InternationalTollFree));
            Assert.Equal(RegionCode.UN001, phoneUtil.GetRegionCodeForNumber(UniversalPremiumRate));
        }

        [Fact]
        public void TestGetCountryCodeForRegion()
        {
            Assert.Equal(1, phoneUtil.GetCountryCodeForRegion(RegionCode.US));
            Assert.Equal(64, phoneUtil.GetCountryCodeForRegion(RegionCode.NZ));
            Assert.Equal(0, phoneUtil.GetCountryCodeForRegion(null));
            Assert.Equal(0, phoneUtil.GetCountryCodeForRegion(RegionCode.ZZ));
            Assert.Equal(0, phoneUtil.GetCountryCodeForRegion(RegionCode.UN001));
            // CS is already deprecated so the library doesn't support it.
            Assert.Equal(0, phoneUtil.GetCountryCodeForRegion(RegionCode.CS));
        }

        [Fact]
        public void TestGetNationalDiallingPrefixForRegion()
        {
            Assert.Equal("1", phoneUtil.GetNddPrefixForRegion(RegionCode.US, false));
            // Test non-main country to see it gets the national dialling prefix for the main country with
            // that country calling code.
            Assert.Equal("1", phoneUtil.GetNddPrefixForRegion(RegionCode.BS, false));
            Assert.Equal("0", phoneUtil.GetNddPrefixForRegion(RegionCode.NZ, false));
            // Test case with non digit in the national prefix.
            Assert.Equal("0~0", phoneUtil.GetNddPrefixForRegion(RegionCode.AO, false));
            Assert.Equal("00", phoneUtil.GetNddPrefixForRegion(RegionCode.AO, true));
            // Test cases with invalid regions.
            Assert.Null(phoneUtil.GetNddPrefixForRegion(null, false));
            Assert.Null(phoneUtil.GetNddPrefixForRegion(RegionCode.ZZ, false));
            Assert.Null(phoneUtil.GetNddPrefixForRegion(RegionCode.UN001, false));
            // CS is already deprecated so the library doesn't support it.
            Assert.Null(phoneUtil.GetNddPrefixForRegion(RegionCode.CS, false));
        }

        [Fact]
        public void TestIsNANPACountry()
        {
            Assert.True(phoneUtil.IsNANPACountry(RegionCode.US));
            Assert.True(phoneUtil.IsNANPACountry(RegionCode.BS));
            Assert.False(phoneUtil.IsNANPACountry(RegionCode.DE));
            Assert.False(phoneUtil.IsNANPACountry(RegionCode.ZZ));
            Assert.False(phoneUtil.IsNANPACountry(RegionCode.UN001));
            Assert.False(phoneUtil.IsNANPACountry(null));
        }

        [Fact]
        public void TestIsPossibleNumber()
        {
            Assert.True(phoneUtil.IsPossibleNumber(GBNumber));
            Assert.True(phoneUtil.IsPossibleNumber(InternationalTollFree));

            Assert.True(phoneUtil.IsPossibleNumber("+1 650 253 0000", RegionCode.US));
            Assert.True(phoneUtil.IsPossibleNumber("+1 650 GOO OGLE", RegionCode.US));
            Assert.True(phoneUtil.IsPossibleNumber("(650) 253-0000", RegionCode.US));
            Assert.True(phoneUtil.IsPossibleNumber("+1 650 253 0000", RegionCode.GB));
            Assert.True(phoneUtil.IsPossibleNumber("+44 20 7031 3000", RegionCode.GB));
            Assert.True(phoneUtil.IsPossibleNumber("(020) 7031 3000", RegionCode.GB));
            Assert.True(phoneUtil.IsPossibleNumber("3331 6005", RegionCode.NZ));
            Assert.True(phoneUtil.IsPossibleNumber("+800 1234 5678", RegionCode.UN001));
        }

        [Fact]
        public void TestIsPossibleNumberWithReason()
        {
            // National numbers for country calling code +1 that are within 7 to 10 digits are possible.
            Assert.Equal(PhoneNumberUtil.ValidationResult.IS_POSSIBLE,
            phoneUtil.IsPossibleNumberWithReason(USNumber));

            Assert.Equal(PhoneNumberUtil.ValidationResult.IS_POSSIBLE_LOCAL_ONLY,
            phoneUtil.IsPossibleNumberWithReason(USLocalNumber));

            Assert.Equal(PhoneNumberUtil.ValidationResult.TOO_LONG,
            phoneUtil.IsPossibleNumberWithReason(USLongNumber));

            var number = new PhoneNumber.Builder().SetCountryCode(0).SetNationalNumber(2530000L).Build();
            Assert.Equal(PhoneNumberUtil.ValidationResult.INVALID_COUNTRY_CODE,
            phoneUtil.IsPossibleNumberWithReason(number));

            number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(253000L).Build();
            Assert.Equal(PhoneNumberUtil.ValidationResult.TOO_SHORT,
            phoneUtil.IsPossibleNumberWithReason(number));

            number = new PhoneNumber.Builder().SetCountryCode(65).SetNationalNumber(1234567890L).Build();
            Assert.Equal(PhoneNumberUtil.ValidationResult.IS_POSSIBLE,
            phoneUtil.IsPossibleNumberWithReason(number));

            Assert.Equal(PhoneNumberUtil.ValidationResult.TOO_LONG,
                 phoneUtil.IsPossibleNumberWithReason(InternationalTollFreeTooLong));

            // Try with number that we don't have metadata for.
            var adNumber = new PhoneNumber.Builder().SetCountryCode(376).SetNationalNumber(12345L).Build();
            Assert.Equal(PhoneNumberUtil.ValidationResult.TOO_SHORT,
                phoneUtil.IsPossibleNumberWithReason(adNumber));
            adNumber = Update(adNumber).SetCountryCode(376).SetNationalNumber(1L).Build();
            Assert.Equal(PhoneNumberUtil.ValidationResult.TOO_SHORT,
                phoneUtil.IsPossibleNumberWithReason(adNumber));
            adNumber = Update(adNumber).SetCountryCode(376).SetNationalNumber(12345678901234567L).Build();
            Assert.Equal(PhoneNumberUtil.ValidationResult.TOO_LONG,
                phoneUtil.IsPossibleNumberWithReason(adNumber));
        }

        [Fact]
        public void TestIsNotPossibleNumber()
        {
            Assert.False(phoneUtil.IsPossibleNumber(USLongNumber));
            Assert.False(phoneUtil.IsPossibleNumber(InternationalTollFreeTooLong));

            var number = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(253000L).Build();
            Assert.False(phoneUtil.IsPossibleNumber(number));

            number = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(300L).Build();
            Assert.False(phoneUtil.IsPossibleNumber(number));

            Assert.False(phoneUtil.IsPossibleNumber("+1 650 253 00000", RegionCode.US));
            Assert.False(phoneUtil.IsPossibleNumber("(650) 253-00000", RegionCode.US));
            Assert.False(phoneUtil.IsPossibleNumber("I want a Pizza", RegionCode.US));
            Assert.False(phoneUtil.IsPossibleNumber("253-000", RegionCode.US));
            Assert.False(phoneUtil.IsPossibleNumber("1 3000", RegionCode.GB));
            Assert.False(phoneUtil.IsPossibleNumber("+44 300", RegionCode.GB));
            Assert.False(phoneUtil.IsPossibleNumber("+800 1234 5678 9", RegionCode.UN001));
        }

        [Fact]
        public void TestTruncateTooLongNumber()
        {
            // GB number 080 1234 5678, but entered with 4 extra digits at the end.
            var tooLongNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(80123456780123L);
            var validNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(8012345678L);
            Assert.True(phoneUtil.TruncateTooLongNumber(tooLongNumber));
            AreEqual(validNumber, tooLongNumber);

            // IT number 022 3456 7890, but entered with 3 extra digits at the end.
            tooLongNumber = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(2234567890123L).SetItalianLeadingZero(true);
            validNumber = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(2234567890L).SetItalianLeadingZero(true);
            Assert.True(phoneUtil.TruncateTooLongNumber(tooLongNumber));
            AreEqual(validNumber, tooLongNumber);

            // US number 650-253-0000, but entered with one additional digit at the end.
            tooLongNumber = new PhoneNumber.Builder()
                .MergeFrom(USLongNumber);
            Assert.True(phoneUtil.TruncateTooLongNumber(tooLongNumber));
            Assert.Equal(USNumber, tooLongNumber.Build());

            tooLongNumber = new PhoneNumber.Builder()
                .MergeFrom(InternationalTollFreeTooLong);
            Assert.True(phoneUtil.TruncateTooLongNumber(tooLongNumber));
            Assert.Equal(InternationalTollFree, tooLongNumber.Build());

            // Tests what happens when a valid number is passed in.
            var validNumberCopy = validNumber.Clone();
            Assert.True(phoneUtil.TruncateTooLongNumber(validNumber));
            // Tests the number is not modified.
            AreEqual(validNumberCopy, validNumber);

            // Tests what happens when a number with invalid prefix is passed in.
            // The test metadata says US numbers cannot have prefix 240.
            var numberWithInvalidPrefix = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(2401234567L);
            var invalidNumberCopy = numberWithInvalidPrefix.Clone();
            Assert.False(phoneUtil.TruncateTooLongNumber(numberWithInvalidPrefix));
            // Tests the number is not modified.
            AreEqual(invalidNumberCopy, numberWithInvalidPrefix);

            // Tests what happens when a too short number is passed in.
            var tooShortNumber = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(1234L);
            var tooShortNumberCopy = tooShortNumber.Clone();
            Assert.False(phoneUtil.TruncateTooLongNumber(tooShortNumber));
            // Tests the number is not modified.
            AreEqual(tooShortNumberCopy, tooShortNumber);
        }

        [Fact]
        public void TestIsViablePhoneNumber()
        {
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("1"));
            // Only one or two digits before strange non-possible punctuation.
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("1+1+1"));
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("80+0"));
             // Two digits is viable.
            Assert.True(PhoneNumberUtil.IsViablePhoneNumber("00"));
            Assert.True(PhoneNumberUtil.IsViablePhoneNumber("111"));
            // Alpha numbers.
            Assert.True(PhoneNumberUtil.IsViablePhoneNumber("0800-4-pizza"));
            Assert.True(PhoneNumberUtil.IsViablePhoneNumber("0800-4-PIZZA"));
            // We need at least three digits before any alpha characters.
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("08-PIZZA"));
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("8-PIZZA"));
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("12. March"));
        }

        [Fact]
        public void TestIsViablePhoneNumberNonAscii()
        {
            // Only one or two digits before possible punctuation followed by more digits.
            Assert.True(PhoneNumberUtil.IsViablePhoneNumber("1\u300034"));
            Assert.False(PhoneNumberUtil.IsViablePhoneNumber("1\u30003+4"));
            // Unicode variants of possible starting character and other allowed punctuation/digits.
            Assert.True(PhoneNumberUtil.IsViablePhoneNumber("\uFF081\uFF09\u30003456789"));
            // Testing a leading + is okay.
            Assert.True(PhoneNumberUtil.IsViablePhoneNumber("+1\uFF09\u30003456789"));
        }

        [Fact]
        public void TestExtractPossibleNumber()
        {
            // Removes preceding funky punctuation and letters but leaves the rest untouched.
            Assert.Equal("0800-345-600", PhoneNumberUtil.ExtractPossibleNumber("Tel:0800-345-600"));
            Assert.Equal("0800 FOR PIZZA", PhoneNumberUtil.ExtractPossibleNumber("Tel:0800 FOR PIZZA"));
            // Should not remove plus sign
            Assert.Equal("+800-345-600", PhoneNumberUtil.ExtractPossibleNumber("Tel:+800-345-600"));
            // Should recognise wide digits as possible start values.
            Assert.Equal("\uFF10\uFF12\uFF13",
            PhoneNumberUtil.ExtractPossibleNumber("\uFF10\uFF12\uFF13"));
            // Dashes are not possible start values and should be removed.
            Assert.Equal("\uFF11\uFF12\uFF13",
            PhoneNumberUtil.ExtractPossibleNumber("Num-\uFF11\uFF12\uFF13"));
            // If not possible number present, return empty string.
            Assert.Equal("", PhoneNumberUtil.ExtractPossibleNumber("Num-...."));
            // Leading brackets are stripped - these are not used when parsing.
            Assert.Equal("650) 253-0000", PhoneNumberUtil.ExtractPossibleNumber("(650) 253-0000"));

            // Trailing non-alpha-numeric characters should be removed.
            Assert.Equal("650) 253-0000", PhoneNumberUtil.ExtractPossibleNumber("(650) 253-0000..- .."));
            Assert.Equal("650) 253-0000", PhoneNumberUtil.ExtractPossibleNumber("(650) 253-0000."));
            // This case has a trailing RTL char.
            Assert.Equal("650) 253-0000", PhoneNumberUtil.ExtractPossibleNumber("(650) 253-0000\u200F"));
        }

        [Fact]
        public void TestMaybeStripNationalPrefix()
        {
            var metadata = new PhoneMetadata.Builder()
                .SetNationalPrefixForParsing("34")
                .SetGeneralDesc(new PhoneNumberDesc.Builder().SetNationalNumberPattern("\\d{4,8}").Build())
                .BuildPartial();
            var numberToStrip = new StringBuilder("34356778");
            var strippedNumber = "356778";
            Assert.True(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.Equal(strippedNumber, numberToStrip.ToString());
            // Retry stripping - now the number should not start with the national prefix, so no more
            // stripping should occur.
            Assert.False(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.Equal(strippedNumber, numberToStrip.ToString());
            // Some countries have no national prefix. Repeat test with none specified.
            metadata = Update(metadata).SetNationalPrefixForParsing("").BuildPartial();
            Assert.False(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.Equal(strippedNumber, numberToStrip.ToString());
            // If the resultant number doesn't match the national rule, it shouldn't be stripped.
            metadata = Update(metadata).SetNationalPrefixForParsing("3").BuildPartial();
            numberToStrip = new StringBuilder("3123");
            strippedNumber = "3123";
            Assert.False(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.Equal(strippedNumber, numberToStrip.ToString());
            // Test extracting carrier selection code.
            metadata = Update(metadata).SetNationalPrefixForParsing("0(81)?").BuildPartial();
            numberToStrip = new StringBuilder("08122123456");
            strippedNumber = "22123456";
            var carrierCode = new StringBuilder();
            Assert.True(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(
                numberToStrip, metadata, carrierCode));
            Assert.Equal("81", carrierCode.ToString());
            Assert.Equal(strippedNumber, numberToStrip.ToString());
            // If there was a transform rule, check it was applied.
            // Note that a capturing group is present here.
            metadata = Update(metadata).SetNationalPrefixTransformRule("5${1}5")
                .SetNationalPrefixForParsing("0(\\d{2})").BuildPartial();
            numberToStrip = new StringBuilder("031123");
            var transformedNumber = "5315123";
            Assert.True(phoneUtil.MaybeStripNationalPrefixAndCarrierCode(numberToStrip, metadata, null));
            Assert.Equal(transformedNumber, numberToStrip.ToString());
        }

        [Fact]
        public void TestMaybeStripInternationalPrefix()
        {
            var internationalPrefix = "00[39]";
            var numberToStrip = new StringBuilder("0034567700-3898003");
            // Note the dash is removed as part of the normalization.
            var strippedNumber = new StringBuilder("45677003898003");
            Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));
            Assert.Equal(strippedNumber.ToString(), numberToStrip.ToString());
            // Now the number no longer starts with an IDD prefix, so it should now report
            // FROM_DEFAULT_COUNTRY.
            Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));

            numberToStrip = new StringBuilder("00945677003898003");
            Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));
            Assert.Equal(strippedNumber.ToString(), numberToStrip.ToString());
            // Test it works when the international prefix is broken up by spaces.
            numberToStrip = new StringBuilder("00 9 45677003898003");
            Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));
            Assert.Equal(strippedNumber.ToString(), numberToStrip.ToString());
            // Now the number no longer starts with an IDD prefix, so it should now report
            // FROM_DEFAULT_COUNTRY.
            Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY,
                phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                    internationalPrefix));

            // Test the + symbol is also recognised and stripped.
            numberToStrip = new StringBuilder("+45677003898003");
            strippedNumber = new StringBuilder("45677003898003");
            Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN,
            phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                                                         internationalPrefix));
            Assert.Equal(strippedNumber.ToString(), numberToStrip.ToString());

            // If the number afterwards is a zero, we should not strip this - no country calling code begins
            // with 0.
            numberToStrip = new StringBuilder("0090112-3123");
            strippedNumber = new StringBuilder("00901123123");
            Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY,
            phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                                                         internationalPrefix));
            Assert.Equal(strippedNumber.ToString(), numberToStrip.ToString());
            // Here the 0 is separated by a space from the IDD.
            numberToStrip = new StringBuilder("009 0-112-3123");
            Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY,
            phoneUtil.MaybeStripInternationalPrefixAndNormalize(numberToStrip,
                                                         internationalPrefix));
        }

        [Fact]
        public void TestMaybeExtractCountryCode()
        {
            var number = new PhoneNumber.Builder();
            var metadata = phoneUtil.GetMetadataForRegion(RegionCode.US);
            // Note that for the US, the IDD is 011.
            try
            {
                var phoneNumber = "011112-3456789";
                var strippedNumber = "123456789";
                var countryCallingCode = 1;
                var numberToFill = new StringBuilder();
                Assert.Equal(countryCallingCode,
                    phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true, number));
                Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD, number.CountryCodeSource);
                // Should strip and normalize national significant number.
                Assert.Equal(strippedNumber,
                    numberToFill.ToString());
            }
            catch (NumberParseException)
            {
                Assert.True(false);
            }
            number = new();
            try
            {
                var phoneNumber = "+6423456789";
                var countryCallingCode = 64;
                var numberToFill = new StringBuilder();
                Assert.Equal(countryCallingCode,
                    phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true, number));
                Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN, number.CountryCodeSource);
            }
            catch (NumberParseException)
            {
                Assert.True(false);
            }
            number = new();
            try
            {
                var phoneNumber = "+80012345678";
                var countryCallingCode = 800;
                var numberToFill = new StringBuilder();
                Assert.Equal(countryCallingCode,
                   phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true, number));
                Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN, number.CountryCodeSource);
            }
            catch (NumberParseException)
            {
                Assert.True(false);
            }
            number = new();
            try
            {
                var phoneNumber = "2345-6789";
                var numberToFill = new StringBuilder();
                Assert.Equal(0,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true, number));
                Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY, number.CountryCodeSource);
            }
            catch (NumberParseException)
            {
                Assert.True(false);
            }
            number = new();
            try
            {
                var phoneNumber = "0119991123456789";
                var numberToFill = new StringBuilder();
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true, number);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected.
                Assert.Equal(ErrorType.INVALID_COUNTRY_CODE,
                e.ErrorType);
            }
            number = new();
            try
            {
                var phoneNumber = "(1 610) 619 4466";
                var countryCallingCode = 1;
                var numberToFill = new StringBuilder();
                Assert.Equal(countryCallingCode,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true,
                number));
                Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN,
                number.CountryCodeSource);
            }
            catch (NumberParseException)
            {
                Assert.True(false);
            }
            number = new();
            try
            {
                var phoneNumber = "(1 610) 619 4466";
                var countryCallingCode = 1;
                var numberToFill = new StringBuilder();
                Assert.Equal(countryCallingCode,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, false,
                number));
                Assert.False(number.HasCountryCodeSource, "Should not contain CountryCodeSource.");
            }
            catch (NumberParseException)
            {
                Assert.True(false);
            }
            number = new();
            try
            {
                var phoneNumber = "(1 610) 619 446";
                var numberToFill = new StringBuilder();
                Assert.Equal(0,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, false,
                number));
                Assert.False(number.HasCountryCodeSource, "Should not contain CountryCodeSource.");
            }
            catch (NumberParseException)
            {
                Assert.True(false);
            }
            number = new();
            try
            {
                var phoneNumber = "(1 610) 619";
                var numberToFill = new StringBuilder();
                Assert.Equal(0,
                phoneUtil.MaybeExtractCountryCode(phoneNumber, metadata, numberToFill, true,
                number));
                Assert.Equal(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY, number.CountryCodeSource);
            }
            catch (NumberParseException)
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void TestParseNationalNumber()
        {
            // National prefix attached.
            Assert.Equal(NZNumber, phoneUtil.Parse("033316005", RegionCode.NZ));
            Assert.Equal(NZNumber, phoneUtil.Parse("33316005", RegionCode.NZ));
            // National prefix attached and some formatting present.
            Assert.Equal(NZNumber, phoneUtil.Parse("03-331 6005", RegionCode.NZ));
            Assert.Equal(NZNumber, phoneUtil.Parse("03 331 6005", RegionCode.NZ));
            // Test parsing RFC3966 format with a phone context.
            Assert.Equal(NZNumber, phoneUtil.Parse("tel:03-331-6005;phone-context=+64", RegionCode.NZ));
            Assert.Equal(NZNumber, phoneUtil.Parse("tel:331-6005;phone-context=+64-3", RegionCode.NZ));
            Assert.Equal(NZNumber, phoneUtil.Parse("tel:331-6005;phone-context=+64-3", RegionCode.US));

            // Test parsing RFC3966 format with optional user-defined parameters. The parameters will appear
            // after the context if present.
            Assert.Equal(NZNumber, phoneUtil.Parse("tel:03-331-6005;phone-context=+64;a=%A1",
                RegionCode.NZ));
            // Test parsing RFC3966 with an ISDN subaddress.
            Assert.Equal(NZNumber, phoneUtil.Parse("tel:03-331-6005;isub=12345;phone-context=+64",
                RegionCode.NZ));
            Assert.Equal(NZNumber, phoneUtil.Parse("tel:+64-3-331-6005;isub=12345", RegionCode.NZ));

            // Testing international prefixes.
            // Should strip country calling code.
            Assert.Equal(NZNumber, phoneUtil.Parse("0064 3 331 6005", RegionCode.NZ));
            // Try again, but this time we have an international number with Region Code US. It should
            // recognise the country calling code and parse accordingly.
            Assert.Equal(NZNumber, phoneUtil.Parse("01164 3 331 6005", RegionCode.US));
            Assert.Equal(NZNumber, phoneUtil.Parse("+64 3 331 6005", RegionCode.US));

            // We should ignore the leading plus here, since it is not followed by a valid country code but
            // instead is followed by the IDD for the US.
            Assert.Equal(NZNumber, phoneUtil.Parse("+01164 3 331 6005", RegionCode.US));
            Assert.Equal(NZNumber, phoneUtil.Parse("+0064 3 331 6005", RegionCode.NZ));
            Assert.Equal(NZNumber, phoneUtil.Parse("+ 00 64 3 331 6005", RegionCode.NZ));

            Assert.Equal(USLocalNumber,
                phoneUtil.Parse("tel:253-0000;phone-context=www.google.com", RegionCode.US));
            Assert.Equal(USLocalNumber,
                phoneUtil.Parse("tel:253-0000;isub=12345;phone-context=www.google.com", RegionCode.US));
            // This is invalid because no "+" sign is present as part of phone-context. The phone context
            // is simply ignored in this case just as if it contains a domain.
            Assert.Equal(USLocalNumber,
                phoneUtil.Parse("tel:2530000;isub=12345;phone-context=1-650", RegionCode.US));
            Assert.Equal(USLocalNumber,
                phoneUtil.Parse("tel:2530000;isub=12345;phone-context=1234.com", RegionCode.US));

            var nzNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(64123456L).Build();
            Assert.Equal(nzNumber, phoneUtil.Parse("64(0)64123456", RegionCode.NZ));
            // Check that using a "/" is fine in a phone number.
            Assert.Equal(DENumber, phoneUtil.Parse("301/23456", RegionCode.DE));

            // Check it doesn't use the '1' as a country calling code when parsing if the phone number was
            // already possible.
            var usNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(1234567890L).Build();
            Assert.Equal(usNumber, phoneUtil.Parse("123-456-7890", RegionCode.US));

            // Test star numbers. Although this is not strictly valid, we would like to make sure we can
            // parse the output we produce when formatting the number.
            Assert.Equal(JPStarNumber, phoneUtil.Parse("+81 *2345", RegionCode.JP));

            var shortNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(12L).Build();
            Assert.Equal(shortNumber, phoneUtil.Parse("12", RegionCode.NZ));

            // Test for short-code with leading zero for a country which has 0 as national prefix. Ensure
            // it's not interpreted as national prefix if the remaining number length is local-only in
            // terms of length. Example: In GB, length 6-7 are only possible local-only.
            shortNumber = new PhoneNumber.Builder().SetCountryCode(44).SetNationalNumber(123456)
            .SetItalianLeadingZero(true).Build();
            Assert.Equal(shortNumber, phoneUtil.Parse("0123456", RegionCode.GB));
        }

        [Fact]
        public void TestParseNumberWithAlphaCharacters()
        {
            // Test case with alpha characters.
            var tollfreeNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(800332005L).Build();
            Assert.Equal(tollfreeNumber, phoneUtil.Parse("0800 DDA 005", RegionCode.NZ));
            var premiumNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(9003326005L).Build();
            Assert.Equal(premiumNumber, phoneUtil.Parse("0900 DDA 6005", RegionCode.NZ));
            // Not enough alpha characters for them to be considered intentional, so they are stripped.
            Assert.Equal(premiumNumber, phoneUtil.Parse("0900 332 6005a", RegionCode.NZ));
            Assert.Equal(premiumNumber, phoneUtil.Parse("0900 332 600a5", RegionCode.NZ));
            Assert.Equal(premiumNumber, phoneUtil.Parse("0900 332 600A5", RegionCode.NZ));
            Assert.Equal(premiumNumber, phoneUtil.Parse("0900 a332 600A5", RegionCode.NZ));
        }

        [Fact]
        public void TestParseMaliciousInput()
        {
            // Lots of leading + signs before the possible number.
            var maliciousNumber = new StringBuilder(6000);
            for(var i = 0; i < 6000; i++)
                maliciousNumber.Append('+');
            maliciousNumber.Append("12222-33-244 extensioB 343+");
            try
            {
                phoneUtil.Parse(maliciousNumber.ToString(), RegionCode.US);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.TOO_LONG,
                           e.ErrorType);
            }
            var maliciousNumberWithAlmostExt = new StringBuilder(6000);
            for(var i = 0; i < 350; i++)
                maliciousNumberWithAlmostExt.Append("200");
            maliciousNumberWithAlmostExt.Append(" extensiOB 345");
            try
            {
                phoneUtil.Parse(maliciousNumberWithAlmostExt.ToString(), RegionCode.US);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.TOO_LONG,
                           e.ErrorType);
            }
        }

        [Fact]
        public void TestParseWithInternationalPrefixes()
        {
            Assert.Equal(USNumber, phoneUtil.Parse("+1 (650) 253-0000", RegionCode.NZ));
            Assert.Equal(InternationalTollFree, phoneUtil.Parse("011 800 1234 5678", RegionCode.US));
            Assert.Equal(USNumber, phoneUtil.Parse("1-650-253-0000", RegionCode.US));
            // Calling the US number from Singapore by using different service providers
            // 1st test: calling using SingTel IDD service (IDD is 001)
            Assert.Equal(USNumber, phoneUtil.Parse("0011-650-253-0000", RegionCode.SG));
            // 2nd test: calling using StarHub IDD service (IDD is 008)
            Assert.Equal(USNumber, phoneUtil.Parse("0081-650-253-0000", RegionCode.SG));
            // 3rd test: calling using SingTel V019 service (IDD is 019)
            Assert.Equal(USNumber, phoneUtil.Parse("0191-650-253-0000", RegionCode.SG));
            // Calling the US number from Poland
            Assert.Equal(USNumber, phoneUtil.Parse("0~01-650-253-0000", RegionCode.PL));
            // Using "++" at the start.
            Assert.Equal(USNumber, phoneUtil.Parse("++1 (650) 253-0000", RegionCode.PL));
            // Using a very strange decimal digit range (Mongolian digits).
            Assert.Equal(USNumber, phoneUtil.Parse("\u1811 \u1816\u1815\u1810 " +
                "\u1812\u1815\u1813 \u1810\u1810\u1810\u1810",
                RegionCode.US));
        }

        [Fact]
        public void TestParseNonAscii()
        {
            // Using a full-width plus sign.
            Assert.Equal(USNumber, phoneUtil.Parse("\uFF0B1 (650) 253-0000", RegionCode.SG));
            // Using a soft hyphen U+00AD.
            Assert.Equal(USNumber, phoneUtil.Parse("1 (650) 253\u00AD-0000", RegionCode.US));
            // The whole number, including punctuation, is here represented in full-width form.
            Assert.Equal(USNumber, phoneUtil.Parse("\uFF0B\uFF11\u3000\uFF08\uFF16\uFF15\uFF10\uFF09" +
                                                "\u3000\uFF12\uFF15\uFF13\uFF0D\uFF10\uFF10\uFF10" +
                                                "\uFF10",
                                                RegionCode.SG));
            // Using U+30FC dash instead.
            Assert.Equal(USNumber, phoneUtil.Parse("\uFF0B\uFF11\u3000\uFF08\uFF16\uFF15\uFF10\uFF09" +
                                                "\u3000\uFF12\uFF15\uFF13\u30FC\uFF10\uFF10\uFF10" +
                                                "\uFF10",
                                                RegionCode.SG));
        }

        [Fact]
        public void TestParseWithLeadingZero()
        {
            Assert.Equal(ITNumber, phoneUtil.Parse("+39 02-36618 300", RegionCode.NZ));
            Assert.Equal(ITNumber, phoneUtil.Parse("02-36618 300", RegionCode.IT));

            Assert.Equal(ITMobile, phoneUtil.Parse("345 678 901", RegionCode.IT));
        }

        [Fact]
        public void TestParseNationalNumberArgentina()
        {
            // Test parsing mobile numbers of Argentina.
            var arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(93435551212L).Build();
            Assert.Equal(arNumber, phoneUtil.Parse("+54 9 343 555 1212", RegionCode.AR));
            Assert.Equal(arNumber, phoneUtil.Parse("0343 15 555 1212", RegionCode.AR));

            arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(93715654320L).Build();
            Assert.Equal(arNumber, phoneUtil.Parse("+54 9 3715 65 4320", RegionCode.AR));
            Assert.Equal(arNumber, phoneUtil.Parse("03715 15 65 4320", RegionCode.AR));
            Assert.Equal(ARMobile, phoneUtil.Parse("911 876 54321", RegionCode.AR));

            // Test parsing fixed-line numbers of Argentina.
            Assert.Equal(ARNumber, phoneUtil.Parse("+54 11 8765 4321", RegionCode.AR));
            Assert.Equal(ARNumber, phoneUtil.Parse("011 8765 4321", RegionCode.AR));

            arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(3715654321L).Build();
            Assert.Equal(arNumber, phoneUtil.Parse("+54 3715 65 4321", RegionCode.AR));
            Assert.Equal(arNumber, phoneUtil.Parse("03715 65 4321", RegionCode.AR));

            arNumber = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(2312340000L).Build();
            Assert.Equal(arNumber, phoneUtil.Parse("+54 23 1234 0000", RegionCode.AR));
            Assert.Equal(arNumber, phoneUtil.Parse("023 1234 0000", RegionCode.AR));
        }

        [Fact]
        public void TestParseWithXInNumber()
        {
            // Test that having an 'x' in the phone number at the start is ok and that it just gets removed.
            Assert.Equal(ARNumber, phoneUtil.Parse("01187654321", RegionCode.AR));
            Assert.Equal(ARNumber, phoneUtil.Parse("(0) 1187654321", RegionCode.AR));
            Assert.Equal(ARNumber, phoneUtil.Parse("0 1187654321", RegionCode.AR));
            Assert.Equal(ARNumber, phoneUtil.Parse("(0xx) 1187654321", RegionCode.AR));
            var arFromUs = new PhoneNumber.Builder()
                .SetCountryCode(54).SetNationalNumber(81429712L).Build();
            // This test is intentionally constructed such that the number of digit after xx is larger than
            // 7, so that the number won't be mistakenly treated as an extension, as we allow extensions up
            // to 7 digits. This assumption is okay for now as all the countries where a carrier selection
            // code is written in the form of xx have a national significant number of length larger than 7.
            Assert.Equal(arFromUs, phoneUtil.Parse("011xx5481429712", RegionCode.US));
        }

        [Fact]
        public void TestParseNumbersMexico()
        {
            // Test parsing fixed-line numbers of Mexico.
            var mxNumber = new PhoneNumber.Builder()
                .SetCountryCode(52).SetNationalNumber(4499780001L).Build();
            Assert.Equal(mxNumber, phoneUtil.Parse("+52 (449)978-0001", RegionCode.MX));
            Assert.Equal(mxNumber, phoneUtil.Parse("01 (449)978-0001", RegionCode.MX));
            Assert.Equal(mxNumber, phoneUtil.Parse("(449)978-0001", RegionCode.MX));

            // Test parsing mobile numbers of Mexico.
            mxNumber = new PhoneNumber.Builder()
                .SetCountryCode(52).SetNationalNumber(13312345678L).Build();
            Assert.Equal(mxNumber, phoneUtil.Parse("+52 1 33 1234-5678", RegionCode.MX));
            Assert.Equal(mxNumber, phoneUtil.Parse("044 (33) 1234-5678", RegionCode.MX));
            Assert.Equal(mxNumber, phoneUtil.Parse("045 33 1234-5678", RegionCode.MX));
        }

        [Fact]
        public void TestFailedParseOnInvalidNumbers()
        {
            try
            {
                var sentencePhoneNumber = "This is not a phone number";
                phoneUtil.Parse(sentencePhoneNumber, RegionCode.NZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            try
            {
                var sentencePhoneNumber = "1 Still not a number";
                phoneUtil.Parse(sentencePhoneNumber, RegionCode.NZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            try
            {
                var sentencePhoneNumber = "1 MICROSOFT";
                phoneUtil.Parse(sentencePhoneNumber, RegionCode.NZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            try
            {
                var sentencePhoneNumber = "12 MICROSOFT";
                phoneUtil.Parse(sentencePhoneNumber, RegionCode.NZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            try
            {
                var tooLongPhoneNumber = "01495 72553301873 810104";
                phoneUtil.Parse(tooLongPhoneNumber, RegionCode.GB);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.TOO_LONG, e.ErrorType);
            }
            try
            {
                var plusMinusPhoneNumber = "+---";
                phoneUtil.Parse(plusMinusPhoneNumber, RegionCode.DE);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            try
            {
                var plusStar = "+***";
                phoneUtil.Parse(plusStar, RegionCode.DE);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            try
            {
                var plusStarPhoneNumber = "+*******91";
                phoneUtil.Parse(plusStarPhoneNumber, RegionCode.DE);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            try
            {
                var tooShortPhoneNumber = "+49 0";
                phoneUtil.Parse(tooShortPhoneNumber, RegionCode.DE);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.TOO_SHORT_NSN, e.ErrorType);
            }
            try
            {
                var invalidCountryCode = "+210 3456 56789";
                phoneUtil.Parse(invalidCountryCode, RegionCode.NZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.INVALID_COUNTRY_CODE, e.ErrorType);
            }
            try
            {
                var plusAndIddAndInvalidCountryCode = "+ 00 210 3 331 6005";
                phoneUtil.Parse(plusAndIddAndInvalidCountryCode, RegionCode.NZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception. 00 is a correct IDD, but 210 is not a valid country code.
                Assert.Equal(ErrorType.INVALID_COUNTRY_CODE, e.ErrorType);
            }
            try
            {
                var someNumber = "123 456 7890";
                phoneUtil.Parse(someNumber, RegionCode.ZZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.INVALID_COUNTRY_CODE, e.ErrorType);
            }
            try
            {
                var someNumber = "123 456 7890";
                phoneUtil.Parse(someNumber, RegionCode.CS);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.INVALID_COUNTRY_CODE, e.ErrorType);
            }
            try
            {
                var someNumber = "123 456 7890";
                phoneUtil.Parse(someNumber, null);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.INVALID_COUNTRY_CODE, e.ErrorType);
            }
            try
            {
                var someNumber = "0044------";
                phoneUtil.Parse(someNumber, RegionCode.GB);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.TOO_SHORT_AFTER_IDD, e.ErrorType);
            }
            try
            {
                var someNumber = "0044";
                phoneUtil.Parse(someNumber, RegionCode.GB);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.TOO_SHORT_AFTER_IDD, e.ErrorType);
            }
            try
            {
                var someNumber = "011";
                phoneUtil.Parse(someNumber, RegionCode.US);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.TOO_SHORT_AFTER_IDD, e.ErrorType);
            }
            try
            {
                var someNumber = "0119";
                phoneUtil.Parse(someNumber, RegionCode.US);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.TOO_SHORT_AFTER_IDD, e.ErrorType);
            }
            try
            {
                var emptyNumber = "";
                // Invalid region.
                phoneUtil.Parse(emptyNumber, RegionCode.ZZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            try
            {
                // Invalid region.
                phoneUtil.Parse(null, RegionCode.ZZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            catch (ArgumentNullException)
            {
                Assert.True(false);
            }
            try
            {
                phoneUtil.Parse(null, RegionCode.US);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.NOT_A_NUMBER, e.ErrorType);
            }
            catch (ArgumentNullException)
            {
                Assert.True(false);
            }
            try
            {
                var domainRfcPhoneContext = "tel:555-1234;phone-context=www.google.com";
                phoneUtil.Parse(domainRfcPhoneContext, RegionCode.ZZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.INVALID_COUNTRY_CODE, e.ErrorType);
            }
            catch (ArgumentNullException)
            {
                Assert.True(false);
            }
            try
            {
                // This is invalid because no "+" sign is present as part of phone-context. This should not
                // succeed in being parsed.
                var invalidRfcPhoneContext = "tel:555-1234;phone-context=1-331";
                phoneUtil.Parse(invalidRfcPhoneContext, RegionCode.ZZ);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.INVALID_COUNTRY_CODE, e.ErrorType);
            }
        }

        [Fact]
        public void TestParseNumbersWithPlusWithNoRegion()
        {
            // RegionCode.ZZ is allowed only if the number starts with a '+' - then the country calling code
            // can be calculated.
            Assert.Equal(NZNumber, phoneUtil.Parse("+64 3 331 6005", RegionCode.ZZ));
            // Test with full-width plus.
            Assert.Equal(NZNumber, phoneUtil.Parse("\uFF0B64 3 331 6005", RegionCode.ZZ));
            // Test with normal plus but leading characters that need to be stripped.
            Assert.Equal(NZNumber, phoneUtil.Parse("Tel: +64 3 331 6005", RegionCode.ZZ));
            Assert.Equal(NZNumber, phoneUtil.Parse("+64 3 331 6005", null));
            Assert.Equal(InternationalTollFree, phoneUtil.Parse("+800 1234 5678", null));
            Assert.Equal(UniversalPremiumRate, phoneUtil.Parse("+979 123 456 789", null));

            // Test parsing RFC3966 format with a phone context.
            Assert.Equal(NZNumber, phoneUtil.Parse("tel:03-331-6005;phone-context=+64", RegionCode.ZZ));
            Assert.Equal(NZNumber, phoneUtil.Parse("  tel:03-331-6005;phone-context=+64", RegionCode.ZZ));
            Assert.Equal(NZNumber, phoneUtil.Parse("tel:03-331-6005;isub=12345;phone-context=+64",
                RegionCode.ZZ));

            var nzNumberWithRawInput = new PhoneNumber.Builder().MergeFrom(NZNumber)
                .SetRawInput("+64 3 331 6005")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN)
                .Build();
            Assert.Equal(nzNumberWithRawInput, phoneUtil.ParseAndKeepRawInput("+64 3 331 6005",
                                                                      RegionCode.ZZ));
            // Null is also allowed for the region code in these cases.
            Assert.Equal(nzNumberWithRawInput, phoneUtil.ParseAndKeepRawInput("+64 3 331 6005", null));
        }

        [Fact]
        public void TestParseExtensions()
        {
            var nzNumber = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(33316005L).SetExtension("3456").Build();
            Assert.Equal(nzNumber, phoneUtil.Parse("03 331 6005 ext 3456", RegionCode.NZ));
            Assert.Equal(nzNumber, phoneUtil.Parse("03-3316005x3456", RegionCode.NZ));
            Assert.Equal(nzNumber, phoneUtil.Parse("03-3316005 int.3456", RegionCode.NZ));
            Assert.Equal(nzNumber, phoneUtil.Parse("03 3316005 #3456", RegionCode.NZ));
            // Test the following do not extract extensions:
            Assert.Equal(AlphaNumericNumber, phoneUtil.Parse("1800 six-flags", RegionCode.US));
            Assert.Equal(AlphaNumericNumber, phoneUtil.Parse("1800 SIX FLAGS", RegionCode.US));
            Assert.Equal(AlphaNumericNumber, phoneUtil.Parse("0~0 1800 7493 5247", RegionCode.PL));
            Assert.Equal(AlphaNumericNumber, phoneUtil.Parse("(1800) 7493.5247", RegionCode.US));
            // Check that the last instance of an extension token is matched.
            var extnNumber = new PhoneNumber.Builder().MergeFrom(AlphaNumericNumber).SetExtension("1234").Build();
            Assert.Equal(extnNumber, phoneUtil.Parse("0~0 1800 7493 5247 ~1234", RegionCode.PL));
            // Verifying fix where the last digit of a number was previously omitted if it was a 0 when
            // extracting the extension. Also verifying a few different cases of extensions.
            var ukNumber = new PhoneNumber.Builder()
                .SetCountryCode(44).SetNationalNumber(2034567890L).SetExtension("456").Build();
            Assert.Equal(ukNumber, phoneUtil.Parse("+44 2034567890x456", RegionCode.NZ));
            Assert.Equal(ukNumber, phoneUtil.Parse("+44 2034567890x456", RegionCode.GB));
            Assert.Equal(ukNumber, phoneUtil.Parse("+44 2034567890 x456", RegionCode.GB));
            Assert.Equal(ukNumber, phoneUtil.Parse("+44 2034567890 X456", RegionCode.GB));
            Assert.Equal(ukNumber, phoneUtil.Parse("+44 2034567890 X 456", RegionCode.GB));
            Assert.Equal(ukNumber, phoneUtil.Parse("+44 2034567890 X  456", RegionCode.GB));
            Assert.Equal(ukNumber, phoneUtil.Parse("+44 2034567890 x 456  ", RegionCode.GB));
            Assert.Equal(ukNumber, phoneUtil.Parse("+44 2034567890  X 456", RegionCode.GB));
            Assert.Equal(ukNumber, phoneUtil.Parse("+44-2034567890;ext=456", RegionCode.GB));
            Assert.Equal(ukNumber, phoneUtil.Parse("tel:2034567890;ext=456;phone-context=+44",
                                           RegionCode.ZZ));
            // Full-width extension, "extn" only.
            Assert.Equal(ukNumber, phoneUtil.Parse("+442034567890\uFF45\uFF58\uFF54\uFF4E456",
                RegionCode.GB));
            // "xtn" only.
            Assert.Equal(ukNumber, phoneUtil.Parse("+442034567890\uFF58\uFF54\uFF4E456",
                RegionCode.GB));
            // "xt" only.
            Assert.Equal(ukNumber, phoneUtil.Parse("+442034567890\uFF58\uFF54456",
                RegionCode.GB));

            var usWithExtension = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(8009013355L).SetExtension("7246433").Build();
            Assert.Equal(usWithExtension, phoneUtil.Parse("(800) 901-3355 x 7246433", RegionCode.US));
            Assert.Equal(usWithExtension, phoneUtil.Parse("(800) 901-3355 , ext 7246433", RegionCode.US));
            Assert.Equal(usWithExtension,
                     phoneUtil.Parse("(800) 901-3355 ,extension 7246433", RegionCode.US));
            Assert.Equal(usWithExtension,
                     phoneUtil.Parse("(800) 901-3355 ,extensi\u00F3n 7246433", RegionCode.US));
            // Repeat with the small letter o with acute accent created by combining characters.
            Assert.Equal(usWithExtension,
                     phoneUtil.Parse("(800) 901-3355 ,extensio\u0301n 7246433", RegionCode.US));
            Assert.Equal(usWithExtension, phoneUtil.Parse("(800) 901-3355 , 7246433", RegionCode.US));
            Assert.Equal(usWithExtension, phoneUtil.Parse("(800) 901-3355 ext: 7246433", RegionCode.US));
            // Testing Russian extension \u0434\u043E\u0431 with variants found online.
            var ruWithExtension = new PhoneNumber.Builder
            {
                CountryCode = 7,
                NationalNumber = 4232022511L,
                Extension = "100"
            }.Build();
            Assert.Equal(ruWithExtension,
                phoneUtil.Parse("8 (423) 202-25-11, \u0434\u043E\u0431. 100", RegionCode.RU));
            Assert.Equal(ruWithExtension,
                phoneUtil.Parse("8 (423) 202-25-11 \u0434\u043E\u0431. 100", RegionCode.RU));
            Assert.Equal(ruWithExtension,
                phoneUtil.Parse("8 (423) 202-25-11, \u0434\u043E\u0431 100", RegionCode.RU));
            Assert.Equal(ruWithExtension,
                phoneUtil.Parse("8 (423) 202-25-11 \u0434\u043E\u0431 100", RegionCode.RU));
            Assert.Equal(ruWithExtension,
                phoneUtil.Parse("8 (423) 202-25-11\u0434\u043E\u0431100", RegionCode.RU));
            // In upper case
            Assert.Equal(ruWithExtension,
                phoneUtil.Parse("8 (423) 202-25-11, \u0414\u041E\u0411. 100", RegionCode.RU));

            // Test that if a number has two extensions specified, we ignore the second.
            var usWithTwoExtensionsNumber = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(2121231234L).SetExtension("508").Build();
            Assert.Equal(usWithTwoExtensionsNumber, phoneUtil.Parse("(212)123-1234 x508/x1234",
                                                                RegionCode.US));
            Assert.Equal(usWithTwoExtensionsNumber, phoneUtil.Parse("(212)123-1234 x508/ x1234",
                                                                RegionCode.US));
            Assert.Equal(usWithTwoExtensionsNumber, phoneUtil.Parse("(212)123-1234 x508\\x1234",
                                                                RegionCode.US));

            // Test parsing numbers in the form (645) 123-1234-910# works, where the last 3 digits before
            // the # are an extension.
            usWithExtension = new PhoneNumber.Builder()
                .SetCountryCode(1).SetNationalNumber(6451231234L).SetExtension("910").Build();
            Assert.Equal(usWithExtension, phoneUtil.Parse("+1 (645) 123 1234-910#", RegionCode.US));
            // Retry with the same number in a slightly different format.
            Assert.Equal(usWithExtension, phoneUtil.Parse("+1 (645) 123 1234 ext. 910#", RegionCode.US));
        }

        [Fact]
        public void TestParseAndKeepRaw()
        {
            var alphaNumericNumber = new PhoneNumber.Builder().MergeFrom(AlphaNumericNumber)
                .SetRawInput("800 six-flags")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY)
                .Build();
            Assert.Equal(alphaNumericNumber,
                phoneUtil.ParseAndKeepRawInput("800 six-flags", RegionCode.US));

            var shorterAlphaNumber = new PhoneNumber.Builder()
                .SetCountryCode(1)
                .SetNationalNumber(8007493524L)
                .SetRawInput("1800 six-flag")
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN)
                .Build();
            Assert.Equal(shorterAlphaNumber,
                phoneUtil.ParseAndKeepRawInput("1800 six-flag", RegionCode.US));

            shorterAlphaNumber = Update(shorterAlphaNumber).SetRawInput("+1800 six-flag").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN).Build();
            Assert.Equal(shorterAlphaNumber,
                phoneUtil.ParseAndKeepRawInput("+1800 six-flag", RegionCode.NZ));

            shorterAlphaNumber = Update(shorterAlphaNumber).SetRawInput("001800 six-flag").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD).Build();
            Assert.Equal(shorterAlphaNumber,
                phoneUtil.ParseAndKeepRawInput("001800 six-flag", RegionCode.NZ));

            // Invalid region code supplied.
            try
            {
                phoneUtil.ParseAndKeepRawInput("123 456 7890", RegionCode.CS);
                Assert.True(false);
            }
            catch (NumberParseException e)
            {
                // Expected this exception.
                Assert.Equal(ErrorType.INVALID_COUNTRY_CODE, e.ErrorType);
            }

            var koreanNumber = new PhoneNumber.Builder()
                .SetCountryCode(82).SetNationalNumber(22123456).SetRawInput("08122123456").
                SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY).
                SetPreferredDomesticCarrierCode("81").Build();
            Assert.Equal(koreanNumber, phoneUtil.ParseAndKeepRawInput("08122123456", RegionCode.KR));
        }

        [Fact]
        public void TestParseItalianLeadingZeros()
        {
            // Test the number "011".
            var oneZero = new PhoneNumber.Builder()
                .SetCountryCode(61)
                .SetNationalNumber(11L)
                .SetItalianLeadingZero(true)
                .Build();
            Assert.Equal(oneZero, phoneUtil.Parse("011", RegionCode.AU));

            // Test the number "001".
            var twoZeros = new PhoneNumber.Builder()
                .SetCountryCode(61)
                .SetNationalNumber(1)
                .SetItalianLeadingZero(true)
                .SetNumberOfLeadingZeros(2)
                .Build();
            Assert.Equal(twoZeros, phoneUtil.Parse("001", RegionCode.AU));

            // Test the number "000". This number has 2 leading zeros.
            var stillTwoZeros = new PhoneNumber.Builder()
                .SetCountryCode(61)
                .SetNationalNumber(0L)
                .SetItalianLeadingZero(true)
                .SetNumberOfLeadingZeros(2)
                .Build();
            Assert.Equal(stillTwoZeros, phoneUtil.Parse("000", RegionCode.AU));

            // Test the number "0000". This number has 3 leading zeros.
            var threeZeros = new PhoneNumber.Builder()
                .SetCountryCode(61)
                .SetNationalNumber(0L)
                .SetItalianLeadingZero(true)
                .SetNumberOfLeadingZeros(3)
                .Build();
            Assert.Equal(threeZeros, phoneUtil.Parse("0000", RegionCode.AU));
        }

        [Fact]
        public void TestCountryWithNoNumberDesc()
        {
            // Andorra is a country where we don't have PhoneNumberDesc info in the metadata.
            var adNumber = new PhoneNumber.Builder()
                .SetCountryCode(376).SetNationalNumber(12345L).Build();
            Assert.Equal("+376 12345", phoneUtil.Format(adNumber, PhoneNumberFormat.INTERNATIONAL));
            Assert.Equal("+37612345", phoneUtil.Format(adNumber, PhoneNumberFormat.E164));
            Assert.Equal("12345", phoneUtil.Format(adNumber, PhoneNumberFormat.NATIONAL));
            Assert.Equal(PhoneNumberType.UNKNOWN, phoneUtil.GetNumberType(adNumber));

            // Test dialing a US number from within Andorra.
            Assert.Equal("00 1 650 253 0000",
            phoneUtil.FormatOutOfCountryCallingNumber(USNumber, RegionCode.AD));
        }

        [Fact]
        public void TestUnknownCountryCallingCodeForValidation()
        {
            var invalidNumber = new PhoneNumber.Builder()
                .SetCountryCode(0).SetNationalNumber(1234L).Build();
            Assert.False(phoneUtil.IsValidNumber(invalidNumber));
        }

        [Fact]
        public void TestIsNumberMatchMatches()
        {
            // Test simple matches where formatting is different, or leading zeroes, or country calling code
            // has been specified.
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331 6005", "+64 03 331 6005"));
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                 phoneUtil.IsNumberMatch("+800 1234 5678", "+80012345678"));
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+64 03 331-6005", "+64 03331 6005"));
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+643 331-6005", "+64033316005"));
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+643 331-6005", "+6433316005"));
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "+6433316005"));
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                 phoneUtil.IsNumberMatch("+64 3 331-6005", "tel:+64-3-331-6005;isub=123"));
            // Test alpha numbers.
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+1800 siX-Flags", "+1 800 7493 5247"));
            // Test numbers with extensions.
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005 extn 1234", "+6433316005#1234"));
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch("+7 423 202-25-11 ext 100",
                    "+7 4232022511 \u0434\u043E\u0431. 100"));
            // Test proto buffers.
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch(NZNumber, "+6403 331 6005"));

            var nzNumber = new PhoneNumber.Builder().MergeFrom(NZNumber).SetExtension("3456").Build();
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch(nzNumber, "+643 331 6005 ext 3456"));
            // Check empty extensions are ignored.
            nzNumber = Update(nzNumber).SetExtension("").Build();
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch(nzNumber, "+6403 331 6005"));
            // Check variant with two proto buffers.
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH, phoneUtil.IsNumberMatch(nzNumber, NZNumber));

            // Check raw_input, country_code_source and preferred_domestic_carrier_code are ignored.
            var brNumberOne = new PhoneNumber.Builder()
                .SetCountryCode(55).SetNationalNumber(3121286979L)
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN)
                .SetPreferredDomesticCarrierCode("12").SetRawInput("012 3121286979").Build();
            var brNumberTwo = new PhoneNumber.Builder()
                .SetCountryCode(55).SetNationalNumber(3121286979L)
                .SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY)
                .SetPreferredDomesticCarrierCode("14").SetRawInput("143121286979").Build();
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH,
                phoneUtil.IsNumberMatch(brNumberOne, brNumberTwo));
        }

        [Fact]
        public void TestIsNumberMatchShortMatchIfDiffNumLeadingZeros()
        {
            var nzNumberOne = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(33316005L).SetItalianLeadingZero(true).Build();
            var nzNumberTwo = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(33316005L).SetItalianLeadingZero(true).SetNumberOfLeadingZeros(2).Build();
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH, phoneUtil.IsNumberMatch(nzNumberOne, nzNumberTwo));

            nzNumberOne = Update(nzNumberOne).SetItalianLeadingZero(false).SetNumberOfLeadingZeros(1).Build();
            nzNumberTwo = Update(nzNumberTwo).SetItalianLeadingZero(true).SetNumberOfLeadingZeros(1).Build();
            // Since one doesn't have the "italian_leading_zero" set to true, we ignore the number of
            // leading zeros present (1 is in any case the default value).
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH, phoneUtil.IsNumberMatch(nzNumberOne, nzNumberTwo));
        }

        [Fact]
        public void TestIsNumberMatchAcceptsProtoDefaultsAsMatch()
        {
            var nzNumberOne = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(33316005L).SetItalianLeadingZero(true).Build();
            // The default for number_of_leading_zeros is 1, so it shouldn't normally be set, however if it
            // is it should be considered equivalent.
            var nzNumberTwo = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(33316005L).SetItalianLeadingZero(true).SetNumberOfLeadingZeros(1).Build();
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH, phoneUtil.IsNumberMatch(nzNumberOne, nzNumberTwo));
        }

        [Fact]
        public void TestIsNumberMatchMatchesDiffLeadingZerosIfItalianLeadingZeroFalse()
        {
            var nzNumberOne = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(33316005L).Build();
            // The default for number_of_leading_zeros is 1, so it shouldn't normally be set, however if it
            // is it should be considered equivalent.
            var nzNumberTwo = new PhoneNumber.Builder()
                .SetCountryCode(64).SetNationalNumber(33316005L).SetNumberOfLeadingZeros(1).Build();
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH, phoneUtil.IsNumberMatch(nzNumberOne, nzNumberTwo));

            // Even if it is set to ten, it is still equivalent because in both cases
            // italian_leading_zero is not true.
            nzNumberTwo = Update(nzNumberTwo).SetNumberOfLeadingZeros(10).Build();
            Assert.Equal(PhoneNumberUtil.MatchType.EXACT_MATCH, phoneUtil.IsNumberMatch(nzNumberOne, nzNumberTwo));
        }

        [Fact]
        public void TestIsNumberMatchNonMatches()
        {
            // Non-matches.
            Assert.Equal(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("03 331 6005", "03 331 6006"));
            Assert.Equal(PhoneNumberUtil.MatchType.NO_MATCH,
                 phoneUtil.IsNumberMatch("+800 1234 5678", "+1 800 1234 5678"));
            // Different country calling code, partial number match.
            Assert.Equal(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "+16433316005"));
            // Different country calling code, same number.
            Assert.Equal(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "+6133316005"));
            // Extension different, all else the same.
            Assert.Equal(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005 extn 1234", "0116433316005#1235"));
            Assert.Equal(PhoneNumberUtil.MatchType.NO_MATCH,
                 phoneUtil.IsNumberMatch(
                     "+64 3 331-6005 extn 1234", "tel:+64-3-331-6005;ext=1235"));
            // NSN matches, but extension is different - not the same number.
            Assert.Equal(PhoneNumberUtil.MatchType.NO_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005 ext.1235", "3 331 6005#1234"));

            // Invalid numbers that can't be parsed.
            Assert.Equal(PhoneNumberUtil.MatchType.NOT_A_NUMBER,
                phoneUtil.IsNumberMatch("4", "3 331 6043"));
            Assert.Equal(PhoneNumberUtil.MatchType.NOT_A_NUMBER,
                phoneUtil.IsNumberMatch("+43", "+64 3 331 6005"));
            Assert.Equal(PhoneNumberUtil.MatchType.NOT_A_NUMBER,
                phoneUtil.IsNumberMatch("+43", "64 3 331 6005"));
            Assert.Equal(PhoneNumberUtil.MatchType.NOT_A_NUMBER,
                phoneUtil.IsNumberMatch("Dog", "64 3 331 6005"));
        }

        [Fact]
        public void TestIsNumberMatchNsnMatches()
        {
            // NSN matches.
            Assert.Equal(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "03 331 6005"));
            Assert.Equal(PhoneNumberUtil.MatchType.NSN_MATCH,
                 phoneUtil.IsNumberMatch(
                     "+64 3 331-6005", "tel:03-331-6005;isub=1234;phone-context=abc.nz"));
            Assert.Equal(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch(NZNumber, "03 331 6005"));
            // Here the second number possibly starts with the country calling code for New Zealand,
            // although we are unsure.
            var unchangedNzNumber = new PhoneNumber.Builder().MergeFrom(NZNumber).Build();
            Assert.Equal(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch(unchangedNzNumber, "(64-3) 331 6005"));
            // Check the phone number proto was not edited during the method call.
            Assert.Equal(NZNumber, unchangedNzNumber);

            // Here, the 1 might be a national prefix, if we compare it to the US number, so the resultant
            // match is an NSN match.
            Assert.Equal(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch(USNumber, "1-650-253-0000"));
            Assert.Equal(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch(USNumber, "6502530000"));
            Assert.Equal(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch("+1 650-253 0000", "1 650 253 0000"));
            Assert.Equal(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch("1 650-253 0000", "1 650 253 0000"));
            Assert.Equal(PhoneNumberUtil.MatchType.NSN_MATCH,
                phoneUtil.IsNumberMatch("1 650-253 0000", "+1 650 253 0000"));
            // For this case, the match will be a short NSN match, because we cannot assume that the 1 might
            // be a national prefix, so don't remove it when parsing.
            var randomNumber = new PhoneNumber.Builder()
                .SetCountryCode(41).SetNationalNumber(6502530000L).Build();
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch(randomNumber, "1-650-253-0000"));
        }


        [Fact]
        public void TestIsNumberMatchShortNsnMatches()
        {
            // Short NSN matches with the country not specified for either one or both numbers.
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "331 6005"));
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                 phoneUtil.IsNumberMatch("+64 3 331-6005", "tel:331-6005;phone-context=abc.nz"));
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                 phoneUtil.IsNumberMatch("+64 3 331-6005",
                     "tel:331-6005;isub=1234;phone-context=abc.nz"));
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                 phoneUtil.IsNumberMatch("+64 3 331-6005",
                     "tel:331-6005;isub=1234;phone-context=abc.nz;a=%A1"));
            // We did not know that the "0" was a national prefix since neither number has a country code,
            // so this is considered a SHORT_NSN_MATCH.
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("3 331-6005", "03 331 6005"));
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("3 331-6005", "331 6005"));
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                 phoneUtil.IsNumberMatch("3 331-6005", "tel:331-6005;phone-context=abc.nz"));
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("3 331-6005", "+64 331 6005"));
            // Short NSN match with the country specified.
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("03 331-6005", "331 6005"));
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("1 234 345 6789", "345 6789"));
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("+1 (234) 345 6789", "345 6789"));
            // NSN matches, country calling code omitted for one number, extension missing for one.
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch("+64 3 331-6005", "3 331 6005#1234"));
            // One has Italian leading zero, one does not.
            var italianNumberOne = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(1234L).SetItalianLeadingZero(true).Build();
            var italianNumberTwo = new PhoneNumber.Builder()
                .SetCountryCode(39).SetNationalNumber(1234L).Build();
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch(italianNumberOne, italianNumberTwo));
            // One has an extension, the other has an extension of "".
            italianNumberOne = Update(italianNumberOne).SetExtension("1234").ClearItalianLeadingZero().Build();
            italianNumberTwo = Update(italianNumberTwo).SetExtension("").Build();
            Assert.Equal(PhoneNumberUtil.MatchType.SHORT_NSN_MATCH,
                phoneUtil.IsNumberMatch(italianNumberOne, italianNumberTwo));
        }

        [Fact]
        public void TestCanBeInternationallyDialled()
        {
            // We have no-international-dialling rules for the US in our test metadata that say that
            // toll-free numbers cannot be dialled internationally.
            Assert.False(phoneUtil.CanBeInternationallyDialled(USTollfree));

            // Normal US numbers can be internationally dialled.
            Assert.True(phoneUtil.CanBeInternationallyDialled(USNumber));

            // Invalid number.
            Assert.True(phoneUtil.CanBeInternationallyDialled(USLocalNumber));

            // We have no data for NZ - should return true.
            Assert.True(phoneUtil.CanBeInternationallyDialled(NZNumber));
            Assert.True(phoneUtil.CanBeInternationallyDialled(InternationalTollFree));
        }

        [Fact]
        public void TestIsAlphaNumber()
        {
            Assert.True(phoneUtil.IsAlphaNumber("1800 six-flags"));
            Assert.True(phoneUtil.IsAlphaNumber("1800 six-flags ext. 1234"));
            Assert.True(phoneUtil.IsAlphaNumber("+800 six-flags"));
            Assert.True(phoneUtil.IsAlphaNumber("180 six-flags"));
            Assert.False(phoneUtil.IsAlphaNumber("1800 123-1234"));
            Assert.False(phoneUtil.IsAlphaNumber("1 six-flags"));
            Assert.False(phoneUtil.IsAlphaNumber("18 six-flags"));
            Assert.False(phoneUtil.IsAlphaNumber("1800 123-1234 extension: 1234"));
            Assert.False(phoneUtil.IsAlphaNumber("+800 1234-1234"));
        }
    }
}
