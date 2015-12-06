using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PipBoy
{
    public class GameStateManager
    {
        private readonly Dictionary<uint, GameObjectEx> _extendedInfo;
        public dynamic GameState { get; private set; }
        public Dictionary<uint, GameObject> GameObjects { get; private set; }
        
        public GameStateManager(Dictionary<uint, DataElement> initialPacket)
        {
            GameObjects = new Dictionary<uint, GameObject>();
            foreach (var element in initialPacket)
            {
                GameObjects.Add(element.Key, CreateGameObject(element.Key, element.Value));
            }
            GameState = GameObjects[0];

            _extendedInfo = new Dictionary<uint, GameObjectEx>();
            Inspect((GameObject)GameState, "[0]", null);
        }

        public void Update(Dictionary<uint, DataElement> data)
        {
            var changedObjectsSources = new List<uint>();
            foreach (var element in data)
            {
                var mapElement = element.Value as MapElement;
                var index = element.Key;
                if (mapElement != null && mapElement.ExtraValues.Length > 0)
                {
                    if (mapElement.ExtraValues.Length != mapElement.Value.Count)
                    {
                        // TODO: handle mapElements where ExtraValues.Length != mapElement.Value.Count
                        continue;
                    }

                    // this is a partial object update
                    var parent = GameObjects[index];
                    changedObjectsSources.Add(parent.Id);
                    Debug.Assert(parent.Type == ObjectType.Object);
                    foreach (var oldIndex in mapElement.ExtraValues)
                    {
                        var nameByIndex = parent.Properties.Single(kvp => kvp.Value == oldIndex).Key;
                        var newElement = mapElement.Value.Single(kvp => kvp.Value == nameByIndex);
                        parent.Properties[nameByIndex] = newElement.Key;
                        var oldGameObject = GameObjects[oldIndex];
                        GameObjects.Remove(oldIndex);
                        _extendedInfo.Remove(oldIndex);
                        Inspect(GameObjects[newElement.Key], nameByIndex, _extendedInfo[index]);
                        //Console.WriteLine($"Removed {oldIndex}");
                        Debug.Assert(oldGameObject.Type != ObjectType.Object);
                        if (oldGameObject.Type == ObjectType.Array)
                        {
                            RemoveOrphanedListItems(oldGameObject, GameObjects[newElement.Key]); // assume newElement.Key has been added already -> ok?
                        }
                    }
                }
                else
                {
                    var gameObject = CreateGameObject(index, element.Value);
                    GameObject oldGameObject;
                    if (GameObjects.TryGetValue(index, out oldGameObject))
                    {
                        Debug.Assert(gameObject.Type != ObjectType.Object);
                        GameObjects[index] = gameObject;
                        if (oldGameObject.Type == ObjectType.Array)
                        {
                            RemoveOrphanedListItems(oldGameObject, gameObject);
                        }
                        changedObjectsSources.Add(index);
                        var stateEx = _extendedInfo[index];
                        _extendedInfo.Remove(index);
                        Inspect(gameObject, stateEx.Name, _extendedInfo[stateEx.ParentId]);
                    }
                    else
                    {
                        GameObjects.Add(index, gameObject);
                    }
                }
            }

            var changedObjects = new Dictionary<uint, List<GameObject>>();
            foreach (var changedObjectId in changedObjectsSources)
            {
                var changedObject = _extendedInfo[changedObjectId];
                foreach (var parentId in changedObject.Path)
                {
                    List<GameObject> gameObjects;
                    if (changedObjects.TryGetValue(parentId, out gameObjects))
                    {
                        gameObjects.Add(changedObject.GameObject);
                    }
                    else
                    {
                        changedObjects[parentId] = new List<GameObject> { changedObject.GameObject };
                    }
                }
            }

            foreach (var changedObject in changedObjects)
            {
                GameObjects[changedObject.Key].RaiseChanged(changedObject.Value);
            }
        }

        public string GetName(uint id)
        {
            var gameObject = _extendedInfo[id];
            if (id == 0)
            {
                return gameObject.Name;
            }

            var sb = new StringBuilder();
            var isArray = false;
            foreach (var parentId in gameObject.Path.Skip(1))
            {
                var parent = _extendedInfo[parentId];
                if (!isArray)
                {
                    sb.Append("::");
                }
                sb.Append(parent.Name);
                isArray = parent.GameObject.Type == ObjectType.Array;
            }
            return sb.ToString().Substring(2);
        }

        private void Inspect(GameObject gameObject, string name, GameObjectEx parent)
        {
            var currentPath = new List<uint>();
            if (parent != null)
            {
                currentPath.AddRange(parent.Path);
            }
            currentPath.Add(gameObject.Id);
            var gameObjectEx = new GameObjectEx(gameObject, currentPath.ToArray(), name);
            _extendedInfo[gameObject.Id] = gameObjectEx;

            if (gameObject.Type == ObjectType.Object)
            {
                foreach (var property in gameObject.Properties)
                {
                    Inspect(GameObjects[property.Value], property.Key, gameObjectEx);
                }
            }
            else if (gameObject.Type == ObjectType.Array)
            {
                var index = 0;
                foreach (var id in gameObject.Array)
                {
                    Inspect(GameObjects[id], "[" + index++ + "]", gameObjectEx);
                }
            }
        }

        private void RemoveOrphanedListItems(GameObject oldList, GameObject newList)
        {
            var removedItems = oldList.Array.Except(newList.Array);
            foreach (var removedItem in removedItems)
            {
                RemoveObject(GameObjects[removedItem]);
            }
        }

        private void RemoveObject(GameObject gameObject)
        {
            if (gameObject.Type == ObjectType.Array)
            {
                foreach (var element in gameObject.Array)
                {
                    RemoveObject(GameObjects[element]);
                }
            }
            else if (gameObject.Type == ObjectType.Object)
            {
                foreach (var element in gameObject.Properties.Values)
                {
                    RemoveObject(GameObjects[element]);
                }
            }
            GameObjects.Remove(gameObject.Id);
            _extendedInfo.Remove(gameObject.Id);
            //Console.WriteLine($"Removed {gameObject.Id}");
        }

        private GameObject CreateGameObject(uint index, DataElement element)
        {
            switch (element.Type)
            {
                case ElementType.Map:
                    return new GameObject(this, index, (MapElement)element);
                case ElementType.List:
                    return new GameObject(this, index, (ListElement)element);
                default:
                    return new GameObject(this, index, element);
            }
        }

        public class GameObjectEx
        {
            public GameObject GameObject { get; }

            public uint[] Path { get; }
            public string Name { get; }

            public uint ParentId { get; }

            public GameObjectEx(GameObject gameObject, uint[] path, string name)
            {
                GameObject = gameObject;
                Path = path;
                Name = name;
                ParentId = path.Length > 1 ? path[path.Length - 2] : 0xFFFFFFFF;
            }
        }
    }
}
