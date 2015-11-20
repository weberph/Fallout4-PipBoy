using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PipBoy
{
    public class GameStateManager
    {
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
        }

        public void Update(Dictionary<uint, DataElement> data)
        {
            foreach (var element in data)
            {
                var mapElement = element.Value as MapElement;
                if (mapElement != null && mapElement.ExtraValues.Length > 0)
                {
                    if (mapElement.ExtraValues.Length != mapElement.Value.Count)
                    {
                        // TODO: handle mapElements where ExtraValues.Length != mapElement.Value.Count
                        continue;
                    }

                    // this is a partial object update
                    var parent = GameObjects[element.Key];
                    Debug.Assert(parent.Type == ObjectType.Object);
                    foreach (var oldIndex in mapElement.ExtraValues)
                    {
                        var nameByIndex = parent.Properties.Single(kvp => kvp.Value == oldIndex).Key;
                        var newElement = mapElement.Value.Single(kvp => kvp.Value == nameByIndex);
                        parent.Properties[nameByIndex] = newElement.Key;
                        var oldGameObject = GameObjects[oldIndex];
                        GameObjects.Remove(oldIndex);
                        //Console.WriteLine($"Removed {oldIndex}");
                        Debug.Assert(oldGameObject.Type != ObjectType.Object);
                        if (oldGameObject.Type == ObjectType.Array)
                        {
                            RemoveOrphanedListItems(oldGameObject, GameObjects[newElement.Key]); // assume newElement.Key has been added already -> ok?
                        }
                        // TODO: notify old element / fire update event?
                    }
                }
                else
                {
                    var gameObject = CreateGameObject(element.Key, element.Value);
                    GameObject oldGameObject;
                    if (GameObjects.TryGetValue(element.Key, out oldGameObject))
                    {
                        Debug.Assert(gameObject.Type != ObjectType.Object);
                        GameObjects[element.Key] = gameObject;
                        if (oldGameObject.Type == ObjectType.Array)
                        {
                            RemoveOrphanedListItems(oldGameObject, gameObject);
                        }
                        // TODO: notify old element / fire update event?
                    }
                    else
                    {
                        GameObjects.Add(element.Key, gameObject);
                    }
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
    }
}
