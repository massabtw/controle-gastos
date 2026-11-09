namespace FinanceTracker.Domain.Results;

public static class ErrorMessages
{
    public static ErrorResult EntityNotFound => new("Registro não encontrado.", 404);
    public static ErrorResult UnknownError => new("Ocorreu um erro interno ao processar a operação.", 500);
    public static ErrorResult InvalidData => new("Dados inválidos fornecidos.", 400);
}
