/*
 * Copyright (C) 2011 Google Inc.
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
using System.Collections.Immutable;
using Xunit;

namespace PhoneNumbers.Test
{
    /**
    * Unittests for MappingFileProvider.java
    *
    * @author Shaopeng Jia
    */
    [Collection("TestMetadataTestCase")]
    public class TestMappingFileProvider
    {
        private readonly MappingFileProvider mappingProvider = new MappingFileProvider();

        public TestMappingFileProvider()
        {
            var mapping = new SortedDictionary<int, ImmutableHashSet<string>>
            {
                [1] = new[] {"en"}.ToImmutableHashSet(),
                [86] = new[] {"zh", "en", "zh_Hant"}.ToImmutableHashSet(),
                [41] = new[] {"de", "fr", "it", "rm"}.ToImmutableHashSet(),
                [65] = new[] {"en", "zh_Hans", "ms", "ta"}.ToImmutableHashSet()
            }.ToImmutableSortedDictionary();
            mappingProvider.ReadFileConfigs(mapping);
        }

        [Fact]
        public void TestGetFileName()
        {
            Assert.Equal("en.1.txt", mappingProvider.GetFileName(1, "en", "", ""));
            Assert.Equal("en.1.txt", mappingProvider.GetFileName(1, "en", "", "US"));
            Assert.Equal("en.1.txt", mappingProvider.GetFileName(1, "en", "", "GB"));
            Assert.Equal("de.41.txt", mappingProvider.GetFileName(41, "de", "", "CH"));
            Assert.Equal("", mappingProvider.GetFileName(44, "en", "", "GB"));
            Assert.Equal("zh.86.txt", mappingProvider.GetFileName(86, "zh", "", ""));
            Assert.Equal("zh.86.txt", mappingProvider.GetFileName(86, "zh", "Hans", ""));
            Assert.Equal("zh.86.txt", mappingProvider.GetFileName(86, "zh", "", "CN"));
            Assert.Equal("", mappingProvider.GetFileName(86, "", "", "CN"));
            Assert.Equal("zh.86.txt", mappingProvider.GetFileName(86, "zh", "Hans", "CN"));
            Assert.Equal("zh.86.txt", mappingProvider.GetFileName(86, "zh", "Hans", "SG"));
            Assert.Equal("zh.86.txt", mappingProvider.GetFileName(86, "zh", "", "SG"));
            Assert.Equal("zh_Hant.86.txt", mappingProvider.GetFileName(86, "zh", "", "TW"));
            Assert.Equal("zh_Hant.86.txt", mappingProvider.GetFileName(86, "zh", "", "HK"));
            Assert.Equal("zh_Hant.86.txt", mappingProvider.GetFileName(86, "zh", "Hant", "TW"));
        }
    }
}