namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    /// <summary>
    /// Used to transfer state data between EXEs during chaining, always 00
    /// Not useful to show or allow editing
    /// </summary>
    public class EnvironmentTransferRecord : IExecutableRecord
    {
        public int Size { get; }

        public EnvironmentTransferRecord(int size)
        {
            Size = size;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            for (var i = startingOffset; i < startingOffset + Size; i++)
            {
                if (fullPayload[i] != 0)
                {
                    throw new System.Exception($"Expected all bytes to be 0, but found {fullPayload[i]} at offset {i}");
                }
            }

            return Size;
        }

        public byte[] WriteBytes()
        {
            return new byte[Size];
        }

        public EnvironmentTransferRecord Clone()
        {
            return new EnvironmentTransferRecord(Size);
        }
    }
}