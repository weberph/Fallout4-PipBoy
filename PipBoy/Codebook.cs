using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PipBoy
{
    public class Codebook : Dictionary<uint, string>
    {
        private List<uint> _visitedElements;

        public void Append(Dictionary<uint, DataElement> data)
        {
            _visitedElements = new List<uint>();
            AppendInternal(data);

            while (data.Keys.Except(Keys).Any())    // while unknown keys exist: visit remaining elements
            {
                AppendInternal(data.Keys.Except(_visitedElements).ToDictionary(k => k, k => data[k]));
            }
        }

        private void AppendInternal(Dictionary<uint, DataElement> data)
        {
            var startIndex = data.Keys.Min();
            string prefix;
            TryGetValue(startIndex, out prefix);
            BuildCodebook(data, startIndex, prefix ?? "", this);
        }

        private void BuildCodebook(Dictionary<uint, DataElement> data, uint index, string prefix, Dictionary<uint, string> codebook)
        {
            DataElement indexedElement;
            if (!data.TryGetValue(index, out indexedElement))
            {
                return;
            }

            _visitedElements.Add(index);

            switch (indexedElement.Type)
            {
                case ElementType.Map:
                    var map = ((MapElement)indexedElement).Value;
                    AddOrUpdate(codebook, index, prefix);

                    foreach (var item in map)
                    {
                        var name = item.Value;
                        if (prefix.Length > 0)
                        {
                            name = prefix + "::" + name;
                        }

                        codebook.Add(item.Key, name);
                        BuildCodebook(data, item.Key, name, codebook);
                    }
                    break;
                case ElementType.List:
                    AddOrUpdate(codebook, index, prefix);

                    var list = ((ListElement)indexedElement).Value;
                    for (var i = 0; i < list.Count; i++)
                    {
                        var name = prefix + $"[{i}]";
                        AddOrUpdate(codebook, list[i], name);
                        BuildCodebook(data, list[i], name, codebook);
                    }
                    break;
            }
        }

        private static void AddOrUpdate(Dictionary<uint, string> codebook, uint index, string name)
        {
            string assertName;
            if (codebook.TryGetValue(index, out assertName))
            {
                if (assertName != name)
                {
                    if (assertName.Contains("[") && name.Contains("["))
                    {
                        if (name.Substring(0, assertName.IndexOf('[')) == assertName.Substring(0, assertName.IndexOf('[')))
                        {
                            codebook[index] = name;
                            return;
                        }
                    }
                    Debug.Assert(false);
                }
            }
            else
            {
                codebook.Add(index, name);
            }
        }
    }
}