using StaffSync.Application.Features.DirectoryExport;

namespace StaffSync.Application.Tests.DirectoryExport;

public class QuotedPrintableTests
{
    [Theory]
    [InlineData("Kaya", "Kaya")]
    [InlineData("Ayşe", "Ay=C5=9Fe")]
    [InlineData("Çelik", "=C3=87elik")]
    [InlineData("a b", "a=20b")]
    [InlineData("a=b", "a=3Db")]
    public void Encodes_utf8_bytes(string input, string expected) =>
        Assert.Equal(expected, QuotedPrintable.Encode(input, 0));

    [Fact]
    public void Lines_never_exceed_76_chars_and_never_split_a_byte()
    {
        string prefix = "FN;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:";
        string encoded = QuotedPrintable.Encode(new string('ş', 60), prefix.Length);
        string[] lines = (prefix + encoded).Split("\r\n");

        Assert.True(lines.Length > 1);
        Assert.All(lines, l => Assert.True(l.Length <= 76, $"{l.Length}: {l}"));
        Assert.All(lines.Take(lines.Length - 1), l => Assert.EndsWith("=", l));
        // "=C" gibi yarım kalmış bir bayt kodu satır sonunda olmamalı.
        Assert.DoesNotContain(lines, l => System.Text.RegularExpressions.Regex.IsMatch(l, "=[0-9A-F]$"));
    }
}
