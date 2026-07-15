using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DAL.Cabeleila
{
    // Tabela de apoio com 5 linhas fixas (Pendente/Confirmado/Em andamento/Concluido/Cancelado).
    // Mantida em cache em memoria para nao gerar uma consulta extra a cada acao de agendamento.
    public static class AppointmentStatuses
    {
        private const string Columns = "id, code, description";
        private const string TableName = "appointment_status";

        private static List<DTO.Cabeleila.AppointmentStatus>? _cache;
        private static readonly SemaphoreSlim Lock = new(1, 1);

        public static async Task<List<DTO.Cabeleila.AppointmentStatus>> GetAll()
        {
            if (_cache != null)
            {
                return _cache;
            }

            await Lock.WaitAsync();
            try
            {
                if (_cache != null)
                {
                    return _cache;
                }

                var objs = new List<DTO.Cabeleila.AppointmentStatus>();
                await using var conn = Connection.Get();
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand($@"
                    SELECT {Columns}
                    FROM {TableName}
                    ORDER BY id;
                ", conn);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    objs.Add(Connection.MapDataReaderToObject<DTO.Cabeleila.AppointmentStatus>(reader));
                }

                _cache = objs;
                return _cache;
            }
            finally
            {
                Lock.Release();
            }
        }

        public static async Task<DTO.Cabeleila.AppointmentStatus?> GetByCode(string code)
        {
            var all = await GetAll();
            return all.FirstOrDefault(s => s.Code == code);
        }

        public static async Task<DTO.Cabeleila.AppointmentStatus?> GetById(int id)
        {
            var all = await GetAll();
            return all.FirstOrDefault(s => s.Id == id);
        }
    }
}
