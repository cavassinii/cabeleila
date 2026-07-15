using Npgsql;
using System;
using System.Data;
using System.Linq;

namespace DAL.Cabeleila
{
    public static class Connection
    {
        // Definida uma unica vez em Program.cs, a partir do appsettings.json / variavel de ambiente.
        // Nao fica hardcoded no fonte para nao expor credenciais no repositorio Git.
        public static string ConnectionString { get; set; } = string.Empty;

        public static NpgsqlConnection Get()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new InvalidOperationException(
                    "DAL.Cabeleila.Connection.ConnectionString nao foi configurada. Defina-a na inicializacao da API.");
            }

            return new NpgsqlConnection(ConnectionString);
        }

        public static T MapDataReaderToObject<T>(IDataReader dr) where T : new()
        {
            var obj = new T();
            var props = typeof(T).GetProperties();

            for (int i = 0; i < dr.FieldCount; i++)
            {
                var columnName = dr.GetName(i);
                var prop = props.FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

                if (prop != null && dr[i] != DBNull.Value)
                {
                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    if (propType.IsEnum)
                    {
                        prop.SetValue(obj, Enum.ToObject(propType, dr[i]));
                    }
                    else
                    {
                        var value = Convert.ChangeType(dr[i], propType);
                        prop.SetValue(obj, value);
                    }
                }
            }

            return obj;
        }
    }
}
