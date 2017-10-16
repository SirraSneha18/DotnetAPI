﻿// 
// Copyright (c) 2004-2017 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;

namespace NLog.Targets
{
    /// <summary>
    /// Interface for serialization of values, maybe even objects to JSON format. 
    /// Useful for wrappers for existing serializers.
    /// </summary>
    [Obsolete("Use NLog.IJsonConverter class instead. Marked obsolete on NLog 4.5")]
    public interface IJsonSerializer
    {
        /// <summary>
        /// Returns a serialization of an object
        /// into JSON format.
        /// </summary>
        /// <param name="value">The object to serialize to JSON.</param>
        /// <returns>Serialized value (null = Serialize failed).</returns>
        string SerializeObject(object value);
    }

#pragma warning disable 618
    internal class JsonConverterLegacy : IJsonConverter, IJsonSerializer
    {
        private readonly IJsonSerializer _jsonSerializer;

        public JsonConverterLegacy(IJsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer;
        }

        public bool SerializeObject(object value, System.Text.StringBuilder builder)
        {
            var text = _jsonSerializer.SerializeObject(value);
            if (text == null)
            {
                return false;
            }
            builder.Append(text);
            return true;
        }

        string IJsonSerializer.SerializeObject(object value)
        {
            return _jsonSerializer.SerializeObject(value);
        }
    }
#pragma warning restore 618 // Type or member is obsolete
}
