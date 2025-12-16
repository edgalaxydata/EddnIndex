using System.Buffers;

namespace EddnIndexUpdate
{
    public class EventReader(Stream stream) : IDisposable
    {
        private Stream? InnerStream = stream;
        private readonly List<byte[]> Buffers = [];
        private readonly List<BufferSegment> Segments = [];

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

            if (Segments.Count != Buffers.Count)
            {
                throw new InvalidOperationException();
            }

            var index = (SegmentNumber: -1, Offset: -1);

            if (Buffers.Count != 0)
            {
                var readpos = BufferReadOffset;

                for (int i = BufferReadSegmentNumber; i < Segments.Count; i++)
                {
                    var pos = Segments[i].Memory.Span[readpos..].IndexOf((byte)'\n');

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
                BufferSegment? lastSegment = Segments.Count == 0 ? null : Segments[^1];
                BufferSegment? prevSegment = Segments.Count <= 1 ? null : Segments[^2];
                byte[]? lastBuffer = Buffers.Count == 0 ? null : Buffers[^1];
                int bufferWritePos = 0;

                if (BufferReadSegmentNumber != 0)
                {
                    for (int i = 0; i < BufferReadSegmentNumber; i++)
                    {
                        ArrayPool<byte>.Shared.Return(Buffers[i]);
                    }

                    Segments.RemoveRange(0, BufferReadSegmentNumber);
                    Buffers.RemoveRange(0, BufferReadSegmentNumber);
                    BufferReadSegmentNumber = 0;

                    if (Segments.Count != 0)
                    {
                        prevSegment = null;
                        lastSegment = new BufferSegment(Segments[0].Memory);

                        for (int i = 1; i < Segments.Count; i++)
                        {
                            prevSegment = lastSegment;
                            Segments[i] = lastSegment = lastSegment.Append(Segments[i].Memory);
                            lastBuffer = Buffers[i];
                            bufferWritePos = lastSegment.Memory.Length;
                        }
                    }
                }

                if (lastBuffer == null || bufferWritePos == lastBuffer.Length)
                {
                    if (lastSegment != null)
                    {
                        prevSegment = lastSegment;
                        lastSegment = prevSegment.Append(ReadOnlyMemory<byte>.Empty);
                    }
                    else
                    {
                        lastSegment = new BufferSegment(ReadOnlyMemory<byte>.Empty);
                    }

                    lastBuffer = ArrayPool<byte>.Shared.Rent(65536);
                    Buffers.Add(lastBuffer);
                    Segments.Add(lastSegment);
                    bufferWritePos = 0;
                }

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

                bufferWritePos += len;

                if (prevSegment != null)
                {
                    Segments[^1] = prevSegment.Append(lastBuffer.AsMemory(0, bufferWritePos));
                }
                else
                {
                    Segments[^1] = new BufferSegment(lastBuffer.AsMemory(0, bufferWritePos));
                }

                var readpos = BufferReadOffset;

                for (int i = BufferReadSegmentNumber; i < Segments.Count; i++)
                {
                    var pos = Segments[i].Memory.Span[readpos..].IndexOf((byte)'\n');

                    if (pos >= 0)
                    {
                        index = (i, readpos + pos);
                        break;
                    }

                    readpos = 0;
                }
            }

            line = new ReadOnlySequence<byte>(Segments[BufferReadSegmentNumber], BufferReadOffset, Segments[index.SegmentNumber], index.Offset + 1);

            if (index.Offset + 1 == Segments[index.SegmentNumber].Memory.Length)
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
                ArrayPool<byte>.Shared.Return(buffer);
            }

            Buffers.Clear();
            Segments.Clear();

            InnerStream?.Dispose();
            InnerStream = null;
            GC.SuppressFinalize(this);
        }
    }
}
