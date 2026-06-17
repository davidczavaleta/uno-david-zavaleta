namespace OrderOrchestration.Domain
{
    /// <summary>
    /// Resultado de la verificación de fraude. Además de aprobar o rechazar, el servicio
    /// puede solicitar una revisión manual (human-in-the-loop) que detiene la orquestación
    /// hasta que un operador resuelva la orden.
    /// </summary>
    public enum FraudDecision
    {
        Approved,
        Rejected,
        ManualReview
    }
}
