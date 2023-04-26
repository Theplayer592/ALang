using static System.String;
using static System.Char;
using static System.Array;

namespace ALang
{
    class Lexer
    {
        private static readonly string[] Keywords =
        {
            "if",
            "elif",
            "else",
            "while",
            "for",
            "def",
            "return",
            "not",
            "and",
            "or",
            "True",
            "False",
            "None"
        };

        private static readonly TokenType[] KeywordTypes =
        {
            TokenType.IF,
            TokenType.ELIF,
            TokenType.ELSE,
            TokenType.WHILE,
            TokenType.FOR,
            TokenType.FUNCTION,
            TokenType.RETURN,
            TokenType.NOT,
            TokenType.AND,
            TokenType.OR,
            TokenType.BOOL,
            TokenType.BOOL,
            TokenType.NULL
        };

        private static readonly string[] JSKeywords =
        {
            "break",
            "case",
            "catch",
            "class",
            "const",
            "continue",
            "debugger",
            "default",
            "delete",
            "do",
            "else",
            "export",
            "extends",
            "false",
            "finally",
            "for",
            "function",
            "if",
            "import",
            "in",
            "instanceof",
            "new",
            "null",
            "return",
            "super",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "var",
            "void",
            "while",
            "with",
            "let",
            "static",
            "yield",
            "await",
            "enum",
            "implements",
            "interface",
            "package",
            "private",
            "protected",
            "public",
            "arguments",
            "as",
            "async",
            "eval",
            "from",
            "get",
            "of",
            "set",
            "globalThis",
            "Infinity",
            "NaN",
            "undefined",
            "isFinite",
            "isNaN",
            "parseFloat",
            "parseInt",
            "decodeURI",
            "decodeURIComponent",
            "encodeURI",
            "encodeURIComponent",
            "escape",
            "unescape",
            "Object",
            "Function",
            "Boolean",
            "Symbol",
            "Number",
            "BigInt",
            "Math",
            "Date",
            "String",
            "Regexp",
            "Array",
            "Int8Array",
            "Uint8Array",
            "Uint8ClampedArray",
            "Int16Array",
            "Uint16Array",
            "Int32Array",
            "Uint32Array",
            "BigInt64Array",
            "BigUint64Array",
            "Float32Array",
            "Float64Array",
            "Map",
            "Set",
            "WeakMap",
            "WeakSet",
            "ArrayBuffer",
            "SharedArrayBuffer",
            "DataView",
            "Atomics",
            "JSON",
            "WeakRef",
            "FinalizationRegistry",
            "Iterator",
            "AsyncInterator",
            "Promise",
            "GeneratorFunction",
            "AsyncGeneratorFunction",
            "Generator",
            "AsyncGenerator",
            "AsyncFunction",
            "Reflect",
            "Proxy",
            "Intl",
            "console"
    };

        private static readonly string[] BuiltIns =
        {
            "print",
            "str",
            "int",
            "float"
        };

        private char[] input;

        private int lineN = 1;

        public Lexer(char[] input)
        {
            this.input = input;
        }

        public Token[] Lex()
        {
            Token[] tokens = new Token[input.Length + 3];

            // My first token is an EOL token
            tokens[0].type = TokenType.EOL;
            tokens[0].val = "\n";

            int tokenN = 1;
            int i = 0;

            while (i < input.Length)
            {
                // Ignore whitespace/null characters (unless it is a new line)
                if (IsNullOrWhiteSpace(input[i].ToString()))
                {
                    // Detect new-line
                    if (input[i] == '\n')
                    {
                        tokens[tokenN].type = TokenType.EOL;
                        tokens[tokenN].val = "\n";
                        tokenN++;
                        lineN++;
                    }

                    // Detect indentation (4 spaces)
                    if (input[i] == ' ' && input[i + 1] == ' ' && input[i + 2] == ' ' && input[i + 3] == ' ')
                    {
                        tokens[tokenN].type = TokenType.INDENT;
                        tokens[tokenN].val = "    ";
                        tokenN++;
                        i += 3;
                    }

                    // Detect indentation (tab)
                    if (input[i] == '\t')
                    {
                        tokens[tokenN].type = TokenType.INDENT;
                        tokens[tokenN].val = "\t";
                        tokenN++;
                    }

                    i++;
                    continue;
                }

                // Identify dictionaries
                if (input[i] == '{')
                {
                    int depth = 1;
                    string val = "";

                    i++;

                    while (depth > 0 && i < input.Length)
                    {
                        if (input[i] == '{') depth++;
                        if (input[i] == '}')depth--;
                        val += input[i];
                        i++;
                    }

                    if (depth != 0) throw new Exception($"Line {lineN}: Syntax Error: Unmatched {{/}}");

                    val = val.Remove(val.Length - 1, 1);

                    tokens[tokenN].type = TokenType.OBJ;
                    tokens[tokenN].val = val;
                    tokenN++;
                    continue;
                }

                // Identify arrays
                if (input[i] == '[')
                {
                    int depth = 1;
                    string val = "";

                    i++;

                    while (depth > 0 && i < input.Length)
                    {
                        if (input[i] == '[') depth++;
                        if (input[i] == ']') depth--;
                        val += input[i];
                        i++;
                    }

                    if (depth != 0) throw new Exception($"Line {lineN}: Syntax Error: Unmatched [/]");

                    val = val.Remove(val.Length - 1, 1);

                    tokens[tokenN].type = TokenType.ARRAY;
                    tokens[tokenN].val = val;
                    tokenN++;
                    continue;
                }

                // Check for single-character/short/symbolic tokens
                switch (input[i])
                {
                    case '(':
                        tokens[tokenN].type = TokenType.LP;
                        tokens[tokenN].val = "(";
                        tokenN++;
                        i++;
                        continue;
                    case ')':
                        tokens[tokenN].type = TokenType.RP;
                        tokens[tokenN].val = ")";
                        tokenN++;
                        i++;
                        continue;
                    case '[':
                        tokens[tokenN].type = TokenType.LSB;
                        tokens[tokenN].val = "[";
                        tokenN++;
                        i++;
                        continue;
                    case ']':
                        tokens[tokenN].type = TokenType.RSB;
                        tokens[tokenN].val = "]";
                        tokenN++;
                        i++;
                        continue;
                    case ';':
                        tokens[tokenN].type = TokenType.EOS;
                        tokens[tokenN].val = ";";
                        tokenN++;
                        i++;
                        continue;
                    case ':':
                        tokens[tokenN].type = TokenType.COLON;
                        tokens[tokenN].val = ":";
                        tokenN++;
                        i++;
                        continue;
                    case ',':
                        tokens[tokenN].type = TokenType.COMMA;
                        tokens[tokenN].val = ",";
                        tokenN++;
                        i++;
                        continue;
                    case '+':
                        tokens[tokenN].type = TokenType.ADD;
                        tokens[tokenN].val = "+";
                        tokenN++;
                        i++;
                        continue;
                    case '-':
                        tokens[tokenN].type = TokenType.MINUS;
                        tokens[tokenN].val = "-";
                        tokenN++;
                        i++;
                        continue;
                    case '*':
                        if (input[i + 1] == '*')
                        {
                            tokens[tokenN].type = TokenType.POW;
                            tokens[tokenN].val = "**";
                            tokenN++;
                            i += 2;
                        }
                        else
                        {
                            tokens[tokenN].type = TokenType.MULTIPLY;
                            tokens[tokenN].val = "*";
                            tokenN++;
                            i++;
                        }
                        continue;
                    case '/':
                        if (input[i + 1] == '/')
                        {
                            tokens[tokenN].type = TokenType.FLOOR_DIVIDE;
                            tokens[tokenN].val = "//";
                            tokenN++;
                            i += 2;
                        }
                        else
                        {
                            tokens[tokenN].type = TokenType.DIVIDE;
                            tokens[tokenN].val = "/";
                            tokenN++;
                            i++;
                        }
                        continue;
                    case '%':
                        tokens[tokenN].type = TokenType.MOD;
                        tokens[tokenN].val = "%";
                        tokenN++;
                        i++;
                        continue;
                    case '=':
                        if (input[i + 1] == '=')
                        {
                            tokens[tokenN].type = TokenType.EQUAL;
                            tokens[tokenN].val = "===";
                            tokenN++;
                            i += 2;
                        }
                        else
                        {
                            tokens[tokenN].type = TokenType.ASSIGN;
                            tokens[tokenN].val = "=";
                            tokenN++;
                            i++;
                        }
                        continue;
                    case '!':
                        if (input[i + 1] == '=')
                        {
                            tokens[tokenN].type = TokenType.NEQUAL;
                            tokens[tokenN].val = "!==";
                            tokenN++;
                            i += 2;
                        }
                        else
                        {
                            throw new Exception($"Line {lineN}: Syntax Error: Unexpected '!'");
                        }
                        continue;
                    case '<':
                        if (input[i + 1] == '=')
                        {
                            tokens[tokenN].type = TokenType.LESSER_EQUAL;
                            tokens[tokenN].val = "<=";
                            tokenN++;
                            i += 2;
                        }
                        else
                        {
                            tokens[tokenN].type = TokenType.LESSER;
                            tokens[tokenN].val = "<";
                            tokenN++;
                            i++;
                        }
                        continue;
                    case '>':
                        if (input[i + 1] == '=')
                        {
                            tokens[tokenN].type = TokenType.GREATER_EQUAL;
                            tokens[tokenN].val = ">=";
                            tokenN++;
                            i += 2;
                        }
                        else
                        {
                            tokens[tokenN].type = TokenType.GREATER;
                            tokens[tokenN].val = ">";
                            tokenN++;
                            i++;
                        }
                        continue;
                    case '.':
                        tokens[tokenN].type = TokenType.DOT;
                        tokens[tokenN].val = ".";
                        tokenN++;
                        i++;
                        continue;
                }

                // Check for string literals
                if (input[i] == '\'' || input[i] == '"')
                {
                    char terminator = input[i];

                    i++;

                    bool isLongString = false;

                    // Check for longstring type
                    if (input[i] == terminator && input[i + 1] == terminator)
                    {
                        isLongString = true;
                    }

                    string val = "";
                    int start = i;
                    bool ended = false;

                    while (!ended && i < input.Length)
                    {
                        if (input[i] == terminator)
                        {
                            if(isLongString && (input[i + 1] != terminator || input[i + 2] != terminator)) {
                                i++;
                                continue;
                            }
                            ended = true;
                            break;
                        }
                        // Ignore carriage returns
                        if (input[i] == '\r')
                        {
                            i++;
                            continue;
                        }
                        // React appropriately to new-line characters
                        if (input[i] == '\n')
                        {
                            if (isLongString)
                            {
                                val += "\\n";
                                i++;
                                continue;
                            }
                            else
                            {
                                break;
                            }
                        }
                        // Replace actual tabs with the tab escape sequence
                        if (input[i] == '\t' && isLongString)
                        {
                            val += "\\t";
                            i++;
                            continue;
                        }
                        // Unescape escape sequences
                        if (input[i] == '\\')
                        {
                            switch (input[i])
                            {
                                case '\\':
                                    val += "\\";
                                    break;
                                case 'n':
                                    val += "n";
                                    break;
                                case 'r':
                                    val += "r";
                                    break;
                                case 't':
                                    val += "t";
                                    break;
                                case '"':
                                    val += "\"";
                                    break;
                                case '\'':
                                    val += "'";
                                    break;
                                case 'b':
                                    val += "b";
                                    break;
                                case 'f':
                                    val += "f";
                                    break;
                                case 'a':
                                    val += "a";
                                    break;
                                case 'v':
                                    val += "v";
                                    break;
                                    // TODO: Implement unicode recognition
                                    // TODO: Implement hexidecimal escape sequences
                                    // TODO: Implement octal escape sequences
                            }
                            i++;
                            continue;
                        }
                        val += input[i];
                        i++;
                    }

                    if (!ended)
                    {
                        throw new Exception($"Line {lineN}: Syntax Error: Unterminated string literal");
                    }

                    tokens[tokenN].type = TokenType.STR;

                    tokens[tokenN].val = val;
                    tokenN++;
                    i++;

                    if (isLongString) i += 2;

                    continue;
                }

                // Check for numeric literals
                if (IsDigit(input[i]))
                {
                    int start = i;

                    i++;

                    while (IsDigit(input[i]))
                    {
                        i++;
                    }

                    if (input[i] == '.')
                    {
                        i++;
                        while (IsDigit(input[i]))
                        {
                            i++;
                        }
                    }

                    int len = i - start;
                    string literal = Join("", input).Substring(start, len);

                    tokens[tokenN].type = TokenType.NUM;
                    tokens[tokenN].val = literal;
                    tokenN++;
                    continue;
                }

                // Check for any keywords, built-in functions and identifiers
                if (IsLetter(input[i]))
                {
                    int start = i;

                    i++;

                    while (IsLetterOrDigit(input[i]))
                    {
                        i++;
                    }

                    int len = i - start;
                    string id = Join("", input).Substring(start, len);

                    if (Keywords.Contains(id))
                    {
                        tokens[tokenN].type = GetKeywordType(id);
                        // All javascript keywords are lowercase
                        tokens[tokenN].val = id.ToLower();

                        // If the type is None, that is special and will have a value of null
                        if (tokens[tokenN].type == TokenType.NULL) tokens[tokenN].val = "null";
                    }
                    else if (IsBuiltIn(id))
                    {
                        tokens[tokenN].type = TokenType.BUILT_IN;
                        tokens[tokenN].val = id;
                    }
                    else
                    {
                        // If the value clashes with a js built-in/keyword, then we need to slightly modify the name

                        if (JSKeywords.Contains(id)) id = "_$" + id;

                        tokens[tokenN].type = TokenType.ID;
                        tokens[tokenN].val = id;
                    }

                    tokenN++;
                    continue;
                }

                // Check for any comments
                if (input[i] == '#')
                {
                    i++;

                    string val = "";

                    while (input[i] != '\n')
                    {
                        val += input[i];
                        i++;

                        if (i == input.Length) break;
                    }

                    tokens[tokenN].type = TokenType.COMMENT;
                    tokens[tokenN].val = val;
                    tokenN++;
                    continue;
                }

                // Throw error if unrecognised token
                throw new Exception($"Line {lineN}: Syntax Error: Unrecognised token '{input[i]}'");
            }

            // Make the last 2 tokens an EOL (end of statement) followed by an EOF (end of file)
            tokens[tokenN].type = TokenType.EOL;
            tokens[tokenN].val = "\n";
            tokenN++;
            tokens[tokenN].type = TokenType.EOF;
            tokens[tokenN].val = "";

            return tokens;
        }

        private static TokenType GetKeywordType(string keyword)
        {
            return KeywordTypes[IndexOf(Keywords, keyword)];
        }

        private static bool IsBuiltIn(string id)
        {
            return BuiltIns.Contains(id);
        }
    }
}