using System.IO;
using System.Text;

namespace PipBoy
{
    public class CommandSender
    {
        private readonly Stream _stream;

        private uint _sequenceId;

        public CommandSender(Stream stream)
        {
            _stream = stream;
        }

        public void Send(Command command)
        {
            var sequenceId = _sequenceId++;
            var commandString = command.Format(sequenceId);
            var commandData = Encoding.ASCII.GetBytes(commandString);

            using (var ms = new MemoryStream())
            {
                var bw = new BinaryWriter(ms);
                bw.Write(commandData.Length);
                bw.Write((byte)0x05);
                bw.Write(commandData);

                var buffer = ms.ToArray();
                _stream.Write(buffer, 0, buffer.Length);
            }
        }
    }
}