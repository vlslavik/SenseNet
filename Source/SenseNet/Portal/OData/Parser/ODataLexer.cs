using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.Portal.OData.Parser
{
    internal enum ASCII
    {
        NULL = 0,    // "\u0000"
        HTAB = 9,
        LF = 10,
        VTAB = 11,
        CR = 13,
        SPC = 32,
        DOLLAR = 36, // $
        AMP = 38,    // &
        APOS = 39,   // '
        LPAREN = 40, // (
        RPAREN = 41, // )
        PLUS = 43,   // +
        COMMA = 44,  //,
        MINUS = 45,  // -
        DOT = 46,    // .
        SLASH = 47,  // /
        ZERO = 48,   // 0
        NINE = 57,   // 9
        COLON = 58,  // :
        A = 65,
        Z = 90,
        UNDERSCORE = 95, // _
        a = 97,
        z = 122
    }
    internal class Chars
    {
        internal static char NULL = (char)0;    // "\u0000"
        internal static char HTAB = (char)9;
        internal static char LF = (char)10;
        internal static char VTAB = (char)11;
        internal static char CR = (char)13;
        internal static char SPC = (char)32;
        internal static char DOLLAR = (char)36; // $
        internal static char AMP = (char)38;    // &
        internal static char APOS = (char)39;   // '
        internal static char LPAREN = (char)40; // (
        internal static char RPAREN = (char)41; // )
        internal static char PLUS = (char)43;   // +
        internal static char COMMA = (char)44;  //,
        internal static char MINUS = (char)45;  // -
        internal static char DOT = (char)46;    // .
        internal static char SLASH = (char)47;  // /
        internal static char ZERO = (char)48;   // 0
        internal static char NINE = (char)57;   // 9
        internal static char COLON = (char)58;  // :
        internal static char A = (char)65;
        internal static char Z = (char)90;
        internal static char UNDERSCORE = (char)95; // _
        internal static char a = (char)97;
        internal static char z = (char)122;
    }
    internal class Strings
    {
        internal static string NULL = null;    // "\u0000"
        internal static string HTAB = new String(new[] { (char)9 });
        internal static string LF = new String(new[] { (char)10 });
        internal static string VTAB = new String(new[] { (char)11 });
        internal static string CR = new String(new[] { (char)13 });
        internal static string SPC = new String(new[] { (char)32 });
        internal static string DOLLAR = new String(new[] { (char)36 }); // $
        internal static string AMP = new String(new[] { (char)38 });    // &
        internal static string APOS = new String(new[] { (char)39 });   // '
        internal static string LPAREN = new String(new[] { (char)40 }); // (
        internal static string RPAREN = new String(new[] { (char)41 }); // )
        internal static string PLUS = new String(new[] { (char)43 });   // +
        internal static string COMMA = new String(new[] { (char)44 });  //,
        internal static string MINUS = new String(new[] { (char)45 });  // -
        internal static string DOT = new String(new[] { (char)46 });    // .
        internal static string SLASH = new String(new[] { (char)47 });  // /
        internal static string ZERO = new String(new[] { (char)48 });   // 0
        internal static string NINE = new String(new[] { (char)57 });   // 9
        internal static string COLON = new String(new[] { (char)58 });  // :
        internal static string A = new String(new[] { (char)65 });
        internal static string Z = new String(new[] { (char)90 });
        internal static string UNDERSCORE = new String(new[] { (char)95 }); // _
        internal static string a = new String(new[] { (char)97 });
        internal static string z = new String(new[] { (char)122 });
    }
    internal enum CharType { EOF, WSP, CtrlChar, Char, Digit, Alpha }
    internal enum TokenType { EOF, WSP, CtrlChar, Char, Digits, Word, String }

    internal class Token
    {
        internal TokenType TokenType = TokenType.EOF;
        internal string Value;
        internal int Line;
        internal int Column;
        public override string ToString()
        {
            return this.TokenType == TokenType.EOF ? "[EOF]" : this.Value;
        }
    }

    internal class ODataLexer
    {
        internal string Src { get; set; }
        internal int SrcLength { get; set; }
        internal int SrcIndex { get; set; }
        internal Token Token { get; set; }
        internal char CurrentChar { get; set; }
        internal CharType CurrentCharType { get; set; }
        internal int Line { get; set; }
        internal int Column { get; set; }

        public ODataLexer(string src)
        {
            Src = src;
            SrcLength = src.Length;
            SrcIndex = 0;
            Token = null;
            CurrentChar = Chars.NULL;
            CurrentCharType = CharType.EOF;
            Line = 0;
            Column = -1;
            NextChar();
            NextToken();
        }

        internal bool NextChar()
        {
            if (SrcIndex >= SrcLength)
            {
                CurrentChar = '\0';
                CurrentCharType = CharType.EOF;
                Column++;
                return false;
            }
            if (CurrentChar == Chars.CR)
            {
                Line++;
                Column = -1;
            }
            CurrentChar = Src[SrcIndex++];
            Column++;
            CurrentCharType = this.GetCharType(CurrentChar);
            return true;
        }
        internal CharType GetCharType(char c)
        {
            if (c == Chars.NULL) return CharType.EOF;
            if (c == Chars.HTAB) return CharType.WSP;
            if (c < Chars.SPC) return CharType.CtrlChar;
            if (c == Chars.SPC) return CharType.WSP;
            if (c < Chars.ZERO) return CharType.Char;
            if (c <= Chars.NINE) return CharType.Digit;
            if (c < Chars.A) return CharType.Char;
            if (c <= Chars.Z) return CharType.Alpha;
            if (c < Chars.a) return CharType.Char;
            if (c <= Chars.z) return CharType.Alpha;
            throw new NotImplementedException("Unknown CharType");
        }
        internal void NextToken()
        {
            /*    
            whitespace:
                SPC HTAB VTAB CR LF
            char:
                '-!$%&()*,./:;?@[]_{}~+= ; ????
            digit:
                0 1 2 3 4 5 6 7 8 9
            word:
                add all ...
            string:
                ' char | whitespace | digit | alpha '
            */
            this.SkipWhiteSpaces();
            var token = new Token();
            Token = token;
            token.Line = Line;
            token.Column = Column;
            switch (CurrentCharType)
            {
                case CharType.EOF: token.TokenType = TokenType.EOF; token.Value = new String(CurrentChar, 1); return;
                case CharType.WSP: token.TokenType = TokenType.WSP; token.Value = new String(CurrentChar, 1); NextChar(); return;
                case CharType.CtrlChar: token.TokenType = TokenType.CtrlChar; token.Value = new String(CurrentChar, 1); NextChar(); return;
                case CharType.Char:
                    var startIndex = SrcIndex;
                    if (CurrentChar == Chars.APOS && ReadString())
                    {
                        token.TokenType = TokenType.String;
                        token.Value = Src.Substring(startIndex, SrcIndex - startIndex - 1).Replace("''", "'");
                        Column += token.Value.Length + 1;
                    }
                    else
                    {
                        token.TokenType = TokenType.Char;
                        token.Value = new String(CurrentChar, 1);
                    }
                    NextChar();
                    return;
                case CharType.Digit: ScanDigits(token); return;
                case CharType.Alpha: ScanWord(token); return;
                default:
                    throw new ApplicationException("Unknown CharType: " + CurrentCharType.ToString());
            }
        }
        private bool ReadString()
        {
            while (true)
            {
                if (!ReadTo("'"))
                    throw ODataParser.SyntaxError(this, "Unterminated string");
                if (SrcIndex >= SrcLength || Src[SrcIndex] != Chars.APOS)
                    return true;
                if (!NextChar())
                    throw ODataParser.SyntaxError(this, "Unterminated string");
            }
        }
        internal void SkipWhiteSpaces()
        {
            while (CurrentCharType == CharType.WSP && this.NextChar()) ;
        }
        internal bool ReadTo(string str)
        {
            if (SrcIndex > SrcLength - str.Length)
                return false;

            var p = Src.IndexOf(str, SrcIndex);
            if (p < 0)
                return false;

            SrcIndex = p + 1;
            return true;
        }
        internal void ScanDigits(Token token)
        {
            var startIndex = SrcIndex - 1;
            var length = 0;
            token.TokenType = TokenType.Digits;
            while (CurrentCharType == CharType.Digit)
            {
                NextChar();
                length++;
            }
            token.Value = Src.Substring(startIndex, length);
        }
        internal void ScanWord(Token token)
        {
            var startIndex = SrcIndex - 1;
            var length = 0;
            while (CurrentCharType == CharType.Alpha)
            {
                NextChar();
                length++;
            }
            token.TokenType = TokenType.Word;
            token.Value = Src.Substring(startIndex, length);
        }

        internal char Peek()
        {
            var i = Token.Column + (Token.Value == null ? 1 : Token.Value.Length);
            while (i < SrcLength && Char.IsWhiteSpace(Src[i]))
                i++;
            return i >= SrcLength ? Chars.NULL : Src[i];
        }
    }
}
