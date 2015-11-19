using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;

namespace PipBoy
{
    public enum ObjectType
    {
        Array,
        Object,
        Primitive
    }

    public class GameObject : DynamicObject
    {
        private readonly GameStateManager _gameStateManager;

        public uint Id { get; private set; }
        public ObjectType Type { get; private set; }

        public Dictionary<string, uint> Properties { get; private set; }
        public uint[] Array { get; private set; }
        public DataElement Primitive { get; private set; }

        public GameObject(GameStateManager gameStateManager, uint id, ObjectType type)
        {
            _gameStateManager = gameStateManager;
            Id = id;
            Type = type;
        }

        public GameObject(GameStateManager gameStateManager, uint id, MapElement map)
            : this(gameStateManager, id, ObjectType.Object)
        {
            Properties = map.Value.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }

        public GameObject(GameStateManager gameStateManager, uint id, ListElement list)
            : this(gameStateManager, id, ObjectType.Array)
        {
            Array = list.Value.ToArray();
        }

        public GameObject(GameStateManager gameStateManager, uint id, DataElement element)
            : this(gameStateManager, id, ObjectType.Primitive)
        {
            Primitive = element;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            if (Type != ObjectType.Object)
            {
                throw new RuntimeBinderException($"Cannot get property '{binder.Name}' because the GameObject type is '{Type}' (expected: '{ObjectType.Object}')");
            }

            var name = binder.Name;
            // allow access to elements where the name is a number (use '_' to prefix the number during dynamic member access)
            if (name.Length > 2 && name[0] == '_')
            {
                int numericName;
                if (int.TryParse(name.Substring(1), out numericName))
                {
                    name = name.Substring(1);
                }
            }

            uint index;
            if (!Properties.TryGetValue(name, out index))
            {
                throw new RuntimeBinderException($"Property '{name}' not found");
            }

            result = _gameStateManager.GameObjects[index];
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;

            if (indexes.Length != 1 || !(indexes[0] is int))
            {
                throw new RuntimeBinderException($"Index for {ObjectType.Array} must be a single int");
            }

            var index = (int)indexes[0];

            if (Type != ObjectType.Array)
            {
                throw new RuntimeBinderException($"Cannot get element by index ('{index}') because the GameObject type is '{Type}' (expected: '{ObjectType.Array}')");
            }

            result = _gameStateManager.GameObjects[Array[index]];
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = null;
            if (Type != ObjectType.Primitive)
            {
                throw new RuntimeBinderException($"Cannot convert to {binder.Type} because the GameObject type is '{Type}' (expected: '{ObjectType.Primitive}')");
            }

            if (!binder.Type.IsAssignableFrom(Primitive.ValueObject.GetType()))
            {
                throw new RuntimeBinderException($"Cannot convert from '{Primitive.ValueObject.GetType()}' to '{binder.Type}'");
            }

            result = Convert.ChangeType(Primitive.ValueObject, binder.Type);
            return true;
        }
    }
}