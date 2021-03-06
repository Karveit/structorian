using System;
using NUnit.Framework;

namespace Structorian.Engine.Tests
{
    [TestFixture]
    public class StructLexerTest
    {
        [Test] public void SimpleLexer()
        {
            StructLexer lexer = new StructLexer("struct BITMAPINFOHEADER { }");
            Assert.AreEqual(StructTokenType.String, lexer.PeekNextToken());
            Assert.AreEqual("struct", lexer.GetNextToken(StructTokenType.String));
            Assert.AreEqual(StructTokenType.String, lexer.PeekNextToken());
            Assert.AreEqual("BITMAPINFOHEADER", lexer.GetNextToken(StructTokenType.String));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.OpenCurly));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.CloseCurly));
            Assert.IsTrue(lexer.EndOfStream());
        }
        
        [Test] public void LineBreak()
        {
            StructLexer lexer = new StructLexer("struct BITMAPINFOHEADER {\n}\n");
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.OpenCurly));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.CloseCurly));
            Assert.IsTrue(lexer.EndOfStream());
        }
        
        [Test] public void EndOfLineComments()
        {
            StructLexer lexer = new StructLexer("// comment\nstruct BITMAPINFOHEADER {\n}\n");
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
        }
        
        [Test] public void QuoteDelimited()
        {
            StructLexer lexer = new StructLexer("struct \"My Structure\" { }");
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            Assert.AreEqual("My Structure", lexer.GetNextToken(StructTokenType.String));
        }

        [Test] public void ParenthesesDelimited()
        {
            StructLexer lexer = new StructLexer("struct (My (Best) Structure) { }");
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            Assert.AreEqual("My (Best) Structure", lexer.GetNextToken(StructTokenType.String));
        }
        
        [Test] public void LineAndCol()
        {
            StructLexer lexer = new StructLexer("struct A {\r\n   u8");
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.OpenCurly));
            TextPosition pos = lexer.CurrentPosition;
            Assert.AreEqual(2, pos.Line);
            Assert.AreEqual(3, pos.Col);
        }
        
        [Test] public void ParseException()
        {
            ParseException exception = null;
            try
            {
                StructLexer lexer = new StructLexer("struct A {\r\n   !");
                Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
                Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
                Assert.IsTrue(lexer.CheckNextToken(StructTokenType.OpenCurly));
                lexer.PeekNextToken();
            }
            catch(ParseException e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(2, exception.Position.Line);
            Assert.AreEqual(3, exception.Position.Col);
        }
        
        [Test, ExpectedException(typeof(ParseException))] 
        public void UnclosedParen()
        {
            StructLexer lexer = new StructLexer("(abc");
            lexer.PeekNextToken();
        }

        [Test] public void CStyleComment()
        {
            StructLexer lexer = new StructLexer("/* test \nalso test\nend*/ struct A { }");
            Assert.AreEqual(StructTokenType.String, lexer.PeekNextToken());
            Assert.AreEqual(3, lexer.CurrentPosition.Line);
            Assert.AreEqual("struct", lexer.GetNextToken(StructTokenType.String));
        }

        [Test] public void MultilineAttributeValue()
        {
            var lexer = new StructLexer("[FileName=(a\nb)] struct A { }");
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.OpenSquare));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.Equals));
            TextPosition pos;
            var value = lexer.GetAttributeValue(out pos);
            Assert.AreEqual("(a\nb)", value);
            Assert.AreEqual(2, lexer.CurrentPosition.Line);
        }

        [Test] public void MultilineDelimitedString()
        {
            var lexer = new StructLexer("struct A { if (\na)");
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.OpenCurly));
            Assert.IsTrue(lexer.CheckNextToken(StructTokenType.String));
            var token = lexer.GetNextToken(StructTokenType.String);
            Assert.AreEqual("\na", token);
            Assert.AreEqual(2, lexer.CurrentPosition.Line);
        }
    }
}
