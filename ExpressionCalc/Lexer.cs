using System;
using System.IO;

// Lexical analyzer

namespace ExpressionCalc
{
    class Lexer
    {
        interface IStream
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

        class FileStream
        : IStream
        {
            public FileStream(System.IO.FileStream fs)
            {
                _fs = fs;
                _eof = false;
            }
            public virtual bool eof()
            {
                if (_eof)
                    return true;
                return false;
            }
            public virtual char next()
            {
                if (_eof)
                    return ' ';
                int b = _fs.ReadByte();
                if (b == -1)
                {
                    _eof = true;
                    _fs.Close();
                    return ' ';
                }
                return Convert.ToChar(b);
            }
            private System.IO.FileStream _fs;
            private bool _eof;
        }

        /// <summary>
        /// Implement the Stream from reading a string
        /// </summary>
        class StringStream
            : IStream
        {
            public StringStream(string s)
            {
                _s = s;
                stringLen = s.Length;
            }
            public bool eof()
            {
                return (i >= stringLen);
            }
            public char next()
            {
                if (!eof())
                    return _s[i++];
                else
                    return ' ';
            }
            private string _s;
            private int i;
            private int stringLen;
        }

        public Lexer(string str)
            : this(new StringStream(str))
        {

        }
        public Lexer(System.IO.FileStream fs)
            : this(new FileStream(fs))
        {
        }

        public struct Token
        {
            //Token type (See TokenType_)
            public int TokenType;
            //Store string(identifier)
            public string strToken;
            //Store number
            public double numToken;
        }

        public const int TOKEN_EOF = -1;
        public const int TOKEN_NUMBER = 1;
        public const int TOKEN_ID = 2;

        /// <summary>
        /// Construct A new Lexical analyzer
        /// </summary>
        /// <param name="source">Code source</param>
        private Lexer(IStream source)
        {
            _source = source;
            _currentStringToken = "";
            _comment = false;
            _line = 1;
            _lastEOF = _source.eof();
            _current = _source.next();
            _currentToken = new Token();
        }
        /// <summary>
        /// Read a number from the source
        /// </summary>
        /// <returns>The number read</returns>
        private double readNumber()
        {
            double d = 0;
            double t = 1;

            while (Char.IsDigit(_current))
            {
                d *= 10;
                d = d + (_current - '0');
                _current = _source.next();
            }

            if (_current == '.')
            {
                _current = _source.next();
                while (Char.IsDigit(_current))
                {
                    t /= 10;
                    d += t * (_current - '0');
                    _current = _source.next();
                }
            }
            return d;
        }

        /// <summary>
        /// Read next token.
        /// </summary>
        /// <returns>The next token.</returns>
        public Token next()
        {
            _lastToken = _currentToken;
            _currentToken = new Token();

            //Skip any white space
            while (Char.IsWhiteSpace(_current) && !_source.eof() && _current != '\n')
                _current = _source.next();

            //See if the source has reached an end
            if (_source.eof())
            {
                if (_lastEOF)
                {
                    _currentToken.TokenType = TOKEN_EOF;
                    return _currentToken;
                }
                _lastEOF = true;
            }
            else
                _lastEOF = false;

            //Read the next character
            while (true)
            {
                //Skip comments
                if (_comment)
                {
                    _current = _source.next();
                    if (_current != '\n') continue;
                }

                switch (_current)
                {
                    // A new line
                    case '\n':
                        _comment = false;
                        _line++;
                        _current = _source.next();
                        break;
                    // Comments
                    case '#':
                        _comment = true;
                        _current = _source.next();
                        break;
                    // -
                    case '-':
                        _currentToken.TokenType = '-';
                        _current = _source.next();
                        return _currentToken;
                    case '=':
                        _currentToken.TokenType = _current;
                        _current = _source.next();
                        return _currentToken;
                    default:
                        //Skip white space
                        if (Char.IsWhiteSpace(_current) && !_source.eof() )
                        {
                            _current = _source.next();
                            continue;
                        }
                        //Number
                        if (Char.IsDigit(_current))
                        {
                            _currentToken.TokenType = TOKEN_NUMBER;
                            _currentToken.numToken = readNumber();
                            return _currentToken;
                        }
                        //Identifier or keywords
                        if (Char.IsLetter(_current) || _current == '_')
                        {
                            do
                            {
                                _currentStringToken += _current;
                                _current = _source.next();
                            } while (Char.IsLetterOrDigit(_current) || _current == '_');
                            //Not keywords, check identifier
                            _currentToken.TokenType = TOKEN_ID;
                            _currentToken.strToken = _currentStringToken;
                            _currentStringToken = "";
                            return _currentToken;
                        }
                        _currentToken.TokenType = _current;
                        _current = _source.next();
                        return _currentToken;
                }

            }

            _currentToken.TokenType = _current;
            return _currentToken;
        }
        /// <summary>
        /// Is the Lexer has come to an end( no Tokens left)
        /// </summary>
        /// <returns></returns>
        public bool eof()
        {
            return _source.eof();
        }
        private string _currentStringToken;
        private IStream _source;
        private bool _comment;
        private int _line;
        private Token _lastToken;
        private Token _currentToken;
        private bool _lastEOF;
        public char _current;
        public int currentLine
        {
            get
            {
                return _line;
            }
        }
    }
}
