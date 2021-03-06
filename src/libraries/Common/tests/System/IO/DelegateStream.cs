// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>Provides a stream whose implementation is supplied by delegates.</summary>
    internal sealed class DelegateStream : Stream
    {
        private readonly Func<bool> _canReadFunc;
        private readonly Func<bool> _canSeekFunc;
        private readonly Func<bool> _canWriteFunc;
        private readonly Action _flushFunc = null;
        private readonly Func<CancellationToken, Task> _flushAsyncFunc = null;
        private readonly Func<long> _lengthFunc;
        private readonly Func<long> _positionGetFunc;
        private readonly Action<long> _positionSetFunc;
        private readonly Func<byte[], int, int, int> _readFunc;
        private readonly Func<byte[], int, int, CancellationToken, Task<int>> _readAsyncFunc;
        private readonly Func<long, SeekOrigin, long> _seekFunc;
        private readonly Action<long> _setLengthFunc;
        private readonly Action<byte[], int, int> _writeFunc;
        private readonly Func<byte[], int, int, CancellationToken, Task> _writeAsyncFunc;
        private readonly Action<bool> _disposeFunc;

        public DelegateStream(
            Func<bool> canReadFunc = null,
            Func<bool> canSeekFunc = null,
            Func<bool> canWriteFunc = null,
            Action flushFunc = null,
            Func<CancellationToken, Task> flushAsyncFunc = null,
            Func<long> lengthFunc = null,
            Func<long> positionGetFunc = null,
            Action<long> positionSetFunc = null,
            Func<byte[], int, int, int> readFunc = null,
            Func<byte[], int, int, CancellationToken, Task<int>> readAsyncFunc = null,
            Func<long, SeekOrigin, long> seekFunc = null,
            Action<long> setLengthFunc = null,
            Action<byte[], int, int> writeFunc = null,
            Func<byte[], int, int, CancellationToken, Task> writeAsyncFunc = null,
            Action<bool> disposeFunc = null)
        {
            _canReadFunc = canReadFunc ?? (() => false);
            _canSeekFunc = canSeekFunc ?? (() => false);
            _canWriteFunc = canWriteFunc ?? (() => false);

            _flushFunc = flushFunc ?? (() => { });
            _flushAsyncFunc = flushAsyncFunc ?? (token => base.FlushAsync(token));

            _lengthFunc = lengthFunc ?? (() => { throw new NotSupportedException(); });
            _positionSetFunc = positionSetFunc ?? (_ => { throw new NotSupportedException(); });
            _positionGetFunc = positionGetFunc ?? (() => { throw new NotSupportedException(); });

            _readAsyncFunc = readAsyncFunc ?? ((buffer, offset, count, token) => base.ReadAsync(buffer, offset, count, token));
            _readFunc = readFunc ?? ((buffer, offset, count) => readAsyncFunc(buffer, offset, count, default).GetAwaiter().GetResult());

            _seekFunc = seekFunc ?? ((_, __) => { throw new NotSupportedException(); });
            _setLengthFunc = setLengthFunc ?? (_ => { throw new NotSupportedException(); });

            _writeAsyncFunc = writeAsyncFunc ?? ((buffer, offset, count, token) => base.WriteAsync(buffer, offset, count, token));
            _writeFunc = writeFunc ?? ((buffer, offset, count) => writeAsyncFunc(buffer, offset, count, default).GetAwaiter().GetResult());

            _disposeFunc = disposeFunc;
        }

        public override bool CanRead { get { return _canReadFunc(); } }
        public override bool CanSeek { get { return _canSeekFunc(); } }
        public override bool CanWrite { get { return _canWriteFunc(); } }

        public override void Flush() { _flushFunc(); }
        public override Task FlushAsync(CancellationToken cancellationToken) { return _flushAsyncFunc(cancellationToken); }

        public override long Length { get { return _lengthFunc(); } }
        public override long Position { get { return _positionGetFunc(); } set { _positionSetFunc(value); } }

        public override int Read(byte[] buffer, int offset, int count) { return _readFunc(buffer, offset, count); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { return _readAsyncFunc(buffer, offset, count, cancellationToken); }

        public override long Seek(long offset, SeekOrigin origin) { return _seekFunc(offset, origin); }
        public override void SetLength(long value) { _setLengthFunc(value); }

        public override void Write(byte[] buffer, int offset, int count) { _writeFunc(buffer, offset, count); }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { return _writeAsyncFunc(buffer, offset, count, cancellationToken); }

        protected override void Dispose(bool disposing) { _disposeFunc?.Invoke(disposing); }
    }
}
