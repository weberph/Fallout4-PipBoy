using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PipBoy
{
    public enum CommandType
    {
        // TODO: use/drop item, toggle favourite, set/remove marker, fast travel, ...?
        ToggleQuest = 5,
        ToggleRadio = 12,
        RequestLocalMap = 13,
    }

    [DataContract]
    public class Command
    {
        [DataMember(Name = "type")]
        public CommandType CommandType { get; private set; }

        [DataMember(Name = "args")]
        public object[] Arguments { get; set; }

        [DataMember(Name = "id")]
        private uint _sequenceId;

        public Command(CommandType commandType, params object[] arguments)
        {
            CommandType = commandType;
            Arguments = arguments;
        }

        public string Format(uint sequenceId)
        {
            _sequenceId = sequenceId;

            var serializer = new DataContractJsonSerializer(GetType(), new[] { typeof(int[]), typeof(uint[]), typeof(string[]) });
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                return Encoding.ASCII.GetString(ms.ToArray());
            }
        }
    }
}