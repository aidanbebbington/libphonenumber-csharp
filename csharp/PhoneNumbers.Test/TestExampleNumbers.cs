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

using System.Collections.Generic;
using Xunit;

namespace PhoneNumbers.Test
{
    [Collection("TestMetadataTestCase")]
    public class TestExampleNumbers
    {
        private readonly PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();
        private readonly List<PhoneNumber> invalidCases = new();
        private readonly List<PhoneNumber> wrongTypeCases = new();

        /**
        * @param exampleNumberRequestedType  type we are requesting an example number for
        * @param possibleExpectedTypes       acceptable types that this number should match, such as
        *     FIXED_LINE and FIXED_LINE_OR_MOBILE for a fixed line example number.
        */
        private void CheckNumbersValidAndCorrectType(PhoneNumberType exampleNumberRequestedType,
            HashSet<PhoneNumberType> possibleExpectedTypes)
        {
            foreach (var regionCode in phoneNumberUtil.GetSupportedRegions())
            {
                var exampleNumber =
                phoneNumberUtil.GetExampleNumberForType(regionCode, exampleNumberRequestedType);
                if (exampleNumber != null)
                {
                    if (!phoneNumberUtil.IsValidNumber(exampleNumber))
                    {
                        invalidCases.Add(exampleNumber);
                        //LOGGER.log(Level.SEVERE, "Failed validation for " + exampleNumber.toString());
                    }
                    else
                    {
                        // We know the number is valid, now we check the type.
                        var exampleNumberType = phoneNumberUtil.GetNumberType(exampleNumber);
                        if (!possibleExpectedTypes.Contains(exampleNumberType))
                        {
                            wrongTypeCases.Add(exampleNumber);
                            //LOGGER.log(Level.SEVERE, "Wrong type for " + exampleNumber.toString() + ": got " + exampleNumberType);
                            //LOGGER.log(Level.WARNING, "Expected types: ");
                            //for (PhoneNumberType type : possibleExpectedTypes) {
                            //LOGGER.log(Level.WARNING, type.toString());
                        }
                    }
                }
            }
        }

        private HashSet<PhoneNumberType> MakeSet(PhoneNumberType t1, PhoneNumberType t2)
        {
            return new HashSet<PhoneNumberType>(new[] { t1, t2 });
        }

        private HashSet<PhoneNumberType> MakeSet(PhoneNumberType t1)
        {
            return MakeSet(t1, t1);
        }

        [Fact]
        public void TestFixedLine()
        {
            var fixedLineTypes = MakeSet(PhoneNumberType.FIXED_LINE,
                                            PhoneNumberType.FIXED_LINE_OR_MOBILE);
            CheckNumbersValidAndCorrectType(PhoneNumberType.FIXED_LINE, fixedLineTypes);
            Assert.Empty(invalidCases);
            Assert.Empty(wrongTypeCases);
        }

        [Fact]
        public void TestMobile()
        {
            var mobileTypes = MakeSet(PhoneNumberType.MOBILE,
                                                          PhoneNumberType.FIXED_LINE_OR_MOBILE);
            CheckNumbersValidAndCorrectType(PhoneNumberType.MOBILE, mobileTypes);
            Assert.Empty(invalidCases);
            Assert.Empty(wrongTypeCases);
        }

        [Fact]
        public void TestTollFree()
        {

            var tollFreeTypes = MakeSet(PhoneNumberType.TOLL_FREE);
            CheckNumbersValidAndCorrectType(PhoneNumberType.TOLL_FREE, tollFreeTypes);
            Assert.Empty(invalidCases);
            Assert.Empty(wrongTypeCases);
        }

        [Fact]
        public void TestPremiumRate()
        {
            var premiumRateTypes = MakeSet(PhoneNumberType.PREMIUM_RATE);
            CheckNumbersValidAndCorrectType(PhoneNumberType.PREMIUM_RATE, premiumRateTypes);
            Assert.Empty(invalidCases);
            Assert.Empty(wrongTypeCases);
        }

        [Fact]
        public void TestVoip()
        {
            var voipTypes = MakeSet(PhoneNumberType.VOIP);
            CheckNumbersValidAndCorrectType(PhoneNumberType.VOIP, voipTypes);
            Assert.Empty(invalidCases);
            Assert.Empty(wrongTypeCases);
        }

        [Fact]
        public void TestPager()
        {
            var pagerTypes = MakeSet(PhoneNumberType.PAGER);
            CheckNumbersValidAndCorrectType(PhoneNumberType.PAGER, pagerTypes);
            Assert.Empty(invalidCases);
            Assert.Empty(wrongTypeCases);
        }

        [Fact]
        public void TestUan()
        {
            var uanTypes = MakeSet(PhoneNumberType.UAN);
            CheckNumbersValidAndCorrectType(PhoneNumberType.UAN, uanTypes);
            Assert.Empty(invalidCases);
            Assert.Empty(wrongTypeCases);
        }

        [Fact]
        public void TestVoicemail()
        {
            var voicemailTypes = MakeSet(PhoneNumberType.VOICEMAIL);
            CheckNumbersValidAndCorrectType(PhoneNumberType.VOICEMAIL, voicemailTypes);
            Assert.Empty(invalidCases);
            Assert.Empty(wrongTypeCases);
        }

        [Fact]
        public void TestSharedCost()
        {
            var sharedCostTypes = MakeSet(PhoneNumberType.SHARED_COST);
            CheckNumbersValidAndCorrectType(PhoneNumberType.SHARED_COST, sharedCostTypes);
            Assert.Empty(invalidCases);
            Assert.Empty(wrongTypeCases);
        }


        // TODO: Update this to use connectsToEmergencyNumber or similar once that is


        [Fact]
        public void TestGlobalNetworkNumbers()
        {
            foreach(var callingCode in phoneNumberUtil.GetSupportedGlobalNetworkCallingCodes())
            {
                var exampleNumber =
                    phoneNumberUtil.GetExampleNumberForNonGeoEntity(callingCode);
                Assert.NotNull(exampleNumber);
                if (!phoneNumberUtil.IsValidNumber(exampleNumber))
                {
                    invalidCases.Add(exampleNumber);
                    // LOGGER.log(Level.SEVERE, "Failed validation for " + exampleNumber.toString());
                }
            }
        }

    }
}
