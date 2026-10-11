namespace Verbose.Infrastructure.Email;

public sealed class FalhaPermanenteDeEmailException(string mensagem, Exception excecaoInterna)
    : Exception(mensagem, excecaoInterna);
