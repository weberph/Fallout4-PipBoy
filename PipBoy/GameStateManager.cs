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
                    // this is a partial object update
                    var parent = GameObjects[element.Key];
                    if (parent.Type != ObjectType.Object)
                    {
                        Debugger.Break();
                    }
                    foreach (var extraValue in mapElement.ExtraValues)
                    {
                        var nameByIndex = parent.Properties.Single(kvp => kvp.Value == extraValue).Key;
                        var newIndex = mapElement.Value.Single(kvp => kvp.Value == nameByIndex).Key;
                        parent.Properties[nameByIndex] = newIndex;
                        // TODO: notify old element / fire update event?
                    }
                }
                else
                {
                    var gameObject = CreateGameObject(element.Key, element.Value);
                    GameObject oldGameObject;
                    if (GameObjects.TryGetValue(element.Key, out oldGameObject))
                    {
                        GameObjects[element.Key] = gameObject;
                        // TODO: notify old element / fire update event?
                    }
                    else
                    {
                        GameObjects.Add(element.Key, gameObject);
                    }
                }
            }
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
