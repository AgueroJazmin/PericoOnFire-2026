namespace PericoOnFire_2026.Shared.DTOs
{
    public class DistribucionMesasDTO
    {
        public int IdSala { get; set; }
        public List<PosicionMesaDTO> Mesas { get; set; } = new();
    }

    public class PosicionMesaDTO
    {
        public int Id { get; set; }
        public int Fila { get; set; }
        public int Columna { get; set; }
        public int FilaOriginal { get; set; }
        public int ColumnaOriginal { get; set; }
    }
}
