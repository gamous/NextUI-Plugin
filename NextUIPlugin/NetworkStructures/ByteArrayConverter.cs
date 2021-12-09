﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextUIPlugin.NetworkStructures {
	public class ByteArrayConverter : JsonConverter {
		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
			if (value == null) {
				writer.WriteNull();
				return;
			}

			var data = (byte[])value;

			// Compose an array.
			writer.WriteStartArray();

			foreach (var t in data) {
				writer.WriteValue(t);
			}

			writer.WriteEndArray();
		}

		public override object ReadJson(
			JsonReader reader,
			Type objectType,
			object? existingValue,
			JsonSerializer serializer
		) {
			if (reader.TokenType != JsonToken.StartArray) {
				throw new Exception(
					$"Unexpected token parsing binary. Expected StartArray, got {reader.TokenType}."
				);
			}

			var byteList = new List<byte>();

			while (reader.Read()) {
				switch (reader.TokenType) {
					case JsonToken.Integer:
						byteList.Add(Convert.ToByte(reader.Value));
						break;
					case JsonToken.EndArray:
						return byteList.ToArray();
					case JsonToken.Comment:
						// skip
						break;
					default:
						throw new Exception(
							$"Unexpected token when reading bytes: {reader.TokenType}"
						);
				}
			}

			throw new Exception("Unexpected end when reading bytes.");
		}

		public override bool CanConvert(Type objectType) {
			return objectType == typeof(byte[]);
		}
	}
}