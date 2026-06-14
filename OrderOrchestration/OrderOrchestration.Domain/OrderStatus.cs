namespace OrderOrchestration.Domain
{
    /// <summary>
    /// Enumerador con las distintas etapas en las cuales puede encontrarse una orden durante su procesamiento.
    /// </summary>
    public enum OrderStatus
    {
        Pending, // Recibida pero sin verificar
        FraudCheckPending, // Enviada al servicio de fraude y en espera de respuesta
        Approved, // Aprobada y lista para cobrar
        Rejected, // Rechazada (ej. por sospecha de fraude)
        PaymentProcessed, // Cobrada exitosamente
        ShipmentRequested, // Solicitud de envío realizada (Flujo Finalizado)
        ManualReviewRequired // Requiere intervención humana (caso twist)
    }
}
