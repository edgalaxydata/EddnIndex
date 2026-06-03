using System.Buffers;

namespace EddnIndexUpdate;

public class EventReader(Stream stream) : IDisposable
{
    private Stream? InnerStream = stream;
    private readonly List<(byte[] Buffer, Memory<byte> Memory)> Buffers = [];

    private int BufferReadOffset;
    private int BufferReadSegmentNumber;
    public long Position { get; private set; } = 0;

    private class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public BufferSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new BufferSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;
            return segment;
        }
    }

    public bool TryReadLine(out ReadOnlySequence<byte> line)
    {
        ObjectDisposedException.ThrowIf(InnerStream == null, this);

        var index = (SegmentNumber: -1, Offset: -1);

        if (Buffers.Count != 0)
        {
            var readpos = BufferReadOffset;

            for (int i = BufferReadSegmentNumber; i < Buffers.Count; i++)
            {
                var pos = Buffers[i].Memory.Span[readpos..].IndexOf((byte)'\n');

                if (pos >= 0)
                {
                    index = (i, readpos + pos);
                    break;
                }

                readpos = 0;
            }
        }

        while (index == (-1, -1))
        {
            if (BufferReadSegmentNumber != 0)
            {
                for (int i = 0; i < BufferReadSegmentNumber; i++)
                {
                    ArrayPool<byte>.Shared.Return(Buffers[i].Buffer);
                }

                Buffers.RemoveRange(0, BufferReadSegmentNumber);
                BufferReadSegmentNumber = 0;
            }

            if (Buffers.Count == 0 || Buffers[^1].Memory.Length == Buffers[^1].Buffer.Length)
            {
                Buffers.Add((ArrayPool<byte>.Shared.Rent(65536), Memory<byte>.Empty));
            }

            var lastBuffer = Buffers[^1].Buffer;
            var bufferWritePos = Buffers[^1].Memory.Length;

            int len;

            try
            {
                len = InnerStream.Read(lastBuffer, bufferWritePos, lastBuffer.Length);
            }
            catch (IOException)
            {
                line = ReadOnlySequence<byte>.Empty;
                return false;
            }

            if (len <= 0)
            {
                line = ReadOnlySequence<byte>.Empty;
                return false;
            }

            Buffers[^1] = (lastBuffer, lastBuffer.AsMemory(0, bufferWritePos + len));

            var readpos = Buffers.Count == BufferReadSegmentNumber + 1 ? BufferReadOffset : 0;

            var pos = Buffers[^1].Memory.Span[readpos..].IndexOf((byte)'\n');

            if (pos >= 0)
            {
                index = (Buffers.Count - 1, readpos + pos);
                break;
            }
        }

        var firstSegment = new BufferSegment(Buffers[BufferReadSegmentNumber].Memory);
        var lastSegment = firstSegment;
        var segmentNumber = BufferReadSegmentNumber;

        while (segmentNumber < index.SegmentNumber)
        {
            lastSegment = lastSegment.Append(Buffers[++segmentNumber].Memory);
        }

        line = new ReadOnlySequence<byte>(firstSegment, BufferReadOffset, lastSegment, index.Offset + 1);

        if (index.Offset + 1 == Buffers[index.SegmentNumber].Memory.Length)
        {
            BufferReadSegmentNumber = index.SegmentNumber + 1;
            BufferReadOffset = 0;
        }
        else
        {
            BufferReadSegmentNumber = index.SegmentNumber;
            BufferReadOffset = index.Offset + 1;
        }

        Position += line.Length;
        return true;
    }

    public void Dispose()
    {
        foreach (var buffer in Buffers)
        {
            ArrayPool<byte>.Shared.Return(buffer.Buffer);
        }

        Buffers.Clear();

        InnerStream?.Dispose();
        InnerStream = null;
        GC.SuppressFinalize(this);
    }
}
