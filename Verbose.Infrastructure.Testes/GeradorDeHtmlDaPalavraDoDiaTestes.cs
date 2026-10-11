using Verbose.Domain;
using Verbose.Infrastructure.Email;

namespace Verbose.Infrastructure.Testes;

public sealed class GeradorDeHtmlDaPalavraDoDiaTestes
{
    [Fact]
    public void Gerar_QuandoConteudoTemCaracteresEspeciais_CodificaHtmlESubstituiMarcadores()
    {
        const string template = "{{PALAVRA}}|{{DEFINICAO}}|{{EXEMPLO}}|{{data}}";
        var gerador = new GeradorDeHtmlDaPalavraDoDia(template);
        var registro = new RegistroPalavraDoDia(
            new DateOnly(2026, 10, 14),
            new PalavraDoDia(
                "idôneo",
                "Adequado & confiável <teste>.",
                "Ele disse: \"idôneo\"."));

        var html = gerador.Gerar(registro);

        Assert.Equal(
            "id&#244;neo|Adequado &amp; confi&#225;vel &lt;teste&gt;.|Ele disse: &quot;id&#244;neo&quot;.|14 de outubro de 2026",
            html);
    }
}
