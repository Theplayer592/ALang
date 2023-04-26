using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ALang
{
    class Compiler
    {
        private static readonly TokenType[] conditionals = new TokenType[] { 
            TokenType.IF,
            TokenType.ELIF,
            TokenType.ELSE,
            TokenType.FOR,
            TokenType.WHILE
        };

        private static readonly TokenType[] valueTypes = new TokenType[]
        {
            TokenType.NUM,
            TokenType.STR,
            TokenType.LONG_STR,
            TokenType.BOOL,
            TokenType.ARRAY,
            TokenType.OBJ,
            TokenType.NULL,
            TokenType.ID
        };

        private Token[] tokens;
        private string fileName;
        private int i = 0;
        private int prevIndent = 0;
        private int indent = 0;
        private int lineN = 0;
        private int lastLineI = 1;
        private List<string> output = new List<string>();
        private List<string> doutput = new List<string>(); // This is debug output: used to check validity of code after compilation

        private int brackets = 0;

        private TokenType[] valuesAccepted = new TokenType[] { TokenType.EOL };

        private Scope globalScope = new Scope(new List<string>(), new List<TokenType>(), 1, 1, null);
        private Scope currentScope;
        private List<int> FNScopeIndentLevels = new List<int>();

        public Compiler(Token[] tokens, string fileName) {
            this.tokens = tokens;
            this.fileName = fileName;

            currentScope = globalScope;

            // All files will be initiated as constants with their own scope
            AddToMainOutput($"const {fileName} = (function() {{");
        }

        public string Compile()
        {
            while (tokens[i].type != TokenType.EOF)
            {
                Token t = tokens[i];

                if (!valuesAccepted.Contains(t.type) && t.type != TokenType.COMMENT)
                {
                    throw new Exception($"Line {lineN}: Syntax Error: Did not expect {t.type} token");
                }

                // Check for numeric literals
                if (t.type == TokenType.NUM)
                {
                    valuesAccepted = new TokenType[] { TokenType.ADD, TokenType.MINUS, TokenType.MULTIPLY, TokenType.DIVIDE, TokenType.EQUAL, TokenType.NEQUAL, TokenType.LESSER, TokenType.GREATER, TokenType.LESSER_EQUAL, TokenType.GREATER_EQUAL, TokenType.MOD, TokenType.FLOOR_DIVIDE, TokenType.POW, TokenType.RP, TokenType.COLON, TokenType.COMMA, TokenType.RSB, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.EOS, TokenType.EOL };

                    AddToMainOutput(tokens[i].val);
                    i++;
                    continue;
                }

                // Check for string literals
                if(t.type == TokenType.STR)
                {
                    AddToMainOutput("\"");
                    while (tokens[i].type == TokenType.STR || tokens[i].type == TokenType.LONG_STR|| tokens[i].type == TokenType.INDENT || tokens[i].type == TokenType.EOL || tokens[i].type == TokenType.COMMENT)
                    {
                        if (tokens[i].type == TokenType.STR || tokens[i].type == TokenType.LONG_STR)
                        {
                            valuesAccepted = new TokenType[] { TokenType.ADD, TokenType.MINUS, TokenType.MULTIPLY, TokenType.DIVIDE, TokenType.EQUAL, TokenType.NEQUAL, TokenType.LESSER, TokenType.GREATER, TokenType.LESSER_EQUAL, TokenType.GREATER_EQUAL, TokenType.MOD, TokenType.FLOOR_DIVIDE, TokenType.POW, TokenType.RP, TokenType.COLON, TokenType.COMMA, TokenType.RSB, TokenType.LSB, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.EOS, TokenType.EOL };
                            AddToMainOutput(tokens[i].val);
                        }
                        if (tokens[i].type == TokenType.EOL)
                        {
                            valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.BOOL, TokenType.STR, TokenType.NULL, TokenType.LSB, TokenType.BUILT_IN, TokenType.IF, TokenType.ELIF, TokenType.ELSE, TokenType.FOR, TokenType.WHILE, TokenType.FUNCTION, TokenType.ID, TokenType.RETURN, TokenType.LP, TokenType.RP, TokenType.LSB, TokenType.RSB, TokenType.ID, TokenType.EOL };
                            lineN++;
                        }
                        i++;
                    }
                    AddToMainOutput("\"");

                    if (brackets == 0) AddToMainOutput(";");

                    continue;
                }

                // Check for arrays
                if(t.type == TokenType.ARRAY)
                {
                    valuesAccepted = new TokenType[] { TokenType.ADD, TokenType.EQUAL, TokenType.NEQUAL, TokenType.LESSER, TokenType.GREATER, TokenType.LESSER_EQUAL, TokenType.GREATER_EQUAL, TokenType.RP, TokenType.COMMA, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.INDENT, TokenType.EOS, TokenType.EOL };

                    AddToMainOutput(ValidateArr(tokens[i].val));
                    i++;
                    continue;
                }

                // Check for objects/dictionaries
                if(t.type == TokenType.OBJ)
                {
                    valuesAccepted = new TokenType[] { TokenType.EQUAL, TokenType.NEQUAL, TokenType.RP, TokenType.COMMA, TokenType.RSB, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.INDENT, TokenType.EOS, TokenType.EOL };

                    AddToMainOutput(ValidateObj(t.val));
                    i++;
                    continue;
                }

                // Check for other literals
                if (t.type == TokenType.BOOL || t.type == TokenType.NULL)
                {
                    valuesAccepted = new TokenType[] { TokenType.ADD, TokenType.MINUS, TokenType.MULTIPLY, TokenType.DIVIDE, TokenType.EQUAL, TokenType.NEQUAL, TokenType.LESSER, TokenType.GREATER, TokenType.LESSER_EQUAL, TokenType.GREATER_EQUAL, TokenType.MOD, TokenType.FLOOR_DIVIDE, TokenType.POW, TokenType.RP, TokenType.COMMA, TokenType.RSB, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.EOS, TokenType.EOL };

                    AddToMainOutput(tokens[i].val);
                    i++;
                    continue;
                }

                // Check for if statement
                if (t.type == TokenType.IF)
                {
                    valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.ID, TokenType.LP };

                    AddToMainOutput("if(");

                    // Evaluate follwoing bool statement
                    ConditionalBoolStatement();

                    continue;
                }

                // Check for else if statement
                if (t.type == TokenType.ELIF)
                {
                    valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.ID, TokenType.LP };

                    AddToMainOutput("else if(");

                    // Evaluate follwoing bool statement
                    ConditionalBoolStatement();

                    continue;
                }

                // Check for else statement
                if(t.type == TokenType.ELSE)
                {
                    valuesAccepted = new TokenType[] { TokenType.COLON };

                    AddToMainOutput("else {");

                    continue;
                }

                // Check for while loop
                if(t.type == TokenType.WHILE)
                {
                    valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.ID, TokenType.LP };

                    AddToMainOutput("while(");

                    // Evaluate folowing bool statement
                    ConditionalBoolStatement();

                    continue;
                }

                // Check for for loop
                if(t.type == TokenType.FOR)
                {
                    valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.ID, TokenType.LP };

                    AddToMainOutput("for(");

                    //Evaluate following bool statement
                    ConditionalBoolStatement();

                    continue;
                }

                // Check for function definition
                if(t.type == TokenType.FUNCTION)
                {
                    AddToMainOutput("function ");
                    i++;

                    if (tokens[i].type != TokenType.ID) throw new Exception("Syntax Error: a new function must have a name");

                    currentScope.ids.Add(tokens[i].val);
                    currentScope.types.Add(TokenType.FUNCTION);

                    NewScope(output.Count + 4, doutput.Count + 4);
                    AddToMainOutput(tokens[i].val);
                    ValidateSetParams();

                    valuesAccepted = new TokenType[] { TokenType.COLON };

                    continue;
                }

                // Check for return statement
                if(t.type == TokenType.RETURN)
                {
                    valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ARRAY, TokenType.OBJ, TokenType.ID, TokenType.EOS, TokenType.EOL };

                    AddToMainOutput("return ");

                    i++;
                    continue;
                }

                // References to variables
                if(t.type == TokenType.ID)
                {
                    // Check if variable exists
                    if (currentScope.ids.Contains(t.val))
                    {
                        TokenType thisType = currentScope.types[currentScope.ids.IndexOf(t.val)];
                        Console.WriteLine(t.val + " " + thisType);
                        if (!valuesAccepted.Contains(thisType) && thisType != TokenType.FUNCTION) throw new Exception($"Line {lineN}: Type Error: did not expect type '{thisType}'");

                        valuesAccepted = new TokenType[] { TokenType.ASSIGN, TokenType.ADD, TokenType.MINUS, TokenType.MULTIPLY, TokenType.DIVIDE, TokenType.EQUAL, TokenType.NEQUAL, TokenType.LESSER, TokenType.GREATER, TokenType.LESSER_EQUAL, TokenType.GREATER_EQUAL, TokenType.MOD, TokenType.FLOOR_DIVIDE, TokenType.POW, TokenType.RP, TokenType.LP, TokenType.COMMA, TokenType.RSB, TokenType.LSB, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.DOT, TokenType.EOS, TokenType.EOL };

                        doutput.Insert(lastLineI + 2, $"$InternalCheckExists(\"{t.val}\", {t.val}, {lineN});"); // In the deubg script, we need to check if this variable exists or not
                        AddToMainOutput(t.val);
                    } else
                    {
                        valuesAccepted = new TokenType[] { TokenType.ASSIGN };

                        if (tokens[i + 1].type != TokenType.ASSIGN) throw new Exception($"Line {lineN}: Syntax Error: Cannot reference variable '{t.val}' before declaration");
                        if (!valueTypes.Contains(tokens[i + 2].type)) throw new Exception($"Line {lineN}: Syntax Error: Expected an expression after assignment operator");
                        
                        currentScope.ids.Add(t.val);

                        if (tokens[i + 2].type == TokenType.ID)
                        {
                            if (!currentScope.ids.Contains(t.val)) throw new Exception($"Line {lineN}: Name Error: Attempted to assign undefined varibale '{tokens[i + 2].val}' to variable '{t.val}'; reference to undefined variable '{tokens[i + 2].val}'");

                            currentScope.types.Add(currentScope.types[currentScope.ids.IndexOf(tokens[i + 2].val)]);
                        }
                        else
                        {
                            currentScope.types.Add(tokens[i + 2].type);
                        }

                        output.Insert(currentScope.index, "var " + t.val + ";");
                        doutput.Insert(currentScope.dindex, "var " + t.val + ";");
                        AddToMainOutput(t.val);
                    }

                    i++;
                    continue;
                }

                // Detect references to built-ins
                if(t.type == TokenType.BUILT_IN)
                {
                    i++;

                    valuesAccepted = new TokenType[] { TokenType.LP };

                    switch(t.val)
                    {
                        case "print":
                            AddToMainOutput("console.log");
                            continue;
                    }
                }

                // Parse colons
                if (t.type == TokenType.COLON)
                {
                    valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.BOOL, TokenType.STR, TokenType.NULL, TokenType.BUILT_IN, TokenType.ID, TokenType.RETURN, TokenType.LP, TokenType.ID, TokenType.EOL };

                    AddToMainOutput("{");
                }

                // Basic symbolic tokens
                switch(tokens[i].type)
                {
                    case TokenType.ASSIGN:
                    case TokenType.EQUAL:
                    case TokenType.NEQUAL:
                    case TokenType.LESSER:
                    case TokenType.GREATER:
                    case TokenType.LESSER_EQUAL:
                    case TokenType.GREATER_EQUAL:
                    case TokenType.ADD:
                    case TokenType.LSB:
                    case TokenType.COMMA:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.ARRAY, TokenType.OBJ, TokenType.LP, TokenType.LSB, TokenType.ID };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.MINUS:
                    case TokenType.MULTIPLY:
                    case TokenType.DIVIDE:
                    case TokenType.MOD:
                    case TokenType.FLOOR_DIVIDE:
                    case TokenType.POW:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ID };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.RSB:
                        valuesAccepted = new TokenType[] { TokenType.COMMA, TokenType.RSB, TokenType.EOL, TokenType.EOS };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.NOT:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ARRAY, TokenType.OBJ, TokenType.ID };
                        AddToMainOutput("!");
                        i++;
                        continue;
                    case TokenType.AND:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ARRAY, TokenType.OBJ, TokenType.ID };
                        AddToMainOutput("&&");
                        i++;
                        continue;
                    case TokenType.OR:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ARRAY, TokenType.OBJ, TokenType.ID };
                        AddToMainOutput("||");
                        i++;
                        continue;
                    case TokenType.DOT:
                        valuesAccepted = new TokenType[] { TokenType.ID, TokenType.BUILT_IN };
                        AddToMainOutput(".");
                        i++;
                        continue;
                    case TokenType.EOS:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.BOOL, TokenType.STR, TokenType.NULL, TokenType.ARRAY, TokenType.OBJ, TokenType.BUILT_IN, TokenType.IF, TokenType.ELIF, TokenType.ELSE, TokenType.FOR, TokenType.WHILE, TokenType.FUNCTION,  TokenType.ID, TokenType.RETURN, TokenType.LP, TokenType.ID, TokenType.EOL };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.LP:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ARRAY, TokenType.OBJ, TokenType.ID, TokenType.RP };
                        AddToMainOutput(tokens[i].val);
                        brackets++;
                        i++;
                        continue;
                    case TokenType.RP:
                        valuesAccepted = new TokenType[] { TokenType.COLON, TokenType.RP, TokenType.COMMA, TokenType.EOS, TokenType.EOL };
                        AddToMainOutput(tokens[i].val);
                        brackets--;
                        i++;
                        continue;
                }

                // Check indentation level and new line
                if(t.type == TokenType.EOL)
                {
                    lineN++;
                    lastLineI = doutput.Count - 1;

                    prevIndent = indent;
                    indent = 0;

                    i++;

                    // Ignore if this is just a blank line or inside a bracket of sorts: preserve the indentation level
                    if (tokens[i].type == TokenType.EOL || brackets != 0)
                    {
                        indent = prevIndent;
                        continue;
                    }

                    while(tokens[i].type == TokenType.INDENT)
                    {
                        indent++;
                        i++;
                    }
                    
                    if(indent < prevIndent)
                    {
                        for (int j = prevIndent; j > indent - 1; j--)
                        {
                            if (FNScopeIndentLevels.Contains(j)) ExitScope();
                        }

                        for (int i = 0; i < prevIndent - indent; i++) {
                            AddToMainOutput("}");
                        }
                    }

                    valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.BOOL, TokenType.STR, TokenType.NULL, TokenType.ARRAY, TokenType.OBJ, TokenType.BUILT_IN, TokenType.IF, TokenType.ELIF, TokenType.ELSE, TokenType.FOR, TokenType.WHILE, TokenType.FUNCTION, TokenType.ID, TokenType.RETURN, TokenType.LP, TokenType.RP, TokenType.LSB, TokenType.RSB, TokenType.ID, TokenType.EOL };
                    
                    // Add a semicolon to show end of statement
                    AddToMainOutput(";");

                    continue;
                }

                i++;
            }

            // Close the scope
            AddToMainOutput("})()");

            // Now let's test the code which we have got so far
            File.WriteAllText("$DEBUG_main.js", File.ReadAllText("lib.js") + "\n" + string.Join("", doutput).Replace(";;", ";"));

            Process p = new Process();
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            p.StartInfo.FileName = "CMD.exe";
            p.StartInfo.Arguments = $"/C node \"{Path.GetFullPath("$DEBUG_main.js")}\"";
            p.Start();

            string err = p.StandardError.ReadToEnd();

            Console.WriteLine(err);

            if (err != "") throw new Exception($"Failed to compile - {err}");

            // Do a bit of cleanup: remove all double semi-colon occurences
            return string.Join("", output).Replace(";;", ";");
        }

        private void ConditionalBoolStatement()
        {
            i++;

            bool ended = false;
            while (!ended && tokens[i].type != TokenType.EOF)
            {
                if (!valuesAccepted.Contains(tokens[i].type) && tokens[i].type != TokenType.COMMENT)
                {
                    throw new Exception($"Syntax Error: Did not expect {tokens[i].type} token");
                }

                switch (tokens[i].type)
                {
                    case TokenType.EQUAL:
                    case TokenType.NEQUAL:
                    case TokenType.LESSER:
                    case TokenType.GREATER:
                    case TokenType.LESSER_EQUAL:
                    case TokenType.GREATER_EQUAL:
                    case TokenType.ADD:
                    case TokenType.LSB:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.LSB, TokenType.ID };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.MINUS:
                    case TokenType.MULTIPLY:
                    case TokenType.DIVIDE:
                    case TokenType.MOD:
                    case TokenType.FLOOR_DIVIDE:
                    case TokenType.POW:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ID };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.RSB:
                        valuesAccepted = new TokenType[] { TokenType.COMMA, TokenType.RSB, TokenType.LSB, TokenType.EOL, TokenType.EOS };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.NOT:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ARRAY, TokenType.OBJ, TokenType.ID };
                        AddToMainOutput("!");
                        i++;
                        continue;
                    case TokenType.AND:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ARRAY, TokenType.OBJ, TokenType.ID };
                        AddToMainOutput("&&");
                        i++;
                        continue;
                    case TokenType.OR:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ARRAY, TokenType.OBJ, TokenType.ID };
                        AddToMainOutput("||");
                        i++;
                        continue;
                    case TokenType.DOT:
                        valuesAccepted = new TokenType[] { TokenType.ID, TokenType.BUILT_IN };
                        AddToMainOutput(".");
                        i++;
                        continue;
                    case TokenType.ID:
                        if (!currentScope.ids.Contains(tokens[i].val)) throw new Exception($"Line {lineN}: Name Error: Variable '{tokens[i].val}' is not defined");

                        TokenType thisType = currentScope.types[currentScope.ids.IndexOf(tokens[i].val)];
                        if (!valuesAccepted.Contains(thisType) && thisType != TokenType.FUNCTION) throw new Exception($"Line {lineN}: Type Error: did not expect type '{thisType}'");

                        valuesAccepted = new TokenType[] { TokenType.ADD, TokenType.MINUS, TokenType.MULTIPLY, TokenType.DIVIDE, TokenType.EQUAL, TokenType.NEQUAL, TokenType.LESSER, TokenType.GREATER, TokenType.LESSER_EQUAL, TokenType.GREATER_EQUAL, TokenType.MOD, TokenType.FLOOR_DIVIDE, TokenType.POW, TokenType.RP, TokenType.LP, TokenType.COMMA, TokenType.RSB, TokenType.LSB, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.DOT, TokenType.EOS, TokenType.EOL };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.NUM:
                        valuesAccepted = new TokenType[] { TokenType.ADD, TokenType.MINUS, TokenType.MULTIPLY, TokenType.DIVIDE, TokenType.EQUAL, TokenType.NEQUAL, TokenType.LESSER, TokenType.GREATER, TokenType.LESSER_EQUAL, TokenType.GREATER_EQUAL, TokenType.MOD, TokenType.FLOOR_DIVIDE, TokenType.POW, TokenType.RP, TokenType.COLON, TokenType.COMMA, TokenType.RSB, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.EOS, TokenType.EOL };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.BOOL:
                    case TokenType.NULL:
                        valuesAccepted = new TokenType[] { TokenType.ADD, TokenType.MINUS, TokenType.MULTIPLY, TokenType.DIVIDE, TokenType.EQUAL, TokenType.NEQUAL, TokenType.LESSER, TokenType.GREATER, TokenType.LESSER_EQUAL, TokenType.GREATER_EQUAL, TokenType.MOD, TokenType.FLOOR_DIVIDE, TokenType.POW, TokenType.RP, TokenType.COMMA, TokenType.RSB, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.EOS, TokenType.EOL };
                        AddToMainOutput(tokens[i].val);
                        i++;
                        continue;
                    case TokenType.STR:
                        valuesAccepted = new TokenType[] { TokenType.ADD, TokenType.MINUS, TokenType.MULTIPLY, TokenType.DIVIDE, TokenType.EQUAL, TokenType.NEQUAL, TokenType.LESSER, TokenType.GREATER, TokenType.LESSER_EQUAL, TokenType.GREATER_EQUAL, TokenType.MOD, TokenType.FLOOR_DIVIDE, TokenType.POW, TokenType.RP, TokenType.COLON, TokenType.COMMA, TokenType.RSB, TokenType.LSB, TokenType.AND, TokenType.OR, TokenType.NOT, TokenType.EOS, TokenType.EOL };
                        AddToMainOutput("\"" + tokens[i].val + "\"");
                        i++;
                        continue;
                    case TokenType.COLON:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.BOOL, TokenType.STR, TokenType.NULL, TokenType.BUILT_IN, TokenType.ID, TokenType.RETURN, TokenType.LP, TokenType.ID, TokenType.EOL };
                        AddToMainOutput(") {");
                        i++;
                        ended = true;
                        continue;
                    case TokenType.LP:
                        valuesAccepted = new TokenType[] { TokenType.NUM, TokenType.STR, TokenType.BOOL, TokenType.NULL, TokenType.LP, TokenType.ARRAY, TokenType.OBJ, TokenType.ID, TokenType.RP };
                        AddToMainOutput(tokens[i].val);
                        brackets++;
                        i++;
                        continue;
                    case TokenType.RP:
                        valuesAccepted = new TokenType[] { TokenType.COLON, TokenType.RP, TokenType.COMMA, TokenType.EOS, TokenType.EOL };
                        AddToMainOutput(tokens[i].val);
                        brackets--;
                        i++;
                        continue;
                }

                throw new Exception($"Syntax Error: Did not expect {tokens[i].type.ToString()} token");
            }

            if (!ended)
            {
                throw new Exception("Syntax Error: Expected colon at end of if statement");
            }
        }

        private void ValidateSetParams()
        {
            i++;

            // Ignore whitespace characters
            while (tokens[i].type == TokenType.EOL || tokens[i].type == TokenType.INDENT)
            {
                i++;
                continue;
            }

            if (tokens[i].type != TokenType.LP) throw new Exception($"Line {lineN}: Syntax Error: Expected opening parenthesis '('");

            AddToMainOutput("(");

            bool expectComma = false;
            i++;

            while(tokens[i].type != TokenType.RP && i < tokens.Length)
            {
                if (tokens[i].type == TokenType.EOL || tokens[i].type == TokenType.INDENT)
                {
                    i++;
                    continue;
                }

                if (expectComma && tokens[i].type != TokenType.COMMA) throw new Exception($"Line {lineN}: Expected a comma; Failed to close opening parenthesis with ')'");
                if (!expectComma && tokens[i].type != TokenType.ID) throw new Exception($"Line {lineN}: Syntax Error: Expected a parameter name");

                AddToMainOutput(tokens[i].val);

                expectComma = !expectComma;

                i++;
            }

            if (tokens[i].type != TokenType.RP) throw new Exception($"Line {lineN}: Syntax Error: '(' not closed with right parenthesis ')'; Expected closing parenthesis");

            AddToMainOutput(")");

            i++;
        }

        private string ValidateArr(string arr)
        {
            Lexer ArrLexer = new Lexer(arr.ToCharArray());
            Token[] lexed = ArrLexer.Lex();

            bool expectComma = false;
            string output = "";
            int j = 1; // Ignore first EOL token

            while(lexed[j].type != TokenType.EOF)
            {
                Token t = lexed[j];
                string val = t.val;

                if(t.type == TokenType.EOL)
                {
                    lineN++;
                    j++;
                    continue;
                }

                // Ignore indents
                if(t.type == TokenType.INDENT)
                {
                    j++;
                    continue;
                }

                if (expectComma && t.type != TokenType.COMMA) throw new Exception($"Line {lineN}: Syntax Error: Expected a comma");

                if (!expectComma && !valueTypes.Contains(lexed[j].type))
                {
                    throw new Exception($"Line {lineN}: Syntax Error: Expected a value");
                }

                if(!expectComma && t.type == TokenType.ARRAY)
                {
                    val = ValidateArr(val);
                }

                if(!expectComma && t.type == TokenType.STR)
                {
                    val = "\"" + val + "\"";
                }

                if(!expectComma && t.type == TokenType.OBJ)
                {
                    val = ValidateObj(val);
                }

                if (!expectComma && t.type == TokenType.ID && !currentScope.ids.Contains(t.val)) throw new Exception($"Line {lineN}: NameError: Variable '{t.val}' referenced before assignment");

                expectComma = !expectComma;
                output += val;

                j++;
            }

            // Ignore the extra EOL at the end of the lexed tokens as well
            lineN--;

            return "[" + output + "]";
        }

        private string ValidateObj(string obj)
        {
            Lexer ArrLexer = new Lexer(obj.ToCharArray());
            Token[] lexed = ArrLexer.Lex();

            TokenType[] expects = valueTypes;
            bool isKey = true;
            string output = "";
            int j = 1; // Ignore first EOL token

            while (lexed[j].type != TokenType.EOF)
            {
                Token t = lexed[j];
                string val = t.val;

                if (t.type == TokenType.EOL)
                {
                    lineN++;
                    j++;
                    continue;
                }

                // Ignore indents
                if (t.type == TokenType.INDENT)
                {
                    j++;
                    continue;
                }

                if (!expects.Contains(t.type)) throw new Exception($"Line {lineN}: Did not expect {t.type}");

                if (t.type == TokenType.ID && !currentScope.ids.Contains(t.val)) throw new Exception($"Line {lineN}: NameError: Variable '{t.val}' referenced before assignment");

                if (expects == valueTypes)
                {
                    if (t.type == TokenType.ARRAY)
                    {
                        val = ValidateArr(val);
                    }

                    if (t.type == TokenType.STR)
                    {
                        val = "\"" + val + "\"";
                    }

                    if (t.type == TokenType.OBJ)
                    {
                        val = ValidateObj(val);
                    }

                    // This utilises an ES6 feature: computed property names
                    // TODO: update the way objects work so that we do not utilise this modern feature (support for older browsers)
                    if(t.type == TokenType.ID && isKey)
                    {
                        val = "[" + val + "]";
                    }

                    if(isKey)
                    {
                        isKey = false;
                        expects = new TokenType[] { TokenType.COLON };
                    } else
                    {
                        expects = new TokenType[] { TokenType.COMMA };
                    }

                    output += val;

                    j++;
                    continue;
                }

                if(expects.Contains(TokenType.COLON))
                {
                    expects = valueTypes;
                }

                if(expects.Contains(TokenType.COMMA))
                {
                    expects = valueTypes;
                    isKey = true;
                }

                output += val;

                j++;
            }

            // Ignore the extra EOL at the end of the lexed tokens as well
            lineN--;

            return "{" + output + "}";
        }

        private void NewScope(int index, int dindex)
        {
            //Console.WriteLine("Entering scope");
            Scope original = currentScope;

            currentScope = new Scope(original.ids.ToList(), original.types.ToList(), index, dindex, original);
            FNScopeIndentLevels.Add(indent);
        }

        private void ExitScope(int depth = 1)
        {
            //Console.WriteLine("Exiting scope");
            for (int j = 0; j < depth; j++)
            {
                Scope original = currentScope;

                // Ignore the attempt if we are trying to leave the global scope
                if (original.parentScope == null) return;

                currentScope = original.parentScope;
            }
        }

        private void AddToMainOutput(string data)
        {
            output.Add(data);
            doutput.Add(data);
        }
    }
}
