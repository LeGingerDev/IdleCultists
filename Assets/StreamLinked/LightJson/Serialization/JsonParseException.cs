using System;

namespace ScoredProductions.StreamLinked.LightJson.Serialization
{
	/// <summary>
	/// The exception that is thrown when a JSON message cannot be parsed.
	/// </summary>
	/// <remarks>
	/// This exception is only intended to be thrown by LightJson.
	/// </remarks>
	[Serializable]
	public sealed class JsonParseException : Exception
	{
		/// <summary>
		/// Gets the text position where the error occurred.
		/// </summary>
		public TextPosition Position { get; private set; }

		/// <summary>
		/// Gets the type of error that caused the exception to be thrown.
		/// </summary>
		public ErrorType Type { get; private set; }

		/// <summary>
		/// Character that caused the error
		/// </summary>
		public char ReadCharacter { get; private set; }

		/// <summary>
		/// Initializes a new instance of JsonParseException.
		/// </summary>
		public JsonParseException()
			: base(GetDefaultMessage(ErrorType.Unknown)) { }

		/// <summary>
		/// Initializes a new instance of JsonParseException.
		/// </summary>
		public JsonParseException(Exception exc)
			: base(GetDefaultMessage(ErrorType.Unknown), exc) { }

		/// <summary>
		/// Initializes a new instance of JsonParseException with the given error type and position.
		/// </summary>
		/// <param name="type">The error type that describes the cause of the error.</param>
		/// <param name="position">The position in the text where the error occurred.</param>
		public JsonParseException(ErrorType type, TextPosition position, char character)
			: base(GetDefaultMessage(type, position)) {
			this.Type = type;
			this.Position = position;
			this.ReadCharacter = character;
		}

		/// <summary>
		/// Initializes a new instance of JsonParseException with the given error type and position.
		/// </summary>
		/// <param name="type">The error type that describes the cause of the error.</param>
		/// <param name="position">The position in the text where the error occurred.</param>
		public JsonParseException(ErrorType type, TextPosition position, char character, Exception exc)
			: base(GetDefaultMessage(type, position), exc) {
			this.Type = type;
			this.Position = position;
			this.ReadCharacter = character;
		}

		/// <summary>
		/// Initializes a new instance of JsonParseException with the given message, error type, and position.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="type">The error type that describes the cause of the error.</param>
		/// <param name="position">The position in the text where the error occurred.</param>
		public JsonParseException(string message, ErrorType type, TextPosition position, char character)
			: base(GetDefaultMessage(type, position, message))
		{
			this.Type = type;
			this.Position = position;
			this.ReadCharacter = character;
		}

		/// <summary>
		/// Initializes a new instance of JsonParseException with the given message, error type, and position.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="type">The error type that describes the cause of the error.</param>
		/// <param name="position">The position in the text where the error occurred.</param>
		public JsonParseException(string message, ErrorType type, TextPosition position, char character, Exception exc)
			: base(GetDefaultMessage(type, position, message), exc)
		{
			this.Type = type;
			this.Position = position;
			this.ReadCharacter = character;
		}

		private static string GetDefaultMessage(ErrorType type, string message = null)
		{
			if (string.IsNullOrWhiteSpace(message)) {
				return type switch {
					ErrorType.IncompleteMessage => "The string ended before a value could be parsed.",
					ErrorType.InvalidOrUnexpectedCharacter => "The parser encountered an invalid or unexpected character.",
					ErrorType.DuplicateObjectKeys => "The parser encountered a JsonObject with duplicate keys.",
					_ => "An error occurred while parsing the JSON message.",
				};
			} else {
				return type switch {
					ErrorType.IncompleteMessage => "The string ended before a value could be parsed. Provided Message: ",
					ErrorType.InvalidOrUnexpectedCharacter => "The parser encountered an invalid or unexpected character. Provided Message: ",
					ErrorType.DuplicateObjectKeys => "The parser encountered a JsonObject with duplicate keys. Provided Message: ",
					_ => "An error occurred while parsing the JSON message. Provided Message: ",
				} + message;
			}
		}

		private static string GetDefaultMessage(ErrorType type, TextPosition Position, string message = null)
		{
			if (string.IsNullOrWhiteSpace(message)) {
				return type switch {
					ErrorType.IncompleteMessage => "The string ended before a value could be parsed. ",
					ErrorType.InvalidOrUnexpectedCharacter => "The parser encountered an invalid or unexpected character. ",
					ErrorType.DuplicateObjectKeys => "The parser encountered a JsonObject with duplicate keys. ",
					_ => "An error occurred while parsing the JSON message. ",
				} + Position.ToString();
			} else {
				return type switch {
					ErrorType.IncompleteMessage => "The string ended before a value could be parsed.",
					ErrorType.InvalidOrUnexpectedCharacter => "The parser encountered an invalid or unexpected character.",
					ErrorType.DuplicateObjectKeys => "The parser encountered a JsonObject with duplicate keys.",
					_ => "An error occurred while parsing the JSON message.",
				} + Position.ToString() + " Provided Message: " + message;
			}
		}

		/// <summary>
		/// Enumerates the types of errors that can occur when parsing a JSON message.
		/// </summary>

		[Serializable]
		public enum ErrorType : byte
		{
			/// <summary>
			/// Indicates that the cause of the error is unknown.
			/// </summary>
			Unknown = 0,

			/// <summary>
			/// Indicates that the text ended before the message could be parsed.
			/// </summary>
			IncompleteMessage,

			/// <summary>
			/// Indicates that a JsonObject contains more than one key with the same name.
			/// </summary>
			DuplicateObjectKeys,

			/// <summary>
			/// Indicates that the parser encountered and invalid or unexpected character.
			/// </summary>
			InvalidOrUnexpectedCharacter,
		}
	}
}
