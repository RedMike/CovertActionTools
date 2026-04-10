using System.Collections.Generic;
using CovertActionTools.Core.Models.Executables.Records.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class RenderingSection : IExecutableSection
    {
        public BlobRecord Padding1 { get; set; } = new(2); //TODO: figure out if the value matters
        public RastPortRecord Record1 { get; set; } = new();
        public PointerRecord Pointer1 { get; set; } = new();
        public RastPortRecord Record2 { get; set; } = new();
        public PointerRecord Pointer2 { get; set; } = new();
        public RastPortRecord Record3 { get; set; } = new();
        public PointerRecord Pointer3 { get; set; } = new();
        public EnvironmentTransferRecord EnvironmentTransfer { get; set; } = new(5056);
        public BlobRecord Padding2 { get; set; } = new(2); //TODO: figure out if the value matters
        public RastPortRecord Record4 { get; set; } = new();
        public PointerRecord Pointer4 { get; set; } = new();
        public RastPortRecord Record5 { get; set; } = new();
        public PointerRecord Pointer5 { get; set; } = new();
        
        public bool Viewable()
        {
            return true;
        }

        public bool Editable()
        {
            return true;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var offset = startingOffset;
            offset += Padding1.ReadBytes(fullPayload, offset);
            offset += Record1.ReadBytes(fullPayload, offset);
            offset += Pointer1.ReadBytes(fullPayload, offset);
            offset += Record2.ReadBytes(fullPayload, offset);
            offset += Pointer2.ReadBytes(fullPayload, offset);
            offset += Record3.ReadBytes(fullPayload, offset);
            offset += Pointer3.ReadBytes(fullPayload, offset);
            offset += EnvironmentTransfer.ReadBytes(fullPayload, offset);
            offset += Padding2.ReadBytes(fullPayload, offset);
            offset += Record4.ReadBytes(fullPayload, offset);
            offset += Pointer4.ReadBytes(fullPayload, offset);
            offset += Record5.ReadBytes(fullPayload, offset);
            offset += Pointer5.ReadBytes(fullPayload, offset);
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var result = new List<byte>();
            result.AddRange(Padding1.WriteBytes());
            result.AddRange(Record1.WriteBytes());
            result.AddRange(Pointer1.WriteBytes());
            result.AddRange(Record2.WriteBytes());
            result.AddRange(Pointer2.WriteBytes());
            result.AddRange(Record3.WriteBytes());
            result.AddRange(Pointer3.WriteBytes());
            result.AddRange(EnvironmentTransfer.WriteBytes());
            result.AddRange(Padding2.WriteBytes());
            result.AddRange(Record4.WriteBytes());
            result.AddRange(Pointer4.WriteBytes());
            result.AddRange(Record5.WriteBytes());
            result.AddRange(Pointer5.WriteBytes());
            return result.ToArray();
        }

        public RenderingSection Clone()
        {
            return new RenderingSection
            {
                Padding1 = Padding1.Clone(),
                Record1 = Record1.Clone(),
                Pointer1 = Pointer1.Clone(),
                Record2 = Record2.Clone(),
                Pointer2 = Pointer2.Clone(),
                Record3 = Record3.Clone(),
                Pointer3 = Pointer3.Clone(),
                EnvironmentTransfer = EnvironmentTransfer.Clone(),
                Padding2 = Padding2.Clone(),
                Record4 = Record4.Clone(),
                Pointer4 = Pointer4.Clone(),
                Record5 = Record5.Clone(),
                Pointer5 = Pointer5.Clone()
            };
        }
    }
}