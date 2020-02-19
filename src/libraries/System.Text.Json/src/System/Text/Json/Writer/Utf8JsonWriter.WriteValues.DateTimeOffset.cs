// Licensed to the .NET Foundation under one or more agreements.
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
        /// Writes the <see cref="DateTimeOffset"/> value (as a JSON string) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="DateTimeOffset"/> using the round-trippable ('O') <see cref="StandardFormat"/> , for example: 2017-06-12T05:30:45.7680000-07:00.
        /// </remarks>
        public void WriteStringValue(DateTimeOffset value)
        {
            ValidateWritingValue();
            if (_options.Indented)
            {
                WriteStringValueIndented(value);
            }
            else
            {
                WriteStringValueMinimized(value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.String;
        }

        private void WriteStringValueMinimized(DateTimeOffset value)
        {
            int maxRequired = JsonConstants.MaximumFormatDateTimeOffsetLength + 3; // 2 quotes, and optionally, 1 list separator

            Span<byte> output = _output.GetSpan(maxRequired);
            int bytesPending = 0;

            if (_currentDepth < 0)
            {
                output[bytesPending++] = JsonConstants.ListSeparator;
            }

            output[bytesPending++] = JsonConstants.Quote;

            JsonWriterHelper.WriteDateTimeOffsetTrimmed(output.Slice(bytesPending), value, out int bytesWritten);
            bytesPending += bytesWritten;

            output[bytesPending++] = JsonConstants.Quote;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }

        private void WriteStringValueIndented(DateTimeOffset value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            // 2 quotes, and optionally, 1 list separator and 1-2 bytes for new line
            int maxRequired = indent + JsonConstants.MaximumFormatDateTimeOffsetLength + 3 + s_newLineLength;

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

            output[bytesPending++] = JsonConstants.Quote;

            JsonWriterHelper.WriteDateTimeOffsetTrimmed(output.Slice(bytesPending), value, out int bytesWritten);
            bytesPending += bytesWritten;

            output[bytesPending++] = JsonConstants.Quote;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }
    }
}
