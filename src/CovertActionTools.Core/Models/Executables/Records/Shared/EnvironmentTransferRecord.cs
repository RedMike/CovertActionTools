namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    /// <summary>
    /// Used to transfer state data between EXEs during chaining, always 00
    /// Not useful to show or allow editing
    /// </summary>
    public class EnvironmentTransferRecord : IExecutableRecord
    {
        private readonly int _size;

        public EnvironmentTransferRecord(int size)
        {
            _size = size;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            for (var i = startingOffset; i < startingOffset + _size; i++)
            {
                if (fullPayload[i] != 0)
                {
                    throw new System.Exception($"Expected all bytes to be 0, but found {fullPayload[i]} at offset {i}");
                }
            }

            return _size;
        }

        public byte[] WriteBytes()
        {
            return new byte[_size];
        }

        public EnvironmentTransferRecord Clone()
        {
            return new EnvironmentTransferRecord(_size);
        }
    }
}