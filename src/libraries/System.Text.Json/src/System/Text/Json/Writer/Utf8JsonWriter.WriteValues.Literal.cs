// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the JSON literal "null" as an element of a JSON array.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        public void WriteNullValue()
        {
            WriteLiteralByOptions(JsonConstants.NullValue);

            SetFlagToAddListSeparatorBeforeNextItem();
            _tokenType = JsonTokenType.Null;
        }

        /// <summary>
        /// Writes the <see cref="bool"/> value (as a JSON literal "true" or "false") as an element of a JSON array.
        /// </summary>
        /// <param name="value">The value write.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this would result in invalid JSON being written (while validation is enabled).
        /// </exception>
        public void WriteBooleanValue(bool value)
        {
            if (value)
            {
                WriteLiteralByOptions(JsonConstants.TrueValue);
                _tokenType = JsonTokenType.True;
            }
            else
            {
                WriteLiteralByOptions(JsonConstants.FalseValue);
                _tokenType = JsonTokenType.False;
            }

            SetFlagToAddListSeparatorBeforeNextItem();
        }

        private void WriteLiteralByOptions(ReadOnlySpan<byte> utf8Value)
        {
            ValidateWritingValue();
            if (_options.Indented)
            {
                WriteLiteralIndented(utf8Value);
            }
            else
            {
                WriteLiteralMinimized(utf8Value);
            }
        }

        private void WriteLiteralMinimized(ReadOnlySpan<byte> utf8Value)
        {
            Debug.Assert(utf8Value.Length <= 5);

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

        private void WriteLiteralIndented(ReadOnlySpan<byte> utf8Value)
        {
            int indent = Indentation;
            Debug.Assert(indent <= 2 * JsonConstants.MaxWriterDepth);
            Debug.Assert(utf8Value.Length <= 5);

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
