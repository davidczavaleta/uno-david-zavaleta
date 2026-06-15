namespace OrderOrchestration.Infrastructure.Mongo
{
    /// <summary>
    /// Configuración de MongoDB para la aplicación, incluyendo la cadena de conexión y el nombre de la base de datos.
    /// </summary>
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty; 
    }
}
