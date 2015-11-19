using System.Collections.Generic;

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
                GameObject gameObject;
                switch (element.Value.Type)
                {
                    case ElementType.Map:
                        gameObject = new GameObject(this, element.Key, (MapElement)element.Value);
                        break;
                    case ElementType.List:
                        gameObject = new GameObject(this, element.Key, (ListElement)element.Value);
                        break;
                    default:
                        gameObject = new GameObject(this, element.Key, element.Value);
                        break;
                }
                GameObjects.Add(element.Key, gameObject);
            }
            GameState = GameObjects[0];
        }

        public void Update(Dictionary<uint, DataElement> data)
        {
            // TODO
        }
    }
}
