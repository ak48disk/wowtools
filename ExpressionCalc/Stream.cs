﻿using System;
using System.IO;

namespace Interpreter
{
    /// <summary>
    /// Implement the Stream from reading a file
    /// </summary>
    class FileStream
        : IStream
    {
        public FileStream(string fileName)
        {
            _fs = new System.IO.FileStream(fileName, FileMode.Open, FileAccess.Read);
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
}
