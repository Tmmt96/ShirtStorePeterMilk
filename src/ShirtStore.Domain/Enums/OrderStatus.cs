namespace ShirtStore.Domain.Enums;

public enum OrderStatus
{
    Draft            = 0,   // carrinho ainda não convertido
    PendingPayment   = 1,   // aguardando pagamento
    Paid             = 2,   // pagamento confirmado
    Processing       = 3,   // em preparação
    Shipped          = 4,   // enviado
    Delivered        = 5,   // entregue
    Cancelled        = 6,   // cancelado
    Refunded         = 7,   // reembolsado
    PaymentFailed    = 8    // pagamento falhou
}
