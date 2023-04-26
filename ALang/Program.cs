using static System.String;
using static System.Char;
using static System.Array;
using System.Diagnostics;

namespace ALang
{
    enum TokenType
    {
        NO_TOKEN,
        NUM,
        STR,
        LONG_STR,
        BOOL,
        ARRAY, 
        OBJ, 
        NULL,
        IF,
        ELIF,
        ELSE,
        WHILE,
        FOR,
        FUNCTION,
        RETURN,
        ASSIGN,
        EQUAL,
        NEQUAL,
        LESSER,
        GREATER,
        LESSER_EQUAL,
        GREATER_EQUAL,
        ADD,
        MINUS,
        MULTIPLY,
        DIVIDE,
        MOD,
        FLOOR_DIVIDE,
        POW,
        LP,
        RP,
        COLON,
        COMMA,
        LSB,
        RSB,
        AND,
        OR,
        NOT,
        DOT,
        ID,
        BUILT_IN,
        INDENT,
        COMMENT,
        EOS,
        EOL,
        EOF
    }

    struct Token
    {
        public TokenType type;
        public string val;
    }

    class Program
    {
        static void Main(string[] args)
        {
            long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Console.WriteLine("Compiling...");

            Lexer lexer = new Lexer(File.ReadAllText("main.al").ToCharArray());

            Token[] tokens = lexer.Lex();
            //foreach (Token t in tokens)
            //{
            //    Console.WriteLine(t.type.ToString() + " " + t.val);
            //}

            Compiler compiler = new Compiler(tokens, "main");
            string compiled = compiler.Compile();

            Console.WriteLine($"Successfully Compiled in {Math.Round((double)DateTimeOffset.Now.ToUnixTimeMilliseconds() - start, 3)}ms\n");

            File.WriteAllText("main.js", compiled);

            Process.Start("CMD.exe", $"/C node \"{Path.GetFullPath("main.js")}\"");
        }
    }
}