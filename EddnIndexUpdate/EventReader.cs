using System.Buffers;

namespace EddnIndexUpdate;

public class EventReader(Stream stream) : IDisposable
{
    private Stream? _innerStream = stream;
    private readonly List<(byte[] Buffer, Memory<byte> Memory)> _buffers = [];

    private int _bufferReadOffset;
    private int _bufferReadSegmentNumber;
    public long Position { get; private set; } = 0;

    private class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
            => Memory = memory;

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
        ObjectDisposedException.ThrowIf(_innerStream == null, this);

        var index = (SegmentNumber: -1, Offset: -1);

        if (_buffers.Count != 0)
        {
            int readpos = _bufferReadOffset;

            for (int i = _bufferReadSegmentNumber; i < _buffers.Count; i++)
            {
                int pos = _buffers[i].Memory.Span[readpos..].IndexOf((byte)'\n');

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
            if (_bufferReadSegmentNumber != 0)
            {
                for (int i = 0; i < _bufferReadSegmentNumber; i++)
                {
                    ArrayPool<byte>.Shared.Return(_buffers[i].Buffer);
                }

                _buffers.RemoveRange(0, _bufferReadSegmentNumber);
                _bufferReadSegmentNumber = 0;
            }

            if (_buffers.Count == 0 || _buffers[^1].Memory.Length == _buffers[^1].Buffer.Length)
            {
                _buffers.Add((ArrayPool<byte>.Shared.Rent(65536), Memory<byte>.Empty));
            }

            byte[] lastBuffer = _buffers[^1].Buffer;
            int bufferWritePos = _buffers[^1].Memory.Length;

            int len;

            try
            {
                len = _innerStream.Read(lastBuffer, bufferWritePos, lastBuffer.Length);
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

            _buffers[^1] = (lastBuffer, lastBuffer.AsMemory(0, bufferWritePos + len));

            int readpos = _buffers.Count == _bufferReadSegmentNumber + 1 ? _bufferReadOffset : 0;

            int pos = _buffers[^1].Memory.Span[readpos..].IndexOf((byte)'\n');

            if (pos >= 0)
            {
                index = (_buffers.Count - 1, readpos + pos);
                break;
            }
        }

        var firstSegment = new BufferSegment(_buffers[_bufferReadSegmentNumber].Memory);
        var lastSegment = firstSegment;
        int segmentNumber = _bufferReadSegmentNumber;

        while (segmentNumber < index.SegmentNumber)
        {
            lastSegment = lastSegment.Append(_buffers[++segmentNumber].Memory);
        }

        line = new ReadOnlySequence<byte>(firstSegment, _bufferReadOffset, lastSegment, index.Offset + 1);

        if (index.Offset + 1 == _buffers[index.SegmentNumber].Memory.Length)
        {
            _bufferReadSegmentNumber = index.SegmentNumber + 1;
            _bufferReadOffset = 0;
        }
        else
        {
            _bufferReadSegmentNumber = index.SegmentNumber;
            _bufferReadOffset = index.Offset + 1;
        }

        Position += line.Length;
        return true;
    }

    public void Dispose()
    {
        foreach (var buffer in _buffers)
        {
            ArrayPool<byte>.Shared.Return(buffer.Buffer);
        }

        _buffers.Clear();

        _innerStream?.Dispose();
        _innerStream = null;
        GC.SuppressFinalize(this);
    }
}
