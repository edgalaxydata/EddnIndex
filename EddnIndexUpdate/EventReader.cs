using System.Buffers;

namespace EddnIndexUpdate
{
    public class EventReader(Stream stream) : IDisposable
    {
        private Stream? InnerStream = stream;
        private readonly byte[] Buffer = ArrayPool<byte>.Shared.Rent(1048576);
        private int BufferOffset;
        private int BufferLength;
        public long Position { get; private set; } = 0;

        public bool TryReadLine(out ReadOnlySpan<byte> line)
        {
            ObjectDisposedException.ThrowIf(InnerStream == null, this);

            var buf = Buffer.AsSpan(BufferOffset, BufferLength - BufferOffset);
            var index = buf.IndexOf((byte)'\n');

            if (index < 0)
            {
                buf.CopyTo(Buffer);
                BufferLength -= BufferOffset;
                BufferOffset = 0;
                var fill = Buffer.AsSpan(BufferLength);
                fill.Clear();
                int len;

                byte[]? readBuffer = ArrayPool<byte>.Shared.Rent(fill.Length);

                try
                {
                    len = InnerStream.Read(readBuffer, 0, fill.Length);
                    new ReadOnlySpan<byte>(readBuffer, 0, fill.Length).CopyTo(fill);
                }
                catch (IOException ex)
                {
                    line = [];
                    return false;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(readBuffer);
                }

                if (len <= 0)
                {
                    line = [];
                    return false;
                }

                BufferLength += len;

                buf = Buffer.AsSpan(BufferOffset, BufferLength - BufferOffset);
                index = buf.IndexOf((byte)'\n');

                if (index < 0)
                {
                    throw new InvalidOperationException("Line too long");
                }
            }

            line = buf[..index];
            BufferOffset += index + 1;
            Position += index + 1;
            return true;
        }

        public void Dispose()
        {
            InnerStream?.Dispose();
            InnerStream = null;
            GC.SuppressFinalize(this);
        }
    }
}
