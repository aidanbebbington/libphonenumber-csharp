/*
 * Copyright (C) 2009 The Libphonenumber Authors
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
 * Library to build phone number metadata from the XML Format.
 *
 * @author Shaopeng Jia
 */

using System;
using System.Collections.Generic;
//using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
#if NET35
using System.Xml;
#endif
using System.Xml.Linq;
using PhoneNumbers.Internal;

namespace PhoneNumbers
{
    public class BuildMetadataFromXml
    {

        // string constants used to fetch the XML nodes and attributes.
        private const string CARRIER_CODE_FORMATTING_RULE = "carrierCodeFormattingRule";
        private const string CARRIER_SPECIFIC = "carrierSpecific";
        private const string COUNTRY_CODE = "countryCode";
        private const string EMERGENCY = "emergency";
        private const string EXAMPLE_NUMBER = "exampleNumber";
        private const string FIXED_LINE = "fixedLine";
        private const string FORMAT = "Format";
        private const string GENERAL_DESC = "generalDesc";
        private const string INTERNATIONAL_PREFIX = "internationalPrefix";
        private const string INTL_FORMAT = "intlFormat";
        private const string LEADING_DIGITS = "leadingDigits";
        private const string MAIN_COUNTRY_FOR_CODE = "mainCountryForCode";
        private const string MOBILE = "mobile";
        private const string MOBILE_NUMBER_PORTABLE_REGION = "mobileNumberPortableRegion";
        private const string NATIONAL_NUMBER_PATTERN = "nationalNumberPattern";
        private const string NATIONAL_PREFIX = "nationalPrefix";
        private const string NATIONAL_PREFIX_FORMATTING_RULE = "nationalPrefixFormattingRule";

        private const string NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING =
            "nationalPrefixOptionalWhenFormatting";

        private const string NATIONAL_PREFIX_FOR_PARSING = "nationalPrefixForParsing";
        private const string NATIONAL_PREFIX_TRANSFORM_RULE = "nationalPrefixTransformRule";
        private const string NO_INTERNATIONAL_DIALLING = "noInternationalDialling";
        private const string NUMBER_FORMAT = "numberFormat";
        private const string PAGER = "pager";
        private const string PATTERN = "pattern";
        private const string PERSONAL_NUMBER = "personalNumber";
        private const string POSSIBLE_LENGTHS = "possibleLengths";
        private const string NATIONAL = "national";
        private const string LOCAL_ONLY = "localOnly";
        private const string PREFERRED_EXTN_PREFIX = "preferredExtnPrefix";
        private const string PREFERRED_INTERNATIONAL_PREFIX = "preferredInternationalPrefix";
        private const string PREMIUM_RATE = "premiumRate";
        private const string SHARED_COST = "sharedCost";
        private const string SHORT_CODE = "shortCode";
        private const string SMS_SERVICES = "smsServices";
        private const string STANDARD_RATE = "standardRate";
        private const string TOLL_FREE = "tollFree";
        private const string UAN = "uan";
        private const string VOICEMAIL = "voicemail";
        private const string VOIP = "voip";

        private static readonly HashSet<string> PhoneNumberDescsWithoutMatchingTypes =
            new HashSet<string> {NO_INTERNATIONAL_DIALLING};

        // Build the PhoneMetadataCollection from the input XML file.
        public static PhoneMetadataCollection BuildPhoneMetadataCollection(string name,
            bool liteBuild, bool specialBuild, bool isShortNumberMetadata,
            bool isAlternateFormatsMetadata)
        {
            XDocument document;
#if (NET35 || NET40)
            var asm = Assembly.GetExecutingAssembly();
#else
            var asm = typeof(PhoneNumberUtil).GetTypeInfo().Assembly;
#endif
            using (var input = asm.GetManifestResourceStream(name))
            {
#if NET35
                document = XDocument.Load(new XmlTextReader(input));
#else
                document = XDocument.Load(input);
#endif
            }

            var metadataCollection = new PhoneMetadataCollection.Builder();
            var numOfTerritories = territory.Count;
            // TODO: Infer filter from a single flag.
            var metadataFilter = GetMetadataFilter(liteBuild, specialBuild);
            for (var i = 0; i < numOfTerritories; i++)
            {
                var territoryElement = territory[i];
                // For the main metadata file this should always be set, but for other supplementary data
                // files the country calling code may be all that is needed.
                var regionCode =
                    territoryElement.Attributes(XName.Get("id")).SingleOrDefault()?.Value ?? ""; // todo port good?
                var metadata = LoadCountryMetadata(regionCode, territoryElement,
                    isShortNumberMetadata, isAlternateFormatsMetadata);
                metadataFilter.FilterMetadata(metadata);
                metadataCollection.AddMetadata(metadata);
            }

            return metadataCollection.Build();
        }

        // Build a mapping from a country calling code to the region codes which denote the country/region
        // represented by that country code. In the case of multiple countries sharing a calling code,
        // such as the NANPA countries, the one indicated with "isMainCountryForCode" in the metadata
        // should be first.
        public static Dictionary<int, List<string>> BuildCountryCodeToRegionCodeMap(
            PhoneMetadataCollection metadataCollection)
        {
            var countryCodeToRegionCodeMap = new Dictionary<int, List<string>>();
            foreach (var metadata in metadataCollection.MetadataList)
            {
                var regionCode = metadata.Id;
                var countryCode = metadata.CountryCode;
                if (countryCodeToRegionCodeMap.ContainsKey(countryCode))
                {
                    if (metadata.MainCountryForCode)
                    {
                        countryCodeToRegionCodeMap[countryCode].Insert(0, regionCode);
                    }
                    else
                    {
                        countryCodeToRegionCodeMap[countryCode].Add(regionCode);
                    }
                }
                else
                {
                    // For most countries, there will be only one region code for the country calling code.
                    var listWithRegionCode = new List<string>(1);
                    if (!regionCode.Equals(""))
                    {
                        // For alternate formats, there are no region codes at all.
                        listWithRegionCode.Add(regionCode);
                    }

                    countryCodeToRegionCodeMap.Add(countryCode, listWithRegionCode);
                }
            }

            return countryCodeToRegionCodeMap;
        }

        static string ValidateRE(string regex, bool removeWhitespace = false)
        {
            // Removes all the whitespace and newline from the regexp. Not using pattern compile options to
            // make it work across programming languages.
            var compressedRegex = removeWhitespace ? regex.Trim() : regex;
            compressedRegex = new Regex(compressedRegex, InternalRegexOptions.Default).ToString();
            // We don't ever expect to see | followed by a ) in our metadata - this would be an indication
            // of a bug. If one wants to make something optional, we prefer ? to using an empty group.
            var errorIndex = compressedRegex.IndexOf("|)");
            if (errorIndex >= 0)
            {
                throw new FormatException($"| followed by )  {compressedRegex} @ {errorIndex}");
            }

            // return the regex if it is of correct syntax, i.e. compile did not fail with a
            // PatternSyntaxException.
            return compressedRegex;
        }

        /**
         * Returns the national prefix of the provided country element.
         */
        static string GetNationalPrefix(XElement element)
        {
            return element.Attributes(XName.Get(NATIONAL_PREFIX)).SingleOrDefault()?.Value ?? "";

        }

        static PhoneMetadata.Builder LoadTerritoryTagMetadata(string regionCode, XElement element,
            string nationalPrefix)
        {
            var metadata = new PhoneMetadata.Builder();
            metadata.SetId(regionCode);
            if (element.HasAttribute(COUNTRY_CODE))
                metadata.SetCountryCode(int.Parse(element.GetAttribute(COUNTRY_CODE)));
            if (element.HasAttribute(LEADING_DIGITS))
                metadata.SetLeadingDigits(ValidateRE(element.GetAttribute(LEADING_DIGITS)));
            if (element.HasAttribute(INTERNATIONAL_PREFIX))
                metadata.SetInternationalPrefix(ValidateRE(element.GetAttribute(INTERNATIONAL_PREFIX)));
            if (element.HasAttribute(PREFERRED_INTERNATIONAL_PREFIX))
                metadata.SetPreferredInternationalPrefix(
                    element.GetAttribute(PREFERRED_INTERNATIONAL_PREFIX));
            if (element.HasAttribute(NATIONAL_PREFIX_FOR_PARSING))
            {
                metadata.SetNationalPrefixForParsing(
                    ValidateRE(element.Attributes(XName.Get(NATIONAL_PREFIX_FOR_PARSING)).Single().Value, true));
                if (element.Attributes(XName.Get(NATIONAL_PREFIX_TRANSFORM_RULE)).Any())
                {
                    metadata.SetNationalPrefixTransformRule(
                        ValidateRE(element.Attributes(XName.Get(NATIONAL_PREFIX_TRANSFORM_RULE)).Single().Value));
                }
            }

            if (nationalPrefix.Length != 0)
            {
                metadata.SetNationalPrefix(nationalPrefix);
                if (!metadata.HasNationalPrefixForParsing)
                {
                    metadata.SetNationalPrefixForParsing(nationalPrefix);
                }
            }

            if (element.Attributes(XName.Get(PREFERRED_EXTN_PREFIX)).Any())
            {
                metadata.SetPreferredExtnPrefix(element.Attributes(XName.Get(PREFERRED_EXTN_PREFIX)).Single().Value);
            }

            if (element.Attributes(XName.Get(MAIN_COUNTRY_FOR_CODE)).Any())
            {
                metadata.SetMainCountryForCode(true);
            }

            if (element.Attributes(XName.Get(MOBILE_NUMBER_PORTABLE_REGION)).Any())
            {
                metadata.SetMobileNumberPortableRegion(true);
            }

            return metadata;
        }

        /**
         * Extracts the pattern for international Format. If there is no intlFormat, default to using the
         * national Format. If the intlFormat is set to "NA" the intlFormat should be ignored.
         *
         * @throws  Exception if multiple intlFormats have been encountered.
         * @return  whether an international number Format is defined.
         */
        static bool LoadInternationalFormat(PhoneMetadata.Builder metadata,
            XElement numberFormatElement,
            NumberFormat nationalFormat)
        {
            var intlFormat = new NumberFormat.Builder();
            var intlFormatPattern = numberFormatElement.Elements(XName.Get(INTL_FORMAT)).ToList();
            var hasExplicitIntlFormatDefined = false;

            if (intlFormatPattern.Count > 1)
            {
                var countryId = metadata.Id.Length > 0
                    ? metadata.Id
                    : metadata.CountryCode.ToString();
                throw new Exception("Invalid number of intlFormat patterns for country: " + countryId);
            }
            else if (intlFormatPattern.Count == 0)
            {
                // Default to use the same as the national pattern if none is defined.
                intlFormat.MergeFrom(nationalFormat);
            }
            else
            {
                intlFormat.SetPattern(numberFormatElement.Attributes(XName.Get(PATTERN)).Single().Value);
                SetLeadingDigitsPatterns(numberFormatElement, intlFormat);
                var intlFormatPatternValue = intlFormatPattern[0].FirstAttribute.Value;
                if (!intlFormatPatternValue.Equals("NA"))
                {
                    intlFormat.SetFormat(intlFormatPatternValue);
                }

                hasExplicitIntlFormatDefined = true;
            }

            if (intlFormat.HasFormat)
            {
                metadata.AddIntlNumberFormat(intlFormat);
            }

            return hasExplicitIntlFormatDefined;
        }

        /**
         * Extracts the pattern for the national Format.
         *
         * @throws  Exception if multiple or no formats have been encountered.
         */
        private static void LoadNationalFormat(PhoneMetadata.Builder metadata, XElement numberFormatElement,
            NumberFormat.Builder format)
        {
            SetLeadingDigitsPatterns(numberFormatElement, format);
            format.SetPattern(ValidateRE(numberFormatElement.Attributes(XName.Get(PATTERN)).Single().Value));

            var formatPattern = numberFormatElement.Elements(XName.Get(FORMAT)).ToList();
            var numFormatPatterns = formatPattern.Count;
            if (numFormatPatterns != 1)
            {
                var countryId = metadata.Id.Length > 0
                    ? metadata.Id
                    : metadata.CountryCode.ToString();
                throw new Exception("Invalid number of Format patterns (" + numFormatPatterns
                                                                          + ") for country: " + countryId);
            }

            format.SetFormat(formatPattern[0].FirstAttribute.Value);
        }

        /**
         * Extracts the available formats from the provided DOM element. If it does not contain any
         * nationalPrefixFormattingRule, the one passed-in is retained; similarly for
         * nationalPrefixOptionalWhenFormatting. The nationalPrefix, nationalPrefixFormattingRule and
         * nationalPrefixOptionalWhenFormatting values are provided from the parent (territory) element.
         */
        private static void LoadAvailableFormats(PhoneMetadata.Builder metadata,
            XElement element, string nationalPrefix,
            string nationalPrefixFormattingRule,
            bool nationalPrefixOptionalWhenFormatting)
        {
            var carrierCodeFormattingRule = "";
            if (element.Attributes(XName.Get(CARRIER_CODE_FORMATTING_RULE)).Any())
            {
                carrierCodeFormattingRule = ValidateRE(
                    GetDomesticCarrierCodeFormattingRuleFromElement(element, nationalPrefix));
            }

            var numberFormatElements = element.Elements(XName.Get(NUMBER_FORMAT)).ToList();
            var hasExplicitIntlFormatDefined = false;

            var numOfFormatElements = numberFormatElements.Count;
            if (numOfFormatElements > 0)
            {
                for (var i = 0; i < numOfFormatElements; i++)
                {
                    var numberFormatElement = numberFormatElements[i];
                    var format = new NumberFormat.Builder();

                    if (numberFormatElement.Attributes(XName.Get(NATIONAL_PREFIX_FORMATTING_RULE)).Any())
                    {
                        format.SetNationalPrefixFormattingRule(
                            GetNationalPrefixFormattingRuleFromElement(numberFormatElement, nationalPrefix));
                    }
                    else if (!nationalPrefixFormattingRule.Equals(""))
                    {
                        format.SetNationalPrefixFormattingRule(nationalPrefixFormattingRule);
                    }

                    if (numberFormatElement.Attributes(XName.Get(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING)).Any())
                    {
                        format.SetNationalPrefixOptionalWhenFormatting(
                            bool.Parse(numberFormatElement.Attributes(XName.Get(
                                NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING)).Single().Value));
                    }
                    else if (format.NationalPrefixOptionalWhenFormatting
                             != nationalPrefixOptionalWhenFormatting)
                    {
                        // Inherit from the parent field if it is not already the same as the default.
                        format.SetNationalPrefixOptionalWhenFormatting(nationalPrefixOptionalWhenFormatting);
                    }

                    if (numberFormatElement.Attributes(XName.Get(CARRIER_CODE_FORMATTING_RULE)).Any())
                    {
                        format.SetDomesticCarrierCodeFormattingRule(ValidateRE(
                            GetDomesticCarrierCodeFormattingRuleFromElement(numberFormatElement,
                                nationalPrefix)));
                    }
                    else if (!carrierCodeFormattingRule.Equals(""))
                    {
                        format.SetDomesticCarrierCodeFormattingRule(carrierCodeFormattingRule);
                    }

                    LoadNationalFormat(metadata, numberFormatElement, format);
                    metadata.AddNumberFormat(format);

                    if (LoadInternationalFormat(metadata, numberFormatElement, format.Build()))
                    {
                        hasExplicitIntlFormatDefined = true;
                    }
                }

                // Only a small number of regions need to specify the intlFormats in the xml. For the majority
                // of countries the intlNumberFormat metadata is an exact copy of the national NumberFormat
                // metadata. To minimize the size of the metadata file, we only keep intlNumberFormats that
                // actually differ in some way to the national formats.
                if (!hasExplicitIntlFormatDefined)
                {
                    metadata.ClearIntlNumberFormat();
                }
            }
        }

        private static void SetLeadingDigitsPatterns(XContainer numberFormatElement, NumberFormat.Builder format)
        {
            var leadingDigitsPatternNodes = numberFormatElement.Elements(XName.Get(LEADING_DIGITS)).ToList();
            var numOfLeadingDigitsPatterns = leadingDigitsPatternNodes.Count();
            if (numOfLeadingDigitsPatterns > 0)
            {
                for (var i = 0; i < numOfLeadingDigitsPatterns; i++)
                {
                    format.AddLeadingDigitsPattern(
                        ValidateRE(leadingDigitsPatternNodes[i].FirstAttribute.Value, true));
                }
            }
        }

        private static string GetNationalPrefixFormattingRuleFromElement(XElement element,
            string nationalPrefix)
        {
            var nationalPrefixFormattingRule =
                element.Attributes(XName.Get(NATIONAL_PREFIX_FORMATTING_RULE)).Single().Value;
            // Replace $NP with national prefix and $FG with the first group ($1).
            nationalPrefixFormattingRule =
                ReplaceFirst(nationalPrefixFormattingRule, "\\$NP", nationalPrefix);
            nationalPrefixFormattingRule = ReplaceFirst(nationalPrefixFormattingRule, "\\$FG", "\\$1");
            return nationalPrefixFormattingRule;
        }

        private static string GetDomesticCarrierCodeFormattingRuleFromElement(XElement element,
            string nationalPrefix)
        {
            var carrierCodeFormattingRule =
                element.Attributes(XName.Get(CARRIER_CODE_FORMATTING_RULE)).Single().Value;
            // Replace $FG with the first group ($1) and $NP with the national prefix.
            carrierCodeFormattingRule = ReplaceFirst(carrierCodeFormattingRule, "\\$FG", "\\$1");
            carrierCodeFormattingRule = ReplaceFirst(carrierCodeFormattingRule, "\\$NP", nationalPrefix);
            return carrierCodeFormattingRule;
        }

    /**
     * Checks if the possible lengths provided as a sorted set are equal to the possible lengths
     * stored already in the description pattern. Note that possibleLengths may be empty but must not
     * be null, and the PhoneNumberDesc passed in should also not be null.
     */
        private static bool ArePossibleLengthsEqual(ICollection<int> possibleLengths,
            PhoneNumberDesc desc)
        {
            if (possibleLengths.Count != desc.PossibleLengthCount)
            {
                return false;
            }

            // Note that both should be sorted already, and we know they are the same Length.
            var i = 0;
            foreach (var length in possibleLengths)
            {
                if (length != desc.GetPossibleLength(i))
                {
                    return false;
                }

                i++;
            }

            return true;
        }

        /**
         * Processes a phone number description element from the XML file and returns it as a
         * PhoneNumberDesc. If the description element is a fixed line or mobile number, the parent
         * description will be used to fill in the whole element if necessary, or any components that are
         * missing. For all other types, the parent description will only be used to fill in missing
         * components if the type has a partial definition. For example, if no "tollFree" element exists,
         * we assume there are no toll free numbers for that locale, and return a phone number description
         * with no national number data and [-1] for the possible lengths. Note that the parent
         * description must therefore already be processed before this method is called on any child
         * elements.
         *
         * @param parentDesc  a generic phone number description that will be used to fill in missing
         *     parts of the description, or null if this is the root node. This must be processed before
         *     this is run on any child elements.
         * @param countryElement  the XML element representing all the country information
         * @param numberType  the name of the number type, corresponding to the appropriate tag in the XML
         *     file with information about that type
         * @return  complete description of that phone number type
         */
        private static PhoneNumberDesc.Builder ProcessPhoneNumberDescElement(PhoneNumberDesc parentDesc,
            XContainer countryElement,
            string numberType)
        {
            var phoneNumberDescList = countryElement.Elements(XName.Get(numberType)).ToList();
            var numberDesc = new PhoneNumberDesc.Builder();
            if (!phoneNumberDescList.Any())
            {
                // -1 will never match a possible phone number Length, so is safe to use to ensure this never
                // matches. We don't leave it empty, since for compression reasons, we use the empty list to
                // mean that the generalDesc possible lengths apply.
                numberDesc.AddPossibleLength(-1);
                return numberDesc;
            }

            if (phoneNumberDescList.Count > 1)
            {
                throw new Exception($"Multiple elements with type {numberType} found.");
            }

            var element = phoneNumberDescList[0];
            if (parentDesc != null)
            {
                // New way of handling possible number lengths. We don't do this for the general
                // description, since these tags won't be present; instead we will calculate its values
                // based on the values for all the other number type descriptions (see
                // setPossibleLengthsGeneralDesc).
                var lengths = new HashSet<int>();
                var localOnlyLengths = new HashSet<int>();
                PopulatePossibleLengthSets(element, lengths, localOnlyLengths);
                SetPossibleLengths(lengths, localOnlyLengths, parentDesc, numberDesc);
            }

            var validPattern = element.Elements(XName.Get(NATIONAL_NUMBER_PATTERN)).ToList();
            if (validPattern.Any())
            {
                numberDesc.SetNationalNumberPattern(
                    ValidateRE(validPattern[0].FirstAttribute.Value, true));
            }

            var exampleNumber = element.Elements(XName.Get(EXAMPLE_NUMBER)).ToList();
            if (exampleNumber.Any())
            {
                numberDesc.SetExampleNumber(exampleNumber[0].FirstAttribute.Value);
            }

            return numberDesc;
        }
        private static void SetRelevantDescPatterns(PhoneMetadata.Builder metadata, XElement element,
            bool isShortNumberMetadata)
        {
            var generalDescBuilder = ProcessPhoneNumberDescElement(null, element,
                GENERAL_DESC);
            // Calculate the possible lengths for the general description. This will be based on the
            // possible lengths of the child elements.
            SetPossibleLengthsGeneralDesc(
                generalDescBuilder, metadata.Id, element, isShortNumberMetadata);
            metadata.SetGeneralDesc(generalDescBuilder);

            var generalDesc = metadata.GeneralDesc;

            if (!isShortNumberMetadata)
            {
                // HashSet fields used by regular Length phone numbers.
                metadata.SetFixedLine(ProcessPhoneNumberDescElement(generalDesc, element, FIXED_LINE));
                metadata.SetMobile(ProcessPhoneNumberDescElement(generalDesc, element, MOBILE));
                metadata.SetSharedCost(ProcessPhoneNumberDescElement(generalDesc, element, SHARED_COST));
                metadata.SetVoip(ProcessPhoneNumberDescElement(generalDesc, element, VOIP));
                metadata.SetPersonalNumber(ProcessPhoneNumberDescElement(generalDesc, element,
                    PERSONAL_NUMBER));
                metadata.SetPager(ProcessPhoneNumberDescElement(generalDesc, element, PAGER));
                metadata.SetUan(ProcessPhoneNumberDescElement(generalDesc, element, UAN));
                metadata.SetVoicemail(ProcessPhoneNumberDescElement(generalDesc, element, VOICEMAIL));
                metadata.SetNoInternationalDialling(ProcessPhoneNumberDescElement(generalDesc, element,
                    NO_INTERNATIONAL_DIALLING));
                var mobileAndFixedAreSame = metadata.Mobile.NationalNumberPattern
                    .Equals(metadata.FixedLine.NationalNumberPattern);
                if (metadata.SameMobileAndFixedLinePattern != mobileAndFixedAreSame)
                {
                    // HashSet this if it is not the same as the default.
                    metadata.SetSameMobileAndFixedLinePattern(mobileAndFixedAreSame);
                }

                metadata.SetTollFree(ProcessPhoneNumberDescElement(generalDesc, element, TOLL_FREE));
                metadata.SetPremiumRate(ProcessPhoneNumberDescElement(generalDesc, element, PREMIUM_RATE));
            }
            else
            {
                // HashSet fields used by short numbers.
                metadata.SetStandardRate(ProcessPhoneNumberDescElement(generalDesc, element, STANDARD_RATE));
                metadata.SetShortCode(ProcessPhoneNumberDescElement(generalDesc, element, SHORT_CODE));
                metadata.SetCarrierSpecific(ProcessPhoneNumberDescElement(generalDesc, element,
                    CARRIER_SPECIFIC));
                metadata.SetEmergency(ProcessPhoneNumberDescElement(generalDesc, element, EMERGENCY));
                metadata.SetTollFree(ProcessPhoneNumberDescElement(generalDesc, element, TOLL_FREE));
                metadata.SetPremiumRate(ProcessPhoneNumberDescElement(generalDesc, element, PREMIUM_RATE));
                metadata.SetSmsServices(ProcessPhoneNumberDescElement(generalDesc, element, SMS_SERVICES));
            }
        }

        /**
         * Parses a possible Length string into a set of the integers that are covered.
         *
         * @param possibleLengthString  a string specifying the possible lengths of phone numbers. Follows
         *     this syntax: ranges or elements are separated by commas, and ranges are specified in
         *     [min-max] notation, inclusive. For example, [3-5],7,9,[11-14] should be parsed to
         *     3,4,5,7,9,11,12,13,14.
         */
        private static HashSet<int> ParsePossibleLengthStringToSet(string possibleLengthString)
        {
            if (possibleLengthString.Length == 0)
            {
                throw new Exception("Empty possibleLength string found.");
            }

            var lengths = possibleLengthString.Split(',');
            var lengthSet = new HashSet<int>();
            foreach (var lengthSubstring in lengths)
            {
                if (lengthSubstring.Length == 0)
                {
                    throw new Exception("Leading, trailing or adjacent commas in possible "
                                        + $"Length string {possibleLengthString}, these should only separate numbers or ranges.");
                }

                if (lengthSubstring[0] == '[')
                {
                    if (lengthSubstring[lengthSubstring.Length - 1] != ']')
                    {
                        throw new Exception("Missing end of range character in possible "
                                            + $"Length string {possibleLengthString}.");
                    }

                    // Strip the leading and trailing [], and split on the -.
                    var minMax = lengthSubstring.Substring(1, lengthSubstring.Length - 1).Split('-');
                    if (minMax.Length != 2)
                    {
                        throw new Exception("Ranges must have exactly one - character in "
                                            + $"missing for {possibleLengthString}");
                    }

                    var min = int.Parse(minMax[0]);
                    var max = int.Parse(minMax[1]);
                    // We don't even accept [6-7] since we prefer the shorter 6,7 variant; for a range to be in
                    // use the hyphen needs to replace at least one digit.
                    if (max - min < 2)
                    {
                        throw new Exception("The first number in a range should be two or "
                                            + $"more digits lower than the second. Culprit possibleLength string: {possibleLengthString}");
                    }

                    for (var j = min; j <= max; j++)
                    {
                        if (!lengthSet.Add(j))
                        {
                            throw new Exception($"Duplicate Length element found ({j}) in "
                                                + $"possibleLength string {possibleLengthString}");
                        }
                    }
                }
                else
                {
                    var length = int.Parse(lengthSubstring);
                    if (!lengthSet.Add(length))
                    {
                        throw new Exception($"Duplicate Length element found ({length}) in "
                                            + $"possibleLength string {possibleLengthString}");
                    }
                }
            }

            return lengthSet;
        }

        /**
         * Reads the possible lengths present in the metadata and splits them into two sets in one for
         * full-Length numbers, one for local numbers.
         *
         * @param data  one or more phone number descriptions, represented as XML nodes
         * @param lengths  a set to which to add possible lengths of full phone numbers
         * @param localOnlyLengths  a set to which to add possible lengths of phone numbers only diallable
         *     locally (e.g. within a province)
         */
        private static void PopulatePossibleLengthSets(XContainer data, HashSet<int> lengths,
            HashSet<int> localOnlyLengths)
        {
            var possibleLengths = data.Elements(XName.Get(POSSIBLE_LENGTHS)).ToList();
            foreach (var element in possibleLengths)
            {
                var nationalLengths =
                    element.Attributes(XName.Get(NATIONAL)).Single().Value; //getAttribute(NATIONAL);
                // We don't add to the phone metadata yet, since we want to sort Length elements found under
                // different nodes first, make sure there are no duplicates between them and that the
                // localOnly lengths don't overlap with the others.
                var thisElementLengths = ParsePossibleLengthStringToSet(nationalLengths);
                if (element.Attributes(XName.Get(LOCAL_ONLY)).Any())
                {
                    var localLengths = element.Attributes(XName.Get(LOCAL_ONLY)).Single().Value;
                    var thisElementLocalOnlyLengths = ParsePossibleLengthStringToSet(localLengths);
                    if (thisElementLengths.Intersect(thisElementLocalOnlyLengths).Any())
                    {
                        throw new Exception("Possible Length(s) found specified as a normal and local-only Length");
                    }

                    // We check again when we set these lengths on the metadata itself in setPossibleLengths
                    // that the elements in localOnly are not also in lengths. For e.g. the generalDesc, it
                    // might have a local-only Length for one type that is a normal Length for another type. We
                    // don't consider this an error, but we do want to remove the local-only lengths.
                    localOnlyLengths.AddAll(thisElementLocalOnlyLengths);
                }

                // It is okay if at this time we have duplicates, because the same Length might be possible
                // for e.g. fixed-line and for mobile numbers, and this method operates potentially on
                // multiple phoneNumberDesc XML elements.
                lengths.AddAll(thisElementLengths);
            }
        }

        /**
         * Sets possible lengths in the general description, derived from certain child elements.
         */
        private static void SetPossibleLengthsGeneralDesc(PhoneNumberDesc.Builder generalDesc, string metadataId,
            XElement data, bool isShortNumberMetadata)
        {
            var lengths = new HashSet<int>();
            var localOnlyLengths = new HashSet<int>();
            // The general description node should *always* be present if metadata for other types is
            // present, aside from in some unit tests.
            // (However, for e.g. formatting metadata in PhoneNumberAlternateFormats, no PhoneNumberDesc
            // elements are present).
            var generalDescNodes = data.Elements(XName.Get(GENERAL_DESC)).ToList();
            if (generalDescNodes.Any())
            {
                var generalDescNode = generalDescNodes[0];
                PopulatePossibleLengthSets(generalDescNode, lengths, localOnlyLengths);
                if (lengths.Count != 0 || localOnlyLengths.Count != 0)
                {
                    // We shouldn't have anything specified at the "general desc" level: we are going to
                    // calculate this ourselves from child elements.
                    throw new Exception("Found possible lengths specified at general "
                                                      + $"desc: this should be derived from child elements. Affected country: {metadataId}");
                }
            }

            if (!isShortNumberMetadata)
            {
                // Make a copy here since we want to remove some nodes, but we don't want to do that on our
                // actual data.
                var allDescData = new XElement(data);
                foreach (var tag in PhoneNumberDescsWithoutMatchingTypes)
                {
                    var nodesToRemove = allDescData.Elements(XName.Get(tag)).ToList();
                    // We check when we process phone number descriptions that there are only one of each
                    // type, so this is safe to do.
                    if (nodesToRemove.Any())
                    {
                        nodesToRemove[0].Remove();
                    }
                }

                PopulatePossibleLengthSets(allDescData, lengths, localOnlyLengths);
            }
            else
            {
                // For short number metadata, we want to copy the lengths from the "short code" section only.
                // This is because it's the more detailed validation pattern, it's not a sub-type of short
                // codes. The other lengths will be checked later to see that they are a sub-set of these
                // possible lengths.
                var shortCodeDescList = data.Elements(XName.Get(SHORT_CODE)).ToList();
                if (shortCodeDescList.Any())
                {
                    var shortCodeDesc = shortCodeDescList[0];
                    PopulatePossibleLengthSets(shortCodeDesc, lengths, localOnlyLengths);
                }

                if (localOnlyLengths.Count > 0)
                {
                    throw new Exception("Found local-only lengths in short-number metadata");
                }
            }

            SetPossibleLengths(lengths, localOnlyLengths, null, generalDesc);
        }

/**
 * Sets the possible Length fields in the metadata from the sets of data passed in. Checks that
 * the Length is covered by the "parent" phone number description element if one is present, and
 * if the lengths are exactly the same as this, they are not filled in for efficiency reasons.
 *
 * @param parentDesc  the "general description" element or null if desc is the generalDesc itself
 * @param desc  the PhoneNumberDesc object that we are going to set lengths for
 */
        private static void SetPossibleLengths(ICollection<int> lengths,
            IEnumerable<int> localOnlyLengths, PhoneNumberDesc parentDesc, PhoneNumberDesc.Builder desc)
        {
            // We clear these fields since the metadata tends to inherit from the parent element for other
            // fields (via a MergeFrom).
            desc.ClearPossibleLength();
            desc.ClearPossibleLengthLocalOnly();
            // Only add the lengths to this sub-type if they aren't exactly the same as the possible
            // lengths in the general desc (for metadata size reasons).
            if (parentDesc == null || !ArePossibleLengthsEqual(lengths, parentDesc))
            {
                foreach (var length in lengths)
                {
                    if (parentDesc == null || parentDesc.PossibleLengthList.Contains(length))
                    {
                        desc.AddPossibleLength(length);
                    }
                    else
                    {
                        // We shouldn't have possible lengths defined in a child element that are not covered by
                        // the general description. We check this here even though the general description is
                        // derived from child elements because it is only derived from a subset, and we need to
                        // ensure *all* child elements have a valid possible Length.
                        throw new Exception(
                            $"Out-of-range possible Length found (${length}), parent lengths ${parentDesc.PossibleLengthList}.");
                    }
                }
            }

            // We check that the local-only Length isn't also a normal possible Length (only relevant for
            // the general-desc, since within elements such as fixed-line we would throw an exception if we
            // saw this) before adding it to the collection of possible local-only lengths.
            foreach (var length in localOnlyLengths)
            {
                if (!lengths.Contains(length))
                {
                    // We check it is covered by either of the possible Length sets of the parent
                    // PhoneNumberDesc, because for example 7 might be a valid localOnly Length for mobile, but
                    // a valid national Length for fixedLine, so the generalDesc would have the 7 removed from
                    // localOnly.
                    if (parentDesc == null || parentDesc.PossibleLengthLocalOnlyList.Contains(length)
                                           || parentDesc.PossibleLengthList.Contains(length))
                    {
                        desc.AddPossibleLengthLocalOnly(length);
                    }
                    else
                    {
                        throw new Exception(
                            $"Out-of-range local-only possible Length found (${length}), parent Length ${parentDesc.PossibleLengthLocalOnlyList}.");
                    }
                }
            }
        }

        private static PhoneMetadata.Builder LoadCountryMetadata(string regionCode,
            XElement element,
            bool isShortNumberMetadata,
            bool isAlternateFormatsMetadata)
        {
            var nationalPrefix = GetNationalPrefix(element);
            var metadata = LoadTerritoryTagMetadata(regionCode, element, nationalPrefix);
            var nationalPrefixFormattingRule =
                GetNationalPrefixFormattingRuleFromElement(element, nationalPrefix);
            LoadAvailableFormats(metadata, element, nationalPrefix,
                nationalPrefixFormattingRule,
                element.Attributes(XName.Get(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING)).Any());
            if (!isAlternateFormatsMetadata)
            {
                // The alternate formats metadata does not need most of the patterns to be set.
                SetRelevantDescPatterns(metadata, element, isShortNumberMetadata);
            }
        }

        public static Dictionary<int, List<string>> GetCountryCodeToRegionCodeMap(string filePrefix)
        {
#if (NET35 || NET40)
            var asm = Assembly.GetExecutingAssembly();
#else
            var asm = typeof(BuildMetadataFromXml).GetTypeInfo().Assembly;
#endif
            var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(filePrefix)) ?? "missing";
            var collection = BuildPhoneMetadataCollection(name, false, false, false, false); // todo lite/special build
            return BuildCountryCodeToRegionCodeMap(collection);
        }

        /**
         * Processes the custom build flags and gets a {@code MetadataFilter} which may be used to
         * filter {@code PhoneMetadata} objects. Incompatible flag combinations throw Exception.
         *
         * @param liteBuild  The liteBuild flag value as given by the command-line
         * @param specialBuild  The specialBuild flag value as given by the command-line
         */
        internal static MetadataFilter GetMetadataFilter(bool liteBuild, bool specialBuild)
        {
            if (specialBuild)
            {
                if (liteBuild)
                {
                    throw new Exception("liteBuild and specialBuild may not both be set");
                }

                return MetadataFilter.ForSpecialBuild();
            }

            if (liteBuild)
            {
                return MetadataFilter.ForLiteBuild();
            }

            return MetadataFilter.EmptyFilter();
        }

        private static string ReplaceFirst(string input, string value, string replacement)
        {
            var p = input.IndexOf(value, StringComparison.Ordinal);
            if (p >= 0)
                input = input.Substring(0, p) + replacement + input.Substring(p + value.Length);
            return input;
        }
    }
}
