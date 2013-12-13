
namespace Interpreter
{
    public interface IStream
    {
        /// <summary>
        /// Read the next character.
        /// </summary>
        /// <returns>The next character in the stream</returns>
        char next();
        /// <summary>
        /// Return if the stream has cone to an end.
        /// </summary>
        /// <returns>If the stream has cone to an end.</returns>
        bool eof();
    }
}
