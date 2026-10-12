using Verbose.Infrastructure.Email;

namespace Verbose.Infrastructure.Testes;

public sealed class GeradorDeHtmlDeConfirmacaoDeInscricaoTestes
{
    [Fact]
    public void Gerar_QuandoLinkContemParametros_CodificaValorParaHtml()
    {
        var gerador = new GeradorDeHtmlDeConfirmacaoDeInscricao(
            "<a href=\"{{LINK_CONFIRMACAO}}\">Confirmar</a>");

        var html = gerador.Gerar(new Uri("https://verbose.exemplo/confirmar?token=abc&origem=email"));

        Assert.Equal(
            "<a href=\"https://verbose.exemplo/confirmar?token=abc&amp;origem=email\">Confirmar</a>",
            html);
    }
}
