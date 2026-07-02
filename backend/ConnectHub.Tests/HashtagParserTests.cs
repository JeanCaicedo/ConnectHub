using ConnectHub.API.Helpers;
using Xunit;

namespace ConnectHub.Tests;

public class HashtagParserTests
{
    [Fact]
    public void Extract_SinHashtags_DevuelveVacio()
    {
        var result = HashtagParser.Extract("un post normal sin etiquetas");
        Assert.Empty(result);
    }

    [Fact]
    public void Extract_ContenidoVacioONulo_DevuelveVacio()
    {
        Assert.Empty(HashtagParser.Extract(""));
        Assert.Empty(HashtagParser.Extract("   "));
        Assert.Empty(HashtagParser.Extract(null));
    }

    [Fact]
    public void Extract_VariosHashtags_LosDevuelveEnMinusculas()
    {
        var result = HashtagParser.Extract("Aprendiendo #DotNet y #Angular hoy");
        Assert.Equal(new[] { "dotnet", "angular" }, result);
    }

    [Fact]
    public void Extract_HashtagsDuplicados_SeDeduplican()
    {
        var result = HashtagParser.Extract("#csharp #CSharp #csharp");
        Assert.Single(result);
        Assert.Equal("csharp", result[0]);
    }

    [Fact]
    public void Extract_IgnoraSimboloSuelto_YCaracteresNoValidos()
    {
        // '#' sin palabra no cuenta; los signos cortan el hashtag.
        var result = HashtagParser.Extract("hola # mundo #web-dev");
        Assert.Equal(new[] { "web" }, result);
    }
}
