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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace PhoneNumbers
{
    /// <summary>
    /// Utility for international phone numbers. Functionality includes formatting, parsing and
    /// validation.
    /// <para>
    /// If you use this library, and want to be notified about important changes, please sign up to
    /// our mailing list: http://groups.google.com/group/libphonenumber-discuss/about
    /// </para>
    /// NOTE: A lot of methods in this class require Region Code strings.These must be provided using
    /// ISO 3166-1 two-letter country-code format.These should be in upper-case. The list of the codes
    /// can be found here:
    /// http://www.iso.org/iso/country_codes/iso_3166_code_lists/country_names_and_code_elements.htm
    /// <!--
    /// @author Shaopeng Jia
    /// @author Lara Rennie
    /// -->
    /// </summary>
    public class PhoneNumberUtil
    {
        // Flags to use when compiling regular expressions for phone numbers.
        internal const RegexOptions RegexFlags = RegexOptions.IgnoreCase | InternalRegexOptions.Default;
        // The minimum and maximum length of the national significant number.
        internal const int MIN_LENGTH_FOR_NSN = 2;
        // The ITU says the maximum length should be 15, but we have found longer numbers in Germany.
        internal const int MAX_LENGTH_FOR_NSN = 17;
        // The maximum length of the country calling code.
        internal const int MAX_LENGTH_COUNTRY_CODE = 3;
        // We don't allow input strings for parsing to be longer than 250 chars. This prevents malicious
        // input from overflowing the regular-expression engine.
        private const int MAX_INPUT_STRING_LENGTH = 250;
        internal const string META_DATA_FILE_PREFIX = "PhoneNumberMetaData.xml";
        internal const string UNKNOWN_REGION = "ZZ";

        // A mapping from a country calling code to the region codes which denote the region represented
        // by that country calling code. In the case of multiple regions sharing a calling code, such as
        // the NANPA regions, the one indicated with "isMainCountryForCode" in the metadata should be
        // first.
        private readonly Dictionary<int, List<string>> countryCallingCodeToRegionCodeMap;

        // The set of regions the library supports.
        // There are roughly 240 of them and we set the initial capacity of the HashSet to 320 to offer a
        // load factor of roughly 0.75.
        private readonly HashSet<string> supportedRegions = new();

        // The set of regions that share country calling code 1.
        private readonly HashSet<string> nanpaRegions = new();
        private const int NANPA_COUNTRY_CODE = 1;

        // The prefix that needs to be inserted in front of a Colombian landline number when dialed from
        // a mobile phone in Colombia.
        private const string COLOMBIA_MOBILE_TO_FIXED_LINE_PREFIX = "3";


        // Map of country calling codes that use a mobile token before the area code. One example of when
        // this is relevant is when determining the length of the national destination code, which should
        // be the length of the area code plus the length of the mobile token.
        private static readonly Dictionary<int, string> MobileTokenMappings = new Dictionary<int, string>
        {
            {52, "1" },
            {54, "9" }
        };

        // Set of country codes that have geographically assigned mobile numbers (see GEO_MOBILE_COUNTRIES
        // below) which are not based on *area codes*. For example, in China mobile numbers start with a
        // carrier indicator, and beyond that are geographically assigned: this carrier indicator is not
        // considered to be an area code.
        private static readonly HashSet<int> GeoMobileCountriesWithoutMobileAreaCodes = new HashSet<int>
        {
            86  // China
        };

        // Set of country calling codes that have geographically assigned mobile numbers. This may not be
        // complete; we add calling codes case by case, as we find geographical mobile numbers or hear
        // from user reports. Note that countries like the US, where we can't distinguish between
        // fixed-line or mobile numbers, are not listed here, since we consider FIXED_LINE_OR_MOBILE to be
        // a possibly geographically-related type anyway (like FIXED_LINE).
        private static readonly HashSet<int> GeoMobileCountries = new HashSet<int>
        {
            52,  // Mexico
            54,  // Argentina
            55,  // Brazil
            62,  // Indonesia: some prefixes only (fixed CMDA wireless)
            86  // China
        };


        // The PLUS_SIGN signifies the international prefix.
        internal const char PLUS_SIGN = '+';

        private const char STAR_SIGN = '*';

        private const string RFC3966_EXTN_PREFIX = ";ext=";
        private const string RFC3966_PREFIX = "tel:";
        private const string RFC3966_PHONE_CONTEXT = ";phone-context=";
        private const string RFC3966_ISDN_SUBADDRESS = ";isub=";

        // A map that contains characters that are essential when dialing. That means any of the
        // characters in this map must not be removed from a number when dialling, otherwise the call will
        // not reach the intended destination.
        private static readonly Dictionary<char, char> DiallableCharMappings;

        // For performance reasons, amalgamate both into one map.
        private static readonly Dictionary<char, char> AlphaPhoneMappings;

        // Separate map of all symbols that we wish to retain when formatting alpha numbers. This
        // includes digits, ASCII letters and number grouping symbols such as "-" and " ".
        private static readonly Dictionary<char, char> AllPlusNumberGroupingSymbols;

        private static readonly object ThisLock;

        // Pattern that makes it easy to distinguish whether a region has a unique international dialing
        // prefix or not. If a region has a unique international prefix (e.g. 011 in USA), it will be
        // represented as a string that contains a sequence of ASCII digits. If there are multiple
        // available international prefixes in a region, they will be represented as a regex string that
        // always contains character(s) other than ASCII digits.
        // Note this regex also includes tilde, which signals waiting for the tone.
        private static readonly PhoneRegex UniqueInternationalPrefix =
            new PhoneRegex("[\\d]+(?:[~\u2053\u223C\uFF5E][\\d]+)?", InternalRegexOptions.Default);

        // Regular expression of acceptable punctuation found in phone numbers. This excludes punctuation
        // found as a leading character only.
        // This consists of dash characters, white space characters, full stops, slashes,
        // square brackets, parentheses and tildes. It also includes the letter 'x' as that is found as a
        // placeholder for carrier information in some phone numbers. Full-width variants are also
        // present.
        internal const string VALID_PUNCTUATION = "-x\u2010-\u2015\u2212\u30FC\uFF0D-\uFF0F " +
            "\u00A0\u00AD\u200B\u2060\u3000()\uFF08\uFF09\uFF3B\uFF3D.\\[\\]/~\u2053\u223C\uFF5E";

        private const string DIGITS = "\\p{Nd}";

        internal const string PLUS_CHARS = "+\uFF0B";
        internal static bool IsPlusChar(char c) => c == '+' || c == '\uFF0B';
        private static readonly PhoneRegex SeparatorPattern = new PhoneRegex("[" + VALID_PUNCTUATION + "]+", InternalRegexOptions.Default);
        private static readonly Regex CapturingDigitPattern;

        /// <summary>Regular expression of acceptable characters that may start a phone number for the purposes of
        /// parsing. This allows us to strip away meaningless prefixes to phone numbers that may be
        /// mistakenly given to us. This consists of digits, the plus symbol and arabic-indic digits. This
        /// does not contain alpha characters, although they may be used later in the number. It also does
        /// not include other punctuation, as this will be stripped later during parsing and is of no
        /// information value when parsing a number.
        /// </summary>
        public static readonly Regex ValidStartCharPattern;

        // Regular expression of characters typically used to start a second phone number for the purposes
        // of parsing. This allows us to strip off parts of the number that are actually the start of
        // another number, such as for: (530) 583-6985 x302/x2303 -> the second extension here makes this
        // actually two phone numbers, (530) 583-6985 x302 and (530) 583-6985 x2303. We remove the second
        // extension so that the first number is parsed correctly.
        private const string SECOND_NUMBER_START = "[\\\\/] *x";

        internal static readonly char[] SecondNumberStartChars = new[] { '\\', '/' };
        internal static readonly Regex SecondNumberStartPattern = new Regex(SECOND_NUMBER_START, InternalRegexOptions.Default);

        // We use this pattern to check if the phone number has at least three letters in it - if so, then
        // we treat it as a number where some phone-number digits are represented by letters.
        private static bool IsValidAlphaPhone(StringBuilder number)
        {
            for (int alpha = 0, i = 0; i < number.Length; i++)
            {
                var lower = number[i] | 0x20;
                if ((uint)(lower - 'a') <= 'z' - 'a' && ++alpha == 3)
                    return true;
            }
            return false;
        }

        // Regular expression of viable phone numbers. This is location independent. Checks we have at
        // least three leading digits, and only valid punctuation, alpha characters and
        // digits in the phone number. Does not include extension data.
        // The symbol 'x' is allowed here as valid punctuation since it is often used as a placeholder for
        // carrier codes, for example in Brazilian phone numbers. We also allow multiple "+" characters at
        // the start.
        // [digits]{minLengthNsn}|
        // plus_sign*(([punctuation]|[star])*[digits]){3,}([punctuation]|[star]|[digits]|[alpha])*
        //
        // The first reg-ex is to allow short numbers (two digits long) to be parsed if they are entered
        // as "15" etc, but only if there is no punctuation in them. The second expression restricts the
        // number of digits to three or more, but then allows them to be in international form, and to
        // have alpha-characters and punctuation.
        //
        // Note VALID_PUNCTUATION starts with a -, so must be the first in the range.

        // Default extension prefix to use when formatting. This will be put in front of any extension
        // component of the number, after the main national number is formatted. For example, if you wish
        // the default extension formatting to be " extn: 3456", then you should specify " extn: " here
        // as the default extension prefix. This can be overridden by region-specific preferences.
        private const string DEFAULT_EXTN_PREFIX = " ext. ";

        // Pattern to capture digits used in an extension. Places a maximum length of "7" for an
        // extension.
        private static readonly string CapturingExtnDigits;
        // Regexp of all possible ways to write extensions, for use when parsing. This will be run as a
        // case-insensitive regexp match. Wide character versions are also provided after each ASCII
        // version.
        internal static readonly string ExtnPatternsForParsing;
        internal static readonly string ExtnPatternsForMatching;

        /// <summary>
        /// Helper initializer method to create the regular-expression pattern to match extensions,
        /// allowing the one-char extension symbols provided by <c>singleExtnSymbols</c>.
        /// </summary>
        private static string CreateExtnPattern(string singleExtnSymbols)
        {
            // There are three regular expressions here. The first covers RFC 3966 format, where the
            // extension is added using ";ext=". The second more generic one starts with optional white
            // space and ends with an optional full stop (.), followed by zero or more spaces/tabs and then
            // the numbers themselves. The other one covers the special case of American numbers where the
            // extension is written with a hash at the end, such as "- 503#".
            // Note that the only capturing groups should be around the digits that you want to capture as
            // part of the extension, or else parsing will fail!
            // Canonical-equivalence doesn't seem to be an option with Android java, so we allow two options
            // for representing the accented o - the character itself, and one in the unicode decomposed
            // form with the combining acute accent.
            return (RFC3966_EXTN_PREFIX + CapturingExtnDigits + "|" + "[ \u00A0\\t,]*" +
            "(?:e?xt(?:ensi(?:o\u0301?|\u00F3))?n?|\uFF45?\uFF58\uFF54\uFF4E?|" +
            "\u0434\u043E\u0431|" + "[" + singleExtnSymbols + "]|int|anexo|\uFF49\uFF4E\uFF54)" +
            "[:\\.\uFF0E]?[ \u00A0\\t,-]*" + CapturingExtnDigits + "#?|" +
            "[- ]+(" + DIGITS + "{1,5})#");
        }

        // Regexp of all known extension prefixes used by different regions followed by 1 or more valid
        // digits, for use when parsing.
        private static readonly Regex ExtnPattern;

        // We append optionally the extension pattern to the end here, as a valid phone number may
        // have an extension prefix appended, followed by 1 or more digits.
        private static readonly PhoneRegex ValidPhoneNumberPattern;

        internal static readonly Regex NonDigitsPattern = new Regex("\\D+", InternalRegexOptions.Default);

        // The FIRST_GROUP_PATTERN was originally set to $1 but there are some countries for which the
        // first group is not used in the national pattern (e.g. Argentina) so the $1 group does not match
        // correctly.  Therefore, we use \d, so that the first group actually used in the pattern will be
        // matched.
        private static readonly Regex FirstGroupPattern = new Regex("(\\$\\d)", InternalRegexOptions.Default);
        // Constants used in the formatting rules to represent the national prefix, first group and
        // carrier code respectively.
        private const string NpPattern = "$NP";
        private const string FgPattern = "$FG";
        private const string CcPattern = "$CC";

        // A pattern that is used to determine if the national prefix formatting rule has the first group
        // only, i.e., does not start with the national prefix. Note that the pattern explicitly allows
        // for unbalanced parentheses.
        private static readonly PhoneRegex FirstGroupOnlyPrefixPattern = new PhoneRegex("\\(?\\$1\\)?", InternalRegexOptions.Default);

        static PhoneNumberUtil()
        {
            ThisLock = new();

            const string asciiDigitMappings = "0123456789";

            var alphaMap = new Dictionary<char, char>
            {
                ['A'] = '2',
                ['B'] = '2',
                ['C'] = '2',
                ['D'] = '3',
                ['E'] = '3',
                ['F'] = '3',
                ['G'] = '4',
                ['H'] = '4',
                ['I'] = '4',
                ['J'] = '5',
                ['K'] = '5',
                ['L'] = '5',
                ['M'] = '6',
                ['N'] = '6',
                ['O'] = '6',
                ['P'] = '7',
                ['Q'] = '7',
                ['R'] = '7',
                ['S'] = '7',
                ['T'] = '8',
                ['U'] = '8',
                ['V'] = '8',
                ['W'] = '9',
                ['X'] = '9',
                ['Y'] = '9',
                ['Z'] = '9'
            };
            var alphaMappings = alphaMap;

            var combinedMap = new Dictionary<char, char>(alphaMappings);
            foreach (var k in asciiDigitMappings)
                combinedMap[k] = k;
            AlphaPhoneMappings = combinedMap;

            var diallableCharMap = new Dictionary<char, char>();
            foreach (var k in asciiDigitMappings)
                diallableCharMap[k] = k;
            diallableCharMap[PLUS_SIGN] = PLUS_SIGN;
            diallableCharMap['*'] = '*';
            DiallableCharMappings = diallableCharMap;

            var allPlusNumberGroupings = new Dictionary<char, char>
            {
                // Put grouping symbols.
                ['-'] = '-',
                ['\uFF0D'] = '-',
                ['\u2010'] = '-',
                ['\u2011'] = '-',
                ['\u2012'] = '-',
                ['\u2013'] = '-',
                ['\u2014'] = '-',
                ['\u2015'] = '-',
                ['\u2212'] = '-',
                ['/'] = '/',
                ['\uFF0F'] = '/',
                [' '] = ' ',
                ['\u3000'] = ' ',
                ['\u2060'] = ' ',
                ['.'] = '.',
                ['\uFF0E'] = '.'
            };
            // Put (lower letter -> upper letter) and (upper letter -> upper letter) mappings.
            foreach (var c in alphaMappings.Keys)
            {
                allPlusNumberGroupings[char.ToLowerInvariant(c)] = c;
                allPlusNumberGroupings[c] = c;
            }

            foreach (var k in asciiDigitMappings)
                allPlusNumberGroupings[k] = k;
            AllPlusNumberGroupingSymbols = allPlusNumberGroupings;

            // We accept alpha characters in phone numbers, ASCII only, upper and lower case.
            var validAlpha = "A-Za-z";

            CapturingDigitPattern = new Regex("(" + DIGITS + ")", InternalRegexOptions.Default);
            var validStartChar = "[" + PLUS_CHARS + DIGITS + "]";
            ValidStartCharPattern = new Regex(validStartChar, InternalRegexOptions.Default);

            CapturingExtnDigits = "(" + DIGITS + "{1,7})";
            var validPhoneNumber = DIGITS + "{" + MIN_LENGTH_FOR_NSN + "}" + "|" +
                                      "[" + PLUS_CHARS + "]*(?:[" + VALID_PUNCTUATION + STAR_SIGN + "]*" + DIGITS + "){3,}[" +
                                      VALID_PUNCTUATION + STAR_SIGN + validAlpha + DIGITS + "]*";

            // One-character symbols that can be used to indicate an extension.
            var singleExtnSymbolsForMatching = "x\uFF58#\uFF03~\uFF5E";
            // For parsing, we are slightly more lenient in our interpretation than for matching. Here we
            // allow a "comma" as a possible extension indicator. When matching, this is hardly ever used to
            // indicate this.
            var singleExtnSymbolsForParsing = ",;" + singleExtnSymbolsForMatching;

            ExtnPatternsForParsing = CreateExtnPattern(singleExtnSymbolsForParsing);
            ExtnPatternsForMatching = CreateExtnPattern(singleExtnSymbolsForMatching);

            ExtnPattern = new Regex("(?:" + ExtnPatternsForParsing + ")$", RegexFlags);

            ValidPhoneNumberPattern =
                new PhoneRegex(validPhoneNumber + "(?:" + ExtnPatternsForParsing + ")?", RegexFlags);
        }

        private static PhoneNumberUtil instance;

        // A mapping from a region code to the PhoneMetadata for that region.
        private readonly Dictionary<string, PhoneMetadata> regionToMetadataMap = new();

        // A mapping from a country calling code for a non-geographical entity to the PhoneMetadata for
        // that country calling code. Examples of the country calling codes include 800 (International
        // Toll Free Service) and 808 (International Shared Cost Service).
        private readonly Dictionary<int, PhoneMetadata> countryCodeToNonGeographicalMetadataMap = new();

        // A cache for frequently used region-specific regular expressions.
        // As most people use phone numbers primarily from one to two countries, and there are roughly 60
        // regular expressions needed, the initial capacity of 100 offers a rough load factor of 0.75.
        private readonly RegexCache regexCache = new(100);

        public const string REGION_CODE_FOR_NON_GEO_ENTITY = "001";

        /// <summary>Types of phone number matches. See detailed description beside the isNumberMatch() method.</summary>
        public enum MatchType
        {
#pragma warning disable 1591
            NOT_A_NUMBER,
            NO_MATCH,
            SHORT_NSN_MATCH,
            NSN_MATCH,
            EXACT_MATCH
#pragma warning restore 1591
        }

        /// <summary>Possible outcomes when testing if a PhoneNumber is possible.</summary>
        public enum ValidationResult
        {
            /// <summary>The number length matches that of valid numbers for this region.</summary>
            IS_POSSIBLE,
            /// <summary>
            /// The number length matches that of local numbers for this region only (i.e. numbers that may
            /// be able to be dialled within an area, but do not have all the information to be dialled from
            /// anywhere inside or outside the country).
            /// </summary>
            IS_POSSIBLE_LOCAL_ONLY,
            /// <summary>The number has an invalid country calling code.</summary>
            INVALID_COUNTRY_CODE,
            /// <summary>The number is shorter than all valid numbers for this region.</summary>
            TOO_SHORT,
            /// <summary>
            /// The number is longer than the shortest valid numbers for this region, shorter than the
            /// longest valid numbers for this region, and does not itself have a number length that matches
            /// valid numbers for this region. This can also be returned in the case where
            /// isPossibleNumberForTypeWithReason was called, and there are no numbers of this type at all
            /// for this region.
            /// </summary>
            INVALID_LENGTH,
            /// <summary>The number is longer than all valid numbers for this region.</summary>
            TOO_LONG
        }

        /// <summary>
        /// Leniency when <see cref="FindNumbers(string, string)"/> finding potential phone numbers in text
        /// segments. The levels here are ordered in increasing strictness.
        /// </summary>
        public enum Leniency
        {
            /// <summary>
            /// Phone numbers accepted are <see cref="IsPossibleNumber(PhoneNumber)"/>
            /// possible, but not necessarily <see cref="IsValidNumber(PhoneNumber)"/> valid.
            /// </summary>
            POSSIBLE,
            /// <summary>
            /// Phone numbers accepted are <see cref="IsPossibleNumber(PhoneNumber)"/>
            /// possible and <see cref="IsValidNumber(PhoneNumber)"/> valid. Numbers written
            /// in national format must have their national-prefix present if it is usually written for a
            /// number of this type.
            /// </summary>
            VALID,
            /// <summary>
            /// Phone numbers accepted are <see cref="IsValidNumber(PhoneNumber)"/> valid and
            /// are grouped in a possible way for this locale. For example, a US number written as
            /// "65 02 53 00 00" and "650253 0000" are not accepted at this leniency level, whereas
            /// "650 253 0000", "650 2530000" or "6502530000" are.
            /// Numbers with more than one '/' symbol are also dropped at this level.
            /// <para>
            /// Warning: This level might result in lower coverage especially for regions outside of country
            /// code "+1". If you are not sure about which level to use, email the discussion group
            /// libphonenumber-discuss@googlegroups.com.
            /// </para>
            /// </summary>
            STRICT_GROUPING,
            /// <summary>
            /// Phone numbers accepted are <see cref="IsValidNumber(PhoneNumber)"/> valid and
            /// are grouped in the same way that we would have formatted it, or as a single block. For
            /// example, a US number written as "650 2530000" is not accepted at this leniency level, whereas
            /// "650 253 0000" or "6502530000" are.
            /// Numbers with more than one '/' symbol are also dropped at this level.
            /// <para>
            /// Warning: This level might result in lower coverage especially for regions outside of country
            /// code "+1". If you are not sure about which level to use, email the discussion group
            /// libphonenumber-discuss@googlegroups.com.
            /// </para>
            /// </summary>
            EXACT_GROUPING
        }

        public bool Verify(
            Leniency leniency,
            PhoneNumber number,
            string candidate,
            PhoneNumberUtil util,
            PhoneNumberMatcher matcher)
        {
            switch (leniency)
            {
                case Leniency.POSSIBLE:
                    return IsPossibleNumber(number);
                case Leniency.VALID:
                    {
                        if (!util.IsValidNumber(number) ||
                            !PhoneNumberMatcher.ContainsOnlyValidXChars(number, candidate, util))
                            return false;
                        return PhoneNumberMatcher.IsNationalPrefixPresentIfRequired(number, util);
                    }
                case Leniency.STRICT_GROUPING:
                    {
                        if (!util.IsValidNumber(number) ||
                           !PhoneNumberMatcher.ContainsOnlyValidXChars(number, candidate, util) ||
                           PhoneNumberMatcher.ContainsMoreThanOneSlash(candidate) ||
                           !PhoneNumberMatcher.IsNationalPrefixPresentIfRequired(number, util))
                        {
                            return false;
                        }
                        return matcher.CheckNumberGroupingIsValid(
                            number, candidate, util, PhoneNumberMatcher.AllNumberGroupsRemainGrouped);
                    }
                case Leniency.EXACT_GROUPING:
                    {
                        if (!util.IsValidNumber(number) ||
                                !PhoneNumberMatcher.ContainsOnlyValidXChars(number, candidate, util) ||
                                PhoneNumberMatcher.ContainsMoreThanOneSlash(candidate) ||
                                !PhoneNumberMatcher.IsNationalPrefixPresentIfRequired(number, util))
                        {
                            return false;
                        }
                        return matcher.CheckNumberGroupingIsValid(
                            number, candidate, util, PhoneNumberMatcher.AllNumberGroupsAreExactlyPresent);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(leniency), leniency, null);
            }
        }

        // Visible for testing
        internal PhoneNumberUtil(string baseFileLocation, Assembly asm = null, Dictionary<int, List<string>> countryCallingCodeToRegionCodeMap = null)
        {
            var phoneMetadata = BuildMetadataFromXml.BuildPhoneMetadata(baseFileLocation, asm);
            this.countryCallingCodeToRegionCodeMap = countryCallingCodeToRegionCodeMap ??= BuildMetadataFromXml.BuildCountryCodeToRegionCodeMap(phoneMetadata);

            foreach (var regionCodes in countryCallingCodeToRegionCodeMap)
                supportedRegions.UnionWith(regionCodes.Value);
            supportedRegions.Remove(REGION_CODE_FOR_NON_GEO_ENTITY);
            if (countryCallingCodeToRegionCodeMap.TryGetValue(NANPA_COUNTRY_CODE, out var regions))
                nanpaRegions.UnionWith(regions);

            foreach (var m in phoneMetadata.MetadataList)
            {
                countryCodeToNonGeographicalMetadataMap[m.CountryCode] = m;
                regionToMetadataMap[m.Id] = m;
            }
            regionToMetadataMap.Remove(REGION_CODE_FOR_NON_GEO_ENTITY);
        }

        /// <summary>
        /// Attempts to extract a possible number from the string passed in. This currently strips all
        /// leading characters that cannot be used to start a phone number. Characters that can be used to
        /// start a phone number are defined in the VALID_START_CHAR_PATTERN. If none of these characters
        /// are found in the number passed in, an empty string is returned. This function also attempts to
        /// strip off any alternative extensions or endings if two or more are present, such as in the case
        /// of: (530) 583-6985 x302/x2303. The second extension here makes this actually two phone numbers,
        /// (530) 583-6985 x302 and (530) 583-6985 x2303. We remove the second extension so that the first
        /// number is parsed correctly.
        /// </summary>
        /// <param name="number">The string that might contain a phone number.</param>
        /// <returns>The number, stripped of any non-phone-number prefix (such as "Tel:") or an empty
        /// string if no character used to start phone numbers (such as + or any digit) is
        /// found in the number.</returns>
        public static string ExtractPossibleNumber(string number)
        {
            if (number.Length == 0)
                return "";
            if (!IsPlusChar(number[0]) && !char.IsDigit(number[0]))
            {
                var m = ValidStartCharPattern.Match(number);
                if (!m.Success)
                    return "";
                number = number.Substring(m.Index);
            }
            // Remove trailing non-alpha non-numerical characters.
            number = PhoneNumberMatcher.TrimAfterUnwantedChars(number);
            // Check for extra numbers at the end.
            var secondStart = number.IndexOfAny(SecondNumberStartChars);
            if (secondStart >= 0)
            {
                var secondNumber = SecondNumberStartPattern.Match(number, secondStart);
                if (secondNumber.Success)
                    number = number.Substring(0, secondNumber.Index);
            }
            return number;
        }

        /// <summary>
        /// Checks to see if the string of characters could possibly be a phone number at all. At the
        /// moment, checks to see that the string begins with at least 2 digits, ignoring any punctuation
        /// commonly found in phone numbers.
        /// This method does not require the number to be normalized in advance - but does assume that
        /// leading non-number symbols have been removed, such as by the method extractPossibleNumber.
        /// </summary>
        /// <param name="number">String to be checked for viability as a phone number.</param>
        /// <returns>True if the number could be a phone number of some sort, otherwise false.</returns>
        public static bool IsViablePhoneNumber(string number)
        {
            if (number.Length < MIN_LENGTH_FOR_NSN)
                return false;
            return ValidPhoneNumberPattern.IsMatchAll(number);
        }

        /// <summary>
        /// Normalizes a string of characters representing a phone number. This performs the following
        /// conversions:
        ///   Punctuation is stripped.
        ///   For ALPHA/VANITY numbers:
        ///   Letters are converted to their numeric representation on a telephone keypad. The keypad
        ///       used here is the one defined in ITU Recommendation E.161. This is only done if there are
        ///       3 or more letters in the number, to lessen the risk that such letters are typos.
        ///   For other numbers:
        ///   Wide-ascii digits are converted to normal ASCII (European) digits.
        ///   Arabic-Indic numerals are converted to European numerals.
        ///   Spurious alpha characters are stripped.
        ///   Arabic-Indic numerals are converted to European numerals.
        /// </summary>
        /// <param name="number">A string of characters representing a phone number.</param>
        /// <returns>The normalized string version of the phone number.</returns>
        public static string Normalize(string number)
        {
            var sb = new StringBuilder(number);
            Normalize(sb);
            return sb.ToString();
        }

        private static void Normalize(StringBuilder number)
        {
            if (IsValidAlphaPhone(number))
            {
                NormalizeHelper(number, AlphaPhoneMappings, true);
            }
            else
            {
                NormalizeDigits(number, false);
            }
        }

        /// <summary>
        /// Normalizes a string of characters representing a phone number. This converts wide-ascii and
        /// arabic-indic numerals to European numerals, and strips punctuation and alpha characters.
        /// </summary>
        /// <param name="number">A string of characters representing a phone number.</param>
        /// <returns>The normalized string version of the phone number.</returns>
        public static string NormalizeDigitsOnly(string number)
        {
            return NormalizeDigits(new StringBuilder(number), false /* strip non-digits */).ToString();
        }

        internal static StringBuilder NormalizeDigits(StringBuilder number, bool keepNonDigits)
        {
            int pos = 0;
            for (int i = 0; i < number.Length; i++)
            {
                var c = number[i];
                if ((uint)(c - '0') <= 9)
                {
                    number[pos++] = c;
                }
                else
                {
                    var digit = (int)char.GetNumericValue(c);
                    if (digit != -1)
                    {
                        var size = number.Length;
                        number.Insert(pos, digit);
                        size = number.Length - size;
                        pos += size;
                        i += size;
                    }
                    else if (keepNonDigits)
                    {
                        number[pos++] = c;
                    }
                }
            }
            number.Length = pos;
            return number;
        }

         /// <summary>
         /// Normalizes a string of characters representing a phone number. This strips all characters which
         /// are not diallable on a mobile phone keypad (including all non-ASCII digits).
         /// </summary>
         /// <param name="number"> a string of characters representing a phone number</param>
         /// <returns> the normalized string version of the phone number</returns>
         public static string NormalizeDiallableCharsOnly(string number)
         {
            return NormalizeHelper(number, DiallableCharMappings, true /* remove non matches */);
         }

        /// <summary>
        /// Converts all alpha characters in a number to their respective digits on a keypad, but retains
        /// existing formatting.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string ConvertAlphaCharactersInNumber(string number)
        {
            return NormalizeHelper(number, AlphaPhoneMappings, false);
        }

        /// <summary>
        /// Gets the length of the geographical area code from the
        /// PhoneNumber object passed in, so that clients could use it
        /// to split a national significant number into geographical area code and subscriber number. It
        /// works in such a way that the resultant subscriber number should be diallable, at least on some
        /// devices. An example of how this could be used:
        ///
        /// <code>
        /// var phoneUtil = PhoneNumberUtil.getInstance();
        /// var number = phoneUtil.parse("16502530000", "US");
        /// var nationalSignificantNumber = phoneUtil.getNationalSignificantNumber(number);
        /// string areaCode;
        /// string subscriberNumber;
        ///
        /// var areaCodeLength = phoneUtil.getLengthOfGeographicalAreaCode(number);
        /// if (areaCodeLength > 0)
        /// {
        ///   areaCode = nationalSignificantNumber.substring(0, areaCodeLength);
        ///   subscriberNumber = nationalSignificantNumber.substring(areaCodeLength);
        /// }
        /// else {
        ///   areaCode = "";
        ///   subscriberNumber = nationalSignificantNumber;
        /// }
        /// </code>
        ///
        /// N.B.: area code is a very ambiguous concept, so the I18N team generally recommends against
        /// using it for most purposes, but recommends using the more general <c>NationalNumber</c>
        /// instead. Read the following carefully before deciding to use this method:
        /// <ul>
        ///  <li> geographical area codes change over time, and this method honors those changes;
        ///    therefore, it doesn't guarantee the stability of the result it produces.</li>
        ///  <li> subscriber numbers may not be diallable from all devices (notably mobile devices, which
        ///    typically requires the full NationalNumber to be dialled in most regions).</li>
        ///  <li> most non-geographical numbers have no area codes, including numbers from non-geographical
        ///    entities</li>
        ///  <li> some geographical numbers have no area codes.</li>
        /// </ul>
        /// </summary>
        ///
        /// <param name="number">the PhoneNumber object for which clients want to know the length of the area code</param>
        /// <returns>the length of area code of the PhoneNumber object passed in</returns>
        public int GetLengthOfGeographicalAreaCode(PhoneNumber number)
        {
            var regionCode = GetRegionCodeForNumber(number);
            if (!IsValidRegionCode(regionCode))
                return 0;
            var metadata = GetMetadataForRegion(regionCode);
            // If a country doesn't use a national prefix, and this number doesn't have an Italian leading
            // zero, we assume it is a closed dialling plan with no area codes.
            if (!metadata.HasNationalPrefix && !number.ItalianLeadingZero)
                return 0;

            var type = GetNumberType(number);
            var countryCallingCode = number.CountryCode;
            if (type == PhoneNumberType.MOBILE
                // Note this is a rough heuristic; it doesn't cover Indonesia well, for example, where area
                // codes are present for some mobile phones but not for others. We have no better way of
                // representing this in the metadata at this point.
                && GeoMobileCountriesWithoutMobileAreaCodes.Contains(countryCallingCode))
            {
                return 0;
            }

            if (!IsNumberGeographical(type, countryCallingCode))
            {
                return 0;
            }
            return GetLengthOfNationalDestinationCode(number);
        }

        /// <summary>
        /// Gets the length of the national destination code (NDC) from the
        /// PhoneNumber object passed in, so that clients could use it
        /// to split a national significant number into NDC and subscriber number. The NDC of a phone
        /// number is normally the first group of digit(s) right after the country calling code when the
        /// number is formatted in the international format, if there is a subscriber number part that
        /// follows.
        ///
        /// N.B.: similar to an area code, not all numbers have an NDC!
        ///
        /// An example of how this could be used:
        ///
        /// <code>
        /// var phoneUtil = PhoneNumberUtil.getInstance();
        /// var number = phoneUtil.parse("18002530000", "US");
        /// var nationalSignificantNumber = phoneUtil.getNationalSignificantNumber(number);
        /// string nationalDestinationCode;
        /// string subscriberNumber;
        ///
        /// var nationalDestinationCodeLength = phoneUtil.getLengthOfNationalDestinationCode(number);
        /// if (nationalDestinationCodeLength > 0)
        /// {
        ///   nationalDestinationCode = nationalSignificantNumber.substring(0,
        ///       nationalDestinationCodeLength);
        ///   subscriberNumber = nationalSignificantNumber.substring(nationalDestinationCodeLength);
        /// }
        /// else
        /// {
        ///   nationalDestinationCode = "";
        ///   subscriberNumber = nationalSignificantNumber;
        /// }
        /// </code>
        ///
        /// Refer to the unit tests to see the difference between this function and
        /// <see cref="GetLengthOfGeographicalAreaCode" />.
        /// </summary>
        ///
        /// <param name="number"> the PhoneNumber object for which clients want to know the length of the NDC.</param>
        /// <returns> the length of NDC of the PhoneNumber object passed in, which could be zero</returns>
        public int GetLengthOfNationalDestinationCode(PhoneNumber number)
        {
            PhoneNumber copiedProto;
            if (number.HasExtension)
            {
                // We don't want to alter the proto given to us, but we don't want to include the extension
                // when we format it, so we copy it and clear the extension here.
                var builder = new PhoneNumber.Builder();
                builder.MergeFrom(number);
                builder.ClearExtension();
                copiedProto = builder.Build();
            }
            else
            {
                copiedProto = number;
            }
            var nationalSignificantNumber = Format(copiedProto, PhoneNumberFormat.INTERNATIONAL);
            var numberGroups = NonDigitsPattern.Split(nationalSignificantNumber);
            // The pattern will start with "+COUNTRY_CODE " so the first group will always be the empty
            // string (before the + symbol) and the second group will be the country calling code. The third
            // group will be area code if it is not the last group.
            if (numberGroups.Length <= 3)
                return 0;

            if (GetNumberType(number) == PhoneNumberType.MOBILE)
            {
                // For example Argentinian mobile numbers, when formatted in the international format, are in
                // the form of +54 9 NDC XXXX.... As a result, we take the length of the third group (NDC) and
                // add the length of the second group (which is the mobile token), which also forms part of
                // the national significant number. This assumes that the mobile token is always formatted
                // separately from the rest of the phone number.
                var mobileToken = GetCountryMobileToken(number.CountryCode);
                if (!mobileToken.Equals(""))
                {
                    return numberGroups[2].Length + numberGroups[3].Length;
                }
            }
            return numberGroups[2].Length;
        }

        /// <summary>
        /// Returns the mobile token for the provided country calling code if it has one, otherwise
        /// returns an empty string. A mobile token is a number inserted before the area code when dialing
        /// a mobile number from that country from abroad.
        /// </summary>
        /// <param name="countryCallingCode">The country calling code for which we want the mobile token.</param>
        /// <returns>The mobile token, as a string, for the given country calling code.</returns>
        public static string GetCountryMobileToken(int countryCallingCode)
        {
            return MobileTokenMappings.TryGetValue(countryCallingCode, out var token) ? token : "";
        }

        /// <summary>
        /// Normalizes a string of characters representing a phone number by replacing all characters found
        /// in the accompanying map with the values therein, and stripping all other characters if
        /// removeNonMatches is true.
        /// </summary>
        /// <param name="number">A string of characters representing a phone number.</param>
        /// <param name="normalizationReplacements">A mapping of characters to what they should be replaced by in
        /// the normalized version of the phone number.</param>
        /// <param name="removeNonMatches">indicates whether characters that are not able to be replaced
        /// should be stripped from the number. If this is false, they
        /// will be left unchanged in the number.</param>
        /// <returns>The normalized string version of the phone number.</returns>
        private static string NormalizeHelper(string number, Dictionary<char, char> normalizationReplacements, bool removeNonMatches)
            => NormalizeHelper(new StringBuilder(number), normalizationReplacements, removeNonMatches).ToString();

        private static StringBuilder NormalizeHelper(StringBuilder number, Dictionary<char, char> normalizationReplacements, bool removeNonMatches)
        {
            int pos = 0;
            for (int i = 0; i < number.Length; i++)
            {
                var character = number[i];
                if (normalizationReplacements.TryGetValue(char.ToUpperInvariant(character), out char newDigit))
                    number[pos++] = newDigit;
                else if (!removeNonMatches)
                    number[pos++] = character;
                // If neither of the above are true, we remove this character.
            }
            number.Length = pos;
            return number;
        }

        /// <summary>
        /// Gets a {@link PhoneNumberUtil} instance to carry out international phone number formatting,
        /// parsing, or validation. The instance is loaded with all phone number metadata.
        /// The <see cref="PhoneNumberUtil" /> is implemented as a singleton.Therefore, calling getInstance
        /// multiple times will only result in one instance being created.
        /// </summary>
        /// <returns> a PhoneNumberUtil instance</returns>
        public static PhoneNumberUtil GetInstance(string baseFileLocation,
            Dictionary<int, List<string>> countryCallingCodeToRegionCodeMap = null)
        {
            lock (ThisLock)
                return instance ??= new PhoneNumberUtil(baseFileLocation, null, countryCallingCodeToRegionCodeMap);
        }

        /// <summary>
        /// Returns all regions the library has metadata for.
        /// </summary>
        /// <returns>An unordered set of the two-letter region codes for every geographical region the
        /// library supports.</returns>
        public HashSet<string> GetSupportedRegions()
        {
            return supportedRegions;
        }

        /// <summary>
        /// Returns all global network calling codes the library has metadata for.
        /// </summary>
        /// <returns>An unordered set of the country calling codes for every non-geographical entity the
        /// library supports.</returns>
        public Dictionary<int, PhoneMetadata>.KeyCollection GetSupportedGlobalNetworkCallingCodes()
        {
            return countryCodeToNonGeographicalMetadataMap.Keys;
        }

        /// <summary>
        /// Returns all country calling codes the library has metadata for, covering both non-geographical
        /// entities (global network calling codes) and those used for geographical entities. This could be
        /// used to populate a drop-down box of country calling codes for a phone-number widget, for
        /// instance.
        /// </summary>
        /// <returns>An unordered set of the country calling codes for every geographical and
        /// non-geographical entity the library supports.</returns>
        public HashSet<int> GetSupportedCallingCodes()
        {
            return new HashSet<int>(countryCallingCodeToRegionCodeMap.Keys);
        }

        /// <summary>
        /// Returns true if there is any possible number data set for a particular PhoneNumberDesc.
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        private static bool DescHasPossibleNumberData(PhoneNumberDesc desc)
        {
            // If this is empty, it means numbers of this type inherit from the "general desc" -> the value
            // "-1" means that no numbers exist for this type.
            return desc.PossibleLengthCount != 1 || desc.PossibleLengthList[0] != -1;
        }

        /// <summary>
        /// Returns true if there is any data set for a particular PhoneNumberDesc.
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        private static bool DescHasData(PhoneNumberDesc desc)
        {
            // Checking most properties since we don't know what's present, since a custom build may have
            // stripped just one of them (e.g. liteBuild strips exampleNumber). We don't bother checking the
            // possibleLengthsLocalOnly, since if this is the only thing that's present we don't really
            // support the type at all: no type-specific methods will work with only this data.
            return desc.HasExampleNumber
                   || DescHasPossibleNumberData(desc)
                   || desc.HasNationalNumberPattern;
        }

        /// <summary>
        /// Returns the types we have metadata for based on the PhoneMetadata object passed in, which must
        /// be non-null.
        /// </summary>
        /// <param name="metadata"></param>
        /// <returns></returns>
        private HashSet<PhoneNumberType> GetSupportedTypesForMetadata(PhoneMetadata metadata)
        {
            var types = new HashSet<PhoneNumberType>();
            foreach (PhoneNumberType type in Enum.GetValues(typeof(PhoneNumberType)))
            {
                if (type == PhoneNumberType.FIXED_LINE_OR_MOBILE || type == PhoneNumberType.UNKNOWN)
                {
                    // Never return FIXED_LINE_OR_MOBILE (it is a convenience type, and represents that a
                    // particular number type can't be determined) or UNKNOWN (the non-type).
                    continue;
                }
                if (DescHasData(GetNumberDescByType(metadata, type)))
                {
                    types.Add(type);
                }
            }
            return types;
        }

        /// <summary>
        /// Returns the types for a given region which the library has metadata for. Will not include
        /// FIXED_LINE_OR_MOBILE (if numbers in this region could be classified as FIXED_LINE_OR_MOBILE,
        /// both FIXED_LINE and MOBILE would be present) and UNKNOWN.
        ///
        /// No types will be returned for invalid or unknown region codes.
        /// </summary>
        /// <param name="regionCode"></param>
        /// <returns></returns>
        public HashSet<PhoneNumberType> GetSupportedTypesForRegion(string regionCode)
        {
            if (!IsValidRegionCode(regionCode))
            {
                return new HashSet<PhoneNumberType>();
            }
            var metadata = GetMetadataForRegion(regionCode);
            return GetSupportedTypesForMetadata(metadata);
        }

        /// <summary>
        /// Returns the types for a country-code belonging to a non-geographical entity which the library
        /// has metadata for. Will not include FIXED_LINE_OR_MOBILE (if numbers for this non-geographical
        /// entity could be classified as FIXED_LINE_OR_MOBILE, both FIXED_LINE and MOBILE would be
        /// present) and UNKNOWN.
        ///
        /// No types will be returned for country calling codes that do not map to a known non-geographical
        /// entity.
        /// </summary>
        /// <param name="countryCallingCode"></param>
        /// <returns></returns>
        public HashSet<PhoneNumberType> GetSupportedTypesForNonGeoEntity(int countryCallingCode)
        {
            var metadata = GetMetadataForNonGeographicalRegion(countryCallingCode);
            return metadata == null ? new HashSet<PhoneNumberType>() : GetSupportedTypesForMetadata(metadata);
        }

        /// <summary>
        /// Gets a <see cref="PhoneNumberUtil"/> instance to carry out international phone number formatting,
        /// parsing, or validation. The instance is loaded with phone number metadata for a number of most
        /// commonly used regions.
        /// <para>
        /// The <see cref="PhoneNumberUtil"/> is implemented as a singleton. Therefore, calling getInstance
        /// multiple times will only result in one instance being created.
        /// </para>
        /// </summary>
        /// <returns>A <see cref="PhoneNumberUtil"/> instance.</returns>
        public static PhoneNumberUtil GetInstance()
        {
            return instance ?? GetInstance(META_DATA_FILE_PREFIX);
        }

        /// <summary>
        /// Helper function to check if the national prefix formatting rule has the first group only, i.e.,
        /// does not start with the national prefix.
        /// </summary>
        /// <param name="nationalPrefixFormattingRule"></param>
        /// <returns></returns>
        internal static bool FormattingRuleHasFirstGroupOnly(string nationalPrefixFormattingRule)
        {
            return nationalPrefixFormattingRule.Length == 0
                   || FirstGroupOnlyPrefixPattern.IsMatchAll(nationalPrefixFormattingRule);
        }

        /// <summary>
        /// Tests whether a phone number has a geographical association. It checks if the number is
        /// associated with a certain region in the country to which it belongs. Note that this doesn't
        /// verify if the number is actually in use.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public bool IsNumberGeographical(PhoneNumber phoneNumber)
        {
            return IsNumberGeographical(GetNumberType(phoneNumber), phoneNumber.CountryCode);
        }

        /// <summary>
        /// Overload of isNumberGeographical(PhoneNumber), since calculating the phone number type is
        /// expensive; if we have already done this, we don't want to do it again.
        /// </summary>
        /// <param name="phoneNumberType"></param>
        /// <param name="countryCallingCode"></param>
        /// <returns></returns>
        public bool IsNumberGeographical(PhoneNumberType phoneNumberType, int countryCallingCode)
        {
            return phoneNumberType == PhoneNumberType.FIXED_LINE
                   || phoneNumberType == PhoneNumberType.FIXED_LINE_OR_MOBILE
                   || GeoMobileCountries.Contains(countryCallingCode)
                       && phoneNumberType == PhoneNumberType.MOBILE;
        }


        /// <summary>
        /// Helper function to check region code is not unknown or null.
        /// </summary>
        private bool IsValidRegionCode(string regionCode)
        {
            return regionCode != null && supportedRegions.Contains(regionCode);
        }

        /// <summary>
        /// Helper function to check the country calling code is valid.
        /// </summary>
        private bool HasValidCountryCallingCode(int countryCallingCode)
        {
            return countryCallingCodeToRegionCodeMap.ContainsKey(countryCallingCode);
        }

        /// <summary>
        /// Formats a phone number in the specified format using default rules. Note that this does not
        /// promise to produce a phone number that the user can dial from where they are - although we do
        /// format in either 'national' or 'international' format depending on what the client asks for, we
        /// do not currently support a more abbreviated format, such as for users in the same "area" who
        /// could potentially dial the number without area code. Note that if the phone number has a
        /// country calling code of 0 or an otherwise invalid country calling code, we cannot work out
        /// which formatting rules to apply so we return the national significant number with no formatting
        /// applied.
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="numberFormat">The format the phone number should be formatted into.</param>
        /// <returns>The formatted phone number.</returns>
        public string Format(PhoneNumber number, PhoneNumberFormat numberFormat)
        {
            if (number.NationalNumber == 0 && number.HasRawInput)
            {
                var rawInput = number.RawInput;
                if (rawInput.Length > 0)
                {
                    return rawInput;
                }
            }
            var formattedNumber = new StringBuilder(20);
            Format(number, numberFormat, formattedNumber);
            return formattedNumber.ToString();
        }

        /// <summary>
        /// Same as <see cref="Format(PhoneNumber, PhoneNumberFormat)"/>, but accepts a mutable StringBuilder as
        /// a parameter to decrease object creation when invoked many times.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="numberFormat"></param>
        /// <param name="formattedNumber"></param>
        public void Format(PhoneNumber number, PhoneNumberFormat numberFormat,
            StringBuilder formattedNumber)
        {
            // Clear the StringBuilder first.
            formattedNumber.Length = 0;
            var countryCallingCode = number.CountryCode;
            var nationalSignificantNumber = GetNationalSignificantNumber(number);
            if (numberFormat == PhoneNumberFormat.E164)
            {
                // Early exit for E164 case since no formatting of the national number needs to be applied.
                // Extensions are not formatted.
                formattedNumber.Append(nationalSignificantNumber);
                PrefixNumberWithCountryCallingCode(countryCallingCode, PhoneNumberFormat.E164,
                    formattedNumber);
                return;
            }
            // Note getRegionCodeForCountryCode() is used because formatting information for regions which
            // share a country calling code is contained by only one region for performance reasons. For
            // example, for NANPA regions it will be contained in the metadata for US.
            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            if (!HasValidCountryCallingCode(countryCallingCode))
            {
                formattedNumber.Append(nationalSignificantNumber);
                return;
            }

            var metadata = GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
            formattedNumber.Append(FormatNsn(nationalSignificantNumber, metadata, numberFormat));
            MaybeAppendFormattedExtension(number, metadata, numberFormat, formattedNumber);
            PrefixNumberWithCountryCallingCode(countryCallingCode, numberFormat, formattedNumber);
        }

        /// <summary>
        /// Formats a phone number in the specified format using client-defined formatting rules. Note that
        /// if the phone number has a country calling code of zero or an otherwise invalid country calling
        /// code, we cannot work out things like whether there should be a national prefix applied, or how
        /// to format extensions, so we return the national significant number with no formatting applied.
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="numberFormat">The format the phone number should be formatted into.</param>
        /// <param name="userDefinedFormats">Formatting rules specified by clients.</param>
        /// <returns>The formatted phone number.</returns>
        public string FormatByPattern(PhoneNumber number, PhoneNumberFormat numberFormat,
            List<NumberFormat> userDefinedFormats)
        {
            var countryCallingCode = number.CountryCode;
            var nationalSignificantNumber = GetNationalSignificantNumber(number);
            // Note getRegionCodeForCountryCode() is used because formatting information for regions which
            // share a country calling code is contained by only one region for performance reasons. For
            // example, for NANPA regions it will be contained in the metadata for US.
            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            if (!HasValidCountryCallingCode(countryCallingCode))
                return nationalSignificantNumber;

            var metadata = GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
            var formattedNumber = new StringBuilder(20);
            var formattingPattern =
                ChooseFormattingPatternForNumber(userDefinedFormats, nationalSignificantNumber);
            if (formattingPattern == null)
            {
                // If no pattern above is matched, we format the number as a whole.
                formattedNumber.Append(nationalSignificantNumber);
            }
            else
            {
                var numFormatCopy = new NumberFormat.Builder();
                // Before we do a replacement of the national prefix pattern $NP with the national prefix, we
                // need to copy the rule so that subsequent replacements for different numbers have the
                // appropriate national prefix.
                numFormatCopy.MergeFrom(formattingPattern);
                var nationalPrefixFormattingRule = formattingPattern.NationalPrefixFormattingRule;
                if (nationalPrefixFormattingRule.Length > 0)
                {
                    var nationalPrefix = metadata.NationalPrefix;
                    if (nationalPrefix.Length > 0)
                    {
                        // Replace $NP with national prefix and $FG with the first group ($1).
                        nationalPrefixFormattingRule = nationalPrefixFormattingRule.Replace(NpPattern, nationalPrefix);
                        nationalPrefixFormattingRule = nationalPrefixFormattingRule.Replace(FgPattern, "$1");
                        numFormatCopy.SetNationalPrefixFormattingRule(nationalPrefixFormattingRule);
                    }
                    else
                    {
                        // We don't want to have a rule for how to format the national prefix if there isn't one.
                        numFormatCopy.ClearNationalPrefixFormattingRule();
                    }
                }
                formattedNumber.Append(
                    FormatNsnUsingPattern(nationalSignificantNumber, numFormatCopy.Build(), numberFormat));
            }
            MaybeAppendFormattedExtension(number, metadata, numberFormat, formattedNumber);
            PrefixNumberWithCountryCallingCode(countryCallingCode, numberFormat, formattedNumber);
            return formattedNumber.ToString();
        }

        /// <summary>
        /// Formats a phone number in national format for dialing using the carrier as specified in the
        /// carrierCode. The carrierCode will always be used regardless of whether the
        /// phone number already has a preferred domestic carrier code stored. If carrierCode
        /// contains an empty string, returns the number in national format without any carrier code.
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="carrierCode">The carrier selection code to be used.</param>
        /// <returns>The formatted phone number in national format for dialing using the carrier as
        /// specified in the carrierCode.</returns>
        public string FormatNationalNumberWithCarrierCode(PhoneNumber number, string carrierCode)
        {
            var countryCallingCode = number.CountryCode;
            var nationalSignificantNumber = GetNationalSignificantNumber(number);
            // Note getRegionCodeForCountryCode() is used because formatting information for regions which
            // share a country calling code is contained by only one region for performance reasons. For
            // example, for NANPA regions it will be contained in the metadata for US.
            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            if (!HasValidCountryCallingCode(countryCallingCode))
                return nationalSignificantNumber;

            var formattedNumber = new StringBuilder(20);
            var metadata = GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
            formattedNumber.Append(FormatNsn(nationalSignificantNumber,
                metadata, PhoneNumberFormat.NATIONAL, carrierCode));
            MaybeAppendFormattedExtension(number, metadata, PhoneNumberFormat.NATIONAL, formattedNumber);
            PrefixNumberWithCountryCallingCode(countryCallingCode, PhoneNumberFormat.NATIONAL, formattedNumber);
            return formattedNumber.ToString();
        }

        private PhoneMetadata GetMetadataForRegionOrCallingCode(
            int countryCallingCode, string regionCode)
        {
            return REGION_CODE_FOR_NON_GEO_ENTITY.Equals(regionCode)
                ? GetMetadataForNonGeographicalRegion(countryCallingCode)
                : GetMetadataForRegion(regionCode);
        }

        /// <summary>
        /// Formats a phone number in national format for dialing using the carrier as specified in the
        /// preferredDomesticCarrierCode field of the PhoneNumber object passed in. If that is missing,
        /// use the fallbackCarrierCode passed in instead. If there is no
        /// preferredDomesticCarrierCode, and the fallbackCarrierCode contains an empty
        /// string, return the number in national format without any carrier code.
        ///
        /// <para>Use <see cref="FormatNationalNumberWithCarrierCode(PhoneNumber, string)"/> instead if the carrier code passed in
        /// should take precedence over the number's preferredDomesticCarrierCode when formatting.</para>
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="fallbackCarrierCode">The carrier selection code to be used, if none is found in the
        /// phone number itself.</param>
        /// <returns>The formatted phone number in national format for dialing using the number's
        /// preferredDomesticCarrierCode, or the fallbackCarrierCode passed in if
        /// none is found.
        /// </returns>
        public string FormatNationalNumberWithPreferredCarrierCode(PhoneNumber number,
            string fallbackCarrierCode)
        {
            return FormatNationalNumberWithCarrierCode(number,
                // Historically, we set this to an empty string when parsing with raw input if none was
                // found in the input string. However, this doesn't result in a number we can dial. For this
                // reason, we treat the empty string the same as if it isn't set at all.
                number.PreferredDomesticCarrierCode.Length > 0
                ? number.PreferredDomesticCarrierCode
                : fallbackCarrierCode);
        }

        /// <summary>
        /// Returns a number formatted in such a way that it can be dialed from a mobile phone in a
        /// specific region.If the number cannot be reached from the region(e.g.some countries block
        /// toll-free numbers from being called outside of the country), the method returns an empty
        /// string.
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="regionCallingFrom">The region where the call is being placed.</param>
        /// <param name="withFormatting">Whether the number should be returned with formatting symbols, such as
        /// spaces and dashes.</param>
        /// <returns>The formatted phone number.</returns>
        public string FormatNumberForMobileDialing(PhoneNumber number, string regionCallingFrom,
            bool withFormatting)
        {
            var countryCallingCode = number.CountryCode;
            if (!HasValidCountryCallingCode(countryCallingCode))
            {
                return number.RawInput ?? "";
            }

            var formattedNumber = "";
            // Clear the extension, as that part cannot normally be dialed together with the main number.
            var numberNoExt = new PhoneNumber.Builder().MergeFrom(number).ClearExtension().Build();
            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            var numberType = GetNumberType(numberNoExt);
            var isValidNumber = numberType != PhoneNumberType.UNKNOWN;
            if (regionCallingFrom == regionCode)
            {
                var isFixedLineOrMobile =
                    numberType == PhoneNumberType.FIXED_LINE || numberType == PhoneNumberType.MOBILE
                                                               || numberType == PhoneNumberType.FIXED_LINE_OR_MOBILE;
                // Carrier codes may be needed in some countries. We handle this here.
                if (regionCode == "CO" && numberType == PhoneNumberType.FIXED_LINE)
                {
                    formattedNumber =
                        FormatNationalNumberWithCarrierCode(numberNoExt, COLOMBIA_MOBILE_TO_FIXED_LINE_PREFIX);
                }
                else if (regionCode == "BR" && isFixedLineOrMobile)
                {
                    // Historically, we set this to an empty string when parsing with raw input if none was
                    // found in the input string. However, this doesn't result in a number we can dial. For this
                    // reason, we treat the empty string the same as if it isn't set at all.
                    formattedNumber = numberNoExt.PreferredDomesticCarrierCode.Length > 0
                        ? formattedNumber = FormatNationalNumberWithPreferredCarrierCode(numberNoExt, "")
                        // Brazilian fixed line and mobile numbers need to be dialed with a carrier code when
                        // called within Brazil. Without that, most of the carriers won't connect the call.
                        // Because of that, we return an empty string here.
                        : "";
                }
                else if
                    (countryCallingCode == NANPA_COUNTRY_CODE)
                {
                    // For NANPA countries, we output international format for numbers that can be dialed
                    // internationally, since that always works, except for numbers which might potentially be
                    // short numbers, which are always dialled in national format.
                    var regionMetadata = GetMetadataForRegion(regionCallingFrom);
                    if (CanBeInternationallyDialled(numberNoExt)
                        && TestNumberLength(GetNationalSignificantNumber(numberNoExt).Length, regionMetadata)
                            != ValidationResult.TOO_SHORT)
                    {
                        formattedNumber = Format(numberNoExt, PhoneNumberFormat.INTERNATIONAL);
                    }
                    else
                    {
                        formattedNumber = Format(numberNoExt, PhoneNumberFormat.NATIONAL);
                    }
                }
                else
                {
                    // For non-geographical countries, and Mexican, Chilean, and Uzbek fixed line and mobile
                    // numbers, we output international format for numbers that can be dialed internationally as
                    // that always works.
                    if (regionCode == REGION_CODE_FOR_NON_GEO_ENTITY
                        // MX fixed line and mobile numbers should always be formatted in international format,
                        // even when dialed within MX. For national format to work, a carrier code needs to be
                        // used, and the correct carrier code depends on if the caller and callee are from the
                        // same local area. It is trickier to get that to work correctly than using
                        // international format, which is tested to work fine on all carriers.
                        // CL fixed line numbers need the national prefix when dialing in the national format,
                        // but don't have it when used for display. The reverse is true for mobile numbers.  As
                        // a result, we output them in the international format to make it work.
                        // UZ mobile and fixed-line numbers have to be formatted in international format or
                        // prefixed with special codes like 03, 04 (for fixed-line) and 05 (for mobile) for
                        // dialling successfully from mobile devices. As we do not have complete information on
                        // special codes and to be consistent with formatting across all phone types we return
                        // the number in international format here.
                        || (regionCode == "MX" || regionCode == "CL"
                            || regionCode == "UZ" && isFixedLineOrMobile)
                        && CanBeInternationallyDialled(numberNoExt))
                    {
                        formattedNumber = Format(numberNoExt, PhoneNumberFormat.INTERNATIONAL);
                    }
                    else
                    {
                        formattedNumber = Format(numberNoExt, PhoneNumberFormat.NATIONAL);
                    }
                }
            }
            else if (isValidNumber && CanBeInternationallyDialled(numberNoExt))
            {
                // We assume that short numbers are not diallable from outside their region, so if a number
                // is not a valid regular length phone number, we treat it as if it cannot be internationally
                // dialled.
                return withFormatting
                    ? Format(numberNoExt, PhoneNumberFormat.INTERNATIONAL)
                    : Format(numberNoExt, PhoneNumberFormat.E164);
            }
            return withFormatting ? formattedNumber
                          : NormalizeDiallableCharsOnly(formattedNumber);
        }

        /// <summary>
        /// Formats a phone number for out-of-country dialing purposes.If no regionCallingFrom is
        /// supplied, we format the number in its INTERNATIONAL format.If the country calling code is the
        /// same as that of the region where the number is from, then NATIONAL formatting will be applied.
        /// <para>
        /// If the number itself has a country calling code of zero or an otherwise invalid country
        /// calling code, then we return the number with no formatting applied.
        /// </para>
        /// <para>Note this function takes care of the case for calling inside of NANPA and between Russia and
        /// Kazakhstan (who share the same country calling code). In those cases, no international prefix
        /// is used.For regions which have multiple international prefixes, the number in its
        /// INTERNATIONAL format will be returned instead.</para>
        /// </summary>
        /// <param name="number">The phone number to be formatted.</param>
        /// <param name="regionCallingFrom">The region where the call is being placed.</param>
        /// <returns>The formatted phone number.</returns>
        public string FormatOutOfCountryCallingNumber(PhoneNumber number, string regionCallingFrom)
        {
            if (!IsValidRegionCode(regionCallingFrom))
            {
                // LOGGER.log(Level.WARNING,
                //      "Trying to format number from invalid region "
                //      + regionCallingFrom
                //      + ". International formatting applied.");
                return Format(number, PhoneNumberFormat.INTERNATIONAL);
            }
            var countryCallingCode = number.CountryCode;
            var nationalSignificantNumber = GetNationalSignificantNumber(number);
            if (!HasValidCountryCallingCode(countryCallingCode))
                return nationalSignificantNumber;
            if (countryCallingCode == NANPA_COUNTRY_CODE)
            {
                if (IsNANPACountry(regionCallingFrom))
                {
                    // For NANPA regions, return the national format for these regions but prefix it with the
                    // country calling code.
                    return countryCallingCode + " " + Format(number, PhoneNumberFormat.NATIONAL);
                }
            }
            else if (countryCallingCode == GetCountryCodeForValidRegion(regionCallingFrom))
            {
                // For regions that share a country calling code, the country calling code need not be dialled.
                // This also applies when dialling within a region, so this if clause covers both these cases.
                // Technically this is the case for dialling from La Reunion to other overseas departments of
                // France (French Guiana, Martinique, Guadeloupe), but not vice versa - so we don't cover this
                // edge case for now and for those cases return the version including country calling code.
                // Details here: http://www.petitfute.com/voyage/225-info-pratiques-reunion
                return Format(number, PhoneNumberFormat.NATIONAL);
            }
            var metadataForRegionCallingFrom = GetMetadataForRegion(regionCallingFrom);
            var internationalPrefix = metadataForRegionCallingFrom.InternationalPrefix;

            // In general, if there is a preferred international prefix, use that. Otherwise, for regions
            // that have multiple international prefixes, the international format of the number is
            // returned since we would not know which one to use.
            var internationalPrefixForFormatting = "";
            if (metadataForRegionCallingFrom.HasPreferredInternationalPrefix)
            {
                internationalPrefixForFormatting =
                    metadataForRegionCallingFrom.PreferredInternationalPrefix;
            }
            else if (UniqueInternationalPrefix.IsMatchAll(internationalPrefix))
            {
                internationalPrefixForFormatting = internationalPrefix;
            }

            var regionCode = GetRegionCodeForCountryCode(countryCallingCode);
            var metadataForRegion =
                GetMetadataForRegionOrCallingCode(countryCallingCode, regionCode);
            var formattedNationalNumber =
                FormatNsn(nationalSignificantNumber,
                    metadataForRegion, PhoneNumberFormat.INTERNATIONAL);
            var formattedNumber = new StringBuilder(formattedNationalNumber);
            MaybeAppendFormattedExtension(number, metadataForRegion, PhoneNumberFormat.INTERNATIONAL,
                formattedNumber);
            if (internationalPrefixForFormatting.Length > 0)
            {
                formattedNumber.Insert(0, " ").Insert(0, countryCallingCode).Insert(0, " ")
                    .Insert(0, internationalPrefixForFormatting);
            }
            else
            {
                PrefixNumberWithCountryCallingCode(countryCallingCode, PhoneNumberFormat.INTERNATIONAL,
                    formattedNumber);
            }
            return formattedNumber.ToString();
        }

        /// <summary>
        /// Formats a phone number using the original phone number format that the number is parsed from.
        /// The original format is embedded in the country_code_source field of the PhoneNumber object
        /// passed in. If such information is missing, the number will be formatted into the NATIONAL
        /// format by default. When we don't have a formatting pattern for the number, the method returns
        /// the raw input when it is available.
        /// <para>
        /// Note this method guarantees no digit will be inserted, removed or modified as a result of
        /// formatting.</para>
        /// </summary>
        /// <param name="number">The phone number that needs to be formatted in its original number format.</param>
        /// <param name="regionCallingFrom">The region whose IDD needs to be prefixed if the original number
        /// has one.</param>
        /// <returns>The formatted phone number in its original number format.</returns>
        public string FormatInOriginalFormat(PhoneNumber number, string regionCallingFrom)
        {
            if (number.HasRawInput && !HasFormattingPatternForNumber(number))
            {
                // We check if we have the formatting pattern because without that, we might format the number
                // as a group without national prefix.
                return number.RawInput;
            }

            if (!number.HasCountryCodeSource)
                return Format(number, PhoneNumberFormat.NATIONAL);

            string formattedNumber;
            switch (number.CountryCodeSource)
            {
                case PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN:
                    formattedNumber = Format(number, PhoneNumberFormat.INTERNATIONAL);
                    break;
                case PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD:
                    formattedNumber = FormatOutOfCountryCallingNumber(number, regionCallingFrom);
                    break;
                case PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN:
                    formattedNumber = Format(number, PhoneNumberFormat.INTERNATIONAL).Substring(1);
                    break;
                    // Fall-through to default case.
                default:
                    var regionCode = GetRegionCodeForCountryCode(number.CountryCode);
                    // We strip non-digits from the NDD here, and from the raw input later, so that we can
                    // compare them easily.
                    var nationalPrefix = GetNddPrefixForRegion(regionCode, true /* strip non-digits */);
                    var nationalFormat = Format(number, PhoneNumberFormat.NATIONAL);
                    if (string.IsNullOrEmpty(nationalPrefix))
                    {
                        // If the region doesn't have a national prefix at all, we can safely return the national
                        // format without worrying about a national prefix being added.
                        formattedNumber = nationalFormat;
                        break;
                    }
                    // Otherwise, we check if the original number was entered with a national prefix.
                    if (RawInputContainsNationalPrefix(
                        number.RawInput, nationalPrefix, regionCode))
                    {
                        // If so, we can safely return the national format.
                        formattedNumber = nationalFormat;
                        break;
                    }
                    var metadata = GetMetadataForRegion(regionCode);
                    var nationalNumber = GetNationalSignificantNumber(number);
                    var formatRule =
                        ChooseFormattingPatternForNumber(metadata.NumberFormatList, nationalNumber);
                    // When the format we apply to this number doesn't contain national prefix, we can just
                    // return the national format.
                    // TODO: Refactor the code below with the code in isNationalPrefixPresentIfRequired.
                    var candidateNationalPrefixRule = formatRule.NationalPrefixFormattingRule;
                    // We assume that the first-group symbol will never be _before_ the national prefix.
                    var indexOfFirstGroup = candidateNationalPrefixRule.IndexOf("${1}", StringComparison.Ordinal);
                    if (indexOfFirstGroup <= 0)
                    {
                        formattedNumber = nationalFormat;
                        break;
                    }
                    candidateNationalPrefixRule =
                        candidateNationalPrefixRule.Substring(0, indexOfFirstGroup);
                    candidateNationalPrefixRule = NormalizeDigitsOnly(candidateNationalPrefixRule);
                    if (candidateNationalPrefixRule.Length == 0)
                    {
                        // National prefix not used when formatting this number.
                        formattedNumber = nationalFormat;
                        break;
                    }
                    // Otherwise, we need to remove the national prefix from our output.
                    var numFormatCopy = new NumberFormat.Builder()
                        .MergeFrom(formatRule)
                        .ClearNationalPrefixFormattingRule()
                        .Build();
                    var numberFormats = new List<NumberFormat>(1)
                    {
                        numFormatCopy
                    };
                    formattedNumber = FormatByPattern(number, PhoneNumberFormat.NATIONAL, numberFormats);
                    break;
            }
            var rawInput = number.RawInput;
            // If no digit is inserted/removed/modified as a result of our formatting, we return the
            // formatted phone number; otherwise we return the raw input the user entered.
            return (formattedNumber != null &&
                NormalizeHelper(formattedNumber, DiallableCharMappings, true /* remove non matches */)
                    .Equals(NormalizeHelper(
                        rawInput, DiallableCharMappings, true /* remove non matches */)))
                ? formattedNumber
                : rawInput;
        }

        // Check if rawInput, which is assumed to be in the national format, has a national prefix. The
        // national prefix is assumed to be in digits-only form.
        private bool RawInputContainsNationalPrefix(string rawInput, string nationalPrefix,
          string regionCode)
        {
            var normalizedNationalNumber = NormalizeDigitsOnly(rawInput);
            if (normalizedNationalNumber.StartsWith(nationalPrefix, StringComparison.Ordinal))
            {
                try
                {
                    // Some Japanese numbers (e.g. 00777123) might be mistaken to contain the national prefix
                    // when written without it (e.g. 0777123) if we just do prefix matching. To tackle that, we
                    // check the validity of the number if the assumed national prefix is removed (777123 won't
                    // be valid in Japan).
                    return IsValidNumber(
                        Parse(normalizedNationalNumber.Substring(nationalPrefix.Length), regionCode));
                }
                catch (NumberParseException)
                {
                    return false;
                }
            }
            return false;
        }

        private bool HasFormattingPatternForNumber(PhoneNumber number)
        {
            var countryCallingCode = number.CountryCode;
            var phoneNumberRegion = GetRegionCodeForCountryCode(countryCallingCode);
            var metadata =
                GetMetadataForRegionOrCallingCode(countryCallingCode, phoneNumberRegion);
            if (metadata == null)
            {
                return false;
            }
            var nationalNumber = GetNationalSignificantNumber(number);
            var formatRule =
                ChooseFormattingPatternForNumber(metadata.NumberFormatList, nationalNumber);
            return formatRule != null;
        }

        /// <summary>
        /// Formats a phone number for out-of-country dialing purposes.
        ///
        /// Note that in this version, if the number was entered originally using alpha characters and
        /// this version of the number is stored in raw_input, this representation of the number will be
        /// used rather than the digit representation. Grouping information, as specified by characters
        /// such as "-" and " ", will be retained.
        ///
        /// <p><b>Caveats:</b></p>
        /// <ul>
        ///  <li> This will not produce good results if the country calling code is both present in the raw
        ///       input _and_ is the start of the national number. This is not a problem in the regions
        ///       which typically use alpha numbers. </li>
        ///  <li> This will also not produce good results if the raw input has any grouping information
        ///       within the first three digits of the national number, and if the function needs to strip
        ///       preceding digits/words in the raw input before these digits. Normally people group the
        ///       first three digits together so this is not a huge problem - and will be fixed if it
        ///       proves to be so. </li>
        /// </ul>
        /// </summary>
        ///
        /// <param name="number">the phone number that needs to be formatted</param>
        /// <param name="regionCallingFrom">the region where the call is being placed</param>
        /// <returns>the formatted phone number</returns>
        ///
        public string FormatOutOfCountryKeepingAlphaChars(PhoneNumber number, string regionCallingFrom)
        {
            var rawInput = number.RawInput;
            // If there is no raw input, then we can't keep alpha characters because there aren't any.
            // In this case, we return formatOutOfCountryCallingNumber.
            if (rawInput.Length == 0)
                return FormatOutOfCountryCallingNumber(number, regionCallingFrom);

            var countryCode = number.CountryCode;
            if (!HasValidCountryCallingCode(countryCode))
                return rawInput;

            // Strip any prefix such as country calling code, IDD, that was present. We do this by comparing
            // the number in raw_input with the parsed number.
            // To do this, first we normalize punctuation. We retain number grouping symbols such as " "
            // only.
            rawInput = NormalizeHelper(rawInput, AllPlusNumberGroupingSymbols, true);
            // Now we trim everything before the first three digits in the parsed number. We choose three
            // because all valid alpha numbers have 3 digits at the start - if it does not, then we don't
            // trim anything at all. Similarly, if the national number was less than three digits, we don't
            // trim anything at all.
            var nationalNumber = GetNationalSignificantNumber(number);
            if (nationalNumber.Length > 3)
            {
                var firstNationalNumberDigit = rawInput.IndexOf(nationalNumber.Substring(0, 3), StringComparison.Ordinal);
                if (firstNationalNumberDigit != -1)
                    rawInput = rawInput.Substring(firstNationalNumberDigit);
            }
            var metadataForRegionCallingFrom = GetMetadataForRegion(regionCallingFrom);
            if (countryCode == NANPA_COUNTRY_CODE)
            {
                if (IsNANPACountry(regionCallingFrom))
                    return countryCode + " " + rawInput;
            }
            else if (IsValidRegionCode(regionCallingFrom) &&
                countryCode == GetCountryCodeForValidRegion(regionCallingFrom))
            {
                var formattingPattern =
                    ChooseFormattingPatternForNumber(metadataForRegionCallingFrom.NumberFormatList,
                        nationalNumber);
                if (formattingPattern == null)
                    // If no pattern above is matched, we format the original input.
                    return rawInput;

                var newFormat = new NumberFormat.Builder();
                newFormat.MergeFrom(formattingPattern);
                // The first group is the first group of digits that the user wrote together.
                newFormat.SetPattern("(\\d+)(.*)");
                // Here we just concatenate them back together after the national prefix has been fixed.
                newFormat.SetFormat("$1$2");
                // Now we format using this pattern instead of the default pattern, but with the national
                // prefix prefixed if necessary.
                // This will not work in the cases where the pattern (and not the leading digits) decide
                // whether a national prefix needs to be used, since we have overridden the pattern to match
                // anything, but that is not the case in the metadata to date.
                return FormatNsnUsingPattern(rawInput, newFormat.Build(), PhoneNumberFormat.NATIONAL);
            }
            var internationalPrefixForFormatting = "";
            // If an unsupported region-calling-from is entered, or a country with multiple international
            // prefixes, the international format of the number is returned, unless there is a preferred
            // international prefix.
            if (metadataForRegionCallingFrom != null)
            {
                var internationalPrefix = metadataForRegionCallingFrom.InternationalPrefix;
                internationalPrefixForFormatting =
                    UniqueInternationalPrefix.IsMatchAll(internationalPrefix)
                        ? internationalPrefix
                        : metadataForRegionCallingFrom.PreferredInternationalPrefix;
            }
            var formattedNumber = new StringBuilder(rawInput);
            var regionCode = GetRegionCodeForCountryCode(countryCode);
            var metadataForRegion = GetMetadataForRegionOrCallingCode(countryCode, regionCode);
            MaybeAppendFormattedExtension(number, metadataForRegion,
                PhoneNumberFormat.INTERNATIONAL, formattedNumber);
            if (internationalPrefixForFormatting.Length > 0)
            {
                formattedNumber.Insert(0, " ").Insert(0, countryCode).Insert(0, " ")
                    .Insert(0, internationalPrefixForFormatting);
            }
            else
            {
                // Invalid region entered as country-calling-from (so no metadata was found for it) or the
                // region chosen has multiple international dialling prefixes.
                // LOGGER.log(Level.WARNING,
                // "Trying to format number from invalid region "
                // + regionCallingFrom
                // + ". International formatting applied.");
                PrefixNumberWithCountryCallingCode(countryCode, PhoneNumberFormat.INTERNATIONAL,
                    formattedNumber);
            }
            return formattedNumber.ToString();
        }

        /// <summary>
        /// Gets the national significant number of the a phone number. Note a national significant number
        /// doesn't contain a national prefix or any formatting.
        /// </summary>
        /// <param name="number">The PhoneNumber object for which the national significant number is needed.</param>
        /// <returns>The national significant number of the PhoneNumber object passed in.</returns>
        public string GetNationalSignificantNumber(PhoneNumber number)
        {
            // If a leading zero(s) has been set, we prefix this now. Note this is not a national prefix.
            var nationalNumber = new StringBuilder();
            if (number.ItalianLeadingZero && number.NumberOfLeadingZeros > 0)
                nationalNumber.Append('0', number.NumberOfLeadingZeros);
            nationalNumber.Append(number.NationalNumber);
            return nationalNumber.ToString();
        }

        /**
        * A helper function that is used by format and formatByPattern.
        */
        private void PrefixNumberWithCountryCallingCode(int countryCallingCode,
            PhoneNumberFormat numberFormat, StringBuilder formattedNumber)
        {
            switch (numberFormat)
            {
                case PhoneNumberFormat.E164:
                    formattedNumber.Insert(0, countryCallingCode).Insert(0, PLUS_SIGN);
                    return;
                case PhoneNumberFormat.INTERNATIONAL:
                    formattedNumber.Insert(0, " ").Insert(0, countryCallingCode).Insert(0, PLUS_SIGN);
                    return;
                case PhoneNumberFormat.RFC3966:
                    formattedNumber.Insert(0, "-").Insert(0, countryCallingCode).Insert(0, PLUS_SIGN)
                         .Insert(0, RFC3966_PREFIX);
                    return;
                case PhoneNumberFormat.NATIONAL:
                    return;
                default:
                    return;
            }
        }

        // Note in some regions, the national number can be written in two completely different ways
        // depending on whether it forms part of the NATIONAL format or INTERNATIONAL format. The
        // numberFormat parameter here is used to specify which format to use for those cases. If a
        // carrierCode is specified, this will be inserted into the formatted string to replace $CC.
        private string FormatNsn(string number, PhoneMetadata metadata,
            PhoneNumberFormat numberFormat, string carrierCode = null)
        {
            var intlNumberFormats = metadata.IntlNumberFormatList;
            // When the intlNumberFormats exists, we use that to format national number for the
            // INTERNATIONAL format instead of using the numberDesc.numberFormats.
            var availableFormats =
                intlNumberFormats.Count == 0 || numberFormat == PhoneNumberFormat.NATIONAL
                ? metadata.NumberFormatList
                : metadata.IntlNumberFormatList;
            var formattingPattern = ChooseFormattingPatternForNumber(availableFormats, number);
            return formattingPattern == null
                ? number
                : FormatNsnUsingPattern(number, formattingPattern, numberFormat, carrierCode);
        }

        internal NumberFormat ChooseFormattingPatternForNumber(IList<NumberFormat> availableFormats,
            string nationalNumber)
        {
            foreach (var numFormat in availableFormats)
            {
                var size = numFormat.LeadingDigitsPatternCount;
                if (size == 0 || regexCache.GetPatternForRegex(
                    // We always use the last leading_digits_pattern, as it is the most detailed.
                    numFormat.LeadingDigitsPatternList[size - 1])
                    .IsMatchBeginning(nationalNumber))
                {
                    if (regexCache.GetPatternForRegex(numFormat.Pattern).IsMatchAll(nationalNumber))
                        return numFormat;
                }
            }
            return null;
        }


        // Simple wrapper of formatNsnUsingPattern for the common case of no carrier code.
        internal string FormatNsnUsingPattern(string nationalNumber,
             NumberFormat formattingPattern, PhoneNumberFormat numberFormat)
        {
            return FormatNsnUsingPattern(nationalNumber, formattingPattern, numberFormat, null);
        }

        // Note that carrierCode is optional - if NULL or an empty string, no carrier code replacement
        // will take place.
        private string FormatNsnUsingPattern(string nationalNumber, NumberFormat formattingPattern,
            PhoneNumberFormat numberFormat, string carrierCode)
        {
            var numberFormatRule = formattingPattern.Format;
            var m = regexCache.GetPatternForRegex(formattingPattern.Pattern);
            string formattedNationalNumber;
            if (numberFormat == PhoneNumberFormat.NATIONAL &&
                !string.IsNullOrEmpty(carrierCode) &&
                formattingPattern.DomesticCarrierCodeFormattingRule.Length > 0)
            {
                // Replace the $CC in the formatting rule with the desired carrier code.
                var carrierCodeFormattingRule = formattingPattern.DomesticCarrierCodeFormattingRule;
                carrierCodeFormattingRule = carrierCodeFormattingRule.Replace(CcPattern, carrierCode);
                // Now replace the $FG in the formatting rule with the first group and the carrier code
                // combined in the appropriate way.
                var r = FirstGroupPattern.Replace(numberFormatRule, carrierCodeFormattingRule, 1);
                formattedNationalNumber = m.Replace(nationalNumber, r);
            }
            else
            {
                // Use the national prefix formatting rule instead.
                var nationalPrefixFormattingRule = formattingPattern.NationalPrefixFormattingRule;
                if (numberFormat == PhoneNumberFormat.NATIONAL &&
                    !string.IsNullOrEmpty(nationalPrefixFormattingRule))
                {
                    var r = FirstGroupPattern.Replace(numberFormatRule,
                        nationalPrefixFormattingRule, 1);
                    formattedNationalNumber = m.Replace(nationalNumber, r);
                }
                else
                {
                    formattedNationalNumber = m.Replace(nationalNumber, numberFormatRule);
                }
            }
            if (numberFormat == PhoneNumberFormat.RFC3966)
            {
                // Strip any leading punctuation.
                var leadingMatch = SeparatorPattern.MatchBeginning(formattedNationalNumber);
                if (leadingMatch.Success)
                {
                    formattedNationalNumber = formattedNationalNumber.Substring(leadingMatch.Length);
                }
                // Replace the rest with a dash between each number group.
                formattedNationalNumber = SeparatorPattern.Replace(formattedNationalNumber, "-");
            }
            return formattedNationalNumber;
        }

        /// <summary>
        /// Gets a valid number for the specified region.
        /// </summary>
        /// <param name="regionCode">Region for which an example number is needed.</param>
        /// <returns>A valid fixed-line number for the specified region.Returns null when the metadata
        /// does not contain such information, or the region 001 is passed in. For 001 (representing
        /// non - geographical numbers), call <see cref="GetExampleNumberForNonGeoEntity(int)"/> instead.</returns>
        public PhoneNumber GetExampleNumber(string regionCode)
        {
            return GetExampleNumberForType(regionCode, PhoneNumberType.FIXED_LINE);
        }

        /// <summary>
        /// Gets a valid number for the specified region and number type.
        /// </summary>
        /// <param name="regionCode">Region for which an example number is needed.</param>
        /// <param name="type">The type of number that is needed.</param>
        /// <returns>A valid number for the specified region and type. Returns null when the metadata
        /// does not contain such information or if an invalid region or region 001 was entered.
        /// For 001 (representing non-geographical numbers), call
        /// <see cref="GetExampleNumberForNonGeoEntity(int)"/> instead.</returns>
        public PhoneNumber GetExampleNumberForType(string regionCode, PhoneNumberType type)
        {
            // Check the region code is valid.
            if (!IsValidRegionCode(regionCode))
                return null;
            var desc = GetNumberDescByType(GetMetadataForRegion(regionCode), type);
            try
            {
                if (desc.HasExampleNumber)
                    return Parse(desc.ExampleNumber, regionCode);
            }
            catch (NumberParseException)
            {
            }
            return null;
        }

        /// <summary>
        /// Gets a valid number for the specified country calling code for a non-geographical entity.
        /// </summary>
        /// <param name="countryCallingCode">The country calling code for a non-geographical entity</param>
        /// <returns>A valid number for the non-geographical entity. Returns null when the metadata
        /// does not contain such information, or the country calling code passed in does not belong
        /// to a non-geographical entity.</returns>
        public PhoneNumber GetExampleNumberForNonGeoEntity(int countryCallingCode)
        {
            var metadata = GetMetadataForNonGeographicalRegion(countryCallingCode);
            if (metadata != null)
            {

                foreach (var desc in new[]
                {
                    metadata.Mobile,
                    metadata.TollFree,
                    metadata.SharedCost,
                    metadata.Voip,
                    metadata.Voicemail,
                    metadata.Uan,
                    metadata.PremiumRate,
                    metadata.FixedLine,
                })
                {
                    try
                    {
                        if (desc != null && desc.HasExampleNumber)
                        {
                            return Parse("+" + countryCallingCode + desc.ExampleNumber, UNKNOWN_REGION);
                        }
                    }
                    catch (NumberParseException)
                    {
                        //LOGGER.log(Level.SEVERE, e.toString());
                    }
                }
            }
            return null;
        }

        /**
        * Appends the formatted extension of a phone number to formattedNumber, if the phone number had
        * an extension specified.
        */
        private static void MaybeAppendFormattedExtension(PhoneNumber number, PhoneMetadata metadata,
            PhoneNumberFormat numberFormat, StringBuilder formattedNumber)
        {
            if (number.HasExtension && number.Extension.Length > 0)
            {
                if (numberFormat == PhoneNumberFormat.RFC3966)
                {
                    formattedNumber.Append(RFC3966_EXTN_PREFIX).Append(number.Extension);
                }
                else
                {
                    if (metadata.HasPreferredExtnPrefix)
                        formattedNumber.Append(metadata.PreferredExtnPrefix).Append(number.Extension);
                    else
                        formattedNumber.Append(DEFAULT_EXTN_PREFIX).Append(number.Extension);
                }
            }
        }

        static PhoneNumberDesc GetNumberDescByType(PhoneMetadata metadata, PhoneNumberType type)
        {
            switch (type)
            {
                case PhoneNumberType.PREMIUM_RATE:
                    return metadata.PremiumRate;
                case PhoneNumberType.TOLL_FREE:
                    return metadata.TollFree;
                case PhoneNumberType.MOBILE:
                    return metadata.Mobile;
                case PhoneNumberType.FIXED_LINE:
                case PhoneNumberType.FIXED_LINE_OR_MOBILE:
                    return metadata.FixedLine;
                case PhoneNumberType.SHARED_COST:
                    return metadata.SharedCost;
                case PhoneNumberType.VOIP:
                    return metadata.Voip;
                case PhoneNumberType.PERSONAL_NUMBER:
                    return metadata.PersonalNumber;
                case PhoneNumberType.PAGER:
                    return metadata.Pager;
                case PhoneNumberType.UAN:
                    return metadata.Uan;
                case PhoneNumberType.VOICEMAIL:
                    return metadata.Voicemail;
                case PhoneNumberType.UNKNOWN:
                    return metadata.GeneralDesc;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        /// Gets the type of a phone number.
        /// </summary>
        /// <param name="number">The phone number that we want to know the type.</param>
        /// <returns>The type of the phone number.</returns>
        public PhoneNumberType GetNumberType(PhoneNumber number)
        {
            var regionCode = GetRegionCodeForNumber(number);
            if (!IsValidRegionCode(regionCode) && !REGION_CODE_FOR_NON_GEO_ENTITY.Equals(regionCode))
                return PhoneNumberType.UNKNOWN;
            var nationalSignificantNumber = GetNationalSignificantNumber(number);
            var metadata = GetMetadataForRegionOrCallingCode(number.CountryCode, regionCode);
            return GetNumberTypeHelper(nationalSignificantNumber, metadata);
        }

        private PhoneNumberType GetNumberTypeHelper(string nationalNumber, PhoneMetadata metadata)
        {
            if (!metadata.GeneralDesc.HasNationalNumberPattern ||
                !IsNumberMatchingDesc(nationalNumber, metadata.GeneralDesc))
                return PhoneNumberType.UNKNOWN;

            if (IsNumberMatchingDesc(nationalNumber, metadata.PremiumRate))
                return PhoneNumberType.PREMIUM_RATE;

            if (IsNumberMatchingDesc(nationalNumber, metadata.TollFree))
                return PhoneNumberType.TOLL_FREE;

            if (IsNumberMatchingDesc(nationalNumber, metadata.SharedCost))
                return PhoneNumberType.SHARED_COST;

            if (IsNumberMatchingDesc(nationalNumber, metadata.Voip))
                return PhoneNumberType.VOIP;

            if (IsNumberMatchingDesc(nationalNumber, metadata.PersonalNumber))
                return PhoneNumberType.PERSONAL_NUMBER;

            if (IsNumberMatchingDesc(nationalNumber, metadata.Pager))
                return PhoneNumberType.PAGER;

            if (IsNumberMatchingDesc(nationalNumber, metadata.Uan))
                return PhoneNumberType.UAN;

            if (IsNumberMatchingDesc(nationalNumber, metadata.Voicemail))
                return PhoneNumberType.VOICEMAIL;

            var isFixedLine = IsNumberMatchingDesc(nationalNumber, metadata.FixedLine);
            if (isFixedLine)
            {
                if (metadata.SameMobileAndFixedLinePattern)
                    return PhoneNumberType.FIXED_LINE_OR_MOBILE;
                if (IsNumberMatchingDesc(nationalNumber, metadata.Mobile))
                    return PhoneNumberType.FIXED_LINE_OR_MOBILE;
                return PhoneNumberType.FIXED_LINE;
            }
            // Otherwise, test to see if the number is mobile. Only do this if certain that the patterns for
            // mobile and fixed line aren't the same.
            if (!metadata.SameMobileAndFixedLinePattern &&
                IsNumberMatchingDesc(nationalNumber, metadata.Mobile))
                return PhoneNumberType.MOBILE;
            return PhoneNumberType.UNKNOWN;
        }

        public PhoneMetadata GetMetadataForRegion(string regionCode)
        {
            return regionCode != null && regionToMetadataMap.TryGetValue(regionCode, out var metadata) ? metadata : null;
        }

        public PhoneMetadata GetMetadataForNonGeographicalRegion(int countryCallingCode)
        {
            return countryCodeToNonGeographicalMetadataMap.TryGetValue(countryCallingCode, out var metadata) ? metadata : null;
        }

        private bool IsNumberMatchingDesc(string nationalNumber, PhoneNumberDesc numberDesc)
        {
            // Check if any possible number lengths are present; if so, we use them to avoid checking the
            // validation pattern if they don't match. If they are absent, this means they match the general
            // description, which we have already checked before checking a specific number type.
            var actualLength = nationalNumber.Length;
            var possibleLengths = numberDesc.PossibleLengthList;
            if (possibleLengths.Count > 0 && !possibleLengths.Contains(actualLength))
            {
                return false;
            }
            return regexCache.GetPatternForRegex(numberDesc.NationalNumberPattern).IsMatchAll(nationalNumber);
        }

        /// <summary>
        /// Tests whether a phone number matches a valid pattern. Note this doesn't verify the number
        /// is actually in use, which is impossible to tell by just looking at a number itself.
        /// </summary>
        /// <param name="number">The phone number that we want to validate.</param>
        /// <returns>A bool that indicates whether the number is of a valid pattern.</returns>
        public bool IsValidNumber(PhoneNumber number)
        {
            var regionCode = GetRegionCodeForNumber(number);
            return IsValidNumberForRegion(number, regionCode);
        }

        /// <summary>
        /// Tests whether a phone number is valid for a certain region. Note this doesn't verify the number
        /// is actually in use, which is impossible to tell by just looking at a number itself. If the
        /// country calling code is not the same as the country calling code for the region, this
        /// immediately exits with false. After this, the specific number pattern rules for the region are
        /// examined. This is useful for determining for example whether a particular number is valid for
        /// Canada, rather than just a valid NANPA number.
        /// </summary>
        /// <param name="number">The phone number that we want to validate.</param>
        /// <param name="regionCode">The region that we want to validate the phone number for.</param>
        /// <returns>A bool that indicates whether the number is of a valid pattern.</returns>
        public bool IsValidNumberForRegion(PhoneNumber number, string regionCode)
        {
            var countryCode = number.CountryCode;
            var metadata = GetMetadataForRegionOrCallingCode(countryCode, regionCode);
            if (metadata == null ||
                !REGION_CODE_FOR_NON_GEO_ENTITY.Equals(regionCode) &&
                countryCode != GetCountryCodeForValidRegion(regionCode))
            {
                // Either the region code was invalid, or the country calling code for this number does not
                // match that of the region code.
                return false;
            }
            var generalNumDesc = metadata.GeneralDesc;
            var nationalSignificantNumber = GetNationalSignificantNumber(number);

            // For regions where we don't have metadata for PhoneNumberDesc, we treat any number passed in
            // as a valid number if its national significant number is between the minimum and maximum
            // lengths defined by ITU for a national significant number.
            if (!generalNumDesc.HasNationalNumberPattern)
            {
                var numberLength = nationalSignificantNumber.Length;
                return numberLength > MIN_LENGTH_FOR_NSN && numberLength <= MAX_LENGTH_FOR_NSN;
            }
            return GetNumberTypeHelper(nationalSignificantNumber, metadata) != PhoneNumberType.UNKNOWN;
        }

        /// <summary>
        /// Returns the region where a phone number is from. This could be used for geocoding at the region
        /// level.
        /// </summary>
        /// <param name="number">The phone number whose origin we want to know.</param>
        /// <returns>The region where the phone number is from, or null if no region matches this calling
        /// code.</returns>
        public string GetRegionCodeForNumber(PhoneNumber number)
        {
            countryCallingCodeToRegionCodeMap.TryGetValue(number.CountryCode, out List<string> regions);
            if (regions == null)
            {
                return null;
            }
            return regions.Count == 1 ? regions[0] : GetRegionCodeForNumberFromRegionList(number, regions);
        }

        private string GetRegionCodeForNumberFromRegionList(PhoneNumber number,
            List<string> regionCodes)
        {
            var nationalNumber = GetNationalSignificantNumber(number);
            foreach (var regionCode in regionCodes)
            {
                // If leadingDigits is present, use this. Otherwise, do full validation.
                var metadata = GetMetadataForRegion(regionCode);
                if (metadata.HasLeadingDigits)
                {
                    if (regexCache.GetPatternForRegex(metadata.LeadingDigits)
                        .IsMatchBeginning(nationalNumber))
                        return regionCode;
                }
                else if (GetNumberTypeHelper(nationalNumber, metadata) != PhoneNumberType.UNKNOWN)
                    return regionCode;
            }
            return null;
        }

        /// <summary>
        /// Returns the region code that matches the specific country calling code. In the case of no
        /// region code being found, ZZ will be returned. In the case of multiple regions, the one
        /// designated in the metadata as the "main" region for this calling code will be returned.
        /// </summary>
        /// <param name="countryCallingCode"></param>
        /// <returns></returns>
        public string GetRegionCodeForCountryCode(int countryCallingCode)
        {
            return countryCallingCodeToRegionCodeMap.TryGetValue(countryCallingCode, out List<string> regionCodes)
                ? regionCodes[0]
                : UNKNOWN_REGION;
        }

        /// <summary>
        /// Returns the country calling code for a specific region. For example, this would be 1 for the
        /// United States, and 64 for New Zealand.
        /// </summary>
        /// <param name="regionCode">Region that we want to get the country calling code for.</param>
        /// <returns>The country calling code for the region denoted by regionCode.</returns>
        public int GetCountryCodeForRegion(string regionCode)
        {
            return !IsValidRegionCode(regionCode) ? 0 : GetCountryCodeForValidRegion(regionCode);
        }

        /// <summary>
        /// Returns the country calling code for a specific region. For example, this would be 1 for the
        /// United States, and 64 for New Zealand. Assumes the region is already valid.
        /// </summary>
        /// <param name="regionCode">The region that we want to get the country calling code for.</param>
        /// <returns>The country calling code for the region denoted by regionCode.</returns>
        private int GetCountryCodeForValidRegion(string regionCode)
        {
            var metadata = GetMetadataForRegion(regionCode);
            return metadata.CountryCode;
        }

        /// <summary>
        /// Returns the national dialling prefix for a specific region. For example, this would be 1 for
        /// the United States, and 0 for New Zealand. Set stripNonDigits to true to strip symbols like "~"
        /// (which indicates a wait for a dialling tone) from the prefix returned. If no national prefix is
        /// present, we return null.
        /// <para>
        /// Warning: Do not use this method for do-your-own formatting - for some regions, the
        /// national dialling prefix is used only for certain types of numbers. Use the library's
        /// formatting functions to prefix the national prefix when required.</para>
        /// </summary>
        /// <param name="regionCode">The region that we want to get the dialling prefix for.</param>
        /// <param name="stripNonDigits">True to strip non-digits from the national dialling prefix.</param>
        /// <returns>The dialling prefix for the region denoted by regionCode.</returns>
        public string GetNddPrefixForRegion(string regionCode, bool stripNonDigits)
        {
            if (!IsValidRegionCode(regionCode))
            {
                //LOGGER.log(Level.WARNING,
                //    "Invalid or missing region code ("
                //    + ((regionCode == null) ? "null" : regionCode)
                //    + ") provided.");
                return null;
            }
            var metadata = GetMetadataForRegion(regionCode);
            var nationalPrefix = metadata.NationalPrefix;
            // If no national prefix was found, we return null.
            if (nationalPrefix.Length == 0)
                return null;
            if (stripNonDigits)
            {
                // Note: if any other non-numeric symbols are ever used in national prefixes, these would have
                // to be removed here as well.
                nationalPrefix = nationalPrefix.Replace("~", "");
            }
            return nationalPrefix;
        }

        /// <summary>
        /// Checks if this is a region under the North American Numbering Plan Administration (NANPA).
        /// </summary>
        /// <param name="regionCode"></param>
        /// <returns>True if regionCode is one of the regions under NANPA.</returns>
        public bool IsNANPACountry(string regionCode)
        {
            return regionCode != null && nanpaRegions.Contains(regionCode);
        }

        /// <summary>
        /// Checks if the number is a valid vanity (alpha) number such as 800 MICROSOFT. A valid vanity
        /// number will start with at least 3 digits and will have three or more alpha characters. This
        /// does not do region-specific checks - to work out if this number is actually valid for a region,
        /// it should be parsed and methods such as <see cref="IsPossibleNumberWithReason(PhoneNumber)"/> and
        /// <see cref="IsValidNumber(PhoneNumber)"/> should be used.
        /// </summary>
        /// <param name="number">The number that needs to be checked.</param>
        /// <returns>True if the number is a valid vanity number.</returns>
        public bool IsAlphaNumber(string number)
        {
            if (!IsViablePhoneNumber(number))
            {
                // Number is too short, or doesn't match the basic phone number pattern.
                return false;
            }
            var strippedNumber = new StringBuilder(number);
            MaybeStripExtension(strippedNumber, number);
            return IsValidAlphaPhone(strippedNumber);
        }

        /// <summary>
        /// Convenience wrapper around <see cref="IsPossibleNumberWithReason(PhoneNumber)"/>. Instead of returning the reason
        /// for failure, this method returns true if the number is either a possible fully-qualified number
        /// (containing the area code and country code), or if the number could be a possible local number
        /// (with a country code, but missing an area code). Local numbers are considered possible if they
        ///could be possibly dialled in this format: if the area code is needed for a call to connect, the
        /// number is not considered possible without it.
        /// </summary>
        /// <param name="number">The number that needs to be checked.</param>
        /// <returns>True if the number is possible.</returns>
        public bool IsPossibleNumber(PhoneNumber number)
        {
            var result = IsPossibleNumberWithReason(number);
            return result == ValidationResult.IS_POSSIBLE || result == ValidationResult.IS_POSSIBLE_LOCAL_ONLY;
        }

        /// <summary>Convenience wrapper around {@link #isPossibleNumberForTypeWithReason}. Instead of returning the
        /// reason for failure, this method returns true if the number is either a possible fully-qualified
        /// number (containing the area code and country code), or if the number could be a possible local
        /// number (with a country code, but missing an area code). Local numbers are considered possible
        /// if they could be possibly dialled in this format: if the area code is needed for a call to
        /// connect, the number is not considered possible without it. </summary>
        ///
        /// <param name="number">the number that needs to be checked</param>
        /// <param name="type">the type we are interested in</param>
        /// <returns>if the number is possible for this particular type</returns>
        public bool IsPossibleNumberForType(PhoneNumber number, PhoneNumberType type)
        {
            var result = IsPossibleNumberForTypeWithReason(number, type);
            return result == ValidationResult.IS_POSSIBLE || result == ValidationResult.IS_POSSIBLE_LOCAL_ONLY;
        }

        /// <summary>
        /// Helper method to check a number against possible lengths for this number, and determine
        /// whether it matches, or is too short or too long.
        /// </summary>
        private static ValidationResult TestNumberLength(int actualLength, PhoneMetadata metadata,
            PhoneNumberType type = PhoneNumberType.UNKNOWN)
        {
            var descForType = GetNumberDescByType(metadata, type);
            // There should always be "possibleLengths" set for every element. This is declared in the XML
            // schema which is verified by PhoneNumberMetadataSchemaTest.
            // For size efficiency, where a sub-description (e.g. fixed-line) has the same possibleLengths
            // as the parent, this is missing, so we fall back to the general desc (where no numbers of the
            // type exist at all, there is one possible length (-1) which is guaranteed not to match the
            // length of any real phone number).
            var possibleLengths = descForType.PossibleLengthList.Count == 0
                ? metadata.GeneralDesc.PossibleLengthList : descForType.PossibleLengthList;

            var localLengths = descForType.PossibleLengthLocalOnlyList;

            if (type == PhoneNumberType.FIXED_LINE_OR_MOBILE)
            {
                if (!DescHasPossibleNumberData(GetNumberDescByType(metadata, PhoneNumberType.FIXED_LINE)))
                {
                    // The rare case has been encountered where no fixedLine data is available (true for some
                    // non-geographical entities), so we just check mobile.
                    return TestNumberLength(actualLength, metadata, PhoneNumberType.MOBILE);
                }
                var mobileDesc = GetNumberDescByType(metadata, PhoneNumberType.MOBILE);
                if (DescHasPossibleNumberData(mobileDesc))
                {
                    // Note that when adding the possible lengths from mobile, we have to again check they
                    // aren't empty since if they are this indicates they are the same as the general desc and
                    // should be obtained from there.
                    var list = possibleLengths.ToList();
                    list.AddRange(mobileDesc.PossibleLengthList.Count == 0
                        ? metadata.GeneralDesc.PossibleLengthList
                        : mobileDesc.PossibleLengthList);
                    // The current list is sorted; we need to merge in the new list and re-sort (duplicates
                    // are okay). Sorting isn't so expensive because the lists are very small.
                    list.Sort();
                    possibleLengths = list;

                    if (localLengths.Count == 0)
                    {
                        localLengths = mobileDesc.PossibleLengthLocalOnlyList;
                    }
                    else
                    {
                        localLengths = localLengths.Concat(mobileDesc.PossibleLengthLocalOnlyList).ToList();
                    }
                }
            }

            // If the type is not supported at all (indicated by the possible lengths containing -1 at this
            // point) we return invalid length.
            if (possibleLengths[0] == -1)
            {
                return ValidationResult.INVALID_LENGTH;
            }

            // This is safe because there is never an overlap between the possible lengths and the local-only
            // lengths; this is checked at build time.
            if (localLengths.Contains(actualLength))
            {
                return ValidationResult.IS_POSSIBLE_LOCAL_ONLY;
            }

            var minimumLength = possibleLengths[0];
            if (minimumLength == actualLength)
            {
                return ValidationResult.IS_POSSIBLE;
            }
            if (minimumLength > actualLength)
            {
                return ValidationResult.TOO_SHORT;
            }
            if (possibleLengths[possibleLengths.Count - 1] < actualLength)
            {
                return ValidationResult.TOO_LONG;
            }
            return possibleLengths.Contains(actualLength)
                ? ValidationResult.IS_POSSIBLE : ValidationResult.INVALID_LENGTH;
        }

        /// <summary>
        /// Check whether a phone number is a possible number. It provides a more lenient check than
        /// <see cref="IsValidNumber" /> in the following sense:
        /// <ol>
        ///   <li> It only checks the length of phone numbers. In particular, it doesn't check starting
        ///        digits of the number. </li>
        ///   <li> It doesn't attempt to figure out the type of the number, but uses general rules which
        ///        applies to all types of phone numbers in a region. Therefore, it is much faster than
        ///        isValidNumber. </li>
        ///   <li> For some numbers (particularly fixed-line), many regions have the concept of area code,
        ///        which together with subscriber number constitute the national significant number. It is
        ///        sometimes okay to dial only the subscriber number when dialing in the same area. This
        ///        function will return IS_POSSIBLE_LOCAL_ONLY if the subscriber-number-only version is
        ///        passed in. On the other hand, because isValidNumber validates using information on both
        ///        starting digits (for fixed line numbers, that would most likely be area codes) and
        ///        length (obviously includes the length of area codes for fixed line numbers), it will
        ///        return false for the subscriber-number-only version. </li>
        /// </ol>
        /// </summary>
        /// <param name="number">the number that needs to be checked</param>
        /// <returns>a ValidationResult object which indicates whether the number is possible</returns>
        public ValidationResult IsPossibleNumberWithReason(PhoneNumber number)
        {
            return IsPossibleNumberForTypeWithReason(number, PhoneNumberType.UNKNOWN);
        }

        /// <summary>
        /// Check whether a phone number is a possible number of a particular type. For types that don't
        /// exist in a particular region, this will return a result that isn't so useful; it is recommended
        /// that you use {@link #getSupportedTypesForRegion} or {@link #getSupportedTypesForNonGeoEntity}
        /// respectively before calling this method to determine whether you should call it for this number
        /// at all.
        ///
        /// This provides a more lenient check than {@link #isValidNumber} in the following sense:
        ///
        /// <ol>
        ///   <li> It only checks the length of phone numbers. In particular, it doesn't check starting
        ///        digits of the number.</li>
        ///   <li> For some numbers (particularly fixed-line), many regions have the concept of area code,
        ///        which together with subscriber number constitute the national significant number. It is
        ///        sometimes okay to dial only the subscriber number when dialing in the same area. This
        ///        function will return IS_POSSIBLE_LOCAL_ONLY if the subscriber-number-only version is
        ///        passed in. On the other hand, because isValidNumber validates using information on both
        ///        starting digits (for fixed line numbers, that would most likely be area codes) and
        ///        length (obviously includes the length of area codes for fixed line numbers), it will
        ///        return false for the subscriber-number-only version.</li>
        /// </ol>
        ///</summary>
        ///
        /// <param name="number">the number that needs to be checked</param>
        /// <param name="type">the type we are interested in </param>
        /// <returns>A ValidationResult object which indicates whether the number is possible</returns>
        public ValidationResult IsPossibleNumberForTypeWithReason(
            PhoneNumber number, PhoneNumberType type)
        {
            var nationalNumber = GetNationalSignificantNumber(number);
            var countryCode = number.CountryCode;
            // Note: For regions that share a country calling code, like NANPA numbers, we just use the
            // rules from the default region (US in this case) since the getRegionCodeForNumber will not
            // work if the number is possible but not valid. There is in fact one country calling code (290)
            // where the possible number pattern differs between various regions (Saint Helena and Tristan
            // da Cuñha), but this is handled by putting all possible lengths for any country with this
            // country calling code in the metadata for the default region in this case.
            if (!HasValidCountryCallingCode(countryCode))
            {
                return ValidationResult.INVALID_COUNTRY_CODE;
            }
            var regionCode = GetRegionCodeForCountryCode(countryCode);
            // Metadata cannot be null because the country calling code is valid.
            var metadata = GetMetadataForRegionOrCallingCode(countryCode, regionCode);
            return TestNumberLength(nationalNumber.Length, metadata, type);
        }

        /// <summary>
        /// Check whether a phone number is a possible number given a number in the form of a string, and
        /// the region where the number could be dialed from. It provides a more lenient check than
        /// <see cref="IsValidNumber(PhoneNumber)"/>. See <see cref="IsPossibleNumber(PhoneNumber)"/> for details.
        /// <para>
        /// This method first parses the number, then invokes <see cref="IsPossibleNumber(PhoneNumber)"/>
        /// with the resultant PhoneNumber object.</para>
        /// </summary>
        /// <param name="number">The number that needs to be checked, in the form of a string.</param>
        /// <param name="regionDialingFrom">The region that we are expecting the number to be dialed from.
        /// Note this is different from the region where the number belongs.  For example, the number
        /// +1 650 253 0000 is a number that belongs to US. When written in this form, it can be
        /// dialed from any region. When it is written as 00 1 650 253 0000, it can be dialed from any
        /// region which uses an international dialling prefix of 00. When it is written as
        /// 650 253 0000, it can only be dialed from within the US, and when written as 253 0000, it
        /// can only be dialed from within a smaller area in the US (Mountain View, CA, to be more
        /// specific).</param>
        /// <returns>True if the number is possible.</returns>
        public bool IsPossibleNumber(string number, string regionDialingFrom)
        {
            try
            {
                return IsPossibleNumber(Parse(number, regionDialingFrom));
            }
            catch (NumberParseException)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to extract a valid number from a phone number that is too long to be valid, and resets
        /// the PhoneNumber object passed in to that valid version. If no valid number could be extracted,
        /// the PhoneNumber object passed in will not be modified.
        /// </summary>
        /// <param name="number">A PhoneNumber object which contains a number that is too long to be valid.</param>
        /// <returns>True if a valid phone number can be successfully extracted.</returns>
        public bool TruncateTooLongNumber(PhoneNumber.Builder number)
        {
            if (IsValidNumber(number.Clone().Build()))
                return true;
            PhoneNumber copy;
            var nationalNumber = number.NationalNumber;
            do
            {
                nationalNumber /= 10;
                var numberCopy = number.Clone();
                numberCopy.SetNationalNumber(nationalNumber);
                copy = numberCopy.Build();
                if (IsPossibleNumberWithReason(copy) == ValidationResult.TOO_SHORT ||
                  nationalNumber == 0)
                    return false;
            }
            while (!IsValidNumber(copy));
            number.SetNationalNumber(nationalNumber);
            return true;
        }

        /// <summary>
        /// Gets an <see cref="AsYouTypeFormatter"/> for the specific region.
        /// </summary>
        /// <param name="regionCode">Region where the phone number is being entered.</param>
        ///
        /// <returns>An <see cref="AsYouTypeFormatter"/> object, which can be used
        /// to format phone numbers in the specific region "as you type".</returns>
        public AsYouTypeFormatter GetAsYouTypeFormatter(string regionCode) => new(regionCode, this);

        // Extracts country calling code from fullNumber, returns it and places the remaining number in
        // nationalNumber. It assumes that the leading plus sign or IDD has already been removed. Returns
        // 0 if fullNumber doesn't start with a valid country calling code, and leaves nationalNumber
        // unmodified.
        internal int ExtractCountryCode(StringBuilder fullNumber, StringBuilder nationalNumber)
        {
            if (fullNumber.Length == 0 || (fullNumber[0] == '0'))
            {
                // Country codes do not begin with a '0'.
                return 0;
            }
            var numberLength = fullNumber.Length;
            for (var i = 1; i <= MAX_LENGTH_COUNTRY_CODE && i <= numberLength; i++)
            {
                var potentialCountryCode = int.Parse(fullNumber.ToString(0, i), CultureInfo.InvariantCulture);
                if (countryCallingCodeToRegionCodeMap.ContainsKey(potentialCountryCode))
                {
                    nationalNumber.Append(fullNumber.ToString(i, fullNumber.Length - i));  //XXX: ToString
                    return potentialCountryCode;
                }
            }
            return 0;
        }

        /// <summary>
        /// Tries to extract a country calling code from a number. This method will return zero if no
        /// country calling code is considered to be present. Country calling codes are extracted in the
        /// following ways:
        /// <ul>
        ///  <li> by stripping the international dialing prefix of the region the person is dialing from,
        ///       if this is present in the number, and looking at the next digits</li>
        ///  <li> by stripping the '+' sign if present and then looking at the next digits</li>
        ///  <li> by comparing the start of the number and the country calling code of the default region.
        ///       If the number is not considered possible for the numbering plan of the default region
        ///       initially, but starts with the country calling code of this region, validation will be
        ///       reattempted after stripping this country calling code. If this number is considered a
        ///       possible number, then the first digits will be considered the country calling code and
        ///       removed as such.</li>
        /// </ul>
        /// It will throw a NumberParseException if the number starts with a '+' but the country calling
        /// code supplied after this does not match that of any known region.
        /// </summary>
        ///
        /// <param name="number">non-normalized telephone number that we wish to extract a country calling
        ///     code from - may begin with '+'</param>
        /// <param name="defaultRegionMetadata">metadata about the region this number may be from</param>
        /// <param name="nationalNumber">a string buffer to store the national significant number in, in the case
        ///     that a country calling code was extracted. The number is appended to any existing contents.
        ///     If no country calling code was extracted, this will be left unchanged.</param>
        /// <param name="keepRawInput">true if the country_code_source and preferred_carrier_code fields of
        ///     phoneNumber should be populated.</param>
        /// <param name="phoneNumber">the PhoneNumber object where the country_code and country_code_source need
        ///     to be populated. Note the country_code is always populated, whereas country_code_source is
        ///     only populated when keepCountryCodeSource is true.</param>
        /// <returns>the country calling code extracted or 0 if none could be extracted</returns>
        public int MaybeExtractCountryCode(string number, PhoneMetadata defaultRegionMetadata,
            StringBuilder nationalNumber, bool keepRawInput, PhoneNumber.Builder phoneNumber)
        {
            if (number.Length == 0)
                return 0;
            var fullNumber = new StringBuilder(number);
            // Set the default prefix to be something that will never match.
            var possibleCountryIddPrefix = "NonMatch";
            if (defaultRegionMetadata != null)
            {
                possibleCountryIddPrefix = defaultRegionMetadata.InternationalPrefix;
            }

            var countryCodeSource =
                MaybeStripInternationalPrefixAndNormalize(fullNumber, possibleCountryIddPrefix);
            if (keepRawInput)
            {
                phoneNumber.SetCountryCodeSource(countryCodeSource);
            }
            if (countryCodeSource != PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY)
            {
                if (fullNumber.Length <= MIN_LENGTH_FOR_NSN)
                {
                    throw new NumberParseException(ErrorType.TOO_SHORT_AFTER_IDD,
                           "Phone number had an IDD, but after this was not "
                           + "long enough to be a viable phone number.");
                }
                var potentialCountryCode = ExtractCountryCode(fullNumber, nationalNumber);
                if (potentialCountryCode != 0)
                {
                    phoneNumber.SetCountryCode(potentialCountryCode);
                    return potentialCountryCode;
                }

                // If this fails, they must be using a strange country calling code that we don't recognize,
                // or that doesn't exist.
                throw new NumberParseException(ErrorType.INVALID_COUNTRY_CODE,
                    "Country calling code supplied was not recognised.");
            }
            if (defaultRegionMetadata != null)
            {
                // Check to see if the number starts with the country calling code for the default region. If
                // so, we remove the country calling code, and do some checks on the validity of the number
                // before and after.
                var defaultCountryCode = defaultRegionMetadata.CountryCode;
                var defaultCountryCodeString = defaultCountryCode.ToString();
                var normalizedNumber = fullNumber.ToString();
                if (normalizedNumber.StartsWith(defaultCountryCodeString, StringComparison.Ordinal))
                {
                    var potentialNationalNumberString = normalizedNumber.Substring(defaultCountryCodeString.Length);
                    var potentialNationalNumber = new StringBuilder(potentialNationalNumberString);
                    var generalDesc = defaultRegionMetadata.GeneralDesc;
                    var validNumberPattern = regexCache.GetPatternForRegex(generalDesc.NationalNumberPattern);
                    if (MaybeStripNationalPrefixAndCarrierCode(potentialNationalNumber, potentialNationalNumberString, defaultRegionMetadata))
                        potentialNationalNumberString = potentialNationalNumber.ToString();
                    // If the number was not valid before but is valid now, or if it was too long before, we
                    // consider the number with the country calling code stripped to be a better result and
                    // keep that instead.
                    if ((!validNumberPattern.IsMatchAll(normalizedNumber) &&
                         validNumberPattern.IsMatchAll(potentialNationalNumberString)) ||
                        TestNumberLength(fullNumber.Length, defaultRegionMetadata) == ValidationResult.TOO_LONG)
                    {
                        nationalNumber.Append(potentialNationalNumberString);
                        if (keepRawInput)
                            phoneNumber.SetCountryCodeSource(PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITHOUT_PLUS_SIGN);
                        phoneNumber.SetCountryCode(defaultCountryCode);
                        return defaultCountryCode;
                    }
                }
            }
            // No country calling code present.
            phoneNumber.SetCountryCode(0);
            return 0;
        }

        /// <summary>
        /// Strips the IDD from the start of the number if present. Helper function used by
        /// <see cref="MaybeStripInternationalPrefixAndNormalize" />.
        /// </summary>
        private static bool ParsePrefixAsIdd(PhoneRegex iddPattern, StringBuilder number)
        {
            var m = iddPattern.MatchBeginning(number.ToString());
            if (m.Success)
            {
                var matchEnd = m.Index + m.Length;
                // Only strip this if the first digit after the match is not a 0, since country calling codes
                // cannot begin with 0.
                var digitMatcher = CapturingDigitPattern.Match(number.ToString().Substring(matchEnd)); //XXX: ToString
                if (digitMatcher.Success)
                {
                    var normalizedGroup = NormalizeDigitsOnly(digitMatcher.Groups[1].Value);
                    if (normalizedGroup == "0")
                        return false;
                }
                number.Remove(0, matchEnd);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Strips any international prefix (such as +, 00, 011) present in the number provided, normalizes
        /// the resulting number, and indicates if an international prefix was present.
        /// </summary>
        /// <param name="number">The non-normalized telephone number that we wish to strip any international
        /// dialing prefix from.</param>
        /// <param name="possibleIddPrefix">The international direct dialing prefix from the region we
        /// think this number may be dialed in.</param>
        /// <returns>The corresponding CountryCodeSource if an international dialing prefix could be
        /// removed from the number, otherwise CountryCodeSource.FROM_DEFAULT_COUNTRY if the number did
        /// not seem to be in international format.</returns>
        public PhoneNumber.Types.CountryCodeSource MaybeStripInternationalPrefixAndNormalize(StringBuilder number,
          string possibleIddPrefix)
        {
            if (number.Length == 0)
                return PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY;
            // Check to see if the number begins with one or more plus signs.
            int m = 0;
            while (m < number.Length && IsPlusChar(number[m])) m++;
            if (m > 0)
            {
                number.Remove(0, m);
                // Can now normalize the rest of the number since we've consumed the "+" sign at the start.
                Normalize(number);
                return PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_PLUS_SIGN;
            }
            // Attempt to parse the first digits as an international prefix.
            var iddPattern = regexCache.GetPatternForRegex(possibleIddPrefix);
            Normalize(number);
            return ParsePrefixAsIdd(iddPattern, number)
                ? PhoneNumber.Types.CountryCodeSource.FROM_NUMBER_WITH_IDD
                : PhoneNumber.Types.CountryCodeSource.FROM_DEFAULT_COUNTRY;
        }

        /// <summary>
        /// Strips any national prefix (such as 0, 1) present in the number provided.
        /// </summary>
        /// <param name="number">The normalized telephone number that we wish to strip any national
        /// dialing prefix from.</param>
        /// <param name="metadata">The metadata for the region that we think this number is from.</param>
        /// <param name="carrierCode">A place to insert the carrier code if one is extracted.</param>
        /// <returns>True if a national prefix or carrier code (or both) could be extracted.</returns>
        public bool MaybeStripNationalPrefixAndCarrierCode(StringBuilder number, PhoneMetadata metadata, StringBuilder carrierCode)
            => MaybeStripNationalPrefixAndCarrierCode(number, null, metadata, carrierCode);

        internal bool MaybeStripNationalPrefixAndCarrierCode(StringBuilder number, string numberString, PhoneMetadata metadata, StringBuilder carrierCode = null)
        {
            var numberLength = numberString?.Length ?? number.Length;
            var possibleNationalPrefix = metadata.NationalPrefixForParsing;
            if (numberLength == 0 || possibleNationalPrefix.Length == 0)
            {
                // Early return for numbers of zero length.
                return false;
            }
            // Attempt to parse the first digits as a national prefix.
            var prefixMatcher = regexCache.GetPatternForRegex(possibleNationalPrefix);
            numberString ??= number.ToString();
            var prefixMatch = prefixMatcher.MatchBeginning(numberString);
            if (prefixMatch.Success)
            {
                var nationalNumberRule =
                    regexCache.GetPatternForRegex(metadata.GeneralDesc.NationalNumberPattern);
                // Check if the original number is viable.
                var isViableOriginalNumber = nationalNumberRule.IsMatchAll(numberString);
                // prefixMatcher.group(numOfGroups) == null implies nothing was captured by the capturing
                // groups in possibleNationalPrefix; therefore, no transformation is necessary, and we just
                // remove the national prefix.
                var numOfGroups = prefixMatch.Groups.Count;
                var transformRule = metadata.NationalPrefixTransformRule;
                if (string.IsNullOrEmpty(transformRule) ||
                    !prefixMatch.Groups[numOfGroups - 1].Success)
                {
                    // If the original number was viable, and the resultant number is not, we return.
                    if (isViableOriginalNumber &&
                        !nationalNumberRule.IsMatchAll(numberString.Substring(prefixMatch.Length)))
                        return false;
                    if (carrierCode != null && numOfGroups > 1 && prefixMatch.Groups[numOfGroups - 1].Success)
                        carrierCode.Append(prefixMatch.Groups[1].Value);
                    number?.Remove(0, prefixMatch.Length);
                    return true;
                }
                // Check that the resultant number is still viable. If not, return. Check this by copying
                // the string buffer and making the transformation on the copy first.
                var transformedNumber = prefixMatch.Result(transformRule) + numberString.Substring(prefixMatch.Length);
                if (isViableOriginalNumber &&
                    !nationalNumberRule.IsMatchAll(transformedNumber))
                    return false;
                if (carrierCode != null && numOfGroups > 2)
                    carrierCode.Append(prefixMatch.Groups[1].Value);
                if (number != null)
                {
                    number.Length = 0;
                    number.Append(transformedNumber);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Strips any extension (as in, the part of the number dialled after the call is connected,
        /// usually indicated with extn, ext, x or similar) from the end of the number, and returns it.
        /// </summary>
        /// <param name="number">The non-normalized telephone number that we wish to strip the extension from.</param>
        /// <param name="numberString">The same number as a string</param>
        /// <returns>The phone extension.</returns>
        static string MaybeStripExtension(StringBuilder number, string numberString)
        {
            var m = ExtnPattern.Match(numberString);
            // If we find a potential extension, and the number preceding this is a viable number, we assume
            // it is an extension.
            if (m.Success && IsViablePhoneNumber(numberString.Substring(0, m.Index)))
            {
                // The numbers are captured into groups in the regular expression.
                for (int i = 1, length = m.Groups.Count; i < length; i++)
                {
                    if (m.Groups[i].Success)
                    {
                        // We go through the capturing groups until we find one that captured some digits. If none
                        // did, then we will return the empty string.
                        var extension = m.Groups[i].Value;
                        number.Remove(m.Index, number.Length - m.Index);
                        return extension;
                    }
                }
            }
            return "";
        }

        /// <summary>
        /// Checks to see that the region code used is valid, or if it is not valid, that the number to
        /// parse starts with a + symbol so that we can attempt to infer the region from the number.
        /// </summary>
        /// <returns>if it can use the region provided or the region can be inferred.</returns>
        private bool CheckRegionForParsing(StringBuilder numberToParse, string defaultRegion)
        {
            return IsPlusChar(numberToParse[0]) || IsValidRegionCode(defaultRegion);
        }

        /// <summary>
        /// Parses a string and returns it in proto buffer format. This method will throw a
        /// <see cref="NumberParseException"/> if the number is not considered to be
        /// a possible number. Note that validation of whether the number is actually a valid number for a
        /// particular region is not performed. This can be done separately with <see cref="IsValidNumber(PhoneNumber)"/>.
        /// </summary>
        /// <param name="numberToParse">Number that we are attempting to parse. This can contain formatting
        /// such as +, ( and -, as well as a phone number extension. It can also
        /// be provided in RFC3966 format.</param>
        /// <param name="defaultRegion">Region that we are expecting the number to be from. This is only used
        /// if the number being parsed is not written in international format.
        /// The country_code for the number in this case would be stored as that
        /// of the default region supplied. If the number is guaranteed to
        /// start with a '+' followed by the country calling code, then "ZZ" or
        /// null can be supplied.</param>
        /// <returns>A phone number proto buffer filled with the parsed number</returns>
        /// <exception cref="NumberParseException">If the string is not considered to be a viable phone number or if
        /// no default region was supplied and the number is not in
        /// international format (does not start with +).
        /// </exception>
        public PhoneNumber Parse(string numberToParse, string defaultRegion)
        {
            var phoneNumber = new PhoneNumber.Builder();
            Parse(numberToParse, defaultRegion, phoneNumber);
            return phoneNumber.Build();
        }

        /// <summary>
        /// Same as <see cref="Parse(string, string)"/>, but accepts mutable PhoneNumber as a parameter to
        /// decrease object creation when invoked many times.
        /// </summary>
        public void Parse(string numberToParse, string defaultRegion, PhoneNumber.Builder phoneNumber)
        {
            ParseHelper(numberToParse, defaultRegion, false, true, phoneNumber);
        }

        /// <summary>
        /// Parses a string and returns it in proto buffer format. This method differs from {@link #parse}
        /// in that it always populates the raw_input field of the protocol buffer with numberToParse as
        /// well as the country_code_source field.
        /// </summary>
        /// <param name="numberToParse">Number that we are attempting to parse. This can contain formatting
        /// such as +, ( and -, as well as a phone number extension.</param>
        /// <param name="defaultRegion">Region that we are expecting the number to be from. This is only used
        /// if the number being parsed is not written in international format.
        /// The country calling code for the number in this case would be stored
        /// as that of the default region supplied.</param>
        /// <returns>A phone number proto buffer filled with the parsed number.</returns>
        /// <exception cref="NumberParseException">If the string is not considered to be a viable phone number or if
        /// no default region was supplied.</exception>
        public PhoneNumber ParseAndKeepRawInput(string numberToParse, string defaultRegion)
        {
            var phoneNumber = new PhoneNumber.Builder();
            ParseAndKeepRawInput(numberToParse, defaultRegion, phoneNumber);
            return phoneNumber.Build();
        }

        /// <summary>
        /// Same as <see cref="ParseAndKeepRawInput(string, string)"/>, but accepts a mutable PhoneNumber as
        /// a parameter to decrease object creation when invoked many times.
        /// </summary>
        /// <param name="numberToParse"></param>
        /// <param name="defaultRegion"></param>
        /// <param name="phoneNumber"></param>
        public void ParseAndKeepRawInput(string numberToParse, string defaultRegion, PhoneNumber.Builder phoneNumber)
        {
            ParseHelper(numberToParse, defaultRegion, true, true, phoneNumber);
        }

        /// <summary>
        /// Returns an iterable over all <see cref="PhoneNumberMatch"/> PhoneNumberMatches in text. This
        /// is a shortcut for <see cref="FindNumbers(string, string, Leniency, long)"/>
        /// getMatcher(text, defaultRegion, Leniency.VALID, Long.MAX_VALUE)}.
        /// </summary>
        /// <param name="text">The text to search for phone numbers, null for no text.</param>
        /// <param name="defaultRegion">Region that we are expecting the number to be from. This is only used
        /// if the number being parsed is not written in international format. The
        /// country_code for the number in this case would be stored as that of
        /// the default region supplied. May be null if only international
        /// numbers are expected.</param>
        /// <returns></returns>
        public IEnumerable<PhoneNumberMatch> FindNumbers(string text, string defaultRegion)
        {
            return FindNumbers(text, defaultRegion, Leniency.VALID, long.MaxValue);
        }

        /// <summary>
        /// Returns an iterable over all <see cref="PhoneNumberMatch"/> PhoneNumberMatches in text.
        /// </summary>
        /// <param name="text">The text to search for phone numbers, null for no text.</param>
        /// <param name="defaultRegion">Tegion that we are expecting the number to be from. This is only used
        /// if the number being parsed is not written in international format. The
        /// country_code for the number in this case would be stored as that of
        /// the default region supplied. May be null if only international
        /// numbers are expected.</param>
        /// <param name="leniency">The leniency to use when evaluating candidate phone numbers.</param>
        /// <param name="maxTries">The maximum number of invalid numbers to try before giving up on the
        /// text. This is to cover degenerate cases where the text has a lot of
        /// false positives in it. Must be {@code >= 0}.</param>
        /// <returns></returns>
        public IEnumerable<PhoneNumberMatch> FindNumbers(string text, string defaultRegion,
            Leniency leniency, long maxTries)
        {
            return new EnumerableFromConstructor<PhoneNumberMatch>(
                () => new PhoneNumberMatcher(this, text, defaultRegion, leniency, maxTries));
        }

        /// <summary>
        /// A helper function to set the values related to leading zeros in a PhoneNumber.
        /// </summary>
        private static void SetItalianLeadingZerosForPhoneNumber(string nationalNumber, PhoneNumber.Builder phoneNumber)
        {
            if (nationalNumber.Length <= 1 || nationalNumber[0] != '0') return;

            phoneNumber.SetItalianLeadingZero(true);
            var numberOfLeadingZeros = 1;
            //Note that if the national number is all "0"s, the last "0" is not counted as a leading zero.
            while (numberOfLeadingZeros < nationalNumber.Length - 1
                   && nationalNumber[numberOfLeadingZeros] == '0')
            {
                numberOfLeadingZeros++;
            }
            if (numberOfLeadingZeros != 1)
            {
                phoneNumber.SetNumberOfLeadingZeros(numberOfLeadingZeros);
            }
        }

        /// <summary>
        /// Parses a string and fills up the phoneNumber. This method is the same as the public
        /// Parse() method, with the exception that it allows the default region to be null, for use by
        /// IsNumberMatch(). checkRegion should be set to false if it is permitted for the default region
        /// to be null or unknown ("ZZ").
        /// </summary>
        private void ParseHelper(string numberToParse, string defaultRegion, bool keepRawInput, bool checkRegion, PhoneNumber.Builder phoneNumber)
        {
            if (numberToParse == null)
                throw new NumberParseException(ErrorType.NOT_A_NUMBER,
                    "The phone number supplied was null.");
            if (numberToParse.Length > MAX_INPUT_STRING_LENGTH)
                throw new NumberParseException(ErrorType.TOO_LONG,
                    "The string supplied was too long to parse.");

            var nationalNumber = new StringBuilder();
            BuildNationalNumberForParsing(numberToParse, nationalNumber);

            var nationalNumberString = nationalNumber.ToString();
            if (!IsViablePhoneNumber(nationalNumberString))
                throw new NumberParseException(ErrorType.NOT_A_NUMBER,
                    "The string supplied did not seem to be a phone number.");

            // Check the region supplied is valid, or that the extracted number starts with some sort of +
            // sign so the number's region can be determined.
            if (checkRegion && !CheckRegionForParsing(nationalNumber, defaultRegion))
                throw new NumberParseException(ErrorType.INVALID_COUNTRY_CODE,
                    "Missing or invalid default region.");

            if (keepRawInput)
                phoneNumber.SetRawInput(numberToParse);

            // Attempt to parse extension first, since it doesn't require region-specific data and we want
            // to have the non-normalised number here.
            var extension = MaybeStripExtension(nationalNumber, nationalNumberString);
            if (extension.Length > 0)
            {
                phoneNumber.SetExtension(extension);
                nationalNumberString = nationalNumber.ToString();
            }

            var regionMetadata = GetMetadataForRegion(defaultRegion);
            // Check to see if the number is given in international format so we know whether this number is
            // from the default region or not.
            var normalizedNationalNumber = new StringBuilder();
            int countryCode;
            try
            {
                // TODO: This method should really just take in the string buffer that has already
                // been created, and just remove the prefix, rather than taking in a string and then
                // outputting a string buffer.
                countryCode = MaybeExtractCountryCode(nationalNumberString, regionMetadata,
                    normalizedNationalNumber, keepRawInput, phoneNumber);
            }
            catch (NumberParseException e) when (e.ErrorType == ErrorType.INVALID_COUNTRY_CODE)
            {
                int m = 0;
                while (m < nationalNumberString.Length && IsPlusChar(nationalNumberString[m])) m++;
                if (m > 0)
                {
                    // Strip the plus-char, and try again.
                    countryCode = MaybeExtractCountryCode(
                        nationalNumberString.Substring(m),
                        regionMetadata, normalizedNationalNumber,
                        keepRawInput, phoneNumber);
                    if (countryCode == 0)
                    {
                        throw new NumberParseException(ErrorType.INVALID_COUNTRY_CODE,
                            "Could not interpret numbers after plus-sign.");
                    }
                }
                else
                {
                    throw;
                }
            }
            if (countryCode != 0)
            {
                var phoneNumberRegion = GetRegionCodeForCountryCode(countryCode);
                if (phoneNumberRegion != defaultRegion)
                    regionMetadata = GetMetadataForRegionOrCallingCode(countryCode, phoneNumberRegion);
            }
            else
            {
                // If no extracted country calling code, use the region supplied instead. The national number
                // is just the normalized version of the number we were given to parse.
                Normalize(nationalNumber);
                normalizedNationalNumber.Append(nationalNumber);
                if (defaultRegion != null)
                {
                    countryCode = regionMetadata.CountryCode;
                    phoneNumber.SetCountryCode(countryCode);
                }
                else if (keepRawInput)
                {
                    phoneNumber.ClearCountryCode();
                }
            }
            if (normalizedNationalNumber.Length < MIN_LENGTH_FOR_NSN)
                throw new NumberParseException(ErrorType.TOO_SHORT_NSN,
                    "The string supplied is too short to be a phone number.");

            var normalizedNationalNumberString = normalizedNationalNumber.ToString();
            if (regionMetadata != null)
            {
                var carrierCode = new StringBuilder();
                var potentialNationalNumber = new StringBuilder(normalizedNationalNumberString);
                MaybeStripNationalPrefixAndCarrierCode(potentialNationalNumber, normalizedNationalNumberString, regionMetadata, carrierCode);
                // We require that the NSN remaining after stripping the national prefix and carrier code be
                // long enough to be a possible length for the region. Otherwise, we don't do the stripping,
                // since the original number could be a valid short number.
                var validationResult = TestNumberLength(potentialNationalNumber.Length, regionMetadata);
                if (validationResult != ValidationResult.TOO_SHORT &&
                    validationResult != ValidationResult.IS_POSSIBLE_LOCAL_ONLY &&
                    validationResult != ValidationResult.INVALID_LENGTH)
                {
                    normalizedNationalNumberString = potentialNationalNumber.ToString();

                    if (keepRawInput && carrierCode.Length > 0)
                        phoneNumber.SetPreferredDomesticCarrierCode(carrierCode.ToString());
                }
            }
            var lengthOfNationalNumber = normalizedNationalNumberString.Length;
            if (lengthOfNationalNumber < MIN_LENGTH_FOR_NSN)
                throw new NumberParseException(ErrorType.TOO_SHORT_NSN,
                    "The string supplied is too short to be a phone number.");

            if (lengthOfNationalNumber > MAX_LENGTH_FOR_NSN)
                throw new NumberParseException(ErrorType.TOO_LONG,
                    "The string supplied is too long to be a phone number.");

            SetItalianLeadingZerosForPhoneNumber(normalizedNationalNumberString, phoneNumber);
            phoneNumber.SetNationalNumber(ulong.Parse(normalizedNationalNumberString, CultureInfo.InvariantCulture));
        }

        private static bool AreEqual(PhoneNumber.Builder p1, PhoneNumber.Builder p2)
        {
            return p1.Clone().Build().Equals(p2.Clone().Build());
        }

        /// <summary>
        /// Converts numberToParse to a form that we can parse and write it to nationalNumber if it is
        /// written in RFC3966; otherwise extract a possible number out of it and write to nationalNumber.
        /// </summary>
        private static void BuildNationalNumberForParsing(string numberToParse, StringBuilder nationalNumber)
        {
            var indexOfPhoneContext = numberToParse.IndexOf(RFC3966_PHONE_CONTEXT, StringComparison.Ordinal);
            if (indexOfPhoneContext > 0)
            {
                var phoneContextStart = indexOfPhoneContext + RFC3966_PHONE_CONTEXT.Length;
                // If the phone context contains a phone number prefix, we need to capture it, whereas domains
                // will be ignored.
                if (numberToParse[phoneContextStart] == PLUS_SIGN)
                {
                    // Additional parameters might follow the phone context. If so, we will remove them here
                    // because the parameters after phone context are not important for parsing the
                    // phone number.
                    var phoneContextEnd = numberToParse.IndexOf(';', phoneContextStart);
                    nationalNumber.Append(numberToParse, phoneContextStart, (phoneContextEnd > 0 ? phoneContextEnd : numberToParse.Length) - phoneContextStart);
                }

                // Now append everything between the "tel:" prefix and the phone-context. This should include
                // the national number, an optional extension or isdn-subaddress component.
                var indexOfPrefix = numberToParse.IndexOf(RFC3966_PREFIX, StringComparison.Ordinal) + RFC3966_PREFIX.Length;
                nationalNumber.Append(numberToParse, indexOfPrefix, indexOfPhoneContext - indexOfPrefix);
            }
            else
            {
                // Extract a possible number from the string passed in (this strips leading characters that
                // could not be the start of a phone number.)
                nationalNumber.Append(ExtractPossibleNumber(numberToParse));
            }

            // Delete the isdn-subaddress and everything after it if it is present. Note extension won't
            // appear at the same time with isdn-subaddress according to paragraph 5.3 of the RFC3966 spec,
            if (numberToParse.Contains(RFC3966_ISDN_SUBADDRESS))
            {
                var indexOfIsdn = nationalNumber.ToString().IndexOf(RFC3966_ISDN_SUBADDRESS, StringComparison.Ordinal);
                if (indexOfIsdn > 0)
                    nationalNumber.Length = indexOfIsdn;
            }
            // If both phone context and isdn-subaddress are absent but other parameters are present, the
            // parameters are left in nationalNumber. This is because we are concerned about deleting
            // content from a potential number string when there is no strong evidence that the number is
            // actually written in RFC3966.
        }

        /// <summary>
        /// Takes two phone numbers and compares them for equality.
        /// <para>
        /// Returns EXACT_MATCH if the country_code, NSN, presence of a leading zero for Italian numbers
        /// and any extension present are the same.
        /// Returns NSN_MATCH if either or both has no region specified, and the NSNs and extensions are
        /// the same.</para>
        /// <para>Returns SHORT_NSN_MATCH if either or both has no region specified, or the region specified is
        /// the same, and one NSN could be a shorter version of the other number. This includes the case
        /// where one has an extension specified, and the other does not.</para>
        /// <para>Returns NO_MATCH otherwise.
        /// For example, the numbers +1 345 657 1234 and 657 1234 are a SHORT_NSN_MATCH.
        /// The numbers +1 345 657 1234 and 345 657 are a NO_MATCH.</para>
        /// </summary>
        /// <param name="firstNumberIn">First number to compare.</param>
        /// <param name="secondNumberIn">Second number to compare.</param>
        ///
        /// <returns>NO_MATCH, SHORT_NSN_MATCH, NSN_MATCH or EXACT_MATCH depending on the level of equality
        /// of the two numbers, described in the method definition.</returns>
        public MatchType IsNumberMatch(PhoneNumber firstNumberIn, PhoneNumber secondNumberIn)
        {
            // Make copies of the phone number so that the numbers passed in are not edited.
            var firstNumber = new PhoneNumber.Builder();
            firstNumber.MergeFrom(firstNumberIn);
            var secondNumber = new PhoneNumber.Builder();
            secondNumber.MergeFrom(secondNumberIn);
            // First clear raw_input, country_code_source and preferred_domestic_carrier_code fields and any
            // empty-string extensions so that we can use the proto-buffer equality method.
            firstNumber.ClearRawInput();
            firstNumber.ClearCountryCodeSource();
            firstNumber.ClearPreferredDomesticCarrierCode();
            secondNumber.ClearRawInput();
            secondNumber.ClearCountryCodeSource();
            secondNumber.ClearPreferredDomesticCarrierCode();
            if (firstNumber.HasExtension &&
                firstNumber.Extension.Length == 0)
                firstNumber.ClearExtension();

            if (secondNumber.HasExtension &&
                secondNumber.Extension.Length == 0)
                secondNumber.ClearExtension();

            // This field is only relevant if there are leading zeros at all.
            if (!firstNumber.ItalianLeadingZero)
                firstNumber.ClearNumberOfLeadingZeros();
            if (!secondNumber.ItalianLeadingZero)
                secondNumber.ClearNumberOfLeadingZeros();

            // Early exit if both had extensions and these are different.
            if (firstNumber.HasExtension && secondNumber.HasExtension &&
                !firstNumber.Extension.Equals(secondNumber.Extension))
                return MatchType.NO_MATCH;

            var firstNumberCountryCode = firstNumber.CountryCode;
            var secondNumberCountryCode = secondNumber.CountryCode;
            // Both had country_code specified.
            if (firstNumberCountryCode != 0 && secondNumberCountryCode != 0)
            {
                if (AreEqual(firstNumber, secondNumber))
                    return MatchType.EXACT_MATCH;
                if (firstNumberCountryCode == secondNumberCountryCode &&
                    IsNationalNumberSuffixOfTheOther(firstNumber, secondNumber))
                {
                    // A SHORT_NSN_MATCH occurs if there is a difference because of the presence or absence of
                    // an 'Italian leading zero', the presence or absence of an extension, or one NSN being a
                    // shorter variant of the other.
                    return MatchType.SHORT_NSN_MATCH;
                }
                // This is not a match.
                return MatchType.NO_MATCH;
            }
            // Checks cases where one or both country_code fields were not specified. To make equality
            // checks easier, we first set the country_code fields to be equal.
            firstNumber.SetCountryCode(secondNumberCountryCode);
            // If all else was the same, then this is an NSN_MATCH.
            if (AreEqual(firstNumber, secondNumber))
                return MatchType.NSN_MATCH;

            if (IsNationalNumberSuffixOfTheOther(firstNumber, secondNumber))
                return MatchType.SHORT_NSN_MATCH;

            return MatchType.NO_MATCH;
        }

        // Returns true when one national number is the suffix of the other or both are the same.
        private static bool IsNationalNumberSuffixOfTheOther(PhoneNumber.Builder firstNumber,
            PhoneNumber.Builder secondNumber)
        {
            var firstNumberNationalNumber = firstNumber.NationalNumber.ToString();
            var secondNumberNationalNumber = secondNumber.NationalNumber.ToString();
            // Note that endsWith returns true if the numbers are equal.
            return firstNumberNationalNumber.EndsWith(secondNumberNationalNumber, StringComparison.Ordinal) ||
                secondNumberNationalNumber.EndsWith(firstNumberNationalNumber, StringComparison.Ordinal);
        }

        /// <summary>
        /// Takes two phone numbers as strings and compares them for equality. This is a convenience
        /// wrapper for <see cref="IsNumberMatch(PhoneNumber, PhoneNumber)"/>. No default region is known.
        /// </summary>
        /// <param name="firstNumber">First number to compare. Can contain formatting, and can have country
        /// calling code specified with + at the start.</param>
        /// <param name="secondNumber">Second number to compare. Can contain formatting, and can have country
        /// calling code specified with + at the start.</param>
        /// <returns>NOT_A_NUMBER, NO_MATCH, SHORT_NSN_MATCH, NSN_MATCH, EXACT_MATCH. See
        /// <see cref="IsNumberMatch(PhoneNumber, PhoneNumber)"/> for more details.</returns>
        public MatchType IsNumberMatch(string firstNumber, string secondNumber)
        {
            try
            {
                var firstNumberAsProto = Parse(firstNumber, UNKNOWN_REGION);
                return IsNumberMatch(firstNumberAsProto, secondNumber);
            }
            catch (NumberParseException e)
            {
                if (e.ErrorType == ErrorType.INVALID_COUNTRY_CODE)
                {
                    try
                    {
                        var secondNumberAsProto = Parse(secondNumber, UNKNOWN_REGION);
                        return IsNumberMatch(secondNumberAsProto, firstNumber);
                    }
                    catch (NumberParseException e2)
                    {
                        if (e2.ErrorType == ErrorType.INVALID_COUNTRY_CODE)
                        {
                            try
                            {
                                var firstNumberProto = new PhoneNumber.Builder();
                                var secondNumberProto = new PhoneNumber.Builder();
                                ParseHelper(firstNumber, null, false, false, firstNumberProto);
                                ParseHelper(secondNumber, null, false, false, secondNumberProto);
                                return IsNumberMatch(firstNumberProto.Build(), secondNumberProto.Build());
                            }
                            catch (NumberParseException)
                            {
                                // Fall through and return MatchType.NOT_A_NUMBER.
                            }
                        }
                    }
                }
            }
            // One or more of the phone numbers we are trying to match is not a viable phone number.
            return MatchType.NOT_A_NUMBER;
        }

        /// <summary>
        /// Takes two phone numbers and compares them for equality. This is a convenience wrapper for
        /// <see cref="IsNumberMatch(PhoneNumber, PhoneNumber)"/>. No default region is known.
        /// </summary>
        /// <param name="firstNumber">First number to compare in proto buffer format.</param>
        /// <param name="secondNumber">Second number to compare. Can contain formatting, and can have country
        /// calling code specified with + at the start.</param>
        /// <returns>NOT_A_NUMBER, NO_MATCH, SHORT_NSN_MATCH, NSN_MATCH, EXACT_MATCH. See
        /// <see cref="IsNumberMatch(PhoneNumber, PhoneNumber)"/> for more details.</returns>
        public MatchType IsNumberMatch(PhoneNumber firstNumber, string secondNumber)
        {
            // First see if the second number has an implicit country calling code, by attempting to parse
            // it.
            try
            {
                var secondNumberAsProto = Parse(secondNumber, UNKNOWN_REGION);
                return IsNumberMatch(firstNumber, secondNumberAsProto);
            }
            catch (NumberParseException e)
            {
                if (e.ErrorType == ErrorType.INVALID_COUNTRY_CODE)
                {
                    // The second number has no country calling code. EXACT_MATCH is no longer possible.
                    // We parse it as if the region was the same as that for the first number, and if
                    // EXACT_MATCH is returned, we replace this with NSN_MATCH.
                    var firstNumberRegion = GetRegionCodeForCountryCode(firstNumber.CountryCode);
                    try
                    {
                        if (!firstNumberRegion.Equals(UNKNOWN_REGION))
                        {
                            var secondNumberWithFirstNumberRegion = Parse(secondNumber, firstNumberRegion);
                            var match = IsNumberMatch(firstNumber, secondNumberWithFirstNumberRegion);
                            if (match == MatchType.EXACT_MATCH)
                                return MatchType.NSN_MATCH;
                            return match;
                        }
                        // If the first number didn't have a valid country calling code, then we parse the
                        // second number without one as well.
                        var secondNumberProto = new PhoneNumber.Builder();
                        ParseHelper(secondNumber, null, false, false, secondNumberProto);
                        return IsNumberMatch(firstNumber, secondNumberProto.Build());
                    }
                    catch (NumberParseException)
                    {
                        // Fall-through to return NOT_A_NUMBER.
                    }
                }
            }
            // One or more of the phone numbers we are trying to match is not a viable phone number.
            return MatchType.NOT_A_NUMBER;
        }

        /// <summary>
        /// Returns true if the number can be dialled from outside the region, or unknown. If the number
        /// can only be dialled from within the region, returns false. Does not check the number is a valid
        /// number.
        /// TODO: Make this method public when we have enough metadata to make it worthwhile.
        /// </summary>
        /// <param name="number">the phone-number for which we want to know whether it is only diallable from
        /// outside the region</param>
        /// <returns></returns>
        public bool CanBeInternationallyDialled(PhoneNumber number)
        {
            var regionCode = GetRegionCodeForNumber(number);
            if (!IsValidRegionCode(regionCode))
                // Note numbers belonging to non-geographical entities (e.g. +800 numbers) are always
                // internationally diallable, and will be caught here.
                return true;
            var metadata = GetMetadataForRegion(regionCode);
            var nationalSignificantNumber = GetNationalSignificantNumber(number);
            return !IsNumberMatchingDesc(nationalSignificantNumber, metadata.NoInternationalDialling);
        }
    }
}
