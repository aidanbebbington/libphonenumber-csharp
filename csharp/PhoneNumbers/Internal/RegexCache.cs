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

namespace PhoneNumbers
{
    public class RegexCache
    {
        private class Entry
        {
            public PhoneRegex Regex;
            public LinkedListNode<string> Node;
        }

        private readonly int size;
        private readonly LinkedList<string> lru;
        private readonly Dictionary<string, Entry> cache;

        public RegexCache(int size)
        {
            this.size = size;
            cache = new Dictionary<string, Entry>(size);
            lru = new LinkedList<string>();
        }

        public PhoneRegex GetPatternForRegex(string regex)
        {
            lock (this)
            {
                Entry e;
                if (!cache.TryGetValue(regex, out e))
                {
                    // Insert new node
                    var r = new PhoneRegex(regex);
                    var n = lru.AddFirst(regex);
                    cache[regex] = e = new Entry { Regex = r, Node = n };
                    // Check cache size
                    if (lru.Count > size)
                    {
                        var o = lru.Last.Value;
                        cache.Remove(o);
                        lru.RemoveLast();
                    }
                }
                else
                {
                    if (e.Node != lru.First)
                    {
                        lru.Remove(e.Node);
                        lru.AddFirst(e.Node);
                    }
                }
                return e.Regex;
            }
        }

        // This method is used for testing.
        public bool ContainsRegex(string regex)
        {
            lock (this)
            {
                return cache.ContainsKey(regex);
            }
        }
    }
}
