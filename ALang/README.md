# PyJScript (ALang) 0.0.1-a.1

PyJScript is a tool/language designed to allow python developers to develop for the web.
 * **Mimic**: as much as possible, PyJScript attempts to replicate all of the quirks and features of the python language, this means no surprises when developing, and allows people to port code easily from other projects
 * **Native**: PyJScript code is compiled directly into javascript so that you are able to run the code in the browser easily. 

 ## Documentation
 ### The Quirks of PyJScript
 ***Number Handling*** - While there are both integer and float casts and number types provided, due to the way javascript works they are treated the same. All numbers are stored using Javascript's default built-in number management system, and therefore are all considered 64-bit double-precision floating point numbers. Consequently, all integers are safe within 15 digits, and you can have a maximum of 17 decimal points. In other words, treat all numbers like floats. Currently, scientific notation is not supported for numbers. 

 ## Under the hood
When a PyJScript program is compiled 2 different outputs are produced: the debug output and the main output. The debug output is used by the compiler to ensure that all of the code is valid, while the main output code is what will actually be run by the browser. Debug code can also be useful while creating applications, as it can explain better issues which may seem to occur inexplicably in the main js output (especially if you do not understand javascript well, some behaviours may seem absurd).