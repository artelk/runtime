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
        /// Writes the <see cref="decimal"/> value (as a JSON number) as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        /// <remarks>
        /// Writes the <see cref="decimal"/> using the default <see cref="StandardFormat"/> (that is, 'G').
        /// </remarks>
        public void WriteNumberValue(decimal value)
        {
            ValidateWritingValue();
            if (_options.Indented)
            {
                WriteNumberValueIndented(value);
            }
            else
            {
                WriteNumberValueMinimized(value);
            }

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Number;
        }

        private void WriteNumberValueMinimized(decimal value)
        {
            int maxRequired = JsonConstants.MaximumFormatDecimalLength + 1; // Optionally, 1 list separator

            Span<byte> output = _output.GetSpan(maxRequired);
            int bytesPending = 0;

            if (_currentDepth < 0)
            {
                output[bytesPending++] = JsonConstants.ListSeparator;
            }

            bool result = Utf8Formatter.TryFormat(value, output.Slice(bytesPending), out int bytesWritten);
            Debug.Assert(result);
            bytesPending += bytesWritten;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }

        private void WriteNumberValueIndented(decimal value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);

            int maxRequired = indent + JsonConstants.MaximumFormatDecimalLength + 1 + s_newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line

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

            bool result = Utf8Formatter.TryFormat(value, output.Slice(bytesPending), out int bytesWritten);
            Debug.Assert(result);
            bytesPending += bytesWritten;
            _bytesAdvanced += bytesPending;
            _output.Advance(bytesPending);
        }
    }
}
