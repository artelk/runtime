using System.Buffers;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        private readonly struct BufferWriter : IEquatable<BufferWriter>
        {
            private delegate Span<byte> GetSpanDelegate(int sizeHint);
            private delegate void AdvanceDelegate(int count);

            private readonly GetSpanDelegate _getSpan;
            private readonly AdvanceDelegate _advance;

            public BufferWriter(IBufferWriter<byte> bufferWriter)
            {
                Type type = bufferWriter?.GetType() ?? throw new ArgumentNullException(nameof(bufferWriter));
                BufferWriterMethods.Methods methods = BufferWriterMethods.Get(type);
                _getSpan = (GetSpanDelegate)Delegate.CreateDelegate(typeof(GetSpanDelegate), bufferWriter, methods.GetSpan);
                _advance = (AdvanceDelegate)Delegate.CreateDelegate(typeof(AdvanceDelegate), bufferWriter, methods.Advance);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<byte> GetSpan(int sizeHint)
            {
                Span<byte> span = _getSpan(sizeHint);
                if (span.Length < sizeHint)
                {
                    ThrowHelper.ThrowInvalidOperationException_NeedLargerSpan();
                }
                return span;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Advance(int count) => _advance(count);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(BufferWriter other) => _getSpan == other._getSpan && _advance == other._advance;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object? obj) => obj is BufferWriter writer && Equals(writer);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() => _getSpan.GetHashCode() ^ _advance.GetHashCode();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(BufferWriter left, BufferWriter right) => left.Equals(right);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(BufferWriter left, BufferWriter right) => !(left == right);
        }

        private static class BufferWriterMethods
        {
            private static readonly Type s_bufferWriterIntefaceType = typeof(IBufferWriter<byte>);
            private static readonly MethodInfo s_bufferWriterIntefaceGetSpanMethodInfo = s_bufferWriterIntefaceType.GetMethod("GetSpan")!;
            private static readonly MethodInfo s_bufferWriterIntefaceAdvanceMethodInfo = s_bufferWriterIntefaceType.GetMethod("Advance")!;
            private static readonly ConcurrentDictionary<Type, Lazy<Methods>> s_methods = new ConcurrentDictionary<Type, Lazy<Methods>>();
            private static readonly Methods s_arrayBufferWriter = Create(typeof(ArrayBufferWriter<byte>));
            private static Methods? s_lastUsed;

            public static Methods Get(Type type)
            {
                if (type == typeof(ArrayBufferWriter<byte>))
                {
                    return s_arrayBufferWriter;
                }

                Methods? lastUsed = s_lastUsed;
                if (lastUsed != null && lastUsed.Type == type)
                {
                    return lastUsed;
                }

                if (s_methods.TryGetValue(type, out Lazy<Methods>? lazy))
                {
                    lastUsed = lazy.Value;
                    s_lastUsed = lastUsed;
                    return lastUsed;
                }

                lazy = s_methods.GetOrAdd(type, t => new Lazy<Methods>(() => Create(t)));
                lastUsed = lazy.Value;
                s_lastUsed = lastUsed;
                return lastUsed;
            }

            public class Methods
            {
                public Methods(Type type, MethodInfo getSpan, MethodInfo advance)
                {
                    Type = type;
                    GetSpan = getSpan;
                    Advance = advance;
                }

                public Type Type { get; }
                public MethodInfo GetSpan { get; }
                public MethodInfo Advance { get; }
            }

            private static Methods Create(Type type)
            {
                InterfaceMapping map = type.GetInterfaceMap(s_bufferWriterIntefaceType);

                int index = Array.IndexOf(map.InterfaceMethods, s_bufferWriterIntefaceGetSpanMethodInfo);
                MethodInfo getSpanMethod = map.TargetMethods[index];
                RuntimeHelpers.PrepareMethod(getSpanMethod.MethodHandle);

                index = Array.IndexOf(map.InterfaceMethods, s_bufferWriterIntefaceAdvanceMethodInfo);
                MethodInfo advanceMethod = map.TargetMethods[index];
                RuntimeHelpers.PrepareMethod(advanceMethod.MethodHandle);

                return new Methods(type, getSpanMethod, advanceMethod);
            }
        }
    }
}
