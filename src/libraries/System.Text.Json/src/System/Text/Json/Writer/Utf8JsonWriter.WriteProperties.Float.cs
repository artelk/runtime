﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the pre-encoded property name and <see cref="float"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The JSON-encoded name of the property to write..</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="float"/> using the default <see cref="StandardFormat"/> (that is, 'G').
        /// </remarks>
        public void WriteNumber(JsonEncodedText propertyName, float value)
            => WriteNumberHelper(propertyName.EncodedUtf8Bytes, value);

        private void WriteNumberHelper(ReadOnlySpan<byte> utf8PropertyName, float value)
        {
            Debug.Assert(utf8PropertyName.Length <= JsonConstants.MaxUnescapedTokenSize);

            JsonWriterHelper.ValidateSingle(value);

            WriteNumberByOptions(utf8PropertyName, value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        /// <summary>
        /// Writes the property name and <see cref="float"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write..</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="propertyName"/> parameter is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="float"/> using the default <see cref="StandardFormat"/> (that is, 'G').
        /// The property name is escaped before writing.
        /// </remarks>
        public void WriteNumber(string propertyName, float value)
            => WriteNumber((propertyName ?? throw new ArgumentNullException(nameof(propertyName))).AsSpan(), value);

        /// <summary>
        /// Writes the property name and <see cref="float"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="propertyName">The name of the property to write..</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="float"/> using the default <see cref="StandardFormat"/> (that is, 'G').
        /// The property name is escaped before writing.
        /// </remarks>
        public void WriteNumber(ReadOnlySpan<char> propertyName, float value)
        {
            JsonWriterHelper.ValidateProperty(propertyName);
            JsonWriterHelper.ValidateSingle(value);

            WriteNumberEscape(propertyName, value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        /// <summary>
        /// Writes the property name and <see cref="float"/> value (as a JSON number) as part of a name/value pair of a JSON object.
        /// </summary>
        /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to write</param>
        /// <param name="value">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified property name is too large.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="float"/> using the default <see cref="StandardFormat"/> (that is, 'G').
        /// The property name is escaped before writing.
        /// </remarks>
        public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, float value)
        {
            JsonWriterHelper.ValidateProperty(utf8PropertyName);
            JsonWriterHelper.ValidateSingle(value);

            WriteNumberEscape(utf8PropertyName, value);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        private void WriteNumberEscape(ReadOnlySpan<char> propertyName, float value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(propertyName, _options.Encoder);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length);

            if (propertyIdx != -1)
            {
                WriteNumberEscapeProperty(propertyName, value, propertyIdx);
            }
            else
            {
                WriteNumberByOptions(propertyName, value);
            }
        }

        private void WriteNumberEscape(ReadOnlySpan<byte> utf8PropertyName, float value)
        {
            int propertyIdx = JsonWriterHelper.NeedsEscaping(utf8PropertyName, _options.Encoder);

            Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length);

            if (propertyIdx != -1)
            {
                WriteNumberEscapeProperty(utf8PropertyName, value, propertyIdx);
            }
            else
            {
                WriteNumberByOptions(utf8PropertyName, value);
            }
        }

        private void WriteNumberEscapeProperty(ReadOnlySpan<char> propertyName, float value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < propertyName.Length);

            char[]? propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

            Span<char> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc char[length] :
                (propertyArray = ArrayPool<char>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);

            WriteNumberByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<char>.Shared.Return(propertyArray);
            }
        }

        private void WriteNumberEscapeProperty(ReadOnlySpan<byte> utf8PropertyName, float value, int firstEscapeIndexProp)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);
            Debug.Assert(firstEscapeIndexProp >= 0 && firstEscapeIndexProp < utf8PropertyName.Length);

            byte[]? propertyArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

            Span<byte> escapedPropertyName = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (propertyArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out int written);

            WriteNumberByOptions(escapedPropertyName.Slice(0, written), value);

            if (propertyArray != null)
            {
                ArrayPool<byte>.Shared.Return(propertyArray);
            }
        }

        private void WriteNumberByOptions(ReadOnlySpan<char> propertyName, float value)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteNumberIndented(propertyName, value);
            }
            else
            {
                WriteNumberMinimized(propertyName, value);
            }
        }

        private void WriteNumberByOptions(ReadOnlySpan<byte> utf8PropertyName, float value)
        {
            ValidateWritingProperty();
            if (_options.Indented)
            {
                WriteNumberIndented(utf8PropertyName, value);
            }
            else
            {
                WriteNumberMinimized(utf8PropertyName, value);
            }
        }

        private void WriteNumberMinimized(ReadOnlySpan<char> escapedPropertyName, float value)
        {
            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - JsonConstants.MaximumFormatSingleLength - 4);

            // All ASCII, 2 quotes for property name, and 1 colon => escapedPropertyName.Length + JsonConstants.MaximumFormatSingleLength + 3
            // Optionally, 1 list separator, and up to 3x growth when transcoding
            int maxRequired = (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + JsonConstants.MaximumFormatSingleLength + 4;

            Span<byte> output = _output.GetSpan(maxRequired);
            int bytesPending = 0;

            if (_currentDepth < 0)
            {
                output[bytesPending++] = JsonConstants.ListSeparator;
            }
            output[bytesPending++] = JsonConstants.Quote;

            bytesPending += TranscodeAndWrite(escapedPropertyName, output.Slice(bytesPending));

            output[bytesPending++] = JsonConstants.Quote;
            output[bytesPending++] = JsonConstants.KeyValueSeperator;

            bool result = TryFormatSingle(value, output.Slice(bytesPending), out int bytesWritten);
            Debug.Assert(result);
            bytesPending += bytesWritten;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }

        private void WriteNumberMinimized(ReadOnlySpan<byte> escapedPropertyName, float value)
        {
            Debug.Assert(escapedPropertyName.Length < int.MaxValue - JsonConstants.MaximumFormatSingleLength - 4);

            int minRequired = escapedPropertyName.Length + JsonConstants.MaximumFormatSingleLength + 3; // 2 quotes for property name, and 1 colon
            int maxRequired = minRequired + 1; // Optionally, 1 list separator

            Span<byte> output = _output.GetSpan(maxRequired);
            int bytesPending = 0;

            if (_currentDepth < 0)
            {
                output[bytesPending++] = JsonConstants.ListSeparator;
            }
            output[bytesPending++] = JsonConstants.Quote;

            escapedPropertyName.CopyTo(output.Slice(bytesPending));
            bytesPending += escapedPropertyName.Length;

            output[bytesPending++] = JsonConstants.Quote;
            output[bytesPending++] = JsonConstants.KeyValueSeperator;

            bool result = TryFormatSingle(value, output.Slice(bytesPending), out int bytesWritten);
            Debug.Assert(result);
            bytesPending += bytesWritten;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }

        private void WriteNumberIndented(ReadOnlySpan<char> escapedPropertyName, float value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedPropertyName.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - indent - JsonConstants.MaximumFormatSingleLength - 5 - s_newLineLength);

            // All ASCII, 2 quotes for property name, 1 colon, and 1 space => escapedPropertyName.Length + JsonConstants.MaximumFormatSingleLength + 4
            // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
            int maxRequired = indent + (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + JsonConstants.MaximumFormatSingleLength + 5 + s_newLineLength;

            Span<byte> output = _output.GetSpan(maxRequired);
            int bytesPending = 0;

            if (_currentDepth < 0)
            {
                output[bytesPending++] = JsonConstants.ListSeparator;
            }

            Debug.Assert(_options.SkipValidation || _tokenType != JsonTokenType.PropertyName);

            if (_tokenType != JsonTokenType.None)
            {
                bytesPending += WriteNewLine(output.Slice(bytesPending));
            }

            JsonWriterHelper.WriteIndentation(output.Slice(bytesPending), indent);
            bytesPending += indent;

            output[bytesPending++] = JsonConstants.Quote;

            bytesPending += TranscodeAndWrite(escapedPropertyName, output.Slice(bytesPending));

            output[bytesPending++] = JsonConstants.Quote;
            output[bytesPending++] = JsonConstants.KeyValueSeperator;
            output[bytesPending++] = JsonConstants.Space;

            bool result = TryFormatSingle(value, output.Slice(bytesPending), out int bytesWritten);
            Debug.Assert(result);
            bytesPending += bytesWritten;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }

        private void WriteNumberIndented(ReadOnlySpan<byte> escapedPropertyName, float value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(escapedPropertyName.Length < int.MaxValue - indent - JsonConstants.MaximumFormatSingleLength - 5 - s_newLineLength);

            int minRequired = indent + escapedPropertyName.Length + JsonConstants.MaximumFormatSingleLength + 4; // 2 quotes for property name, 1 colon, and 1 space
            int maxRequired = minRequired + 1 + s_newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line

            Span<byte> output = _output.GetSpan(maxRequired);
            int bytesPending = 0;

            if (_currentDepth < 0)
            {
                output[bytesPending++] = JsonConstants.ListSeparator;
            }

            Debug.Assert(_options.SkipValidation || _tokenType != JsonTokenType.PropertyName);

            if (_tokenType != JsonTokenType.None)
            {
                bytesPending += WriteNewLine(output.Slice(bytesPending));
            }

            JsonWriterHelper.WriteIndentation(output.Slice(bytesPending), indent);
            bytesPending += indent;

            output[bytesPending++] = JsonConstants.Quote;

            escapedPropertyName.CopyTo(output.Slice(bytesPending));
            bytesPending += escapedPropertyName.Length;

            output[bytesPending++] = JsonConstants.Quote;
            output[bytesPending++] = JsonConstants.KeyValueSeperator;
            output[bytesPending++] = JsonConstants.Space;

            bool result = TryFormatSingle(value, output.Slice(bytesPending), out int bytesWritten);
            Debug.Assert(result);
            bytesPending += bytesWritten;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }
    }
}
