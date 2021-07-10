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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Xunit;

namespace PhoneNumbers.Test
{
    [Collection("TestMetadataTestCase")]
    public class TestPhoneNumberMatcher
    {
        private readonly PhoneNumberUtil phoneUtil;

        public TestPhoneNumberMatcher()
        {
            phoneUtil = TestMetadataTestCase.PhoneUtil;
        }

        /** See {@link PhoneNumberUtilTest#testParseNationalNumber()}. */
        [Fact]
        public void TestFindNationalNumber()
        {
            // same cases as in testParseNationalNumber
            DoTestFindInContext("033316005", "NZ");
            // ("33316005", RegionCode.NZ) is omitted since the national prefix is obligatory for these
            // types of numbers in New Zealand.
            // National prefix attached and some formatting present.
            DoTestFindInContext("03-331 6005", "NZ");
            DoTestFindInContext("03 331 6005", "NZ");
            // Testing international prefixes.
            // Should strip country code.
            DoTestFindInContext("0064 3 331 6005", "NZ");
            // Try again, but this time we have an international number with Region Code US. It should
            // recognize the country code and parse accordingly.
            DoTestFindInContext("01164 3 331 6005", "US");
            DoTestFindInContext("+64 3 331 6005", "US");

            DoTestFindInContext("64(0)64123456", "NZ");
            // Check that using a "/" is fine in a phone number.
            DoTestFindInContext("123/45678", "DE");
            DoTestFindInContext("123-456-7890", "US");
        }

        /** See {@link PhoneNumberUtilTest#testParseWithInternationalPrefixes()}. */
        [Fact]
        public void TestFindWithInternationalPrefixes()
        {
            DoTestFindInContext("+1 (650) 333-6000", "NZ");
            DoTestFindInContext("1-650-333-6000", "US");
            // Calling the US number from Singapore by using different service providers
            // 1st test: calling using SingTel IDD service (IDD is 001)
            DoTestFindInContext("0011-650-333-6000", "SG");
            // 2nd test: calling using StarHub IDD service (IDD is 008)
            DoTestFindInContext("0081-650-333-6000", "SG");
            // 3rd test: calling using SingTel V019 service (IDD is 019)
            DoTestFindInContext("0191-650-333-6000", "SG");
            // Calling the US number from Poland
            DoTestFindInContext("0~01-650-333-6000", "PL");
            // Using "++" at the start.
            DoTestFindInContext("++1 (650) 333-6000", "PL");
            // Using a full-width plus sign.
            DoTestFindInContext("\uFF0B1 (650) 333-6000", "SG");
            // The whole number, including punctuation, is here represented in full-width form.
            DoTestFindInContext("\uFF0B\uFF11\u3000\uFF08\uFF16\uFF15\uFF10\uFF09" +
                "\u3000\uFF13\uFF13\uFF13\uFF0D\uFF16\uFF10\uFF10\uFF10",
                "SG");
        }

        /** See {@link PhoneNumberUtilTest#testParseWithLeadingZero()}. */
        [Fact]
        public void TestFindWithLeadingZero()
        {
            DoTestFindInContext("+39 02-36618 300", "NZ");
            DoTestFindInContext("02-36618 300", "IT");
            DoTestFindInContext("312 345 678", "IT");
        }

        /** See {@link PhoneNumberUtilTest#testParseNationalNumberArgentina()}. */
        [Fact]
        public void TestFindNationalNumberArgentina()
        {
            // Test parsing mobile numbers of Argentina.
            DoTestFindInContext("+54 9 343 555 1212", "AR");
            DoTestFindInContext("0343 15 555 1212", "AR");

            DoTestFindInContext("+54 9 3715 65 4320", "AR");
            DoTestFindInContext("03715 15 65 4320", "AR");

            // Test parsing fixed-line numbers of Argentina.
            DoTestFindInContext("+54 11 3797 0000", "AR");
            DoTestFindInContext("011 3797 0000", "AR");

            DoTestFindInContext("+54 3715 65 4321", "AR");
            DoTestFindInContext("03715 65 4321", "AR");

            DoTestFindInContext("+54 23 1234 0000", "AR");
            DoTestFindInContext("023 1234 0000", "AR");
        }

        /** See {@link PhoneNumberUtilTest#testParseWithXInNumber()}. */
        [Fact]
        public void TestFindWithXInNumber()
        {
            DoTestFindInContext("(0xx) 123456789", "AR");
            // A case where x denotes both carrier codes and extension symbol.
            DoTestFindInContext("(0xx) 123456789 x 1234", "AR");

            // This test is intentionally constructed such that the number of digit after xx is larger than
            // 7, so that the number won't be mistakenly treated as an extension, as we allow extensions up
            // to 7 digits. This assumption is okay for now as all the countries where a carrier selection
            // code is written in the form of xx have a national significant number of length larger than 7.
            DoTestFindInContext("011xx5481429712", "US");
        }

        /** See {@link PhoneNumberUtilTest#testParseNumbersMexico()}. */
        [Fact]
        public void TestFindNumbersMexico()
        {
            // Test parsing fixed-line numbers of Mexico.
            DoTestFindInContext("+52 (449)978-0001", "MX");
            DoTestFindInContext("01 (449)978-0001", "MX");
            DoTestFindInContext("(449)978-0001", "MX");

            // Test parsing mobile numbers of Mexico.
            DoTestFindInContext("+52 1 33 1234-5678", "MX");
            DoTestFindInContext("044 (33) 1234-5678", "MX");
            DoTestFindInContext("045 33 1234-5678", "MX");
        }

        /** See {@link PhoneNumberUtilTest#testParseNumbersWithPlusWithNoRegion()}. */
        [Fact]
        public void TestFindNumbersWithPlusWithNoRegion()
        {
            // "ZZ" is allowed only if the number starts with a '+' - then the country code can be
            // calculated.
            DoTestFindInContext("+64 3 331 6005", "ZZ");
            // Null is also allowed for the region code in these cases.
            DoTestFindInContext("+64 3 331 6005", null);
        }

        /** See {@link PhoneNumberUtilTest#testParseExtensions()}. */
        [Fact]
        public void TestFindExtensions()
        {
            DoTestFindInContext("03 331 6005 ext 3456", "NZ");
            DoTestFindInContext("03-3316005x3456", "NZ");
            DoTestFindInContext("03-3316005 int.3456", "NZ");
            DoTestFindInContext("03 3316005 #3456", "NZ");
            DoTestFindInContext("0~0 1800 7493 524", "PL");
            DoTestFindInContext("(1800) 7493.524", "US");
            // Check that the last instance of an extension token is matched.
            DoTestFindInContext("0~0 1800 7493 524 ~1234", "PL");
            // Verifying fix where the last digit of a number was previously omitted if it was a 0 when
            // extracting the extension. Also verifying a few different cases of extensions.
            DoTestFindInContext("+44 2034567890x456", "NZ");
            DoTestFindInContext("+44 2034567890x456", "GB");
            DoTestFindInContext("+44 2034567890 x456", "GB");
            DoTestFindInContext("+44 2034567890 X456", "GB");
            DoTestFindInContext("+44 2034567890 X 456", "GB");
            DoTestFindInContext("+44 2034567890 X  456", "GB");
            DoTestFindInContext("+44 2034567890  X 456", "GB");

            DoTestFindInContext("(800) 901-3355 x 7246433", "US");
            DoTestFindInContext("(800) 901-3355 , ext 7246433", "US");
            DoTestFindInContext("(800) 901-3355 ,extension 7246433", "US");
            // The next test differs from PhoneNumberUtil -> when matching we don't consider a lone comma to
            // indicate an extension, although we accept it when parsing.
            DoTestFindInContext("(800) 901-3355 ,x 7246433", "US");
            DoTestFindInContext("(800) 901-3355 ext: 7246433", "US");
        }

        [Fact]
        public void TestFindInterspersedWithSpace()
        {
            DoTestFindInContext("0 3   3 3 1   6 0 0 5", "NZ");
        }

        /**
        * Test matching behavior when starting in the middle of a phone number.
        */
        [Fact]
        public void TestIntermediateParsePositions()
        {
            const string text = "Call 033316005  or 032316005!";
            //                   |    |    |    |    |    |
            //                   0    5   10   15   20   25

            // Iterate over all possible indices.
            for (var i = 0; i <= 5; i++)
                AssertEqualRange(text, i, 5, 14);
            // 7 and 8 digits in a row are still parsed as number.
            AssertEqualRange(text, 6, 6, 14);
            AssertEqualRange(text, 7, 7, 14);
            // Anything smaller is skipped to the second instance.
            for (var i = 8; i <= 19; i++)
                AssertEqualRange(text, i, 19, 28);
        }

        [Fact]
        public void TestMatchWithSurroundingZipcodes()
        {
            var number = "415-666-7777";
            var zipPreceding = "My address is CA 34215 - " + number + " is my number.";
            var expectedResult = phoneUtil.Parse(number, "US");

            var iterator = phoneUtil.FindNumbers(zipPreceding, "US").GetEnumerator();
            var match = iterator.MoveNext() ? iterator.Current : null;
            Assert.NotNull(match);
            Assert.Equal(expectedResult, match.Number);
            Assert.Equal(number, match.RawString);
            iterator.Dispose();

            // Now repeat, but this time the phone number has spaces in it. It should still be found.
            number = "(415) 666 7777";

            var zipFollowing = "My number is " + number + ". 34215 is my zip-code.";
            iterator = phoneUtil.FindNumbers(zipFollowing, "US").GetEnumerator();

            var matchWithSpaces = iterator.MoveNext() ? iterator.Current : null;
            Assert.NotNull(matchWithSpaces);
            Assert.Equal(expectedResult, matchWithSpaces.Number);
            Assert.Equal(number, matchWithSpaces.RawString);
            iterator.Dispose();
        }

        [Fact]
        public void TestIsLatinLetter()
        {
            Assert.True(PhoneNumberMatcher.IsLatinLetterInternal('c'));
            Assert.True(PhoneNumberMatcher.IsLatinLetterInternal('C'));
            Assert.True(PhoneNumberMatcher.IsLatinLetterInternal('\u00C9'));
            Assert.True(PhoneNumberMatcher.IsLatinLetterInternal('\u0301'));  // Combining acute accent
            // Punctuation, digits and white-space are not considered "latin letters".
            Assert.False(PhoneNumberMatcher.IsLatinLetterInternal(':'));
            Assert.False(PhoneNumberMatcher.IsLatinLetterInternal('5'));
            Assert.False(PhoneNumberMatcher.IsLatinLetterInternal('-'));
            Assert.False(PhoneNumberMatcher.IsLatinLetterInternal('.'));
            Assert.False(PhoneNumberMatcher.IsLatinLetterInternal(' '));
            Assert.False(PhoneNumberMatcher.IsLatinLetterInternal('\u6211'));  // Chinese character
            Assert.False(PhoneNumberMatcher.IsLatinLetterInternal('\u306E'));  // Hiragana letter no
        }

        [Fact]
        public void TestMatchesWithSurroundingLatinChars()
        {
            var possibleOnlyContexts = new List<NumberContext>(5)
            {
                new NumberContext("abc", "def"),
                new NumberContext("abc", ""),
                new NumberContext("", "def"),
                new NumberContext("\u00C9", ""), // Latin capital letter e with an acute accent.
                new NumberContext("e\u0301", "") // e with an acute accent decomposed (with combining mark)
            };

            // Numbers should not be considered valid, if they are surrounded by Latin characters, but
            // should be considered possible.
            FindMatchesInContexts(possibleOnlyContexts, false, true);
        }

        [Fact]
        public void TestMoneyNotSeenAsPhoneNumber()
        {
            var possibleOnlyContexts = new List<NumberContext>
            {
                new NumberContext("$", ""),
                new NumberContext("", "$"),
                new NumberContext("\u00A3", ""), // Pound sign
                new NumberContext("\u00A5", "")  // Yen sign
            };
            FindMatchesInContexts(possibleOnlyContexts, false, true);
        }

        [Fact]
        public void TestPercentageNotSeenAsPhoneNumber()
        {
            var possibleOnlyContexts = new List<NumberContext> {new NumberContext("", "%")};
            // Numbers followed by % should be dropped.
            FindMatchesInContexts(possibleOnlyContexts, false, true);
        }


        [Fact]
        public void TestPhoneNumberWithLeadingOrTrailingMoneyMatches()
        {
            // Because of the space after the 20 (or before the 100) these dollar amounts should not stop
            // the actual number from being found.
            var contexts =
                new List<NumberContext> {new NumberContext("$20 ", ""), new NumberContext("", " 100$")};
            FindMatchesInContexts(contexts, true, true);
        }

        [Fact]
        public void TestMatchesWithSurroundingLatinCharsAndLeadingPunctuation()
        {
            // Contexts with trailing characters. Leading characters are okay here since the numbers we will
            // insert start with punctuation, but trailing characters are still not allowed.
            var possibleOnlyContexts = new List<NumberContext>
            {
                new NumberContext("abc", "def"),
                new NumberContext("", "def"),
                new NumberContext("", "\u00C9")
            };

            // Numbers should not be considered valid, if they have trailing Latin characters, but should be
            // considered possible.
            const string numberWithPlus = "+14156667777";
            const string numberWithBrackets = "(415)6667777";
            FindMatchesInContexts(possibleOnlyContexts, false, true, "US", numberWithPlus);
            FindMatchesInContexts(possibleOnlyContexts, false, true, "US", numberWithBrackets);

            var validContexts = new List<NumberContext>
            {
                new NumberContext("abc", ""),
                new NumberContext("\u00C9", ""),
                new NumberContext("\u00C9", "."),   // Trailing punctuation.
                new NumberContext("\u00C9", " def") // Trailing white-space.
            };

            // Numbers should be considered valid, since they start with punctuation.
            FindMatchesInContexts(validContexts, true, true, "US", numberWithPlus);
            FindMatchesInContexts(validContexts, true, true, "US", numberWithBrackets);
        }

        [Fact]
        public void TestMatchesWithSurroundingChineseChars()
        {
            var validContexts = new List<NumberContext>
            {
                new NumberContext("\u6211\u7684\u7535\u8BDD\u53F7\u7801\u662F", ""),
                new NumberContext("", "\u662F\u6211\u7684\u7535\u8BDD\u53F7\u7801"),
                new NumberContext("\u8BF7\u62E8\u6253", "\u6211\u5728\u660E\u5929")
            };

            // Numbers should be considered valid, since they are surrounded by Chinese.
            FindMatchesInContexts(validContexts, true, true);
        }

        [Fact]
        public void TestMatchesWithSurroundingPunctuation()
        {
            var validContexts = new List<NumberContext>
            {
                new NumberContext("My number-", ""), // At end of text.
                new NumberContext("", ".Nice day."), // At start of text.
                new NumberContext("Tel:", "."),      // Punctuation surrounds number.
                new NumberContext("Tel: ", " on Saturdays.") // White-space is also fine.
            };

            // Numbers should be considered valid, since they are surrounded by punctuation.
            FindMatchesInContexts(validContexts, true, true);
        }

        [Fact]
        public void TestMatchesMultiplePhoneNumbersSeparatedByPhoneNumberPunctuation()
        {
            const string text = "Call 650-253-4561 -- 455-234-3451";
            const string region = "US";

            var number1 = new PhoneNumber.Builder()
                .SetCountryCode(phoneUtil.GetCountryCodeForRegion(region))
                .SetNationalNumber(6502534561L)
                .Build();
            var match1 = new PhoneNumberMatch(5, "650-253-4561", number1);

            var number2 = new PhoneNumber.Builder()
                .SetCountryCode(phoneUtil.GetCountryCodeForRegion(region))
                .SetNationalNumber(4552343451L)
                .Build();
            var match2 = new PhoneNumberMatch(21, "455-234-3451", number2);

            var matches = phoneUtil.FindNumbers(text, region).GetEnumerator();
            matches.MoveNext();
            Assert.Equal(match1, matches.Current);
            matches.MoveNext();
            Assert.Equal(match2, matches.Current);
            matches.Dispose();
        }

        [Fact]
        public void TestDoesNotMatchMultiplePhoneNumbersSeparatedWithNoWhiteSpace()
        {
            // No white-space found between numbers - neither is found.
            const string text = "Call 650-253-4561--455-234-3451";
            const string region = "US";

            Assert.True(HasNoMatches(phoneUtil.FindNumbers(text, region)));
        }

        /**
         * Strings with number-like things that shouldn't be found under any level.
         */
        private static readonly NumberTest[] ImpossibleCases =
        {
            new NumberTest("12345", "US"),
            new NumberTest("23456789", "US"),
            new NumberTest("234567890112", "US"),
            new NumberTest("650+253+1234", "US"),
            new NumberTest("3/10/1984", "CA"),
            new NumberTest("03/27/2011", "US"),
            new NumberTest("31/8/2011", "US"),
            new NumberTest("1/12/2011", "US"),
            new NumberTest("10/12/82", "DE"),
            new NumberTest("650x2531234", RegionCode.US),
            new NumberTest("2012-01-02 08:00", RegionCode.US),
            new NumberTest("2012/01/02 08:00", RegionCode.US),
            new NumberTest("20120102 08:00", RegionCode.US)
        };

        /**
         * Strings with number-like things that should only be found under "possible".
         */
        private static readonly NumberTest[] PossibleOnlyCases =
        {
            // US numbers cannot start with 7 in the test metadata to be valid.
            new NumberTest("7121115678", "US"),
            // 'X' should not be found in numbers at leniencies stricter than POSSIBLE, unless it represents
            // a carrier code or extension.
            new NumberTest("1650 x 253 - 1234", "US"),
            new NumberTest("650 x 253 - 1234", "US"),
            new NumberTest("(20) 3346 1234", RegionCode.GB) // Non-optional NP omitted
        };

        /**
         * Strings with number-like things that should only be found up to and including the "valid"
         * leniency level.
         */
        private static readonly NumberTest[] ValidCases =
        {
            new NumberTest("65 02 53 00 00", "US"),
            new NumberTest("6502 538365", "US"),
            new NumberTest("650//253-1234", "US"), // 2 slashes are illegal at higher levels
            new NumberTest("650/253/1234", "US"),
            new NumberTest("9002309. 158", "US"),
            new NumberTest("12 7/8 - 14 12/34 - 5", "US"),
            new NumberTest("12.1 - 23.71 - 23.45", "US"),
            new NumberTest("800 234 1 111x1111", "US"),
            new NumberTest("1979-2011 100", RegionCode.US),
            new NumberTest("+494949-4-94", "DE"), // National number in wrong format
            new NumberTest("\uFF14\uFF11\uFF15\uFF16\uFF16\uFF16\uFF16-\uFF17\uFF17\uFF17", RegionCode.US),
            new NumberTest("2012-0102 08", RegionCode.US), // Very strange formatting.
            new NumberTest("2012-01-02 08", RegionCode.US),
            // Breakdown assistance number with unexpected formatting.
            new NumberTest("1800-1-0-10 22", RegionCode.AU),
            new NumberTest("030-3-2 23 12 34", RegionCode.DE),
            new NumberTest("03 0 -3 2 23 12 34", RegionCode.DE),
            new NumberTest("(0)3 0 -3 2 23 12 34", RegionCode.DE),
            new NumberTest("0 3 0 -3 2 23 12 34", RegionCode.DE)
        };

        /**
         * Strings with number-like things that should only be found up to and including the
         * "strict_grouping" leniency level.
         */
        private static readonly NumberTest[] StrictGroupingCases =
        {
            new NumberTest("(415) 6667777", "US"),
            new NumberTest("415-6667777", "US"),
            // Should be found by strict grouping but not exact grouping, as the last two groups are
            // formatted together as a block.
            new NumberTest("0800-2491234", "DE"),
            // Doesn't match any formatting in the test file, but almost matches an alternate format (the
            // last two groups have been squashed together here).
            new NumberTest("0900-1 123123", RegionCode.DE),
            new NumberTest("(0)900-1 123123", RegionCode.DE),
            new NumberTest("0 900-1 123123", RegionCode.DE)
        };

        /**
         * Strings with number-like things that should be found at all levels.
         */
        private static readonly NumberTest[] ExactGroupingCases = {
            new NumberTest("\uFF14\uFF11\uFF15\uFF16\uFF16\uFF16\uFF17\uFF17\uFF17\uFF17", "US"),
            new NumberTest("\uFF14\uFF11\uFF15-\uFF16\uFF16\uFF16-\uFF17\uFF17\uFF17\uFF17", "US"),
            new NumberTest("4156667777", "US"),
            new NumberTest("4156667777 x 123", "US"),
            new NumberTest("415-666-7777", "US"),
            new NumberTest("415/666-7777", "US"),
            new NumberTest("415-666-7777 ext. 503", "US"),
            new NumberTest("1 415 666 7777 x 123", "US"),
            new NumberTest("+1 415-666-7777", "US"),
            new NumberTest("+494949 49", "DE"),
            new NumberTest("+49-49-34", "DE"),
            new NumberTest("+49-4931-49", "DE"),
            new NumberTest("04931-49", "DE"),  // With National Prefix
            new NumberTest("+49-494949", "DE"),  // One group with country code
            new NumberTest("+49-494949 ext. 49", "DE"),
            new NumberTest("+49494949 ext. 49", "DE"),
            new NumberTest("0494949", "DE"),
            new NumberTest("0494949 ext. 49", "DE"),
            new NumberTest("01 (33) 3461 2234", RegionCode.MX),  // Optional NP present
            new NumberTest("(33) 3461 2234", RegionCode.MX),  // Optional NP omitted
            new NumberTest("1800-10-10 22", RegionCode.AU),  // Breakdown assistance number.
            // Doesn't match any formatting in the test file, but matches an alternate format exactly.
            new NumberTest("0900-1 123 123", RegionCode.DE),
            new NumberTest("(0)900-1 123 123", RegionCode.DE),
            new NumberTest("0 900-1 123 123", RegionCode.DE)
        };

        [Fact]
        public void TestMatchesWithPossibleLeniency()
        {
            var testCases = new List<NumberTest>();
            testCases.AddRange(StrictGroupingCases);
            testCases.AddRange(ExactGroupingCases);
            testCases.AddRange(ValidCases);
            testCases.AddRange(PossibleOnlyCases);
            DoTestNumberMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.POSSIBLE);
        }

        [Fact]
        public void TestNonMatchesWithPossibleLeniency()
        {
            var testCases = new List<NumberTest>();
            testCases.AddRange(ImpossibleCases);
            DoTestNumberNonMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.POSSIBLE);
        }

        [Fact]
        public void TestMatchesWithValidLeniency()
        {
            var testCases = new List<NumberTest>();
            testCases.AddRange(StrictGroupingCases);
            testCases.AddRange(ExactGroupingCases);
            testCases.AddRange(ValidCases);
            DoTestNumberMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.VALID);
        }

        [Fact]
        public void TestNonMatchesWithValidLeniency()
        {
            var testCases = new List<NumberTest>();
            testCases.AddRange(ImpossibleCases);
            testCases.AddRange(PossibleOnlyCases);
            DoTestNumberNonMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.VALID);
        }

        [Fact]
        public void TestMatchesWithStrictGroupingLeniency()
        {
            var testCases = new List<NumberTest>();
            testCases.AddRange(StrictGroupingCases);
            testCases.AddRange(ExactGroupingCases);
            DoTestNumberMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.STRICT_GROUPING);
        }

        [Fact]
        public void TestNonMatchesWithStrictGroupLeniency()
        {
            var testCases = new List<NumberTest>();
            testCases.AddRange(ImpossibleCases);
            testCases.AddRange(PossibleOnlyCases);
            testCases.AddRange(ValidCases);
            DoTestNumberNonMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.STRICT_GROUPING);
        }

        [Fact]
        public void TestMatchesWithExactGroupingLeniency()
        {
            var testCases = new List<NumberTest>();
            testCases.AddRange(ExactGroupingCases);
            DoTestNumberMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.EXACT_GROUPING);
        }

        [Fact]
        public void TestNonMatchesExactGroupLeniency()
        {
            var testCases = new List<NumberTest>();
            testCases.AddRange(ImpossibleCases);
            testCases.AddRange(PossibleOnlyCases);
            testCases.AddRange(ValidCases);
            testCases.AddRange(StrictGroupingCases);
            DoTestNumberNonMatchesForLeniency(testCases, PhoneNumberUtil.Leniency.EXACT_GROUPING);
        }

        private void DoTestNumberMatchesForLeniency(List<NumberTest> testCases,
                                                    PhoneNumberUtil.Leniency leniency)
        {
            var noMatchFoundCount = 0;
            var wrongMatchFoundCount = 0;
            foreach (var test in testCases)
            {
                var iterator =
                    FindNumbersForLeniency(test.RawString, test.Region, leniency);
                var match = iterator.FirstOrDefault();
                if (match == null)
                {
                    noMatchFoundCount++;
                    Console.WriteLine("No match found in " + test + " for leniency: " + leniency);
                }
                else if (!test.RawString.Equals(match.RawString))
                {
                    wrongMatchFoundCount++;
                    Console.WriteLine("Found wrong match in test " + test +
                                      ". Found " + match.RawString);

                }
            }
            Assert.Equal(0, noMatchFoundCount);
            Assert.Equal(0, wrongMatchFoundCount);
        }

        private void DoTestNumberNonMatchesForLeniency(List<NumberTest> testCases,
                                                       PhoneNumberUtil.Leniency leniency)
        {
            var matchFoundCount = 0;
            foreach (var test in testCases)
            {
                var iterator =
                    FindNumbersForLeniency(test.RawString, test.Region, leniency);
                var match = iterator.FirstOrDefault();
                if (match != null)
                {
                    matchFoundCount++;
                    Console.WriteLine("Match found in " + test + " for leniency: " + leniency);
                }
            }
            Assert.Equal(0, matchFoundCount);
        }

        /**
         * Helper method which tests the contexts provided and ensures that:
         * -- if isValid is true, they all find a test number inserted in the middle when leniency of
         *  matching is set to VALID; else no test number should be extracted at that leniency level
         * -- if isPossible is true, they all find a test number inserted in the middle when leniency of
         *  matching is set to POSSIBLE; else no test number should be extracted at that leniency level
         */
        private void FindMatchesInContexts(List<NumberContext> contexts, bool isValid,
                                           bool isPossible, string region, string number)
        {
            if (isValid)
            {
                DoTestInContext(number, region, contexts, PhoneNumberUtil.Leniency.VALID);
            }
            else
            {
                foreach (var context in contexts)
                {
                    var text = context.LeadingText + number + context.TrailingText;
                    Assert.True(HasNoMatches(phoneUtil.FindNumbers(text, region)),
                        "Should not have found a number in " + text);
                }
            }
            if (isPossible)
            {
                DoTestInContext(number, region, contexts, PhoneNumberUtil.Leniency.POSSIBLE);
            }
            else
            {
                foreach (var context in contexts)
                {
                    var text = context.LeadingText + number + context.TrailingText;
                    Assert.True(HasNoMatches(phoneUtil.FindNumbers(text, region, PhoneNumberUtil.Leniency.POSSIBLE,
                                                                  long.MaxValue)),
                                                                  "Should not have found a number in " + text);
                }
            }
        }

        /**
         * Variant of FindMatchesInContexts that uses a default number and region.
         */
        private void FindMatchesInContexts(List<NumberContext> contexts, bool isValid, bool isPossible)
        {
            const string region = "US";
            const string number = "415-666-7777";

            FindMatchesInContexts(contexts, isValid, isPossible, region, number);
        }


        [Fact]
        public void TestNonMatchingBracketsAreInvalid()
        {
            // The digits up to the ", " form a valid US number, but it shouldn't be matched as one since
            // there was a non-matching bracket present.
            Assert.True(HasNoMatches(phoneUtil.FindNumbers(
            "80.585 [79.964, 81.191]", "US")));

            // The trailing "]" is thrown away before parsing, so the resultant number, while a valid US
            // number, does not have matching brackets.
            Assert.True(HasNoMatches(phoneUtil.FindNumbers(
            "80.585 [79.964]", "US")));

            Assert.True(HasNoMatches(phoneUtil.FindNumbers(
            "80.585 ((79.964)", "US")));

            // This case has too many sets of brackets to be valid.
            Assert.True(HasNoMatches(phoneUtil.FindNumbers(
            "(80).(585) (79).(9)64", "US")));
        }

        [Fact]
        public void TestNoMatchIfRegionIsNull()
        {
            // Fail on non-international prefix if region code is null.
            Assert.True(HasNoMatches(phoneUtil.FindNumbers(
                "Random text body - number is 0331 6005, see you there", null)));
        }

        [Fact]
        public void TestNoMatchInEmptyString()
        {
            Assert.True(HasNoMatches(phoneUtil.FindNumbers("", "US")));
            Assert.True(HasNoMatches(phoneUtil.FindNumbers("  ", "US")));
        }

        [Fact]
        public void TestNoMatchIfNoNumber()
        {
            Assert.True(HasNoMatches(phoneUtil.FindNumbers(
                "Random text body - number is foobar, see you there", "US")));
        }

        [Fact]
        public void TestSequences()
        {
            // Test multiple occurrences.
            var text = "Call 033316005  or 032316005!";
            var region = "NZ";

            var number1 = new PhoneNumber.Builder()
                .SetCountryCode(phoneUtil.GetCountryCodeForRegion(region))
                .SetNationalNumber(33316005).Build();
            var match1 = new PhoneNumberMatch(5, "033316005", number1);

            var number2 = new PhoneNumber.Builder()
                .SetCountryCode(phoneUtil.GetCountryCodeForRegion(region))
                .SetNationalNumber(32316005).Build();
            var match2 = new PhoneNumberMatch(19, "032316005", number2);

            var matches = phoneUtil.FindNumbers(text, region, PhoneNumberUtil.Leniency.POSSIBLE, long.MaxValue).GetEnumerator();
            matches.MoveNext();
            Assert.Equal(match1, matches.Current);
            matches.MoveNext();
            Assert.Equal(match2, matches.Current);
            matches.Dispose();
        }

        [Fact]
        public void TestNullInput()
        {
            Assert.True(HasNoMatches(phoneUtil.FindNumbers(null, "US")));
            Assert.True(HasNoMatches(phoneUtil.FindNumbers(null, null)));
        }

        [Fact]
        public void TestMaxMatches()
        {
            // Set up text with 100 valid phone numbers.
            var numbers = new StringBuilder();
            for (var i = 0; i < 100; i++)
                numbers.Append("My info: 415-666-7777,");

            // Matches all 100. Max only applies to failed cases.
            var expected = new List<PhoneNumber>(100);
            var number = phoneUtil.Parse("+14156667777", null);
            for (var i = 0; i < 100; i++)
                expected.Add(number);

            var iterable = phoneUtil.FindNumbers(numbers.ToString(), "US", PhoneNumberUtil.Leniency.VALID, 10);
            var actual = new List<PhoneNumber>(100);
            foreach (var match in iterable)
                actual.Add(match.Number);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestMaxMatchesInvalid()
        {
            // Set up text with 10 invalid phone numbers followed by 100 valid.
            var numbers = new StringBuilder();
            for (var i = 0; i < 10; i++)
                numbers.Append("My address 949-8945-0");
            for (var i = 0; i < 100; i++)
                numbers.Append("My info: 415-666-7777,");

            var iterable = phoneUtil.FindNumbers(numbers.ToString(), "US", PhoneNumberUtil.Leniency.VALID, 10);
            Assert.False(iterable.GetEnumerator().MoveNext());
        }

        [Fact]
        public void TestMaxMatchesMixed()
        {
            // Set up text with 100 valid numbers inside an invalid number.
            var numbers = new StringBuilder();
            for (var i = 0; i < 100; i++)
                numbers.Append("My info: 415-666-7777 123 fake street");

            // Only matches the first 10 despite there being 100 numbers due to max matches.
            var expected = new List<PhoneNumber>(100);
            var number = phoneUtil.Parse("+14156667777", null);
            for (var i = 0; i < 10; i++)
                expected.Add(number);

            var iterable = phoneUtil.FindNumbers(numbers.ToString(), "US", PhoneNumberUtil.Leniency.VALID, 10);
            var actual = new List<PhoneNumber>(100);
            foreach (var match in iterable)
                actual.Add(match.Number);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestNonPlusPrefixedNumbersNotFoundForInvalidRegion()
        {
            // Does not start with a "+", we won't match it.
            var iterable = phoneUtil.FindNumbers("1 456 764 156", RegionCode.ZZ);
            var iterator = iterable.GetEnumerator();
            
            Assert.False(iterator.MoveNext());
            Assert.False(iterator.MoveNext());
            iterator.Dispose();
        }


        [Fact]
        public void TestEmptyIteration()
        {
            var iterable = phoneUtil.FindNumbers("", "ZZ");
            var iterator = iterable.GetEnumerator();

            Assert.False(iterator.MoveNext());
            Assert.False(iterator.MoveNext());
            iterator.Dispose();
        }

        [Fact]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void TestSingleIteration()
        {
            var iterable = phoneUtil.FindNumbers("+14156667777", "ZZ");

            // With hasNext() -> next().
            var iterator = iterable.GetEnumerator();
            // Double hasNext() to ensure it does not advance.
            Assert.True(iterator.MoveNext());
            Assert.NotNull(iterator.Current);
            Assert.False(iterator.MoveNext());
            iterator.Dispose();

            // With next() only.
            iterator = iterable.GetEnumerator();
            Assert.True(iterator.MoveNext());
            Assert.False(iterator.MoveNext());
            iterator.Dispose();
        }

        /**
        * Asserts that another number can be found in {@code text} starting at {@code index}, and that
        * its corresponding range is {@code [start, end)}.
        */
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private void AssertEqualRange(string text, int index, int start, int end)
        {
            var sub = text.Substring(index);
            var matches =
                phoneUtil.FindNumbers(sub, "NZ", PhoneNumberUtil.Leniency.POSSIBLE, long.MaxValue).GetEnumerator();
            Assert.True(matches.MoveNext());
            var match = matches.Current;
            Assert.Equal(start - index, match.Start);
            Assert.Equal(end - start, match.Length);
            Assert.Equal(sub.Substring(match.Start, match.Length), match.RawString);
            matches.Dispose();
        }

        /**
        * Tests numbers found by {@link PhoneNumberUtil#FindNumbers(CharSequence, String)} in various
        * textual contexts.
        *
        * @param number the number to test and the corresponding region code to use
        */
        private void DoTestFindInContext(string number, string defaultCountry)
        {
            FindPossibleInContext(number, defaultCountry);

            var parsed = phoneUtil.Parse(number, defaultCountry);
            if (phoneUtil.IsValidNumber(parsed))
                FindValidInContext(number, defaultCountry);
        }

        private void FindPossibleInContext(string number, string defaultCountry)
        {
            var contextPairs = new List<NumberContext>
            {
                new NumberContext("", ""), // no context
                new NumberContext("   ", "\t"), // whitespace only
                new NumberContext("Hello ", ""), // no context at end
                new NumberContext("", " to call me!"), // no context at start
                new NumberContext("Hi there, call ", " to reach me!"), // no context at start
                new NumberContext("Hi there, call ", ", or don't"), // with commas
                // Three examples without whitespace around the number.
                new NumberContext("Hi call", ""),
                new NumberContext("", "forme"),
                new NumberContext("Hi call", "forme"),
                // With other small numbers.
                new NumberContext("It's cheap! Call ", " before 6:30"),
                // With a second number later.
                new NumberContext("Call ", " or +1800-123-4567!"),
                new NumberContext("Call me on June 2 at", ""), // with a Month-Day date
                // With publication pages.
                new NumberContext(
                    "As quoted by Alfonso 12-15 (2009), you may call me at ", ""),
                new NumberContext(
                    "As quoted by Alfonso et al. 12-15 (2009), you may call me at ", ""),
                // With dates, written in the American style.
                new NumberContext(
                    "As I said on 03/10/2011, you may call me at ", ""),
                // With trailing numbers after a comma. The 45 should not be considered an extension.
                new NumberContext("", ", 45 days a year"),
                // With a postfix stripped off as it looks like the start of another number.
                new NumberContext("Call ", "/x12 more")
            };

            DoTestInContext(number, defaultCountry, contextPairs, PhoneNumberUtil.Leniency.POSSIBLE);
        }

        /**
        * Tests valid numbers in contexts that fail for {@link Leniency#POSSIBLE}.
        */
        private void FindValidInContext(string number, string defaultCountry)
        {
            var contextPairs = new List<NumberContext> {
            // With other small numbers.
            new NumberContext("It's only 9.99! Call ", " to buy"),
            // With a number Month/Day date.
            new NumberContext("Call me on 06/21 at ", ""),
            // With a number Day.Month date.
            new NumberContext("Call me on 21.6. at ", ""),
            // With a number Month/Day/Year date.
            new NumberContext("Call me on 06/21/84 at ", "")};
            DoTestInContext(number, defaultCountry, contextPairs, PhoneNumberUtil.Leniency.VALID);
        }

        private void DoTestInContext(string number, string defaultCountry,
            List<NumberContext> contextPairs, PhoneNumberUtil.Leniency leniency)
        {
            foreach (var context in contextPairs)
            {
                var prefix = context.LeadingText;
                var text = prefix + number + context.TrailingText;

                var start = prefix.Length;
                var length = number.Length;
                var iterator = phoneUtil.FindNumbers(text, defaultCountry, leniency, long.MaxValue);

                var match = iterator.First();
                Assert.NotNull(match);

                var extracted = text.Substring(match.Start, match.Length);
                Assert.True(start == match.Start && length == match.Length,
                    "Unexpected phone region in '" + text + "'; extracted '" + extracted + "'");
                Assert.Equal(number, extracted);
                Assert.Equal(match.RawString, extracted);

                EnsureTermination(text, defaultCountry, leniency);
            }
        }

        /**
        * Exhaustively searches for phone numbers from each index within {@code text} to test that
        * finding matches always terminates.
        */
        private void EnsureTermination(string text, string defaultCountry, PhoneNumberUtil.Leniency leniency)
        {
            for (var index = 0; index <= text.Length; index++)
            {
                var sub = text.Substring(index);
                var matches = new StringBuilder();
                // Iterates over all matches.
                foreach (var match in phoneUtil.FindNumbers(sub, defaultCountry, leniency, long.MaxValue))
                    matches.Append(", ").Append(match);
            }
        }

        private IEnumerable<PhoneNumberMatch> FindNumbersForLeniency(
      string text, string defaultCountry, PhoneNumberUtil.Leniency leniency)
        {
            return phoneUtil.FindNumbers(text, defaultCountry, leniency, long.MaxValue);
        }

        /**
        * Returns true if there were no matches found.
        */
        private bool HasNoMatches(IEnumerable<PhoneNumberMatch> iterable)
        {
            return !iterable.GetEnumerator().MoveNext();
        }

        /**
        * Small class that holds the context of the number we are testing against. The test will
        * insert the phone number to be found between leadingText and trailingText.
        */
        private class NumberContext
        {
            public readonly string LeadingText;
            public readonly string TrailingText;

            public NumberContext(string leadingText, string trailingText)
            {
                LeadingText = leadingText;
                TrailingText = trailingText;
            }
        }

        /**
           * Small class that holds the number we want to test and the region for which it should be valid.
           */
        private class NumberTest
        {
            public readonly string RawString;
            public readonly string Region;

            public NumberTest(string rawString, string region)
            {
                RawString = rawString;
                Region = region;
            }

            public override string ToString()
            {
                return RawString + " (" + Region + ")";
            }
        }

    }
}
