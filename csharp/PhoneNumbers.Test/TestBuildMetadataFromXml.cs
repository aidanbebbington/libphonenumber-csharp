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
using System.IO;
using System.Xml.Linq;
using Xunit;

namespace PhoneNumbers.Test
{
    public class TestBuildMetadataFromXml
    {
        // Helper method that outputs a DOM element from a XML string.
        private static XElement ParseXmlString(string xmlString)
        {
            using (var reader = new StringReader(xmlString))
            {
                return XDocument.Load(reader).Root;
            }
        }

        // Tests validateRE().
        [Fact]
        public void TestValidateRERemovesWhiteSpaces()
        {
            var input = " hello world ";
            // Should remove all the white spaces contained in the provided string.
            Assert.Equal("helloworld", BuildMetadataFromXml.ValidateRegex(input, true));
            // Make sure it only happens when the last parameter is set to true.
            Assert.Equal(" hello world ", BuildMetadataFromXml.ValidateRegex(input, false));
        }

        [Fact]
        public void TestValidateREThrowsException()
        {
            var invalidPattern = "[";
            // Should throw an exception when an invalid pattern is provided independently of the last
            // parameter (remove white spaces).
            try
            {
                BuildMetadataFromXml.ValidateRegex(invalidPattern, false);
                Assert.True(false);
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
            try
            {
                BuildMetadataFromXml.ValidateRegex(invalidPattern, true);
                Assert.True(false);
            }
            catch (ArgumentException)
            {
                // Test passed.
            }
        }

        [Fact]
        public void TestValidateRE()
        {
            var validPattern = "[a-zA-Z]d{1,9}";
            // The provided pattern should be left unchanged.
            Assert.Equal(validPattern, BuildMetadataFromXml.ValidateRegex(validPattern, false));
        }

        // Tests NationalPrefix.
        [Fact]
        public void TestGetNationalPrefix()
        {
            var xmlInput = "<territory nationalPrefix='00'/>";
            var territoryElement = ParseXmlString(xmlInput);
            Assert.Equal("00", BuildMetadataFromXml.GetNationalPrefixInternal(territoryElement));
        }

        // Tests LoadTerritoryTagMetadata().
        [Fact]
        public void TestLoadTerritoryTagMetadata()
        {
            var xmlInput =
                "<territory countryCode='33' leadingDigits='2' internationalPrefix='00'" +
                "           preferredInternationalPrefix='00~11' nationalPrefixForParsing='0'" +
                "           nationalPrefixTransformRule='9$1'" + // nationalPrefix manually injected.
                "           preferredExtnPrefix=' x' mainCountryForCode='true'" +
                "           leadingZeroPossible='true' mobileNumberPortableRegion='true'>" +
                "</territory>";
            var territoryElement = ParseXmlString(xmlInput);
            var phoneMetadata =
            BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "0");
            Assert.Equal(33, phoneMetadata.CountryCode);
            Assert.Equal("2", phoneMetadata.LeadingDigits);
            Assert.Equal("00", phoneMetadata.InternationalPrefix);
            Assert.Equal("00~11", phoneMetadata.PreferredInternationalPrefix);
            Assert.Equal("0", phoneMetadata.NationalPrefixForParsing);
            Assert.Equal("9$1", phoneMetadata.NationalPrefixTransformRule);
            Assert.Equal("0", phoneMetadata.NationalPrefix);
            Assert.Equal(" x", phoneMetadata.PreferredExtnPrefix);
            Assert.True(phoneMetadata.MainCountryForCode);
            Assert.True(phoneMetadata.MobileNumberPortableRegion);
        }

        [Fact]
        public void TestLoadTerritoryTagMetadataSetsBooleanFieldsToFalseByDefault()
        {
            var xmlInput = "<territory countryCode='33'/>";
            var territoryElement = ParseXmlString(xmlInput);
            var phoneMetadata =
                BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "");
            Assert.False(phoneMetadata.MainCountryForCode);
            Assert.False(phoneMetadata.LeadingZeroPossible);
        }

        [Fact]
        public void TestLoadTerritoryTagMetadataSetsNationalPrefixForParsingByDefault()
        {
            var xmlInput = "<territory countryCode='33'/>";
            var territoryElement = ParseXmlString(xmlInput);
            var phoneMetadata =
                BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "00");
            // When unspecified, nationalPrefixForParsing defaults to nationalPrefix.
            Assert.Equal("00", phoneMetadata.NationalPrefix);
            Assert.Equal(phoneMetadata.NationalPrefix, phoneMetadata.NationalPrefixForParsing);
        }

        [Fact]
        public void TestLoadTerritoryTagMetadataWithRequiredAttributesOnly()
        {
            var xmlInput = "<territory countryCode='33' internationalPrefix='00'/>";
            var territoryElement = ParseXmlString(xmlInput);
            // Should not throw any exception.
            BuildMetadataFromXml.LoadTerritoryTagMetadata("33", territoryElement, "");
        }

        // Tests loadInternationalFormat().
        [Fact]
        public void TestLoadInternationalFormat()
        {
            var intlFormat = "$1 $2";
            var xmlInput = "<numberFormat><intlFormat>" + intlFormat + "</intlFormat></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var nationalFormat = "";

            Assert.True(BuildMetadataFromXml.LoadInternationalFormatInternal(metadata, numberFormatElement,
                                                                    nationalFormat));
            Assert.Equal(intlFormat, metadata.IntlNumberFormatList[0].Format);
        }

        [Fact]
        public void TestLoadInternationalFormatWithBothNationalAndIntlFormatsDefined()
        {
            var intlFormat = "$1 $2";
            var xmlInput = "<numberFormat><intlFormat>" + intlFormat + "</intlFormat></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var nationalFormat = "$1";

            Assert.True(BuildMetadataFromXml.LoadInternationalFormatInternal(metadata, numberFormatElement,
                                                                    nationalFormat));
            Assert.Equal(intlFormat, metadata.IntlNumberFormatList[0].Format);
        }

        [Fact]
        public void TestLoadInternationalFormatExpectsOnlyOnePattern()
        {
            var xmlInput = "<numberFormat><intlFormat/><intlFormat/></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();

            // Should throw an exception as multiple intlFormats are provided.
            try
            {
                BuildMetadataFromXml.LoadInternationalFormatInternal(metadata, numberFormatElement, "");
                Assert.True(false);
            }
            catch (Exception)
            {
                // Test passed.
            }
        }

        [Fact]
        public void TestLoadInternationalFormatUsesNationalFormatByDefault()
        {
            var xmlInput = "<numberFormat></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var nationalFormat = "$1 $2 $3";

            Assert.False(BuildMetadataFromXml.LoadInternationalFormatInternal(metadata, numberFormatElement,
                                                                     nationalFormat));
            Assert.Equal(nationalFormat, metadata.IntlNumberFormatList[0].Format);
        }

        // Tests LoadNationalFormat().
        [Fact]
        public void TestLoadNationalFormat()
        {
            var nationalFormat = "$1 $2";
            var xmlInput = string.Format("<numberFormat><format>{0}</format></numberFormat>",
                                            nationalFormat);
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var numberFormat = new NumberFormat.Builder();

            Assert.Equal(nationalFormat,
                         BuildMetadataFromXml.LoadNationalFormatInternal(metadata, numberFormatElement,
                                                                 numberFormat));
        }

        [Fact]
        public void TestLoadNationalFormatRequiresFormat()
        {
            var xmlInput = "<numberFormat></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var numberFormat = new NumberFormat.Builder();

            try
            {
                BuildMetadataFromXml.LoadNationalFormatInternal(metadata, numberFormatElement, numberFormat);
                Assert.True(false);
            }
            catch (Exception)
            {
                // Test passed.
            }
        }

        [Fact]
        public void TestLoadNationalFormatExpectsExactlyOneFormat()
        {
            var xmlInput = "<numberFormat><format/><format/></numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            var numberFormat = new NumberFormat.Builder();

            try
            {
                BuildMetadataFromXml.LoadNationalFormatInternal(metadata, numberFormatElement, numberFormat);
                Assert.True(false);
            }
            catch (Exception)
            {
                // Test passed.
            }
        }

        // Tests loadAvailableFormats().
        [Fact]
        public void TestLoadAvailableFormats()
        {
            var xmlInput =
                "<territory >" +
                "  <availableFormats>" +
                "    <numberFormat nationalPrefixFormattingRule='($FG)'" +
                "                  carrierCodeFormattingRule='$NP $CC ($FG)'>" +
                "      <format>$1 $2 $3</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormatsInternal(
                metadata, element, "0", "", false /* NP not optional */);
            Assert.Equal("(${1})", metadata.NumberFormatList[0].NationalPrefixFormattingRule);
            Assert.Equal("0 $CC (${1})", metadata.NumberFormatList[0].DomesticCarrierCodeFormattingRule);
            Assert.Equal("$1 $2 $3", metadata.NumberFormatList[0].Format);
        }

        [Fact]
        public void TestLoadAvailableFormatsPropagatesCarrierCodeFormattingRule()
        {
            var xmlInput =
                "<territory carrierCodeFormattingRule='$NP $CC ($FG)'>" +
                "  <availableFormats>" +
                "    <numberFormat nationalPrefixFormattingRule='($FG)'>" +
                "      <format>$1 $2 $3</format>" +
                "    </numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormatsInternal(
                metadata, element, "0", "", false /* NP not optional */);
            Assert.Equal("(${1})", metadata.NumberFormatList[0].NationalPrefixFormattingRule);
            Assert.Equal("0 $CC (${1})", metadata.NumberFormatList[0].DomesticCarrierCodeFormattingRule);
            Assert.Equal("$1 $2 $3", metadata.NumberFormatList[0].Format);
        }

        [Fact]
        public void TestLoadAvailableFormatsSetsProvidedNationalPrefixFormattingRule()
        {
            var xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormatsInternal(
                metadata, element, "0", "($1)", false /* NP not optional */);
            Assert.Equal("($1)", metadata.NumberFormatList[0].NationalPrefixFormattingRule);
        }

        [Fact]
        public void TestLoadAvailableFormatsClearsIntlFormat()
        {
            var xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormatsInternal(
                metadata, element, "0", "($1)", false /* NP not optional */);
            Assert.Equal(0, metadata.IntlNumberFormatCount);
        }

        [Fact]
        public void TestLoadAvailableFormatsHandlesMultipleNumberFormats()
        {
            var xmlInput =
                "<territory>" +
                "  <availableFormats>" +
                "    <numberFormat><format>$1 $2 $3</format></numberFormat>" +
                "    <numberFormat><format>$1-$2</format></numberFormat>" +
                "  </availableFormats>" +
                "</territory>";
            var element = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadAvailableFormatsInternal(
                metadata, element, "0", "($1)", false /* NP not optional */);
            Assert.Equal("$1 $2 $3", metadata.NumberFormatList[0].Format);
            Assert.Equal("$1-$2", metadata.NumberFormatList[1].Format);
        }

        // Tests setLeadingDigitsPatterns().
        [Fact]
        public void TestSetLeadingDigitsPatterns()
        {
            var xmlInput =
                "<numberFormat>" +
                "<leadingDigits>1</leadingDigits><leadingDigits>2</leadingDigits>" +
                "</numberFormat>";
            var numberFormatElement = ParseXmlString(xmlInput);
            var numberFormat = new NumberFormat.Builder();
            BuildMetadataFromXml.SetLeadingDigitsPatterns(numberFormatElement, numberFormat);

            Assert.Equal("1", numberFormat.LeadingDigitsPatternList[0]);
            Assert.Equal("2", numberFormat.LeadingDigitsPatternList[1]);
        }

        // Tests GetNationalPrefixFormattingRuleFromElement().
        [Fact]
        public void TestGetNationalPrefixFormattingRuleFromElement()
        {
            var xmlInput = "<territory nationalPrefixFormattingRule='$NP$FG'/>";
            var element = ParseXmlString(xmlInput);
            Assert.Equal("0${1}",
                         BuildMetadataFromXml.GetNationalPrefixFormattingRuleFromElementInternal(element, "0"));
        }

        // Tests getDomesticCarrierCodeFormattingRuleFromElement().
        [Fact]
        public void TestGetDomesticCarrierCodeFormattingRuleFromElement()
        {
            var xmlInput = "<territory carrierCodeFormattingRule='$NP$CC $FG'/>";
            var element = ParseXmlString(xmlInput);
            // C#: the output regex differs from Java one
            Assert.Equal("0$CC ${1}",
                         BuildMetadataFromXml.GetDomesticCarrierCodeFormattingRuleFromElement(element,
                                                                                              "0"));
        }

        // Tests ProcessPhoneNumberDescElement().
        [Fact]
        public void TestProcessPhoneNumberDescElementWithInvalidInput()
        {
            var territoryElement = ParseXmlString("<territory/>");

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElementInternal(
                null, territoryElement, "invalidType");
            Assert.False(phoneNumberDesc.HasNationalNumberPattern);
        }

        [Fact]
        public void TestProcessPhoneNumberDescElementOverridesGeneralDesc()
        {
            var generalDesc = new PhoneNumberDesc.Builder()
                .SetNationalNumberPattern("\\d{8}").Build();
            var xmlInput =
                "<territory><fixedLine>" +
                "  <nationalNumberPattern>\\d{6}</nationalNumberPattern>" +
                "</fixedLine></territory>";
            var territoryElement = ParseXmlString(xmlInput);

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElementInternal(
                generalDesc, territoryElement, "fixedLine");
            Assert.Equal("\\d{6}", phoneNumberDesc.NationalNumberPattern);
        }

        [Fact]
        public void TestProcessPhoneNumberDescOutputsExampleNumberByDefault()
        {
            var xmlInput =
                "<territory><fixedLine>" +
                 "  <exampleNumber>01 01 01 01</exampleNumber>" +
                 "</fixedLine></territory>";
            var territoryElement = ParseXmlString(xmlInput);

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElementInternal(
                null, territoryElement, "fixedLine");
            Assert.Equal("01 01 01 01", phoneNumberDesc.ExampleNumber);
        }

        [Fact]
        public void TestProcessPhoneNumberDescRemovesWhiteSpacesInPatterns()
        {
            var xmlInput =
                "<territory><fixedLine>" +
                 "  <nationalNumberPattern>\t \\d { 6 } </nationalNumberPattern>" +
                 "</fixedLine></territory>";
            var countryElement = ParseXmlString(xmlInput);

            var phoneNumberDesc = BuildMetadataFromXml.ProcessPhoneNumberDescElementInternal(
                null, countryElement, "fixedLine");
            Assert.Equal("\\d{6}", phoneNumberDesc.NationalNumberPattern);
        }

        // Tests LoadGeneralDesc().
        [Fact]
        public void TestLoadGeneralDescSetsSameMobileAndFixedLinePattern()
        {
            var xmlInput =
                "<territory countryCode=\"33\">" +
                "  <fixedLine><nationalNumberPattern>\\d{6}</nationalNumberPattern></fixedLine>" +
                "  <mobile><nationalNumberPattern>\\d{6}</nationalNumberPattern></mobile>" +
                "</territory>";
            var territoryElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            // Should set sameMobileAndFixedPattern to true.
            BuildMetadataFromXml.LoadGeneralDesc(metadata, territoryElement);
            Assert.True(metadata.SameMobileAndFixedLinePattern);
        }

        [Fact]
        public void TestLoadGeneralDescSetsAllDescriptions()
        {
            var xmlInput =
                "<territory countryCode=\"33\">" +
                "  <fixedLine><nationalNumberPattern>\\d{1}</nationalNumberPattern></fixedLine>" +
                "  <mobile><nationalNumberPattern>\\d{2}</nationalNumberPattern></mobile>" +
                "  <pager><nationalNumberPattern>\\d{3}</nationalNumberPattern></pager>" +
                "  <tollFree><nationalNumberPattern>\\d{4}</nationalNumberPattern></tollFree>" +
                "  <premiumRate><nationalNumberPattern>\\d{5}</nationalNumberPattern></premiumRate>" +
                "  <sharedCost><nationalNumberPattern>\\d{6}</nationalNumberPattern></sharedCost>" +
                "  <personalNumber><nationalNumberPattern>\\d{7}</nationalNumberPattern></personalNumber>" +
                "  <voip><nationalNumberPattern>\\d{8}</nationalNumberPattern></voip>" +
                "  <uan><nationalNumberPattern>\\d{9}</nationalNumberPattern></uan>" +
                "  <shortCode><nationalNumberPattern>\\d{10}</nationalNumberPattern></shortCode>" +
                 "</territory>";
            var territoryElement = ParseXmlString(xmlInput);
            var metadata = new PhoneMetadata.Builder();
            BuildMetadataFromXml.LoadGeneralDesc(metadata, territoryElement);
            Assert.Equal("\\d{1}", metadata.FixedLine.NationalNumberPattern);
            Assert.Equal("\\d{2}", metadata.Mobile.NationalNumberPattern);
            Assert.Equal("\\d{3}", metadata.Pager.NationalNumberPattern);
            Assert.Equal("\\d{4}", metadata.TollFree.NationalNumberPattern);
            Assert.Equal("\\d{5}", metadata.PremiumRate.NationalNumberPattern);
            Assert.Equal("\\d{6}", metadata.SharedCost.NationalNumberPattern);
            Assert.Equal("\\d{7}", metadata.PersonalNumber.NationalNumberPattern);
            Assert.Equal("\\d{8}", metadata.Voip.NationalNumberPattern);
            Assert.Equal("\\d{9}", metadata.Uan.NationalNumberPattern);
        }
    }
}