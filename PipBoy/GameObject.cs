using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
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

        private string _path;

        public uint Id { get; private set; }
        public ObjectType Type { get; private set; }

        public Dictionary<string, uint> Properties { get; private set; }
        public uint[] Array { get; private set; }
        public DataElement Primitive { get; private set; }

        public string Path => _path ?? (_path = _gameStateManager.GetName(Id));

        public event EventHandler<GameObjectChangedEvent> Changed;

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
            return ConvertImpl(binder.Type, out result);
        }

        private bool ConvertImpl(Type type, out object result)
        {
            result = null;

            if (Type == ObjectType.Array && type.BaseType == typeof(Array))
            {
                var elementType = type.GetElementType();
                var array = (Array)Activator.CreateInstance(type, Array.Length);
                for (int i = 0; i < Array.Length; i++)
                {
                    object element;
                    _gameStateManager.GameObjects[Array[i]].ConvertImpl(elementType, out element);
                    array.SetValue(element, i);
                }
                result = array;
                return true;
            }

            if (Type != ObjectType.Primitive)
            {
                throw new RuntimeBinderException($"Cannot convert to {type} because the GameObject type is '{Type}' (expected: '{ObjectType.Primitive}')");
            }

            if (!type.IsInstanceOfType(Primitive.ValueObject))
            {
                throw new RuntimeBinderException($"Cannot convert from '{Primitive.ValueObject.GetType()}' to '{type}'");
            }

            result = Convert.ChangeType(Primitive.ValueObject, type);
            return true;
        }

        public T As<T>()
        {
            return (T)(dynamic)this;
        }

        public void RaiseChanged(List<GameObject> changedChildren)
        {
            Changed?.Invoke(this, new GameObjectChangedEvent(changedChildren));
        }

        private void ToString(StringBuilder sb, int indent = 0)
        {
            sb.Append('\t', indent);
            sb.Append($"[{Id}: {Path}] = {ToString()}");

            if (Type == ObjectType.Object)
            {
                foreach (var property in Properties)
                {
                    sb.AppendLine();
                    _gameStateManager.GameObjects[property.Value].ToString(sb, indent + 1);
                }
            }

            if (Type == ObjectType.Array)
            {
                foreach (var id in Array)
                {
                    sb.AppendLine();
                    _gameStateManager.GameObjects[id].ToString(sb, indent + 1);
                }
            }
        }

        public string ToString(bool full)
        {
            if (!full)
            {
                return ToString();
            }

            var sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        public override string ToString()
        {
            switch (Type)
            {
                case ObjectType.Primitive:
                    return Primitive.ToString();
                case ObjectType.Object:
                    return $"[Object of {Properties.Count}]";
                case ObjectType.Array:
                    return $"[Array of {Array.Length}]";
            }
            throw new InvalidOperationException();
        }
    }

    public class GameObjectChangedEvent : EventArgs
    {
        public List<GameObject> ChangedChildren { get; }

        public GameObjectChangedEvent(List<GameObject> changedChildren)
        {
            ChangedChildren = changedChildren;
        }
    }
}