namespace API
{
    public static class BusinessRules
    {
        // Regra da Leila: alteracao/cancelamento pelo sistema so ate 2 dias antes do agendado.
        // Com menos de 2 dias, o cliente precisa ligar para o salao.
        public const int MinDaysForCustomerChange = 2;
    }
}
