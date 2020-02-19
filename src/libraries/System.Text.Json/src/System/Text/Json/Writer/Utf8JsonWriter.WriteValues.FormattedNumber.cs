// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the value (as a JSON number) as an element of a JSON array.
        /// </summary>
        /// <param name="utf8FormattedNumber">The value to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="utf8FormattedNumber"/> does not represent a valid JSON number.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="int"/> using the default <see cref="StandardFormat"/> (that is, 'G'), for example: 32767.
        /// </remarks>
        internal void WriteNumberValue(ReadOnlySpan<byte> utf8FormattedNumber)
        {
            JsonWriterHelper.ValidateValue(utf8FormattedNumber);
            JsonWriterHelper.ValidateNumber(utf8FormattedNumber);
            ValidateWritingValue();

            if (_options.Indented)
            {
                WriteNumberValueIndented(utf8FormattedNumber);
            }
            else
            {
                WriteNumberValueMinimized(utf8FormattedNumber);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        private void WriteNumberValueMinimized(ReadOnlySpan<byte> utf8Value)
        {
            int maxRequired = utf8Value.Length + 1; // Optionally, 1 list separator

            Span<byte> output = _output.GetSpan(maxRequired);
            int bytesPending = 0;

            if (_currentDepth < 0)
            {
                output[bytesPending++] = JsonConstants.ListSeparator;
            }

            utf8Value.CopyTo(output.Slice(bytesPending));
            bytesPending += utf8Value.Length;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }

        private void WriteNumberValueIndented(ReadOnlySpan<byte> utf8Value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            Debug.Assert(utf8Value.Length < int.MaxValue - indent - 1 - s_newLineLength);

            int maxRequired = indent + utf8Value.Length + 1 + s_newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line

            Span<byte> output = _output.GetSpan(maxRequired);
            int bytesPending = 0;

            if (_currentDepth < 0)
            {
                output[bytesPending++] = JsonConstants.ListSeparator;
            }

            if (_tokenType != JsonTokenType.PropertyName)
            {
                if (_tokenType != JsonTokenType.None)
                {
                    bytesPending += WriteNewLine(output.Slice(bytesPending));
                }
                JsonWriterHelper.WriteIndentation(output.Slice(bytesPending), indent);
                bytesPending += indent;
            }

            utf8Value.CopyTo(output.Slice(bytesPending));
            bytesPending += utf8Value.Length;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }
    }
}
