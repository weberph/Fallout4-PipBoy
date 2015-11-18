using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PipBoy
{
    public enum ElementType
    {
        Boolean,
        Int8,
        UInt8,
        Int32,
        UInt32,
        Float,
        String,
        List,
        Map,
    }

    public class DataElement
    {
        public ElementType Type { get; private set; }

        protected DataElement(ElementType type)
        {
            Type = type;
        }
    }

    public class DataElement<T> : DataElement
    {
        public T Value { get; protected set; }

        protected DataElement(ElementType type, T value)
            : base(type)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class BoolElement : DataElement<bool>
    {
        public BoolElement(bool value)
            : base(ElementType.Boolean, value)
        {
        }
    }

    public class UInt8Element : DataElement<byte>
    {
        public UInt8Element(byte value)
            : base(ElementType.UInt8, value)
        {
        }
    }

    public class Int8Element : DataElement<sbyte>
    {
        public Int8Element(sbyte value)
            : base(ElementType.Int8, value)
        {
        }
    }

    public class UInt32Element : DataElement<UInt32>
    {
        public UInt32Element(UInt32 value)
            : base(ElementType.UInt32, value)
        {
        }
    }

    public class Int32Element : DataElement<Int32>
    {
        public Int32Element(Int32 value)
            : base(ElementType.Int32, value)
        {
        }
    }

    public class FloatElement : DataElement<Single>
    {
        public FloatElement(Single value)
            : base(ElementType.Float, value)
        {
        }
    }

    public class StringElement : DataElement<string>
    {
        public StringElement(string value)
            : base(ElementType.String, value)
        {
        }
    }

    public class ListElement : DataElement<List<UInt32>>
    {
        public ListElement(List<UInt32> value)
            : base(ElementType.List, value)
        {
        }

        public override string ToString()
        {
            if (Value.Count == 0)
            {
                return "[]";
            }
            return "[" + Value.Aggregate(new StringBuilder(), (a, b) => a.Append(", ").Append(b)).ToString().Substring(2) + "]";
        }
    }

    public class MapElement : DataElement<Dictionary<UInt32, string>>
    {
        private readonly uint[] _extraValues;       // TODO: not sure yet what these extraValues are and if the type arg of DataElement<T> should contain them

        public MapElement(Dictionary<UInt32, string> value, uint[] extraValues)
            : base(ElementType.Map, value)
        {
            _extraValues = extraValues;
        }

        public override string ToString()
        {
            if (Value.Count == 0)
            {
                return "[]";
            }
            return "[" + Value.Aggregate(new StringBuilder(), (a, b) => a.Append(", ").AppendFormat("[{0} = {1}]", b.Key, b.Value)).ToString().Substring(2) + "]";
        }
    }
}
