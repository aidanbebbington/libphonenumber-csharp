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
using Xunit;

namespace PhoneNumbers.Test
{
    public class TestPhoneNumberMatch
    {
        [Fact]
        public void TestValueTypeSemantics()
        {
            var number = PhoneNumber.DefaultInstance;
            var match1 = new PhoneNumberMatch(10, "1 800 234 45 67", number);
            var match2 = new PhoneNumberMatch(10, "1 800 234 45 67", number);

            Assert.Equal(match1, match2);
            Assert.Equal(match1.GetHashCode(), match2.GetHashCode());
            Assert.Equal(match1.Start, match2.Start);
            Assert.Equal(match1.Length, match2.Length);
            Assert.Equal(match1.Number, match2.Number);
            Assert.Equal(match1.RawString, match2.RawString);
            Assert.Equal("1 800 234 45 67", match1.RawString);
        }

        /**
        * Tests the value type semantics for matches with a null number.
        */
        [Fact]
        public void TestIllegalArguments()
        {
            Assert.Throws<ArgumentException>(() => new PhoneNumberMatch(-110, "1 800 234 45 67", PhoneNumber.DefaultInstance));
            Assert.Throws<ArgumentNullException>(() => new PhoneNumberMatch(10, "1 800 234 45 67", null));
            Assert.Throws<ArgumentNullException>(() => new PhoneNumberMatch(10, null, PhoneNumber.DefaultInstance));
            Assert.Throws<ArgumentNullException>(() => new PhoneNumberMatch(10, null, null));
        }
    }
}
