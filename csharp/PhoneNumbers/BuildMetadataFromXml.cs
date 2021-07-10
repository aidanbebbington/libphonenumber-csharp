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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
#if NET35
using System.Xml;
#endif
using System.Xml.Linq;

namespace PhoneNumbers
{
    public class BuildMetadataFromXml
    {
        // String constants used to fetch the XML nodes and attributes.
        private const string CARRIER_CODE_FORMATTING_RULE = "carrierCodeFormattingRule";

        private const string CARRIER_SPECIFIC = "carrierSpecific";
        private const string COUNTRY_CODE = "countryCode";
        private const string EMERGENCY = "emergency";
        private const string EXAMPLE_NUMBER = "exampleNumber";
        private const string FIXED_LINE = "fixedLine";
        private const string FORMAT = "format";
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

        // Build the PhoneMetadataCollection from the input XML file.
        [Obsolete("This method was public to be @VisibleForTesting, it will be moved to internal in a future release.")]
        public static PhoneMetadataCollection BuildPhoneMetadataCollection(string name, bool liteBuild, bool specialBuild, bool isShortNumberMetadata, bool isAlternateFormatsMetadata)
            => BuildPhoneMetadataCollection(name, null, liteBuild, specialBuild, isShortNumberMetadata, isAlternateFormatsMetadata, nameSuffix: false);

        internal static PhoneMetadataCollection BuildPhoneMetadataCollection(string name, Assembly assembly = null,
            bool liteBuild = false, bool specialBuild = false, bool isShortNumberMetadata = false,
            bool isAlternateFormatsMetadata = false, bool nameSuffix = true)
        {
            XDocument document;
#if NETSTANDARD1_3 || PORTABLE
            assembly ??= typeof(PhoneNumber).GetTypeInfo().Assembly;
#else
            assembly ??= typeof(PhoneNumber).Assembly;
#endif

            if (nameSuffix)
                name = assembly.GetManifestResourceNames()
                           .FirstOrDefault(n => n.EndsWith(name, StringComparison.Ordinal)) ??
                       throw new ArgumentException(name + " resource not found");

            using (var input = assembly.GetManifestResourceStream(name))
            {
#if NET35
                document = XDocument.Load(new XmlTextReader(input));
#else
                document = XDocument.Load(input);
#endif
            }

            var metadataCollection = new PhoneMetadataCollection.Builder();
            var metadataFilter = GetMetadataFilter(liteBuild, specialBuild);
            foreach (var metadata in document.Root.Element("territories")
                .Elements()
                .Select(territory => LoadCountryMetadataInternal(territory.GetAttribute("id"), territory,
                    isShortNumberMetadata, isAlternateFormatsMetadata)))
            {
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
            var countryCodeToRegionCodeMap =
                new Dictionary<int, List<string>>();
            foreach (var metadata in metadataCollection.MetadataList)
            {
                var regionCode = metadata.Id;
                var countryCode = metadata.CountryCode;
                if (countryCodeToRegionCodeMap.TryGetValue(countryCode, out var list))
                {
                    if (metadata.MainCountryForCode)
                        list.Insert(0, regionCode);
                    else
                        list.Add(regionCode);
                }
                else
                {
                    // For most countries, there will be only one region code for the country calling code.
                    var listWithRegionCode = new List<string>(1);
                    if (regionCode.Length > 0)
                        listWithRegionCode.Add(regionCode);
                    countryCodeToRegionCodeMap[countryCode] = listWithRegionCode;
                }
            }
            return countryCodeToRegionCodeMap;
        }

        private static readonly HashSet<string> ValidPatterns = new HashSet<string>();

        [Obsolete("This method was public to be @VisibleForTesting, it will be moved to internal in a future release.")]
        public static string ValidateRE(string regex, bool removeWhitespace = false)
            => ValidateRegex(regex, removeWhitespace);

        public static string ValidateRegex(string regex, bool removeWhitespace = false)
        {
            // Removes all the whitespace and newline from the regexp. Not using pattern compile options to
            // make it work across programming languages.
            if (removeWhitespace)
            {
                for (var i = 0; i < regex.Length; i++)
                    if (char.IsWhiteSpace(regex[i]))
                    {
                        var sb = new StringBuilder(regex, 0, i, regex.Length);
                        while (++i < regex.Length)
                        {
                            if (!char.IsWhiteSpace(regex[i]))
                                sb.Append(regex[i]);
                        }
                        regex = sb.ToString();
                        break;
                    }
            }

            lock (ValidPatterns)
                if (!ValidPatterns.Contains(regex))
                {
#pragma warning disable S1848
                    // ReSharper disable once ObjectCreationAsStatement
                    new Regex(regex, RegexOptions.CultureInvariant);
#pragma warning restore S1848
                    ValidPatterns.Add(regex);
                }
            // return regex itself if it is of correct regex syntax
            // i.e. compile did not fail with a PatternSyntaxException.
            return regex;
        }

        /**
        * Returns the national prefix of the provided country element.
        */
        [Obsolete("This method was public to be @VisibleForTesting, it will be moved to internal in a future release.")]
        public static string GetNationalPrefix(XElement element) => GetNationalPrefixInternal(element);

        internal static string GetNationalPrefixInternal(XElement element)
        {
            return element.GetAttribute(NATIONAL_PREFIX);
        }

        public static PhoneMetadata.Builder LoadTerritoryTagMetadata(string regionCode, XElement element,
            string nationalPrefix)
        {
            var metadata = new PhoneMetadata.Builder();
            metadata.SetId(regionCode);
            if (element.HasAttribute(COUNTRY_CODE))
                metadata.SetCountryCode(int.Parse(element.GetAttribute(COUNTRY_CODE)));
            if (element.HasAttribute(LEADING_DIGITS))
                metadata.SetLeadingDigits(ValidateRegex(element.GetAttribute(LEADING_DIGITS)));
            if (element.HasAttribute(INTERNATIONAL_PREFIX))
                metadata.SetInternationalPrefix(ValidateRegex(element.GetAttribute(INTERNATIONAL_PREFIX)));
            if (element.HasAttribute(PREFERRED_INTERNATIONAL_PREFIX))
                metadata.SetPreferredInternationalPrefix(
                    element.GetAttribute(PREFERRED_INTERNATIONAL_PREFIX));
            if (element.HasAttribute(NATIONAL_PREFIX_FOR_PARSING))
            {
                metadata.SetNationalPrefixForParsing(
                    ValidateRegex(element.GetAttribute(NATIONAL_PREFIX_FOR_PARSING), true));
                if (element.HasAttribute(NATIONAL_PREFIX_TRANSFORM_RULE))
                    metadata.SetNationalPrefixTransformRule(
                        ValidateRegex(element.GetAttribute(NATIONAL_PREFIX_TRANSFORM_RULE)));
            }
            if (!string.IsNullOrEmpty(nationalPrefix))
            {
                metadata.SetNationalPrefix(nationalPrefix);
                if (!metadata.HasNationalPrefixForParsing)
                    metadata.SetNationalPrefixForParsing(nationalPrefix);
            }
            if (element.HasAttribute(PREFERRED_EXTN_PREFIX))
                metadata.SetPreferredExtnPrefix(element.GetAttribute(PREFERRED_EXTN_PREFIX));
            if (element.HasAttribute(MAIN_COUNTRY_FOR_CODE))
                metadata.SetMainCountryForCode(true);
            if (element.HasAttribute(MOBILE_NUMBER_PORTABLE_REGION))
                metadata.SetMobileNumberPortableRegion(true);
            return metadata;
        }

        /**
        * Extracts the pattern for international format. If there is no intlFormat, default to using the
        * national format. If the intlFormat is set to "NA" the intlFormat should be ignored.
        *
        * @throws  RuntimeException if multiple intlFormats have been encountered.
        * @return  whether an international number format is defined.
        */
        [Obsolete("This method was public to be @VisibleForTesting, it will be moved to internal in a future release.")]
        public static bool LoadInternationalFormat(PhoneMetadata.Builder metadata, XElement numberFormatElement,
            string nationalFormat)
            => LoadInternationalFormatInternal(metadata, numberFormatElement, nationalFormat);

        public static bool LoadInternationalFormatInternal(PhoneMetadata.Builder metadata,
            XElement numberFormatElement,
            string nationalFormat)
        {
            var intlFormat = new NumberFormat.Builder();
            SetLeadingDigitsPatterns(numberFormatElement, intlFormat);
            intlFormat.SetPattern(numberFormatElement.GetAttribute(PATTERN));
            var intlFormatPattern = numberFormatElement.Elements(INTL_FORMAT).ToList();
            var hasExplicitIntlFormatDefined = false;

            if (intlFormatPattern.Count > 1)
                throw new Exception("Invalid number of intlFormat patterns for country: " +
                                    metadata.Id);
            if (intlFormatPattern.Count == 0)
            {
                // Default to use the same as the national pattern if none is defined.
                intlFormat.SetFormat(nationalFormat);
            }
            else
            {
                var intlFormatPatternValue = intlFormatPattern.First().Value;
                intlFormat.SetFormat(intlFormatPatternValue);
                hasExplicitIntlFormatDefined = true;
            }

            if (intlFormat.HasFormat)
                metadata.AddIntlNumberFormat(intlFormat);
            return hasExplicitIntlFormatDefined;
        }

        /**
         * Extracts the pattern for the national format.
         *
         * @throws  RuntimeException if multiple or no formats have been encountered.
         * @return  the national format string.
         */
        [Obsolete("This method was public to be @VisibleForTesting, it will be moved to internal in a future release.")]
        public static string LoadNationalFormat(PhoneMetadata.Builder metadata, XElement numberFormatElement,
            NumberFormat.Builder format)
            => LoadNationalFormatInternal(metadata, numberFormatElement, format);

        internal static string LoadNationalFormatInternal(PhoneMetadata.Builder metadata, XElement numberFormatElement,
            NumberFormat.Builder format)
        {
            SetLeadingDigitsPatterns(numberFormatElement, format);
            format.SetPattern(ValidateRegex(numberFormatElement.GetAttribute(PATTERN)));

            var formatPattern = numberFormatElement.Elements(FORMAT).ToList();
            if (formatPattern.Count != 1)
                throw new Exception("Invalid number of format patterns for country: " +
                                    metadata.Id);
            var nationalFormat = formatPattern[0].Value;
            format.SetFormat(nationalFormat);
            return nationalFormat;
        }

        /**
        *  Extracts the available formats from the provided DOM element. If it does not contain any
        *  nationalPrefixFormattingRule, the one passed-in is retained. The nationalPrefix,
        *  nationalPrefixFormattingRule and nationalPrefixOptionalWhenFormatting values are provided from
        *  the parent (territory) element.
        */


        [Obsolete("This method was public to be @VisibleForTesting, it will be moved to internal in a future release.")]
        public static void LoadAvailableFormats(PhoneMetadata.Builder metadata, XElement element, string nationalPrefix,
            string nationalPrefixFormattingRule, bool nationalPrefixOptionalWhenFormatting)
            => LoadAvailableFormatsInternal(metadata, element, nationalPrefix, nationalPrefixFormattingRule,
                nationalPrefixOptionalWhenFormatting);

        internal static void LoadAvailableFormatsInternal(PhoneMetadata.Builder metadata, XElement element, string nationalPrefix,
            string nationalPrefixFormattingRule, bool nationalPrefixOptionalWhenFormatting)
        {
            var carrierCodeFormattingRule = "";
            if (element.HasAttribute(CARRIER_CODE_FORMATTING_RULE))
                carrierCodeFormattingRule = ValidateRegex(
                    GetDomesticCarrierCodeFormattingRuleFromElement(element, nationalPrefix));

            var availableFormats = element.Element("availableFormats");
            var hasExplicitIntlFormatDefined = false;

            if (availableFormats?.HasElements == true)
            {
                foreach (var numberFormatElement in availableFormats.Elements())
                {
                    var format = new NumberFormat.Builder();

                    if (numberFormatElement.HasAttribute(NATIONAL_PREFIX_FORMATTING_RULE))
                    {
                        format.SetNationalPrefixFormattingRule(
                            GetNationalPrefixFormattingRuleFromElementInternal(numberFormatElement, nationalPrefix));
                    }
                    else if (!nationalPrefixFormattingRule.Equals(""))
                    {
                        format.SetNationalPrefixFormattingRule(nationalPrefixFormattingRule);
                    }

                    if (numberFormatElement.HasAttribute(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING))
                        format.SetNationalPrefixOptionalWhenFormatting(
                            bool.Parse(numberFormatElement.Attribute(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING).Value));
                    else if (format.NationalPrefixOptionalWhenFormatting
                             != nationalPrefixOptionalWhenFormatting)
                    {
                        // Inherit from the parent field if it is not already the same as the default.
                        format.SetNationalPrefixOptionalWhenFormatting(nationalPrefixOptionalWhenFormatting);
                    }
                    if (numberFormatElement.HasAttribute("carrierCodeFormattingRule"))
                        format.SetDomesticCarrierCodeFormattingRule(ValidateRegex(
                            GetDomesticCarrierCodeFormattingRuleFromElement(
                                numberFormatElement, nationalPrefix)));
                    else if (!carrierCodeFormattingRule.Equals(""))
                        format.SetDomesticCarrierCodeFormattingRule(carrierCodeFormattingRule);

                    // Extract the pattern for the national format.
                    var nationalFormat =
                        LoadNationalFormatInternal(metadata, numberFormatElement, format);
                    metadata.AddNumberFormat(format);

                    if (LoadInternationalFormatInternal(metadata, numberFormatElement, nationalFormat))
                        hasExplicitIntlFormatDefined = true;
                }
                // Only a small number of regions need to specify the intlFormats in the xml. For the majority
                // of countries the intlNumberFormat metadata is an exact copy of the national NumberFormat
                // metadata. To minimize the size of the metadata file, we only keep intlNumberFormats that
                // actually differ in some way to the national formats.
                if (!hasExplicitIntlFormatDefined)
                    metadata.ClearIntlNumberFormat();
            }
        }

        public static void SetLeadingDigitsPatterns(XElement numberFormatElement, NumberFormat.Builder format)
        {
            foreach (var e in numberFormatElement.Elements(LEADING_DIGITS))
                format.AddLeadingDigitsPattern(ValidateRegex(e.Value, true));
        }

        internal static string GetNationalPrefixFormattingRuleFromElementInternal(XElement element,
            string nationalPrefix)
        {
            var nationalPrefixFormattingRule = element.GetAttribute(NATIONAL_PREFIX_FORMATTING_RULE);
            // Replace $NP with national prefix and $FG with the first group ($1).
            nationalPrefixFormattingRule = ReplaceFirst(nationalPrefixFormattingRule, "$NP", nationalPrefix);
            nationalPrefixFormattingRule = ReplaceFirst(nationalPrefixFormattingRule, "$FG", "${1}");
            return nationalPrefixFormattingRule;
        }

        public static string GetDomesticCarrierCodeFormattingRuleFromElement(XElement element,
            string nationalPrefix)
        {
            var carrierCodeFormattingRule = element.GetAttribute(CARRIER_CODE_FORMATTING_RULE);
            // Replace $FG with the first group ($1) and $NP with the national prefix.
            carrierCodeFormattingRule = ReplaceFirst(carrierCodeFormattingRule, "$FG", "${1}");
            carrierCodeFormattingRule = ReplaceFirst(carrierCodeFormattingRule, "$NP", nationalPrefix);
            return carrierCodeFormattingRule;
        }

        /**
        * Checks if the possible lengths provided as a sorted set are equal to the possible lengths
        * stored already in the description pattern. Note that possibleLengths may be empty but must not
        * be null, and the PhoneNumberDesc passed in should also not be null.
        */
        private static bool ArePossibleLengthsEqual(SortedSet<int> possibleLengths,
            PhoneNumberDesc desc)
        {
            if (possibleLengths.Count != desc.PossibleLengthCount)
                return false;
            // Note that both should be sorted already, and we know they are the same length.
            var i = 0;
            foreach (var length in possibleLengths)
            {
                if (length != desc.PossibleLengthList[i])
                    return false;
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
        * @param generalDesc  a generic phone number description that will be used to fill in missing
        *                     parts of the description
        * @param countryElement  the XML element representing all the country information
        * @param numberType  the name of the number type, corresponding to the appropriate tag in the XML
        *                    file with information about that type
        * @return  complete description of that phone number type
        */
        [Obsolete("This method was public to be @VisibleForTesting, it will be moved to internal in a future release.")]
        public static PhoneNumberDesc.Builder ProcessPhoneNumberDescElement(PhoneNumberDesc parentDesc,
            XElement countryElement, string numberType)
            => ProcessPhoneNumberDescElementInternal(parentDesc, countryElement, numberType);

        internal static PhoneNumberDesc.Builder ProcessPhoneNumberDescElementInternal(PhoneNumberDesc parentDesc,
            XElement countryElement, string numberType)
        {
            var phoneNumberDescList = countryElement.Elements(numberType).ToList();
            var numberDesc = new PhoneNumberDesc.Builder();
            if (phoneNumberDescList.Count == 0)
            {
                // -1 will never match a possible phone number length, so is safe to use to ensure this never
                // matches. We don't leave it empty, since for compression reasons, we use the empty list to
                // mean that the generalDesc possible lengths apply.
                numberDesc.AddPossibleLength(-1);
                return numberDesc;
            }
            if (phoneNumberDescList.Count > 1)
                throw new Exception($"Multiple elements with type {numberType} found.");
            var element = phoneNumberDescList[0];

            parentDesc ??= new PhoneNumberDesc();
            var lengths = new SortedSet<int>();
            var localOnlyLengths = new SortedSet<int>();
            PopulatePossibleLengthSets(element.Elements(POSSIBLE_LENGTHS), lengths, localOnlyLengths);
            SetPossibleLengths(lengths, localOnlyLengths, parentDesc, numberDesc);

            var validPattern = element.Element(NATIONAL_NUMBER_PATTERN);
            if (validPattern != null)
                numberDesc.SetNationalNumberPattern(ValidateRegex(validPattern.Value, true));

            var exampleNumber = element.Element(EXAMPLE_NUMBER);
            if (exampleNumber != null)
                numberDesc.SetExampleNumber(exampleNumber.Value);

            return numberDesc;
        }

        private static void SetRelevantDescPatterns(PhoneMetadata.Builder metadata, XElement element,
            bool isShortNumberMetadata)
        {
            var generalDescBuilder = ProcessPhoneNumberDescElementInternal(null, element,
                GENERAL_DESC);
            // Calculate the possible lengths for the general description. This will be based on the
            // possible lengths of the child elements.
            SetPossibleLengthsGeneralDesc(
                generalDescBuilder, metadata.Id, element, isShortNumberMetadata);
            metadata.SetGeneralDesc(generalDescBuilder);

            var generalDesc = metadata.GeneralDesc;

            if (!isShortNumberMetadata)
            {
                // Set fields used by regular length phone numbers.
                metadata.SetFixedLine(ProcessPhoneNumberDescElementInternal(generalDesc, element, FIXED_LINE));
                metadata.SetMobile(ProcessPhoneNumberDescElementInternal(generalDesc, element, MOBILE));
                metadata.SetSharedCost(ProcessPhoneNumberDescElementInternal(generalDesc, element, SHARED_COST));
                metadata.SetVoip(ProcessPhoneNumberDescElementInternal(generalDesc, element, VOIP));
                metadata.SetPersonalNumber(ProcessPhoneNumberDescElementInternal(generalDesc, element,
                    PERSONAL_NUMBER));
                metadata.SetPager(ProcessPhoneNumberDescElementInternal(generalDesc, element, PAGER));
                metadata.SetUan(ProcessPhoneNumberDescElementInternal(generalDesc, element, UAN));
                metadata.SetVoicemail(ProcessPhoneNumberDescElementInternal(generalDesc, element, VOICEMAIL));
                metadata.SetNoInternationalDialling(ProcessPhoneNumberDescElementInternal(generalDesc, element,
                    NO_INTERNATIONAL_DIALLING));
                var mobileAndFixedAreSame = metadata.Mobile.NationalNumberPattern
                    .Equals(metadata.FixedLine.NationalNumberPattern);
                if (metadata.SameMobileAndFixedLinePattern != mobileAndFixedAreSame)
                    metadata.SetSameMobileAndFixedLinePattern(mobileAndFixedAreSame);
                metadata.SetTollFree(ProcessPhoneNumberDescElementInternal(generalDesc, element, TOLL_FREE));
                metadata.SetPremiumRate(ProcessPhoneNumberDescElementInternal(generalDesc, element, PREMIUM_RATE));
            }
            else
            {
                // Set fields used by short numbers.
                metadata.SetStandardRate(ProcessPhoneNumberDescElementInternal(generalDesc, element, STANDARD_RATE));
                metadata.SetShortCode(ProcessPhoneNumberDescElementInternal(generalDesc, element, SHORT_CODE));
                metadata.SetCarrierSpecific(ProcessPhoneNumberDescElementInternal(generalDesc, element,
                    CARRIER_SPECIFIC));
                metadata.SetEmergency(ProcessPhoneNumberDescElementInternal(generalDesc, element, EMERGENCY));
                metadata.SetTollFree(ProcessPhoneNumberDescElementInternal(generalDesc, element, TOLL_FREE));
                metadata.SetPremiumRate(ProcessPhoneNumberDescElementInternal(generalDesc, element, PREMIUM_RATE));
                metadata.SetSmsServices(ProcessPhoneNumberDescElementInternal(generalDesc, element, SMS_SERVICES));
            }
        }

        private static SortedSet<int> ParsePossibleLengthStringToSet(string possibleLengthString)
        {
            if (possibleLengthString.Length == 0)
                throw new Exception("Empty possibleLength string found.");
            var lengths = possibleLengthString.Split(',');
            var lengthSet = new SortedSet<int>();
            foreach (var lengthSubstring in lengths)
            {
                if (lengthSubstring.Length == 0)
                    throw new Exception("Leading, trailing or adjacent commas in possible " +
                                        $"length string {possibleLengthString}, these should only separate numbers or ranges.");
                if (lengthSubstring[0] == '[')
                {
                    if (lengthSubstring[lengthSubstring.Length - 1] != ']')
                        throw new Exception("Missing end of range character in possible " +
                                            $"length string {possibleLengthString}.");
                    // Strip the leading and trailing [], and split on the -.
                    var minMax = lengthSubstring.Substring(1, lengthSubstring.Length - 2).Split('-');
                    if (minMax.Length != 2)
                        throw new Exception("Ranges must have exactly one - character: " +
                                            $"missing for {possibleLengthString}.");
                    var min = int.Parse(minMax[0]);
                    var max = int.Parse(minMax[1]);
                    // We don't even accept [6-7] since we prefer the shorter 6,7 variant; for a range to be in
                    // use the hyphen needs to replace at least one digit.
                    if (max - min < 2)
                        throw new Exception("The first number in a range should be two or " +
                                            $"more digits lower than the second. Culprit possibleLength string: {possibleLengthString}");
                    for (var j = min; j <= max; j++)
                        if (!lengthSet.Add(j))
                            throw new Exception($"Duplicate length element found ({j}) in " +
                                                $"possibleLength string {possibleLengthString}");
                }
                else
                {
                    var length = int.Parse(lengthSubstring);
                    if (!lengthSet.Add(length))
                        throw new Exception($"Duplicate length element found ({length}) in " +
                                            $"possibleLength string {possibleLengthString}");
                }
            }
            return lengthSet;
        }

        /**
         * Reads the possible lengths present in the metadata and splits them into two sets: one for
         * full-length numbers, one for local numbers.
         *
         * @param data  one or more phone number descriptions, represented as XML nodes
         * @param lengths  a set to which to add possible lengths of full phone numbers
         * @param localOnlyLengths  a set to which to add possible lengths of phone numbers only diallable
         *     locally (e.g. within a province)
         */
        private static void PopulatePossibleLengthSets(IEnumerable<XElement> possibleLengths, SortedSet<int> lengths,
            SortedSet<int> localOnlyLengths)
        {
            foreach (var element in possibleLengths)
            {
                var nationalLengths = element.GetAttribute(NATIONAL);
                // We don't add to the phone metadata yet, since we want to sort length elements found under
                // different nodes first, make sure there are no duplicates between them and that the
                // localOnly lengths don't overlap with the others.
                var thisElementLengths = ParsePossibleLengthStringToSet(nationalLengths);
                if (element.Attribute(LOCAL_ONLY) is { } localLengths)
                {
                    var thisElementLocalOnlyLengths = ParsePossibleLengthStringToSet(localLengths.Value);
                    if (thisElementLengths.Overlaps(thisElementLocalOnlyLengths))
                        throw new Exception(
                            $"Possible length(s) found specified as a normal and local-only length: {thisElementLengths.Intersect(thisElementLocalOnlyLengths)}");
                    // We check again when we set these lengths on the metadata itself in setPossibleLengths
                    // that the elements in localOnly are not also in lengths. For e.g. the generalDesc, it
                    // might have a local-only length for one type that is a normal length for another type. We
                    // don't consider this an error, but we do want to remove the local-only lengths.
                    foreach (var length in thisElementLocalOnlyLengths)
                        localOnlyLengths.Add(length);
                }
                // It is okay if at this time we have duplicates, because the same length might be possible
                // for e.g. fixed-line and for mobile numbers, and this method operates potentially on
                // multiple phoneNumberDesc XML elements.
                foreach (var length in thisElementLengths)
                    lengths.Add(length);
            }
        }

        /**
         * Sets possible lengths in the general description, derived from certain child elements.
         */
        private static void SetPossibleLengthsGeneralDesc(PhoneNumberDesc.Builder generalDesc, string metadataId,
            XElement data, bool isShortNumberMetadata)
        {
            var lengths = new SortedSet<int>();
            var localOnlyLengths = new SortedSet<int>();
            // The general description node should *always* be present if metadata for other types is
            // present, aside from in some unit tests.
            // (However, for e.g. formatting metadata in PhoneNumberAlternateFormats, no PhoneNumberDesc
            // elements are present).
            var generalDescNode = data.Element(GENERAL_DESC);
            if (generalDescNode != null)
            {
                PopulatePossibleLengthSets(generalDescNode.Elements(POSSIBLE_LENGTHS), lengths, localOnlyLengths);
                if (lengths.Count != 0 || localOnlyLengths.Count != 0)
                    throw new Exception("Found possible lengths specified at general " +
                                        $"desc: this should be derived from child elements. Affected country: {metadataId}");
            }
            if (!isShortNumberMetadata)
            {
                var allDescData = data.Descendants(POSSIBLE_LENGTHS).Where(e => e.Parent?.Name != NO_INTERNATIONAL_DIALLING);
                PopulatePossibleLengthSets(allDescData, lengths, localOnlyLengths);
            }
            else
            {
                // For short number metadata, we want to copy the lengths from the "short code" section only.
                // This is because it's the more detailed validation pattern, it's not a sub-type of short
                // codes. The other lengths will be checked later to see that they are a sub-set of these
                // possible lengths.
                var shortCodeDesc = data.Element(SHORT_CODE);
                if (shortCodeDesc != null)
                {
                    PopulatePossibleLengthSets(shortCodeDesc.Elements(POSSIBLE_LENGTHS), lengths, localOnlyLengths);
                }
                if (localOnlyLengths.Count > 0)
                    throw new Exception("Found local-only lengths in short-number metadata");
            }
            SetPossibleLengths(lengths, localOnlyLengths, null, generalDesc);
        }

        /**
        * Sets the possible length fields in the metadata from the sets of data passed in. Checks that
        * the length is covered by the "parent" phone number description element if one is present, and
        * if the lengths are exactly the same as this, they are not filled in for efficiency reasons.
        *
        * @param parentDesc  the "general description" element or null if desc is the generalDesc itself
        * @param desc  the PhoneNumberDesc object that we are going to set lengths for
        */
        private static void SetPossibleLengths(SortedSet<int> lengths,
            SortedSet<int> localOnlyLengths, PhoneNumberDesc parentDesc, PhoneNumberDesc.Builder desc)
        {
            // Only add the lengths to this sub-type if they aren't exactly the same as the possible
            // lengths in the general desc (for metadata size reasons).
            if (parentDesc == null || !ArePossibleLengthsEqual(lengths, parentDesc))
                foreach (var length in lengths)
                    if (parentDesc?.PossibleLengthList.Contains(length) != false)
                        desc.PossibleLengthList.Add(length);
                    else
                        throw new Exception(
#if NET35
                            $"Out-of-range possible length found ({length}), parent lengths {string.Join(", ", parentDesc.PossibleLengthList.Select(x => x.ToString()).ToArray())}.");
#else
                            $"Out-of-range possible length found ({length}), parent lengths {string.Join(", ", parentDesc.PossibleLengthList)}.");
#endif
            // We check that the local-only length isn't also a normal possible length (only relevant for
            // the general-desc, since within elements such as fixed-line we would throw an exception if we
            // saw this) before adding it to the collection of possible local-only lengths.
            foreach (var length in localOnlyLengths)
                if (!lengths.Contains(length))
                    if (parentDesc?.PossibleLengthLocalOnlyList.Contains(length) != false || parentDesc.PossibleLengthList.Contains(length))
                        desc.PossibleLengthLocalOnlyList.Add(length);
                    else
                        throw new Exception(
#if NET35
                            $"Out-of-range local-only possible length found ({length}), parent length {string.Join(", ", parentDesc.PossibleLengthLocalOnlyList.Select(x => x.ToString()).ToArray())}.");
#else
                            $"Out-of-range local-only possible length found ({length}), parent length {string.Join(", ", parentDesc.PossibleLengthLocalOnlyList)}.");
#endif
        }


        private static string ReplaceFirst(string input, string value, string replacement)
        {
            var p = input.IndexOf(value, StringComparison.Ordinal);
            if (p >= 0)
                input = input.Substring(0, p) + replacement + input.Substring(p + value.Length);
            return input;
        }

        internal static void LoadGeneralDesc(PhoneMetadata.Builder metadata, XElement element)
        {
            var generalDescBuilder = ProcessPhoneNumberDescElementInternal(null, element, GENERAL_DESC);
            SetPossibleLengthsGeneralDesc(generalDescBuilder, metadata.Id, element, false);
            var generalDesc = generalDescBuilder.Build();

            metadata.SetFixedLine(ProcessPhoneNumberDescElementInternal(generalDesc, element, FIXED_LINE));
            metadata.SetMobile(ProcessPhoneNumberDescElementInternal(generalDesc, element, MOBILE));
            metadata.SetTollFree(ProcessPhoneNumberDescElementInternal(generalDesc, element, TOLL_FREE));
            metadata.SetPremiumRate(ProcessPhoneNumberDescElementInternal(generalDesc, element, PREMIUM_RATE));
            metadata.SetSharedCost(ProcessPhoneNumberDescElementInternal(generalDesc, element, SHARED_COST));
            metadata.SetVoip(ProcessPhoneNumberDescElementInternal(generalDesc, element, VOIP));
            metadata.SetPersonalNumber(ProcessPhoneNumberDescElementInternal(generalDesc, element, PERSONAL_NUMBER));
            metadata.SetPager(ProcessPhoneNumberDescElementInternal(generalDesc, element, PAGER));
            metadata.SetUan(ProcessPhoneNumberDescElementInternal(generalDesc, element, UAN));
            metadata.SetVoicemail(ProcessPhoneNumberDescElementInternal(generalDesc, element, VOICEMAIL));
            metadata.SetEmergency(ProcessPhoneNumberDescElementInternal(generalDesc, element, EMERGENCY));
            metadata.SetNoInternationalDialling(
                ProcessPhoneNumberDescElementInternal(generalDesc, element, NO_INTERNATIONAL_DIALLING));
            metadata.SetSameMobileAndFixedLinePattern(
                metadata.Mobile.NationalNumberPattern.Equals(
                    metadata.FixedLine.NationalNumberPattern));
        }

        [Obsolete("This method was public to be @VisibleForTesting, it will be moved to private in a future release.")]
        public static PhoneMetadata.Builder LoadCountryMetadata(string regionCode, XElement element,
            bool isShortNumberMetadata, bool isAlternateFormatsMetadata)
            => LoadCountryMetadataInternal(regionCode, element, isShortNumberMetadata, isAlternateFormatsMetadata);

        private static PhoneMetadata.Builder LoadCountryMetadataInternal(string regionCode,
            XElement element,
            bool isShortNumberMetadata,
            bool isAlternateFormatsMetadata)
        {
            var nationalPrefix = GetNationalPrefixInternal(element);
            var metadata = LoadTerritoryTagMetadata(regionCode, element, nationalPrefix);
            var nationalPrefixFormattingRule = GetNationalPrefixFormattingRuleFromElementInternal(element, nationalPrefix);
            LoadAvailableFormatsInternal(metadata, element, nationalPrefix,
                nationalPrefixFormattingRule,
                element.HasAttribute(NATIONAL_PREFIX_OPTIONAL_WHEN_FORMATTING));
            LoadGeneralDesc(metadata, element);
            if (!isAlternateFormatsMetadata)
                SetRelevantDescPatterns(metadata, element, isShortNumberMetadata);
            return metadata;
        }

        public static Dictionary<int, List<string>> GetCountryCodeToRegionCodeMap(string filePrefix)
        {
            var collection = BuildPhoneMetadataCollection(filePrefix); // todo lite/special build
            return BuildCountryCodeToRegionCodeMap(collection);
        }

        /**
         * Processes the custom build flags and gets a {@code MetadataFilter} which may be used to
        * filter {@code PhoneMetadata} objects. Incompatible flag combinations throw RuntimeException.
        *
        * @param liteBuild  The liteBuild flag value as given by the command-line
        * @param specialBuild  The specialBuild flag value as given by the command-line
        */
        internal static MetadataFilter GetMetadataFilter(bool liteBuild, bool specialBuild)
        {
            if (specialBuild)
            {
                if (liteBuild)
                    throw new Exception("liteBuild and specialBuild may not both be set");
                return MetadataFilter.ForSpecialBuild();
            }
            return liteBuild ? MetadataFilter.ForLiteBuild() : MetadataFilter.EmptyFilter();
        }
    }
}
